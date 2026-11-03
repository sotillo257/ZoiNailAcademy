## Why

Buyers need their confirmation and payment details by email, and the client needs to know about every sale, pending payment, waiting-list sign-up and contact message. Emails must never break a booking or a Stripe webhook, never be duplicated, and survive provider outages (see `Docs/features/F06-emails.md`). The Resend account does not exist yet, so the API must work without it: everything is built and testable now, and sending starts as soon as the keys are set.

## What Changes

- Outbox `EmailSaliente` (table `EmailsSalientes`, unique `(Tipo, ReferenciaId)` when the reference is set) and migration `EmailsSalientes`.
- Handlers of `ReservaPagadaEvent`, `ReservaManualCreadaEvent`, `ReservaExpiradaEvent` (manual only), `PagoTrasCaducidadSinPlazaEvent` and `ListaEsperaAltaEvent` that render the 9 templates of the spec and enqueue them.
- `EnviarEmailsJob` (every 30 s): sends up to 20 due emails through `IEmailSender` behind a PostgreSQL advisory lock, backoff 1 min / 5 min / 15 min / 1 h, `Fallido` after 5 attempts; once a day deletes sent emails older than 90 days.
- `IEmailSender`: `ResendEmailSender` (HTTP API, `Idempotency-Key` = outbox id) and `LogEmailSender` (log, plus `.html` files in development). `Email:Provider` selects it; **Resend without `Resend:ApiKey` or `Email:From` falls back to the log with a startup warning**, so nothing fails before the account exists.
- `POST /api/contacto` (rate limit `formularios`, honeypot) → 202, email to the client with Reply-To = sender; optional acknowledgement.
- `GET /api/admin/emails/preview/{tipo}` (SuperAdmin) with sample data.
- New configuration: `App:NombreAcademia`, `App:DireccionAcademia`, `Email:Provider`, `Email:From`, `Email:ReplyTo`, `Email:AvisosClienta`, `Email:AcuseContacto`, `Email:DirectorioLog`, `Resend:ApiKey`.

## Capabilities

### New Capabilities
- `emails`: outbox, sending job, templates, provider selection and preview.
- `contacto`: the contact form.

### Modified Capabilities
- None (the events of F03–F05 are consumed as they are).

## Non-goals

- DNS / domain verification in Resend (F10); new-edition notices to the waiting list (F11, reuses this outbox).
- Final texts, colours and logo: provisional until the client reviews them in the preview.
- Email for `ReservaCanceladaEvent` (v1 without email).

## Impact

- **Domain**: `EmailSaliente`, `EstadoEmail`.
- **Application**: `Emails/` (templates, `BandejaSalida`, event handlers, `EnviarEmailsPendientes`, `LimpiarEmailsEnviados`, `PrevisualizarEmail`), `Contacto/EnviarMensajeContacto`; `IEmailSender`, `IEmailSettings`; `IAppSettings` gains the academy name and address.
- **Infrastructure**: `Email/` (options, settings, provider selection, Resend and log senders), `EmailSalienteConfiguration`, migration.
- **Host**: `EnviarEmailsJob`, `ContactoController`, `AdminEmailsController`; startup warning when Resend is not configured.
- No new packages (Resend through `HttpClient`).
