## Why

Many buyers prefer paying by bank transfer or Bizum. F04 lets them book a seat that stays reserved for a limited time while they send the receipt by WhatsApp, and lets the client confirm the payment from the backoffice. Unpaid bookings must free their seat on their own, and no race between the expiry job and the client may leave a booking in an incoherent state or send duplicate emails (see `Docs/features/F04-reserva-manual.md` and `Docs/00-contrato-y-convenciones.md`). It also closes the F02 `TODO`: cancelling an edition cancels its pending bookings.

## What Changes

- `POST /api/reservas/manual` (rate limit `reservas`, honeypot, `aceptaCondiciones`): a `Manual` booking through `CrearReservaService` with `ExpiraEn = now + Reservas:ManualHorasCaducidad`, clipped to the edition's booking closing time; fewer than 2 h to the closing time → `409 edicion_no_reservable` ("Para esta fecha solo se puede pagar con tarjeta"). Publishes `ReservaManualCreadaEvent` and answers `ReservaManualCreadaDto` with `datosPago` and a prefilled `whatsappUrl`.
- `GET /api/reservas/{codigo}` returns `datosPago` and `whatsappUrl` while a manual booking is `Pendiente`.
- Expiry job: `ExpirarReservasJob` (`BackgroundService`, `PeriodicTimer` every `Reservas:JobIntervaloMinutos`) sends `ExpirarReservasCaducadasCommand`, which expires up to 100 due pending bookings, each on its own save guarded by `xmin`, expiring Stripe sessions first, behind a PostgreSQL advisory lock so two replicas never process the same batch.
- Client actions (`Backoffice`): `POST /api/admin/reservas/{id}/confirmar`, `/cancelar`, `/ampliar`, all with `version` (`xmin`) → `409 concurrencia`; responses `{reserva: ReservaAdminDto, avisos}`.
- Editions: moving to `Cancelada` cancels every pending booking in the same save (Stripe sessions expired after the commit); the warning now counts the paid bookings that must be refunded.
- Events `ReservaManualCreadaEvent`, `ReservaCanceladaEvent`.
- New configuration: `Reservas:ManualHorasCaducidad` (48), `Reservas:JobIntervaloMinutos` (5), `Pago:Titular`, `Pago:Iban`, `Pago:BizumTelefono`, `Pago:WhatsAppTelefono`.

## Capabilities

### New Capabilities
- `reservas-manuales`: manual bookings, payment details and the WhatsApp link.
- `caducidad-reservas`: the expiry job.
- `acciones-reservas`: confirm, cancel and extend from the backoffice.

### Modified Capabilities
- `ediciones`: cancelling an edition cascades to its pending bookings.

## Non-goals

- Automatic bank reconciliation; uploading receipts on the website (they go by WhatsApp).
- Backoffice listings and booking search (F07); emails (F06).
- Bank details editable from the backoffice (v1: environment variables).

## Impact

- **Domain**: `Reserva` gains `ConfirmarPagoManual`, `CancelarDesdeBackoffice`, `Ampliar` and `CalcularCaducidadManual`; `Edicion.Actualizar` receives the paid count for its warning.
- **Application**: `Reservas/ReservarManual`, `Reservas/ExpirarReservasCaducadas`, `Reservas/ConfirmarReserva`, `CancelarReserva`, `AmpliarReserva`; `ReservaAdminDto`; `ReservaPublicaDtoFactory` + `WhatsAppUrl`; `IPagoSettings`; `NuevaReserva` computes `ExpiraEn` from the locked edition; `IAppDbContext` gains `TryAdquirirLockAsync` and `DescartarCambios`.
- **Infrastructure**: advisory lock, `PagoOptions`/`PagoSettings`, two more `ReservasOptions`. No migration.
- **Host**: `ExpirarReservasJob`, `AdminReservasController`, `POST /api/reservas/manual`.
- No new packages.
