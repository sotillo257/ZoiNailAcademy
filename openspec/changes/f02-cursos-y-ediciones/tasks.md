## 1. Domain: error codes, seats and enums

- [x] 1.1 Add `ErrorCodes`, `BusinessRuleException` and the enums `EstadoEdicion`, `EstadoEdicionPublico`, `EstadoReserva`
- [x] 1.2 Test `PlazasCalculator` (no bookings, paid, valid pending, expired pending, `Expirada`/`Cancelada`, never negative, public state per threshold), then implement it

## 2. Domain: Curso and Edicion

- [x] 2.1 Test `Curso.Create` invariants, `Actualizar` (slug change flag, deactivation rule) and `Desactivar`, then implement `Curso`
- [x] 2.2 Test `Edicion.Create` invariants and `EsReservable` with a fixed clock, then implement them
- [x] 2.3 Test `Edicion.Actualizar` (state machine, started/cancelled only notes, seats below occupied, date change confirmation, past date, cancellation warning), then implement it

## 3. Application: shared pieces

- [x] 3.1 Add `Cursos`/`Ediciones` to `IAppDbContext`, `IReservasSettings`, `IOcupacionReader` + `Ocupacion` + `OcupacionReader` (no bookings until F03), `EdicionPublicadaEvent`, DTOs and `EdicionesPublicas`; `ConflictException` gains `Code`/`Extensions`

## 4. Application: editions

- [x] 4.1 Test `CrearEdicionValidator` and `EditarEdicionValidator`, then implement them
- [x] 4.2 Test `CrearEdicionHandler` (default state, event only when `Programada`, unknown course), then implement it
- [x] 4.3 Test `EditarEdicionHandler` (stale version, publish only on `Oculta → Programada`, occupancy from the reader, cancellation warning), then implement it
- [x] 4.4 Test `BorrarEdicionHandler` (with/without bookings, unknown), then implement it
- [x] 4.5 Test `ListarEdicionesAdminHandler` (filter, past editions last, occupancy) and `ObtenerEdicionAdminHandler`, then implement them

## 5. Application: courses

- [x] 5.1 Test the course validators (shared `CursoCamposValidator`), then implement them
- [x] 5.2 Test `CrearCursoHandler` (duplicate slug), `EditarCursoHandler` (slug warning, deactivation rule) and `DesactivarCursoHandler`, then implement them
- [x] 5.3 Test `ListarCursosHandler` (active only, order, `proximaEdicion`) and `ObtenerCursoHandler` (inactive → 404, visible editions), then implement them and `ListarEdicionesPublicasHandler`

## 6. Infrastructure

- [x] 6.1 `CursoConfiguration`, `EdicionConfiguration` (`xmin`, `Restrict`, index `(CursoId, Inicio)`), `DbSet`s and migration `CursosYEdiciones`; `has-pending-model-changes` is clean
- [x] 6.2 `ReservasOptions`/`ReservasSettings` bound from `Reservas`; `Reservas:*` and `App:TimeZone` in `appsettings.json` and `.env.example`
- [x] 6.3 `CursoSeeder` with the embedded `Data/Seed/cursos.json` (`[PENDIENTE]` placeholders), run after `UserSeeder`

## 7. Host

- [x] 7.1 `ExceptionHandlingMiddleware` writes `code` and extensions for `BusinessRuleException`/`ConflictException`, and `no_encontrado` for `NotFoundException`
- [x] 7.2 `CursosController` (anonymous, `no-store` on the calendar), `AdminCursosController` (Backoffice; SuperAdmin on writes), `AdminEdicionesController` (Backoffice)

## 8. Integration tests (Testcontainers)

- [x] 8.1 Test support: `TestOcupacionReader` (declares bookings per edition) and `RecordingEdicionPublicadaHandler`
- [x] 8.2 Public endpoints: seeded courses, active only, visible editions only, `Disponible/UltimasPlazas/Completo`, `proximaEdicion`, `404 no_encontrado`, `Cache-Control: no-store`
- [x] 8.3 Edition CRUD: create (event / no event, validation, unknown course), list (filter, past), stale `version` → `409 concurrencia`, seats below occupied → `409`, date change with/without confirmation, events only on `Oculta → Programada`, cancellation warning and no reactivation, started edition only notes, delete with/without bookings
- [x] 8.4 Course CRUD: Admin gets `403` on create, SuperAdmin creates, duplicate slug `400`, slug warning, deactivation with/without future scheduled editions; the dynamic `AuthorizationTests` cover the new `/api/admin` routes
- [x] 8.5 Seeder idempotency: running it again neither duplicates nor overwrites courses
- [x] 8.6 `GET /api/admin/cursos/{id}` (SuperAdmin, `CursoAdminDto`, inactive courses included, `404`), added after F11 for the F09 edit screen

## 9. Manual

- [ ] 9.1 [MANUAL] Replace the `[PENDIENTE]` texts, prices and slugs of `cursos.json` with the client's data before the first deploy (or edit them through the API afterwards)
- [ ] 9.2 [MANUAL] Regenerate the front's TypeScript types from Swagger (F08/F09)
- [ ] 9.3 [MANUAL] Check the SQL in the logs of a real run: each listing issues one query for courses, one for editions (and, from F03, one for occupancy)
