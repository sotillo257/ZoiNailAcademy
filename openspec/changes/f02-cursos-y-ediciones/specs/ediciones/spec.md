## ADDED Requirements

### Requirement: Edition creation invariants
An `Edicion` SHALL be created only through `Edicion.Create(curso, inicio, plazas, estado, notas, now)`. It MUST reject (with `DomainException`) an inactive course, an `inicio` that is not in the future, `plazas` outside 1–200 and an initial state other than `Programada` or `Oculta`.

#### Scenario: Valid edition
- **WHEN** `Edicion.Create` is called for an active course, a future start and 12 seats
- **THEN** the edition has the given values and the course id
- **[UNIT]**

#### Scenario: Invalid creation data
- **WHEN** the course is inactive, or the start is in the past, or seats are 0 or 201, or the state is `Cancelada`
- **THEN** a `DomainException` is thrown
- **[UNIT]**

### Requirement: Bookable and visible editions
An edition SHALL be bookable when `Estado = Programada`, its course is active and `Inicio − now > Reservas:CierreHorasAntes` hours. An edition SHALL be visible on the public web exactly when it is bookable, even if it has no free seats.

#### Scenario: Bookable rule
- **WHEN** `EsReservable` is evaluated with a fake clock
- **THEN** it is true for a scheduled edition of an active course starting after the closing window, and false when the state is `Oculta` or `Cancelada`, when the course is inactive, when the start is inside the closing window, and when the edition has already started
- **[UNIT]**

#### Scenario: Only visible editions are public
- **WHEN** a course has a scheduled, a hidden, a cancelled and a scheduled-inside-the-closing-window edition
- **THEN** `GET /api/cursos/{slug}/ediciones` returns only the first one
- **[INTEGRATION]**

### Requirement: Free seats computation
`PlazasCalculator` SHALL count as occupied the bookings that are `Pagada` or `Pendiente` with `ExpiraEn > now`; `libres = max(0, plazas − ocupadas)`. The public state SHALL be `Completo` when `libres = 0`, `UltimasPlazas` when `libres <= Reservas:UmbralUltimasPlazas`, and `Disponible` otherwise. Occupancy MUST be computed in the query for all the editions of a response at once (no N+1) and MUST NOT be stored.

#### Scenario: Occupied seats
- **WHEN** the calculator receives no bookings; paid ones; pending ones not yet expired; pending ones already expired; `Expirada` and `Cancelada` ones
- **THEN** occupied seats are 0; the paid count; the pending count; 0; 0 respectively
- **[UNIT]**

#### Scenario: Never negative
- **WHEN** occupied seats exceed the total
- **THEN** free seats are 0
- **[UNIT]**

#### Scenario: Public state per free seats
- **WHEN** a course has three visible editions with 10, 3 and 0 free seats (threshold 3)
- **THEN** their states are `Disponible`, `UltimasPlazas` and `Completo`, and `plazasLibres`/`plazasTotales` are reported
- **[INTEGRATION]**

#### Scenario: Calendar is never cached
- **WHEN** `GET /api/cursos/{slug}/ediciones` is called
- **THEN** the response carries `Cache-Control: no-store`
- **[INTEGRATION]**

### Requirement: Backoffice edition listing
`GET /api/admin/ediciones?cursoId=&incluirPasadas=false` (policy `Backoffice`) SHALL return `EdicionAdminDto[]` with paid, pending (not expired) and free seats, `reservable` and `version`. Upcoming editions come first ordered by `Inicio`; past editions are excluded unless `incluirPasadas=true`, in which case they follow. `cursoId` filters by course. `GET /api/admin/ediciones/{id}` returns one edition or `404 no_encontrado`.

#### Scenario: Filter and past editions
- **WHEN** a course has an upcoming and a past edition
- **THEN** listing with `cursoId` returns only the upcoming one, and with `incluirPasadas=true` returns both with the upcoming one first
- **[INTEGRATION]**

### Requirement: Creating editions
`POST /api/admin/ediciones` (policy `Backoffice`) with `{cursoId, inicio, plazas, estado?, notas?}` SHALL create the edition (default state `Programada`) and return `201 EdicionAdminDto`. Validation errors (`plazas` outside 1–200, `inicio` not in the future, `estado = Cancelada`, `notas` over 500 characters) MUST return `400` per field. An unknown course returns `404`; an inactive course returns `400`. When the edition is created as `Programada`, `EdicionPublicadaEvent(edicionId)` MUST be published after the commit.

#### Scenario: Create a scheduled edition
- **WHEN** an Admin creates an edition without `estado`
- **THEN** the response is `201` with `estado = Programada`, `libres = plazas`, and one `EdicionPublicadaEvent` for that id is published
- **[INTEGRATION]**

#### Scenario: Create a hidden edition
- **WHEN** an Admin creates an edition with `estado = Oculta`
- **THEN** the response is `201` and no `EdicionPublicadaEvent` is published
- **[INTEGRATION]**

#### Scenario: Validation
- **WHEN** the validator receives 0 seats, a past start, state `Cancelada` or 501-character notes
- **THEN** it reports an error on the matching field
- **[UNIT]**

### Requirement: Editing editions
`PUT /api/admin/ediciones/{id}` (policy `Backoffice`) with `{inicio, plazas, estado, notas?, version, confirmarCambioFecha?}` SHALL update the edition and return `200 {edicion, avisos}`, applying in order:
- a `version` different from the stored `xmin`, or a concurrent update, MUST return `409 concurrencia`;
- a `Cancelada` edition MUST NOT change state (`409 estado_invalido`);
- an edition that has started or is cancelled accepts only `notas`; any other change MUST return `409 estado_invalido`;
- `plazas` below occupied seats MUST return `409 plazas_por_debajo_de_ocupadas` with `extensions.ocupadas`;
- changing `inicio` while there are paid or valid pending bookings without `confirmarCambioFecha: true` MUST return `409 cambio_fecha_requiere_confirmacion` with `extensions.reservas`;
- `Oculta → Programada` MUST publish `EdicionPublicadaEvent` after the commit; other transitions MUST NOT;
- moving to `Cancelada` with bookings MUST succeed with the warning `Tiene X inscritos: avisa y reembolsa manualmente` in `avisos`.

#### Scenario: Stale version
- **WHEN** the edition is updated with a `version` read before a previous successful update
- **THEN** the response is `409` with `code = concurrencia`
- **[INTEGRATION]**

#### Scenario: Seats below occupied
- **WHEN** an edition with 5 occupied seats is updated to 4 seats
- **THEN** the response is `409` with `code = plazas_por_debajo_de_ocupadas` and `ocupadas = 5`
- **[INTEGRATION]**

#### Scenario: Date change with bookings
- **WHEN** an edition with bookings is moved to another date without `confirmarCambioFecha`
- **THEN** the response is `409` with `code = cambio_fecha_requiere_confirmacion` and `reservas` = number of bookings; the same request with `confirmarCambioFecha: true` returns `200`
- **[INTEGRATION]**

#### Scenario: Publishing a hidden edition
- **WHEN** an `Oculta` edition is updated to `Programada`
- **THEN** one `EdicionPublicadaEvent` is published; updating `Programada → Oculta`, editing notes or cancelling publishes none
- **[INTEGRATION]**

#### Scenario: Cancelling with bookings warns
- **WHEN** an edition with 2 bookings is updated to `Cancelada`
- **THEN** the response is `200` and `avisos` contains `Tiene 2 inscritos: avisa y reembolsa manualmente`
- **[INTEGRATION]**

#### Scenario: State machine
- **WHEN** `Edicion.Actualizar` is called on a domain object
- **THEN** `Programada ↔ Oculta`, `Programada → Cancelada` and `Oculta → Cancelada` succeed; `Cancelada → Programada` and `Cancelada → Oculta` throw `BusinessRuleException(estado_invalido)`; a started edition accepts a notes change and rejects a seats change with `estado_invalido`
- **[UNIT]**

#### Scenario: Seats and date rules in the domain
- **WHEN** `Edicion.Actualizar` is called with seats below occupied, or with a new date and bookings without confirmation, or with a new date in the past
- **THEN** it throws `BusinessRuleException(plazas_por_debajo_de_ocupadas)`, `BusinessRuleException(cambio_fecha_requiere_confirmacion)` and `DomainException` respectively
- **[UNIT]**

### Requirement: Deleting editions
`DELETE /api/admin/ediciones/{id}` (policy `Backoffice`) SHALL delete an edition without any booking (of any state) and return `204`; with bookings it MUST return `409 edicion_con_reservas`; an unknown id returns `404`.

#### Scenario: Delete without bookings
- **WHEN** an Admin deletes an edition without bookings
- **THEN** the response is `204` and `GET /api/admin/ediciones/{id}` returns `404`
- **[INTEGRATION]**

#### Scenario: Delete with bookings
- **WHEN** the edition has bookings
- **THEN** the response is `409` with `code = edicion_con_reservas`
- **[INTEGRATION]**
