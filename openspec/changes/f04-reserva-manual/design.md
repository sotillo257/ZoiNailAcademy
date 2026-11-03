## Context

F03 built `Reserva`, `CrearReservaService` (row lock on the edition) and the card flow. Manual payments differ in three ways: the seat is held for hours instead of minutes, a human confirms the payment, and something must expire what nobody confirmed. Seats are already freed by time alone (`OcupaPlazaEn` ignores pending bookings past `ExpiraEn`), so the job only makes the state coherent and triggers the email.

## Goals / Non-Goals

**Goals:** manual booking with payment details and WhatsApp link; expiry job safe with several replicas; confirm / cancel / extend with optimistic concurrency; cascade on edition cancellation.

**Non-Goals:** listings and search (F07), emails (F06), reconciliation.

## Aggregate rules (`Reserva`)

- `CalcularCaducidadManual(now, duracion, cierre)` (static): `min(now + duracion, cierre)`; if `cierre − now < 2 h` → `BusinessRuleException(edicion_no_reservable, "Para esta fecha solo se puede pagar con tarjeta")`. `cierre = Inicio − Reservas:CierreHorasAntes`.
- `ConfirmarPagoManual(now, hayPlaza, nota)` → returns whether it changed:

| Current | Manual | Stripe |
| --- | --- | --- |
| `Pendiente` | → `Pagada` | `estado_invalido` ("Los pagos con tarjeta se confirman solos") |
| `Expirada` | `hayPlaza` → `Pagada`; else `sin_plazas` ("La plaza ya se ha ocupado; habla con el comprador") | `estado_invalido` |
| `Pagada` | no change (idempotent) | no change |
| `Cancelada` | `estado_invalido` | `estado_invalido` |

- "No seat held" means `Expirada` **or** `Pendiente` with `ExpiraEn` already passed (the job has not run yet): such a booking no longer counts as occupied, so confirming or extending it also requires a free seat — otherwise the client could oversell between expiry and the next job run.
- `CancelarDesdeBackoffice(now, confirmarPagada, motivo)` → returns whether it changed: `Pendiente`/`Expirada` → `Cancelada`; `Pagada` → `Cancelada` only with `confirmarPagada` (warning "Recuerda reembolsar el pago"), else `estado_invalido`; `Cancelada` → no change.
- `Ampliar(now, horas, cierre, hayPlaza)`: only `Manual`; `horas` in 1–168. `Pendiente` → `ExpiraEn = min(max(ExpiraEn, now) + horas, cierre)`. `Expirada` → needs `hayPlaza` (else `sin_plazas`), back to `Pendiente` with `ExpiraEn = min(now + horas, cierre)`. The response warns when the expiry was clipped. If `cierre <= now` → `edicion_no_reservable`. Stripe, `Pagada`, `Cancelada` → `estado_invalido`.
- Notes and reasons are appended to `NotaInterna` (≤ 500, truncated).

## Decisions

**D-01 `NuevaReserva` computes `ExpiraEn` from the locked edition.** The manual expiry depends on the edition's start, which must be read under the row lock. `NuevaReserva` carries `CalcularExpiraEn(Edicion, now)`; the card flow passes a constant function. The 2-hour rule runs after the bookable check, so a non-bookable edition still answers `edicion_no_reservable`.

**D-02 Optimistic concurrency on every client action.** The request's `version` must equal the loaded `xmin` (else `409 concurrencia`), and `DbUpdateConcurrencyException` on save → `409 concurrencia`. The job's writes change `xmin`, so a confirmation racing the job either wins (the job then skips it) or gets `409`; never both `Pagada` and `Expirada`, never two events.

**D-03 Seat checks lock the edition.** Confirming or extending an `Expirada` booking opens a transaction, locks the edition (`BloquearEdicionAsync`) and recomputes occupancy, as in F03.

**D-04 Expiry job.** In integration tests the hosted job is removed from the shared host and started only by the test that checks it (`CreateHost(clock, conJob: true)`), so it never interferes with other tests. `ExpirarReservasJob` (Host, `BackgroundService`) uses `PeriodicTimer(interval, TimeProvider)` — with the integration tests' frozen `FakeTimeProvider` it never fires on its own — and sends `ExpirarReservasCaducadasCommand` in a new scope; exceptions are logged and the loop continues. The handler:
1. `TryAdquirirLockAsync(key)` — session-level `pg_try_advisory_lock` on a connection kept open for the batch; if another replica holds it, the cycle does nothing.
2. Loads up to 100 ids of `Pendiente` bookings with `ExpiraEn <= now`, oldest first.
3. For each: reload it, skip if no longer pending; for Stripe call `ExpirarSesionAsync` first and skip on `YaCompletada`; `Expirar(now)`; save. `DbUpdateConcurrencyException` or any error → log, `DescartarCambios()` (clear the change tracker so the failure does not leak into the next save), continue.
4. Publishes one `ReservaExpiradaEvent` per expired booking after its save.

**D-05 Payment details and WhatsApp link.** `IPagoSettings` (`Pago:*`). `ReservaPublicaDtoFactory` fills `datosPago` (`concepto = codigo`) and `whatsappUrl` only for `Manual` + `Pendiente`. `WhatsAppUrl.Construir` → `https://wa.me/{Pago:WhatsAppTelefono}?text=` + `Uri.EscapeDataString("Hola, envío el comprobante de la reserva {codigo} ({curso}, {dd/MM/yyyy HH:mm Madrid}). Importe: {importe es-ES} €.")`.

**D-06 Edition cancellation cascade.** `EditarEdicionHandler` loads the pending bookings of the edition, cancels them in the same `SaveChanges` as the edition (one implicit transaction), then — after the commit — expires their Stripe sessions (errors logged) and publishes `ReservaCanceladaEvent` for each. The warning reports the paid bookings: `Tiene X inscritos: avisa y reembolsa manualmente`.

**D-07 `ReservaAdminDto`** is defined now with the F07 shape (`id, codigo, edicionId, curso, inicio, metodo, estado, nombre, email, telefono, importe, creadaEn, pagadaEn, expiraEn, requiereRevision, notaInterna, stripePaymentIntentId, version`) and is the response of the three actions inside `{reserva, avisos}`.

**D-08 Honeypot** as in F03: a fake `ReservaManualCreadaDto` (invented code, configured payment details), nothing stored.

## Endpoints

| Method | Route | Auth | Request | Success | Errors |
| --- | --- | --- | --- | --- | --- |
| POST | `/api/reservas/manual` | anonymous, `reservas` | same as `tarjeta` | `200 ReservaManualCreadaDto` | `400`, `404`, `409 edicion_no_reservable / sin_plazas`, `429 limite_pendientes` |
| POST | `/api/admin/reservas/{id}/confirmar` | Backoffice | `{version, nota?}` | `200 {reserva, avisos}` | `400`, `404`, `409 concurrencia / estado_invalido / sin_plazas` |
| POST | `/api/admin/reservas/{id}/cancelar` | Backoffice | `{version, motivo?, confirmarPagada?}` | `200 {reserva, avisos}` | `400`, `404`, `409 concurrencia / estado_invalido` |
| POST | `/api/admin/reservas/{id}/ampliar` | Backoffice | `{version, horas}` | `200 {reserva, avisos}` | `400` (`horas` 1–168), `404`, `409 concurrencia / estado_invalido / sin_plazas / edicion_no_reservable` |

## Risks / Trade-offs

- [A session-level advisory lock outlives a crashed request only until its connection closes] → the connection is opened for the batch and closed (unlocking) in `DisposeAsync`; a killed process drops the connection and the lock with it.
- [Stripe unreachable during the job] → that booking is skipped and retried next cycle; its seat is already free by time.
- [Bank details in environment variables] → changing them needs a redeploy (open question of the PRD).

## Migration Plan

No schema change. Railway needs the `Pago__*` variables before the manual flow goes live.
