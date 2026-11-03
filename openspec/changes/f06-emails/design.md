## Context

F02–F05 publish domain events after the commit with `PublicarTrasCommitAsync`, which logs a failing handler instead of failing the request. F06 adds the handlers. The client has no Resend account yet.

## Goals / Non-Goals

**Goals:** reliable, non-duplicated emails that never break the flows that trigger them; all templates reviewable now; the API running in production without Resend.

**Non-Goals:** DNS setup, F11 notices, final design.

## Decisions

**D-01 Outbox.** Handlers only insert rows in `EmailsSalientes`; the job sends them. A provider outage or a 429 never reaches the booking or the webhook, and nothing is lost.

**D-02 No duplicates.** Unique index `(Tipo, ReferenciaId)` filtered to non-null references, plus a check before inserting. If two handlers race, the insert that loses is detached and ignored. The waiting-list notice and the contact emails have no reference: one request publishes one event, and a re-sign-up must notify the client again.

**D-03 Retries.** `EmailSaliente.RegistrarFallo` increments `Intentos`; after attempts 1–4 the next one is due in 1 min, 5 min, 15 min and 1 h; the 5th failure is final (`Fallido`, logged as error). The spec also lists 4 h, but it could only apply after the 5th failure. Resend gets `Idempotency-Key` = outbox id, so a retry after a timeout does not send twice.

**D-04 Advisory lock.** `EnviarEmailsPendientesCommand` takes `pg_try_advisory_lock(0x5A4F_4900_0002)` like the expiry job (different key), loads up to 20 due ids oldest first and sends them one by one, each saved separately; a database error on one email is logged and the batch continues.

**D-05 Provider selection without Resend.** `EmailProveedorSeleccion.Desde(configuration)` at startup: `Log` → log; `Resend` with `Resend:ApiKey` and `Email:From` → Resend; otherwise → log with a warning naming the missing variables (logged at startup). With the log provider emails end `Enviado` with `ProveedorId = "log"`, so they do not pile up and are not sent weeks later once Resend is configured. A missing `Email:AvisosClienta` skips the client's notices with a warning.

**D-06 Templates.** One static class per type returning `EmailContenido(asunto, html, texto)`. `EmailCuerpo` builds HTML and text in the same calls (title, paragraph, highlighted code, label/value table, button, warning), HTML-encoding every value (`HtmlEncoder` with all Unicode ranges, line by line so line breaks become `<br>`). `EmailLayout` wraps the body: tables, inline styles, 600 px, logo at `{App:BaseUrl}/email/logo.png`, footer with name, address and legal links. Dates `sábado, 5 de diciembre de 2026, 10:00` in `App:TimeZone` (`es-ES`), amounts `149,00 €`. Colours and texts are provisional.

**D-07 Links.** Buyer: `/reserva/{codigo}`, `/reservar/{slug}`; client: `/admin/ediciones/{id}`, `/admin/reservas`, `/admin/lista-espera` (F09 routes).

**D-08 Contact.** `POST /api/contacto` validates name, email, optional phone, message 10–3000 and privacy; honeypot → 202 without storing. It enqueues `Contacto.Clienta` with Reply-To = sender. `Contacto.Acuse` only with `Email:AcuseContacto` and never repeats the message (not a spam relay).

**D-09 Preview.** `GET /api/admin/emails/preview/{tipo}` (SuperAdmin) renders the template with sample data and the real settings; `?formato=texto` returns the text version; unknown type → 404.

**D-10 Cleanup.** The job runs `LimpiarEmailsEnviadosCommand` on its first cycle and then once a day: `ExecuteDelete` of `Enviado` rows with `EnviadoEn` older than 90 days.

## Endpoints

| Method | Route | Auth | Request | Success | Errors |
| --- | --- | --- | --- | --- | --- |
| POST | `/api/contacto` | anonymous, `formularios` | `{nombre, email, telefono?, mensaje, aceptaPrivacidad, website?}` | `202` | `400`, `429` |
| GET | `/api/admin/emails/preview/{tipo}` | SuperAdmin | `?formato=html\|texto` | `200 text/html` | `401`, `403`, `404` |

## Risks / Trade-offs

- [Emails "sent" to the log while Resend is missing are lost] → intended (the user asked for the step to be skipped); the startup warning makes it visible.
- [Outbox rows hold personal data] → sent rows are deleted after 90 days.

## Migration Plan

Migration `EmailsSalientes`. To start sending: create the Resend account, verify the domain (F10), set `Resend__ApiKey`, `Email__From`, `Email__ReplyTo` and `Email__AvisosClienta` on Railway and redeploy; the startup warning disappears.
