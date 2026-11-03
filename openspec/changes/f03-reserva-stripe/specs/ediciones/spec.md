## MODIFIED Requirements

### Requirement: Free seats computation
`PlazasCalculator` SHALL count as occupied the bookings that are `Pagada` or `Pendiente` with `ExpiraEn > now`; `libres = max(0, plazas − ocupadas)`. The public state SHALL be `Completo` when `libres = 0`, `UltimasPlazas` when `libres <= Reservas:UmbralUltimasPlazas`, and `Disponible` otherwise. Occupancy MUST be read from the `Reservas` table with one grouped query for all the editions of a response (no N+1) and MUST NOT be stored.

#### Scenario: Occupied seats
- **WHEN** the calculator receives no bookings; paid ones; pending ones not yet expired; pending ones already expired; `Expirada` and `Cancelada` ones
- **THEN** occupied seats are 0; the paid count; the pending count; 0; 0 respectively
- **[UNIT]**

#### Scenario: Never negative
- **WHEN** occupied seats exceed the total
- **THEN** free seats are 0
- **[UNIT]**

#### Scenario: Public state from real bookings
- **WHEN** three visible editions of 10 seats have 0, 7 (paid + valid pending) and 10 occupied seats stored as bookings, plus expired and cancelled bookings
- **THEN** their states are `Disponible`, `UltimasPlazas` and `Completo`, and expired/cancelled bookings do not count
- **[INTEGRATION]**

#### Scenario: Calendar is never cached
- **WHEN** `GET /api/cursos/{slug}/ediciones` is called
- **THEN** the response carries `Cache-Control: no-store`
- **[INTEGRATION]**

### Requirement: Deleting editions
`DELETE /api/admin/ediciones/{id}` (policy `Backoffice`) SHALL delete an edition without any booking (of any state) and return `204`; with bookings it MUST return `409 edicion_con_reservas`; an unknown id returns `404`.

#### Scenario: Delete without bookings
- **WHEN** an Admin deletes an edition without bookings
- **THEN** the response is `204` and `GET /api/admin/ediciones/{id}` returns `404`
- **[INTEGRATION]**

#### Scenario: Delete with bookings
- **WHEN** the edition has only an `Expirada` booking
- **THEN** the response is `409` with `code = edicion_con_reservas`
- **[INTEGRATION]**
