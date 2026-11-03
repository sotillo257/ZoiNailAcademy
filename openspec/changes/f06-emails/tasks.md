## 1. Domain

- [x] 1.1 Test `EmailSaliente` (pending and due on creation, backoff 1/5/15/60 min, `Fallido` after 5, sent with provider id, no second send, truncation), then implement it with `EstadoEmail`

## 2. Application

- [x] 2.1 Test the templates (code, Spanish long date, comma amount in HTML and text, escaped `<script>`, manual payment details and links, line breaks), then implement `EmailCuerpo`, `EmailLayout`, `EmailFormato` and the 9 templates
- [x] 2.2 Test the event handlers (paid → buyer + client with free seats, dedupe, missing client address, expiry only for manual, manual payment details, payment without seat, unknown booking), then implement them with `BandejaSalida`
- [x] 2.3 Test `EnviarEmailsPendientesHandler` (due only, failure retried and batch continues, lock held elsewhere), then implement it; `LimpiarEmailsEnviados`
- [x] 2.4 Test the contact validator and handler (Reply-To, optional acknowledgement, honeypot) and the preview of every type, then implement them

## 3. Infrastructure and Host

- [x] 3.1 `EmailSalienteConfiguration` and migration `EmailsSalientes`; `EmailOptions`, `ResendOptions`, `EmailSettings`, `IAppSettings` academy name/address
- [x] 3.2 `ResendEmailSender` (`HttpClient`, `Idempotency-Key`), `LogEmailSender`, `EmailProveedorSeleccion` with fallback to the log and startup warning
- [x] 3.3 `EnviarEmailsJob`, `ContactoController`, `AdminEmailsController`; keys in `appsettings*.json` and `.env.example`

## 4. Integration tests (Testcontainers)

- [x] 4.1 Confirmed manual booking → 2 + 2 emails, republished events do not duplicate; card webhook → 2 emails; expired Stripe → 0, manual → 1; waiting list notifies each sign-up
- [x] 4.2 Job: fails twice → sent on the 3rd attempt after the backoff; always fails → `Fallido` after 5; cleanup after 90 days
- [x] 4.3 Contact → 202 with Reply-To; honeypot and validation; provider selection; preview only for SuperAdmin

## 5. Manual

- [ ] 5.1 [MANUAL] Review texts, colours and logo with the client through the preview
- [ ] 5.2 [MANUAL] Create the Resend account, verify the domain (F10) and set `Resend__ApiKey`, `Email__From`, `Email__ReplyTo`, `Email__AvisosClienta` on Railway
- [ ] 5.3 [MANUAL] Send every type to Gmail and Outlook: inbox (not spam), mobile rendering, production links
