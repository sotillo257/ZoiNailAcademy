## 1. Domain

- [x] 1.1 Test `CodigoReserva` (format, alphabet without ambiguous characters, randomness), then implement it
- [x] 1.2 Test `Reserva.Create` invariants (code, name, expiry, price copied) and `OcupaPlaza`/`OcupaPlazaEn`, then implement them; add `MetodoPago`
- [x] 1.3 Test every `Reserva` transition and the rejection of invalid ones (`estado_invalido`), then implement them
- [x] 1.4 Add `StripeEvento`

## 2. Application: shared pieces

- [x] 2.1 `IAppDbContext` gains `Reservas`, `StripeEventos`, `BeginTransactionAsync`, `BloquearEdicionAsync`; `IReservasSettings` gains `StripeBloqueo` and `MaxPendientesPorContacto`; new `IAppSettings`, `IStripeGateway`, `IStripeWebhookParser`, `TooManyRequestsException`, `PaymentProviderException`
- [x] 2.2 Test `TelefonoNormalizer` (Spanish formats, foreign prefix, invalid input), then implement it with `libphonenumber-csharp`
- [x] 2.3 `OcupacionReader` reads real bookings in one grouped query; `TieneReservasAsync` checks any booking

## 3. Application: booking core and card flow

- [x] 3.1 Test `CrearReservaService` (lock + commit, `sin_plazas`, `edicion_no_reservable`, unknown edition, `limite_pendientes` by phone, expired/paid bookings do not count, invalid phone), then implement it
- [x] 3.2 Test `ReservarConTarjetaValidator` (shared `ReservaFormularioValidator`), then implement it
- [x] 3.3 Test `ReservarConTarjetaHandler` (session data in cents and Madrid time, idempotency key, Stripe failure cancels, honeypot), then implement it
- [x] 3.4 Test `ProcesarWebhookStripeHandler` (pay, duplicate, unknown session, unpaid, other types, already paid, client reference fallback, expired, late payment with and without seat, failing event handler), then implement it
- [x] 3.5 Test `AbandonarReservaHandler` (pending, already completed in Stripe, paid, unknown), then implement it; add `ObtenerReservaPublicaHandler` and `ReservaPublicaDto`

## 4. Infrastructure

- [x] 4.1 `ReservaConfiguration` (indexes of the spec, filtered unique `StripeSessionId`, `xmin`, `Restrict`), `StripeEventoConfiguration`, `AppDbContext` transaction and `FOR UPDATE` lock; migration `Reservas`; `has-pending-model-changes` is clean
- [x] 4.2 `ReservasOptions` (+2 settings), `AppOptions`/`AppSettings`, `StripeOptions`; keys in `appsettings.json` and `.env.example`
- [x] 4.3 `StripeGateway` (Checkout session with idempotency key; expire with fallback to the session status) and `StripeWebhookParser` (`EventUtility.ConstructEvent`)

## 5. Host

- [x] 5.1 Middleware maps `TooManyRequestsException` → 429 with `code` and `PaymentProviderException` → 502 with a generic message
- [x] 5.2 `ReservasController` (`tarjeta`, `{codigo}`, `{codigo}/abandonar`, rate limit `reservas`) and `StripeWebhookController` (raw body, anonymous, no rate limit)

## 6. Integration tests (Testcontainers)

- [x] 6.1 Test support: `FakeStripeGateway`, `EventRecorder`, signed webhook payloads (HMAC-SHA256); F02 tests seed real bookings and `TestOcupacionReader` is removed
- [x] 6.2 Card booking: success, 10 parallel requests for the last seat → 1 × 200 and 9 × `sin_plazas` (verified to fail without the row lock), not bookable, unknown edition, per-contact limit, honeypot, validation, Stripe failure → 502 and seat freed
- [x] 6.3 Webhook: invalid signature, duplicate event, already-paid booking, expired, late payment with and without seat, client-reference fallback, unknown session, other event types
- [x] 6.4 Public status without personal data, unknown code, abandon (pending, paid, already completed in Stripe)

## 7. Manual (Stripe test mode, F10)

- [ ] 7.1 [MANUAL] Full payment with `4242 4242 4242 4242`
- [ ] 7.2 [MANUAL] Declined card `4000 0000 0000 0002`: the booking stays `Pendiente` and expires
- [ ] 7.3 [MANUAL] Close the Checkout tab: the seat comes back after 30–35 minutes
- [ ] 7.4 [MANUAL] `stripe trigger checkout.session.completed` through the front's domain: the signature verifies through the Next.js rewrite
