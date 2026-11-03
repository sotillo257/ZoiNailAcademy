## Why

The website sells seats for 4 courses, each with dated editions and a limited number of seats. Nothing can be booked (F03/F04), listed on the public site (F08) or managed from the backoffice (F07/F09) until courses and editions exist with a single, reusable computation of free seats. The client needs to create, edit, hide, cancel and delete editions on her own; the developer maintains the course catalogue (see `Docs/features/F02-cursos-y-ediciones.md` and `Docs/00-contrato-y-convenciones.md`).

## What Changes

- New `Curso` and `Edicion` aggregates (EF configuration, migration, `xmin` concurrency token on `Edicion`, index `(CursoId, Inicio)`).
- Seed of the 4 initial courses from an editable JSON file in the repository (`[PENDIENTE]` placeholders until the client sends the texts); existing slugs are never overwritten.
- Domain service `PlazasCalculator`: which bookings occupy a seat, free seats (never negative) and the public state (`Disponible`, `UltimasPlazas`, `Completo`). The occupancy read (`IOcupacionReader`) is a single grouped query per listing; until F03 adds `Reserva` it reports zero occupied seats.
- Public read endpoints: `GET /api/cursos`, `GET /api/cursos/{slug}`, `GET /api/cursos/{slug}/ediciones` (`Cache-Control: no-store`).
- Backoffice endpoints: `GET /api/admin/cursos` (selector), edition CRUD under `/api/admin/ediciones` (policy `Backoffice`), course create/update/deactivate under `/api/admin/cursos` (policy `SuperAdmin`).
- `EdicionPublicadaEvent(edicionId)` is published after the commit when an edition is created as `Programada` or moves from `Oculta` to `Programada`.
- Business error codes in `ProblemDetails.extensions.code` (`concurrencia`, `plazas_por_debajo_de_ocupadas`, `cambio_fecha_requiere_confirmacion`, `estado_invalido`, `edicion_con_reservas`, `curso_con_ediciones`, `no_encontrado`), with extra extensions (`ocupadas`, `reservas`).
- New configuration keys `Reservas:CierreHorasAntes` (24), `Reservas:UmbralUltimasPlazas` (3) and `App:TimeZone` (`Europe/Madrid`).

## Capabilities

### New Capabilities
- `cursos`: course catalogue, seed, public course endpoints and SuperAdmin course CRUD.
- `ediciones`: editions, bookable/visible rules, free-seat computation, public calendar and the client's edition CRUD.

### Modified Capabilities
- `shared-kernel`: business error codes in `extensions.code`; a domain-level business-rule exception mapped to `409`; `NotFoundException` carries `no_encontrado`.

## Non-goals

- The `Reserva` entity and any booking flow (F03/F04). Occupied seats are `0` until F03 plugs `Reserva` into `IOcupacionReader`.
- Cancelling the pending bookings of a cancelled edition (F04) — left as a `TODO` next to the cancellation.
- Event handlers for `EdicionPublicadaEvent` (F11).
- Image upload (images live in the front's `public/`).
- Regenerating the front's TypeScript types (done in F08/F09 from Swagger) — `[MANUAL]`.

## Impact

- **Domain**: `Curso`, `Edicion`, enums `EstadoEdicion`, `EstadoEdicionPublico`, `EstadoReserva`; `PlazasCalculator`; `BusinessRuleException` + `ErrorCodes`.
- **Application**: `Cursos/*` and `Ediciones/*` operations, `IReservasSettings`, `IOcupacionReader` + `OcupacionReader`, `EdicionPublicadaEvent`; `ConflictException` gains a code and extensions.
- **Infrastructure**: configurations, migration `CursosYEdiciones`, `CursoSeeder` + embedded `cursos.json`, `ReservasSettings` bound from `Reservas`.
- **Host**: `CursosController` (public), `AdminCursosController`, `AdminEdicionesController`; middleware writes `extensions.code`; seeder called at startup.
- **Config**: `Reservas:*`, `App:TimeZone` in `appsettings.json` and `.env.example`.
