## ADDED Requirements

### Requirement: Seats are freed by time before the job runs
A pending booking past its `ExpiraEn` SHALL stop occupying its seat immediately, whether or not the expiry job has run.

#### Scenario: Clock moves past the expiry
- **WHEN** the clock moves past a manual booking's `ExpiraEn` and the job has not run
- **THEN** the public calendar shows the seat as free again
- **[INTEGRATION]**

### Requirement: Expiry job
`ExpirarReservasCaducadasCommand` SHALL expire up to 100 `Pendiente` bookings whose `ExpiraEn` has passed, each one saved separately with its `xmin`, publishing one `ReservaExpiradaEvent` per expired booking. For Stripe bookings the session MUST be expired first; if Stripe reports it already completed, the booking MUST NOT be expired. A failing booking MUST NOT stop the batch. The command MUST hold a PostgreSQL advisory lock for the whole batch, so concurrent runs (several replicas) never expire the same booking twice. `ExpirarReservasJob` sends it every `Reservas:JobIntervaloMinutos`.

#### Scenario: Run the job
- **WHEN** the command runs after two bookings expired
- **THEN** both are `Expirada` with `ExpiradaEn` and exactly one `ReservaExpiradaEvent` is published for each
- **[INTEGRATION]**

#### Scenario: Two replicas at once
- **WHEN** two commands run in parallel over the same expired bookings
- **THEN** each booking is expired once and has one `ReservaExpiradaEvent`
- **[INTEGRATION]**

#### Scenario: Stripe session already paid
- **WHEN** an expired card booking's session is already completed in Stripe
- **THEN** the job leaves it `Pendiente`
- **[UNIT]**

#### Scenario: One failure does not stop the batch
- **WHEN** saving one booking fails with a concurrency error
- **THEN** the other bookings are still expired
- **[UNIT]**
