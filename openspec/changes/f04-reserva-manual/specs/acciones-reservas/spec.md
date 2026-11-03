## ADDED Requirements

### Requirement: Optimistic concurrency on booking actions
`POST /api/admin/reservas/{id}/confirmar | cancelar | ampliar` (policy `Backoffice`) SHALL require the `version` (`xmin`) the client read; a different version or a concurrent change MUST answer `409 concurrencia`. Each answers `200 {reserva: ReservaAdminDto, avisos: string[]}`; an unknown id answers `404`.

#### Scenario: Job and confirmation race
- **WHEN** the expiry job and a confirmation run at the same time on the same pending manual booking
- **THEN** exactly one wins: either it is `Pagada` with one `ReservaPagadaEvent`, or it is `Expirada` with one `ReservaExpiradaEvent` and the confirmation got `409 concurrencia`
- **[INTEGRATION]**

#### Scenario: Stale version
- **WHEN** a confirmation carries a version read before the job expired the booking
- **THEN** the response is `409` with `code = concurrencia`
- **[INTEGRATION]**

### Requirement: Confirm a manual payment
Confirming SHALL follow the matrix: `Pendiente` Manual → `Pagada` with `ReservaPagadaEvent`; `Expirada` Manual → lock the edition, `Pagada` if a seat is free, else `409 sin_plazas`; Stripe `Pendiente`/`Expirada` → `409 estado_invalido`; `Pagada` → `200` without a new event; `Cancelada` → `409 estado_invalido`. An optional `nota` is appended to `NotaInterna`.

#### Scenario: Matrix in the domain
- **WHEN** `ConfirmarPagoManual` is called for every state and method
- **THEN** each combination produces the outcome of the matrix
- **[UNIT]**

#### Scenario: Expired with and without a seat
- **WHEN** an expired manual booking is confirmed while its edition has a free seat, and another one while it is full
- **THEN** the first becomes `Pagada`; the second answers `409 sin_plazas`
- **[INTEGRATION]**

#### Scenario: Card booking
- **WHEN** a pending card booking is confirmed
- **THEN** the response is `409 estado_invalido`
- **[INTEGRATION]**

### Requirement: Cancel a booking
Cancelling SHALL turn `Pendiente` (any method; a Stripe session is expired too) or `Expirada` into `Cancelada` and publish `ReservaCanceladaEvent`. A `Pagada` booking MUST only be cancelled with `confirmarPagada: true`, adding the warning `Recuerda reembolsar el pago`; otherwise `409 estado_invalido`. An already cancelled booking answers `200` without a new event.

#### Scenario: Matrix in the domain
- **WHEN** `CancelarDesdeBackoffice` is called for every state, with and without `confirmarPagada`
- **THEN** each combination produces the outcome above
- **[UNIT]**

#### Scenario: Cancel a paid booking
- **WHEN** a paid booking is cancelled without and then with `confirmarPagada`
- **THEN** the first answers `409 estado_invalido`; the second `200` with the refund warning and `estado = Cancelada`
- **[INTEGRATION]**

### Requirement: Extend a manual booking
Extending SHALL accept `horas` between 1 and 168 and only `Manual` bookings: `Pendiente` → `ExpiraEn = min(max(ExpiraEn, now) + horas, closing time)`; `Expirada` → back to `Pendiente` if a seat is free (`409 sin_plazas` otherwise). A closing time already reached answers `409 edicion_no_reservable`; Stripe, `Pagada` and `Cancelada` answer `409 estado_invalido`.

#### Scenario: Matrix in the domain
- **WHEN** `Ampliar` is called for every state and method
- **THEN** each combination produces the outcome above, with `ExpiraEn` clipped to the closing time
- **[UNIT]**

#### Scenario: Extend and clip
- **WHEN** a pending manual booking is extended by 24 h and by 168 h on an edition that closes in 100 h
- **THEN** `ExpiraEn` moves 24 h later the first time and is clipped to the closing time the second
- **[INTEGRATION]**
