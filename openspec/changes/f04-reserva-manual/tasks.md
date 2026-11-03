## 1. Domain

- [x] 1.1 Test `Reserva.CalcularCaducidadManual` (lifetime, clipped to the closing time, < 2 h → `edicion_no_reservable`), then implement it; add `Edicion.CierreReservas`
- [x] 1.2 Test the `ConfirmarPagoManual` matrix (pending/expired/paid/cancelled × manual/card, seat check also for pending bookings past their expiry), then implement it
- [x] 1.3 Test the `CancelarDesdeBackoffice` matrix (with and without `confirmarPagada`, idempotent on cancelled), then implement it
- [x] 1.4 Test the `Ampliar` matrix (add from current expiry, clip, reactivate expired from now, seat check, closed edition, hour range, invalid method/state), then implement it; notes appended and truncated to 500
- [x] 1.5 Test that `Edicion.Actualizar` warns with the paid count when cancelling, then implement it

## 2. Application

- [x] 2.1 `IAppDbContext.TryAdquirirLockAsync` / `DescartarCambios`, `IReservasSettings` (+`ManualCaducidad`, `JobIntervalo`), `IPagoSettings`; `NuevaReserva` computes `ExpiraEn` from the locked edition
- [x] 2.2 Test `WhatsAppUrl` (accents, `€`, spaces, Spanish number format), then implement it with `ReservaPublicaDtoFactory` (payment details only for pending manual bookings)
- [x] 2.3 Test `ReservarManualHandler` (48 h expiry and payment details, clipped expiry, < 2 h rejected without commit, honeypot), then implement it
- [x] 2.4 Test `ExpirarReservasCaducadasHandler` (due bookings only, Stripe session already paid, one failure does not stop the batch, lock held elsewhere), then implement it
- [x] 2.5 Test `ConfirmarReservaHandler`, `CancelarReservaHandler` and `AmpliarReservaHandler` (version, seat lock, events, warnings, Stripe session), then implement them with `ReservaAdminDto`
- [x] 2.6 `EditarEdicionHandler`: cancelling an edition cancels its pending bookings in the same save, expires their Stripe sessions and publishes `ReservaCanceladaEvent` after the commit (closes the F02 `TODO`)

## 3. Infrastructure and Host

- [x] 3.1 Advisory lock on a connection kept open for the batch; `PagoOptions`/`PagoSettings`; new `ReservasOptions`; keys in `appsettings.json`, `appsettings.Development.json` and `.env.example`; no migration needed
- [x] 3.2 `POST /api/reservas/manual`, `AdminReservasController` (Backoffice), `ExpirarReservasJob` (`PeriodicTimer` on `TimeProvider`)

## 4. Integration tests (Testcontainers)

- [x] 4.1 Manual booking: details and WhatsApp link, `GET` keeps them until paid, < 2 h → 409, clipped expiry, honeypot, validation
- [x] 4.2 Actions: confirm (idempotent, stale version, card booking, expired with and without seat), cancel (paid with/without confirmation, card session expired), extend (24 h and clipped 168 h, card booking, invalid hours), unknown booking, edition cancellation cascade
- [x] 4.3 Expiry (own host and clock): seat freed by time before the job, the job expires with one event each, three parallel runs expire each booking once, job vs confirmation race (exactly one wins), the background job fires when its interval elapses

## 5. Manual

- [ ] 5.1 [MANUAL] Set `Pago__*` on Railway with the client's real bank and Bizum details and check the WhatsApp link opens with the message on a phone
