## Context

F02 built courses and editions, F03 the contact normalization (`TelefonoNormalizer`) and the honeypot pattern, F01 the `formularios` rate-limit policy. The waiting list is a simple CRUD-like feature; the only subtle points are the upsert, the GDPR proof of consent and searching without accents.

## Goals / Non-Goals

**Goals:** public sign-up without duplicates; backoffice listing, "contacted" mark and CSV; a reusable CSV helper.

**Non-Goals:** emails (F06, F11), unsubscribe endpoint (F11).

## Aggregate rules (`ListaEspera`)

- `Create(curso, edicionPreferida?, fechaPreferida?, nombre, email, telefono, textoConsentimiento, now)`: email trimmed and lower-cased, required fields not blank, `fechaPreferida` ≤ 200 (blank → null), the preferred edition must belong to the course, `ConsentimientoEn = now`, `BajaToken` = 32 random bytes in base64url (43 chars).
- `ActualizarAlta(...)`: refreshes name, phone, preferred date/edition and consent; `Contactado = false`, `ContactadoEn = null`, `BajaEn = null`; keeps `Notas` and `BajaToken`.
- `MarcarContactado(contactado, now)`: marking sets `ContactadoEn` the first time only; unmarking clears it.
- `CambiarNotas(notas)`: trimmed, blank → null, > 500 → `DomainException`.
- `Busqueda` = `TextoBusqueda.Normalizar("{Nombre} {Email} {Telefono}")`, refreshed on every change.

## Decisions

**D-01 Upsert by `(CursoId, Email)`.** The handler looks up the entry for the course and the normalized email and creates or updates it; the answer is `201` without body either way, so the form never reveals whether someone was already on the list. Two simultaneous sign-ups of the same person make the second insert fail on the unique index (`DbUpdateException`): the handler discards the tracked changes and retries once as an update.

**D-02 Consent from the backend.** The stored `TextoConsentimiento` is `ConsentimientoListaEspera.Vigente` (`= V1`), whatever the front sends. The text covers phone, WhatsApp and email about new dates, which is what F11 relies on. Changing the wording adds `V2` and moves `Vigente`; existing rows keep the text they accepted.

**D-03 Search without accents through a normalized column.** Instead of the `unaccent` extension (not immutable, provider-specific, not usable in unit tests), the entity keeps `Busqueda` (lower-case, no diacritics: FormD minus non-spacing marks) and the query normalizes `q` the same way and uses `Contains` (SQL `LIKE`/`strpos`). When `q` is a phone typed with spaces or dashes (`612 34 56`), its digits are also matched against `Telefono`.

**D-04 Order and paging.** `Contactado` ascending, then `CreatedAt` ascending (who has waited longest first), then `Id` as a tie-breaker. `page` ≥ 1 and `pageSize` 1–200 (default 50) are validated (400 otherwise). `PagedResult<T>` becomes `(Items, Total, Page, PageSize)` to match the contract.

**D-05 Notes are optional on `PUT …/contactado`.** `notas` is `Optional<string?>`: absent keeps the notes, `null`/empty clears them, a value replaces them.

**D-06 `CsvBuilder`.** `Application/Common/Csv`: header in the constructor, `Fila(...)`, `ToBytes()` (UTF-8 with BOM), separator `;`, CRLF. A field with `;`, `"`, CR or LF is quoted with doubled quotes; a field that starts with `=`, `+`, `-`, `@`, tab or CR gets a leading `'` (OWASP CSV injection). The waiting list CSV has the columns of the spec, dates `dd/MM/yyyy HH:mm` in `App:TimeZone`, `Sí`/`No` for booleans, the listing's order, and the name `lista-espera-{slug}-{yyyyMMdd}.csv` (date in Madrid; `todos` when no course is given; unknown course → 404).

**D-07 Preferred edition.** FK with `ON DELETE SET NULL`: an edition can be deleted only without bookings, and people who preferred it must not block that. An edition of another course → `400` on `EdicionPreferidaId`.

**D-08 Honeypot** as in F03: `201`, nothing stored, no event.

## Endpoints

| Method | Route | Auth | Request | Success | Errors |
| --- | --- | --- | --- | --- | --- |
| POST | `/api/lista-espera` | anonymous, `formularios` | `{cursoId, edicionPreferidaId?, fechaPreferida?, nombre, email, telefono, aceptaPrivacidad, textoConsentimiento?, website?}` | `201` (no body) | `400`, `404` (unknown or inactive course), `429` |
| GET | `/api/admin/lista-espera` | Backoffice | `?cursoId=&contactado=&q=&page=&pageSize=` | `200 {items, total, page, pageSize}` | `400` |
| PUT | `/api/admin/lista-espera/{id}/contactado` | Backoffice | `{contactado, notas?}` | `200 ListaEsperaAdminDto` | `400`, `404` |
| GET | `/api/admin/lista-espera.csv` | Backoffice | `?cursoId=` | `200 text/csv; charset=utf-8` | `404` |

## Risks / Trade-offs

- [`Busqueda` duplicates data] → it is derived and always rewritten by the entity; a `LIKE '%…%'` without index is fine for a list of hundreds of rows.
- [The phone shows as `'+34…` in Excel] → required by the formula protection of the spec; Excel displays the text without evaluating it.

## Migration Plan

Migration `ListaEspera` (new table only), applied at startup. No configuration changes.
