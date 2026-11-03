## ADDED Requirements

### Requirement: Booking invariants and code
A `Reserva` SHALL be created only through `Reserva.Create(...)` in state `Pendiente`, with `Importe` copied from the course price and `ConsentimientoEn` set to the creation time. It MUST reject a code that does not match `CUR-` + 6 characters of `23456789ABCDEFGHJKMNPQRSTUVWXYZ`, a blank name, email or phone, and an `ExpiraEn` not in the future. Generated codes MUST use that alphabet (no `0/O/1/I/L`) and a cryptographic random generator.

#### Scenario: Valid booking
- **WHEN** `Reserva.Create` is called with valid data
- **THEN** the booking is `Pendiente`, its `Importe` equals the course price and it occupies a seat until `ExpiraEn`
- **[UNIT]**

#### Scenario: Invalid data
- **WHEN** the code is malformed, the name is blank or `ExpiraEn` is in the past
- **THEN** a `DomainException` is thrown
- **[UNIT]**

#### Scenario: Generated codes
- **WHEN** many codes are generated
- **THEN** each matches the format and none contains an ambiguous character
- **[UNIT]**

### Requirement: Booking state transitions
`Reserva` SHALL expose one method per transition: `Pagar` (`Pendiente → Pagada`), `PagarTrasCaducidad` (`Expirada | Cancelada → Pagada`), `RegistrarPagoSinPlaza` (`Expirada | Cancelada`, sets `RequiereRevision`), `Expirar` (`Pendiente → Expirada`), `Cancelar` (`Pendiente → Cancelada`) and `AsignarSesionStripe` (`Pendiente` Stripe bookings). Any other transition MUST throw `BusinessRuleException` with code `estado_invalido`.

#### Scenario: Allowed transitions
- **WHEN** each method is called from its allowed states
- **THEN** the state and timestamps (`PagadaEn`, `ExpiradaEn`, `CanceladaEn`) are updated
- **[UNIT]**

#### Scenario: Rejected transitions
- **WHEN** `Pagar` is called on a `Pagada` booking, `Expirar` or `Cancelar` on a non-pending one, or `PagarTrasCaducidad` on a `Pendiente` one
- **THEN** a `BusinessRuleException(estado_invalido)` is thrown and nothing changes
- **[UNIT]**

### Requirement: Contact normalization
Emails SHALL be trimmed and lower-cased. Phones SHALL be normalized to E.164 with default region `ES`; an invalid phone MUST be a `400` validation error on `telefono`.

#### Scenario: Spanish phone formats
- **WHEN** the phone is `612 34 56 78`, `+34612345678` or `0034612345678`
- **THEN** it is normalized to `+34612345678`
- **[UNIT]**

#### Scenario: Invalid phones
- **WHEN** the phone is `123`, `abc` or empty
- **THEN** normalization fails
- **[UNIT]**

### Requirement: Booking core never oversells
`CrearReservaService` SHALL, inside one transaction: lock the edition row (`SELECT … FOR UPDATE`), reject a non-bookable edition (`409 edicion_no_reservable`; unknown edition `404`), reject a contact (same email or phone) that already has `Reservas:MaxPendientesPorContacto` valid pending bookings (`429 limite_pendientes`), reject when there are no free seats (`409 sin_plazas`), and insert a `Pendiente` booking with a unique code. Concurrent requests for the same edition MUST never create more bookings than free seats.

#### Scenario: Last seat under concurrency
- **WHEN** 10 card bookings for an edition with 1 seat are sent in parallel by different contacts
- **THEN** exactly one returns `200` and nine return `409` with `code = sin_plazas`, and the edition has exactly one booking
- **[INTEGRATION]**

#### Scenario: Not bookable
- **WHEN** the edition is hidden, cancelled or inside the closing window
- **THEN** the response is `409` with `code = edicion_no_reservable`
- **[INTEGRATION]**

#### Scenario: Per-contact pending limit
- **WHEN** a contact with two valid pending bookings books a third time with the same phone (different email)
- **THEN** the response is `429` with `code = limite_pendientes`
- **[INTEGRATION]**

#### Scenario: Service rules with mocks
- **WHEN** the service runs with a non-bookable edition, a contact over the limit or a full edition
- **THEN** it throws the matching exception, does not add a booking and does not commit
- **[UNIT]**

### Requirement: Public booking status
`GET /api/reservas/{codigo}` (anonymous) SHALL return `ReservaPublicaDto` (`codigo, estado, metodo, curso, inicio, importe, expiraEn, datosPago, whatsappUrl`) and MUST NOT expose name, email or phone. Unknown codes return `404 no_encontrado`.

#### Scenario: No personal data
- **WHEN** the status of a booking is requested
- **THEN** the JSON contains the public fields and none of the buyer's name, email or phone
- **[INTEGRATION]**

### Requirement: Abandoning a checkout frees the seat
`POST /api/reservas/{codigo}/abandonar` (anonymous, rate limit `reservas`) SHALL expire the Stripe session and cancel a `Pendiente` Stripe booking; if Stripe reports the session already completed, nothing changes. Any other state answers `204` without changes.

#### Scenario: Abandon a pending booking
- **WHEN** the buyer abandons a pending card booking
- **THEN** the response is `204`, the booking is `Cancelada` and the seat is free again
- **[INTEGRATION]**

#### Scenario: Abandon a paid booking
- **WHEN** the booking is already `Pagada`
- **THEN** the response is `204` and it stays `Pagada`
- **[INTEGRATION]**
