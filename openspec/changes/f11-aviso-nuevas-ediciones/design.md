## Context

`EdicionPublicadaEvent(edicionId)` is published after the commit when an edition is created `Programada` or moves from `Oculta` to `Programada` (F02). `ListaEspera` has one row per course and email with `BajaToken` and `BajaEn` (F05; `ConsentimientoEn` is never null, the stored consent covers these emails). The outbox of F06 sends `EmailSaliente` rows with retries and is unique on `(Tipo, ReferenciaId)`.

## Goals / Non-Goals

**Goals:** one email per person with every new date of the course, no date ever notified twice to the same person, unsubscribe without JavaScript (Gmail's one-click), notice within 15 minutes of publishing.

**Non-Goals:** other channels, scheduling, manual recipient selection, front pages.

## Decisions

**D-01 The event only marks the course.** `MarcarCursoPendienteHandler` inserts `AvisoPendiente(CursoId, PendienteDesde, Publicaciones = 1)` or increments `Publicaciones` if the course is already marked. A separate table (instead of a column on `Curso` or `Edicion`) keeps the job away from the `xmin` the backoffice uses for optimistic concurrency, so the job never causes a spurious 409 `concurrencia`. A concurrent insert or update is retried up to 3 times.

**D-02 The job groups.** Every `Avisos:JobIntervaloMinutos` (10, so < 15 min) the job takes each marked course and:
1. Dates = the course's bookable editions now (same rule as the public web: `Programada`, course active, outside the closing window; `EdicionesPublicas.PorCursoAsync`) with `plazasLibres > 0`. Cancelled, hidden, full or closing-window editions are left out. As the spec says, these are *all* the bookable dates not notified to the person yet, not only the ones that triggered the mark: someone who joined after a date was announced also learns about it the next time a date is published.
2. Recipients = waiting list of the course with `BajaEn == null`, including the contacted ones.
3. Per person, the dates without an `AvisoEnviado` row; nobody with zero dates gets an email.
4. Insert the `AvisoEnviado` rows, the emails, the summary for the client (when `Email:AvisosClienta` is set and someone was emailed) and delete the mark **in one `SaveChanges`**: a failure leaves everything as it was and the course is retried next cycle.
Runs behind the advisory lock `0x5A4F_4900_0003` (expiry job `…0001`, outbox `…0002`).

**D-03 Race with a new publication.** `AvisoPendiente.Publicaciones` is a concurrency token. If a date is published while the job is notifying that course, the delete of the mark affects no row, the whole save fails with `DbUpdateConcurrencyException`, and the next cycle notifies everything (including the new date). Nothing is lost and nothing is duplicated.

**D-04 Idempotency key.** `ReferenciaId = first 16 bytes of SHA-256(listaEsperaId ‖ sorted edicionIds)` (`AvisoReferencia`), as the spec asks; `AvisoEnviado` is what really prevents a second notice of the same date. The client's summary has no reference (it is saved atomically with the notices, so it cannot be duplicated).

**D-05 Batches.** Instead of sleeping in the job, emails are enqueued with `ProximoIntentoEn = now + PausaEntreLotes × (i / LoteTamano)`: batch 0 is due now, batch 1 two seconds later… The outbox (20 emails per 30 s cycle) still sends them, so Resend's rate limit is respected by both.

**D-06 Unsubscribe links.** The footer links to the front's confirmation page `{App:BaseUrl}/baja?token={BajaToken}&curso={slug}` (`curso` lets the page say "Ya no te avisaremos de nuevas fechas de {curso}" even though the endpoint answers 204 without a body). The `List-Unsubscribe` header points to `{App:BaseUrl}/api/lista-espera/baja?token={BajaToken}` (through the Next `/api` rewrite), stored in `EmailSaliente.BajaUrl` and sent by `ResendEmailSender` as `headers` with `List-Unsubscribe-Post: List-Unsubscribe=One-Click` (RFC 8058).

**D-07 `POST /api/lista-espera/baja`.** No model binding of the body: the action reads `?token=` first, else a JSON body `{token}`, else a form field `token`; a malformed body is treated as no token. Always 204 (valid, invalid, missing or already used), rate limit `formularios`. `ListaEspera.DarDeBaja` keeps the first `BajaEn`; a new sign-up of the same email and course clears it (F05). Only that entry (that course) is affected. A `GET` never unsubscribes (mail scanners open links).

## Endpoints

| Method | Route | Request | Success | Errors |
| --- | --- | --- | --- | --- |
| POST | `/api/lista-espera/baja` | `?token=` or `{token}` (JSON) or `token` (form) | `204` | `429` |

## Risks / Trade-offs

- [Dates published just before and just after a job tick go in two emails] → acceptable; waiting up to one interval keeps the 15-minute promise.
- [The full waiting list of a course is loaded in memory] → hundreds of rows at most.
- [Tokens in URLs end up in the outbox and provider logs] → they only unsubscribe one course; sent emails are deleted after 90 days (F06).

## Migration Plan

Migration `AvisosNuevasEdiciones`: tables `AvisosPendientes` (FK `Cursos`, cascade) and `AvisosEnviados` (FKs `ListaEspera` and `Ediciones`, cascade; index on `EdicionId`), column `EmailsSalientes.BajaUrl` (`varchar(500)`, nullable). Nothing to backfill: editions published before the deploy are only announced once another date of their course is published. New keys in `appsettings.json` and `.env.example`.
