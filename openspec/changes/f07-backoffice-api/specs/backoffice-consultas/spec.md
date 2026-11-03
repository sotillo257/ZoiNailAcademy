## ADDED Requirements

### Requirement: Home counters
`GET /api/admin/resumen` (policy `Backoffice`) SHALL answer `ResumenDto`: `pendientesDePago` (manual, `Pendiente`, not expired), `pendientesQueCaducanHoy` (those expiring before the next midnight in `App:TimeZone`), `requierenRevision` (`RequiereRevision = true`), `listaEsperaSinContactar` (not contacted, not unsubscribed) and `proximasEdiciones` (the next 5 editions not started and not cancelled, as `EdicionAdminDto`, soonest first).

#### Scenario: Counters
- **WHEN** the summary is requested with seeded bookings and waiting-list entries
- **THEN** each counter counts exactly its rows
- **[INTEGRATION]**

#### Scenario: Expiring today around Madrid midnight
- **WHEN** the clock is at 23:30 Madrid time with manual bookings expiring at 23:45 and at 00:15 of the next day; and then at 00:30 with bookings expiring at 23:50 that day and at 00:10 of the following day
- **THEN** at 23:30 only the 23:45 one expires today; at 00:30 only the 23:50 one does (both still count as pending)
- **[INTEGRATION]** **[UNIT]**

### Requirement: Bookings of an edition
`GET /api/admin/ediciones/{id}/reservas?incluir=` (policy `Backoffice`) SHALL answer `{edicion: EdicionAdminDto, reservas: ReservaAdminDto[]}` with the paid and pending bookings by default, and the expired or cancelled ones when `incluir` lists them (`pagadas`, `pendientes`, `expiradas`, `canceladas`; other values → `400`). Paid come first by `PagadaEn`, then pending by `ExpiraEn`. An unknown edition → `404`.

#### Scenario: Default and incluir
- **WHEN** an edition has paid, pending, expired and cancelled bookings
- **THEN** the default answer has only paid and pending (paid first), and `incluir=expiradas,canceladas` returns only those
- **[INTEGRATION]**

### Requirement: Attendee CSV
`GET /api/admin/ediciones/{id}/reservas.csv` (policy `Backoffice`) SHALL return only `Pagada` bookings as a CSV built with `CsvBuilder` (BOM, `;`, formula protection) with the columns `Código; Nombre; Teléfono; Email; Método; Importe; Fecha de pago` (Madrid time) and the file name `asistentes-{slug}-{yyyyMMdd-HHmm}.csv` (edition start in Madrid time).

#### Scenario: Only paid
- **WHEN** the CSV of an edition with paid and pending bookings is downloaded
- **THEN** it starts with the BOM, uses `;`, and lists only the paid ones
- **[INTEGRATION]**

### Requirement: Pending bookings and search by code
`GET /api/admin/reservas/pendientes?q=&metodo=` (policy `Backoffice`) SHALL list the valid `Pendiente` bookings, the `Manual` bookings expired in the last 7 days and the bookings with `RequiereRevision`, needing review first, then pending by `ExpiraEn` ascending, then expired most recent first. `q` that looks like a code (optional `CUR-`, 6 characters of the alphabet, any case, blanks and dashes ignored) MUST find that booking; otherwise `q` is searched in name, email and phone ignoring case and accents, and phone digits are compared without prefix or separators. Expired bookings carry `plazaDisponible` (their edition has a free seat).

#### Scenario: Code typed in different ways
- **WHEN** `q` is `CUR-7K4Q2M`, `cur 7k4q2m` or `7K4Q2M`
- **THEN** the same booking is returned
- **[INTEGRATION]** **[UNIT]**

#### Scenario: Phone without prefix
- **WHEN** `q` is `612345678` or `612 34 56 78` for a booking with `+34612345678`
- **THEN** the booking is returned
- **[INTEGRATION]**

#### Scenario: Expired window
- **WHEN** a manual booking expired 3 days ago, another 10 days ago and a card booking expired yesterday
- **THEN** only the first is listed
- **[INTEGRATION]**

#### Scenario: Free seat for an expired booking
- **WHEN** an expired manual booking belongs to an edition with a free seat, and another to a full edition
- **THEN** `plazaDisponible` is true for the first and false for the second; pending bookings omit it
- **[INTEGRATION]**

### Requirement: Booking detail
`GET /api/admin/reservas/{id}` (policy `Backoffice`) SHALL answer `ReservaAdminDto` of a booking in any state; an unknown id → `404 no_encontrado`.

#### Scenario: Detail
- **WHEN** the detail of a cancelled booking is requested
- **THEN** it is returned with its state and version
- **[INTEGRATION]**

### Requirement: Mark as reviewed
`POST /api/admin/reservas/{id}/marcar-revisada` (policy `Backoffice`) with `{version, nota?}` SHALL clear `RequiereRevision` and append the note to `NotaInterna`, requiring the version the client read (`409 concurrencia` otherwise), and answer `200 {reserva, avisos}`.

#### Scenario: Clear the mark
- **WHEN** a booking that needs review is marked as reviewed with a note
- **THEN** `RequiereRevision` is false, the note is stored and it no longer counts in the summary
- **[INTEGRATION]** **[UNIT]**

#### Scenario: Without token
- **WHEN** any F07 endpoint is called without a token
- **THEN** the response is `401`
- **[INTEGRATION]**
