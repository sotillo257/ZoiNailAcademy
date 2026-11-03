## Why

When the client publishes a new date of a course, the people on its waiting list should hear about it without her having to call each of them, without duplicates and with a way to stop the emails (see `Docs/features/F11-aviso-nuevas-ediciones.md`). This is an extra delivered as a **gift**: the hand-over must say so in writing. F02 already publishes `EdicionPublicadaEvent`, F05 stores the consent and `BajaToken`, and F06 provides the outbox.

## What Changes

- Handler of `EdicionPublicadaEvent` that only **marks the course** as pending notice (table `AvisosPendientes`).
- `AvisosNuevasEdicionesJob` (every `Avisos:JobIntervaloMinutos`, 10): for each marked course, one email per subscribed person of its waiting list with every bookable date (with free seats) not notified to them yet, plus a summary for the client (`Aviso enviado a {N} interesados · {curso}`). Behind a PostgreSQL advisory lock.
- Table `AvisosEnviados` (PK `(ListaEsperaId, EdicionId)`), saved together with the emails, so nobody gets the same date twice (hiding and publishing again does not resend).
- Templates `AvisoNuevasEdiciones` (subject `Nuevas fechas para {curso}`) and `AvisoNuevasEdiciones.Clienta`, also in the preview endpoint.
- `EmailSaliente.BajaUrl` (nullable): the outbox hands it to the provider and Resend sends `List-Unsubscribe: <url>` and `List-Unsubscribe-Post: List-Unsubscribe=One-Click`.
- `POST /api/lista-espera/baja` (rate limit `formularios`): token in the query, a JSON body or a form field; always 204; sets `BajaEn` once.
- Migration `AvisosNuevasEdiciones`. New configuration `Avisos:LoteTamano` (20), `Avisos:PausaEntreLotesSegundos` (2), `Avisos:JobIntervaloMinutos` (10).

## Capabilities

### New Capabilities
- `avisos-nuevas-ediciones`: grouped new-dates notices to the waiting list, without duplicates, and unsubscribe (link and one-click).

### Modified Capabilities
- None (the backoffice already shows `baja` in `ListaEsperaAdminDto` and the CSV since F05).

## Non-goals

- WhatsApp or SMS notices.
- Scheduling the notice for later or choosing recipients by hand.
- The `/baja` page (F08) and the "Se dio de baja de avisos" mark in the card (F09): the API side is ready.

## Impact

- **Domain**: `AvisoEnviado`, `AvisoPendiente`, `ListaEspera.DarDeBaja`, `EmailSaliente.BajaUrl`.
- **Application**: `AvisosNuevasEdiciones/MarcarCursoPendiente`, `AvisosNuevasEdiciones/EnviarAvisos`, `AvisoReferencia`, `ListasEspera/DarseDeBaja`, two templates, `EmailLayout` footer link, `EmailMensaje.BajaUrl`, `IAvisosSettings`.
- **Infrastructure**: configurations of the two tables, `AvisosOptions`/`AvisosSettings`, `ResendEmailSender` headers, migration `AvisosNuevasEdiciones`.
- **Host**: `POST lista-espera/baja` in `ListaEsperaController`, `AvisosNuevasEdicionesJob`.
- No new packages.
