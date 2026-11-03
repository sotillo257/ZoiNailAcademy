## 1. Domain

- [x] 1.1 Test `ListaEspera.DarDeBaja` (keeps the first moment; a new sign-up clears it), then implement it
- [x] 1.2 `AvisoEnviado` and `AvisoPendiente` (with `Publicaciones` as concurrency token) with tests
- [x] 1.3 `EmailSaliente.BajaUrl` (absolute http(s), ≤ 500) with tests

## 2. Application

- [x] 2.1 `AvisoReferencia` (hash of entry + sorted editions) with tests
- [x] 2.2 Templates `AvisoNuevasEdiciones` and `AvisoNuevasEdiciones.Clienta`, footer link in `EmailLayout`, preview cases; tests (dates in Madrid, seats, links, escaping, singular/plural)
- [x] 2.3 `MarcarCursoPendienteHandler` (new mark, existing mark, unknown edition) with tests
- [x] 2.4 `EnviarAvisosNuevasEdicionesHandler` (recipients, already notified, not bookable, batches, no client address, lock) with tests
- [x] 2.5 `DarseDeBajaHandler` (valid, invalid, already unsubscribed) with tests
- [x] 2.6 `EmailMensaje.BajaUrl` passed by the outbox, with test

## 3. Infrastructure and Host

- [x] 3.1 Configurations `AvisosPendientes`, `AvisosEnviados`, `EmailsSalientes.BajaUrl`; migration `AvisosNuevasEdiciones`
- [x] 3.2 `AvisosOptions`/`AvisosSettings`; keys in `appsettings.json` and `.env.example`
- [x] 3.3 `ResendEmailSender` sends the `List-Unsubscribe` headers (test with a stubbed `HttpClient`)
- [x] 3.4 `POST /api/lista-espera/baja` (query, JSON or form; always 204; `formularios`) and `AvisosNuevasEdicionesJob`

## 4. Integration tests (Testcontainers)

- [x] 4.1 Publish → after the job one email per subscribed person of the course, none to other courses or unsubscribed; client summary
- [x] 4.2 Three dates before the job → one email with the three
- [x] 4.3 Hide and publish again → no resend; a later sign-up gets the earlier date with the next one
- [x] 4.4 Cancelled before the job / created hidden → no notice
- [x] 4.5 Unsubscribe with valid, invalid, repeated, missing and malformed token → 204; only the valid one changes; `baja` in the backoffice listing
- [x] 4.6 One-click with Gmail's form body on the `List-Unsubscribe` URL; token in a form field
- [x] 4.7 Email stores `BajaUrl` and contains the unsubscribe page link

## 5. Manual

- [ ] 5.1 [MANUAL] With Resend configured, check in Gmail that the native "Cancelar suscripción" button appears and unsubscribes
- [ ] 5.2 [MANUAL] Tell the client in writing that this feature is a gift
