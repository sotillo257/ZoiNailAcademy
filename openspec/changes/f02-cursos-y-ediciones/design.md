## Context

F01 left a closed backoffice (Admin/SuperAdmin), the `/api` prefix, `TimeProvider` everywhere and `ProblemDetails` errors without business codes. F02 adds the first business aggregates. `Reserva` arrives in F03, but the free-seat computation must already be written in terms of bookings so that F03/F04/F07 reuse it without rework.

## Goals / Non-Goals

**Goals:** courses and editions persisted and seeded; public read endpoints with free seats and public state; edition CRUD for the client with optimistic concurrency and the contract's business error codes; course CRUD for the developer; `EdicionPublicadaEvent` after commit.

**Non-Goals:** `Reserva` and bookings (F03/F04), handlers for `EdicionPublicadaEvent` (F11), image upload, front types.

## Aggregates and invariants

**`Curso`** (aggregate root). `Nombre` (≤120), `Slug` (≤120, `^[a-z0-9]+(-[a-z0-9]+)*$`, unique), `DescripcionCorta` (≤300), `Descripcion` (text, markdown), `Precio` (`decimal(10,2)`, > 0), `Imagen` (≤300), `Ubicacion?` (≤300), `DuracionTexto?` (≤100), `Activo`, `Orden`.
- Invariants (`DomainException` → 400): required texts not blank, slug format, price > 0.
- `Actualizar(...)` reports whether the slug changed. Deactivating (`Desactivar` or `Actualizar` with `activo = false` on an active course) while the course has future `Programada` editions throws `BusinessRuleException(curso_con_ediciones)` → 409. Whether such editions exist is a query answered by the handler and passed in.
- Slug uniqueness is checked by the handler (field error `slug`, 400) and backed by a unique index.

**`Edicion`** (aggregate root; references `Curso` by `CursoId`, `OnDelete: Restrict`). `Inicio` (UTC), `Plazas` (1–200), `Estado` (`Programada | Oculta | Cancelada`, text), `Notas?` (≤500), `Version` (`xmin`).
- `Create(curso, inicio, plazas, estado, notas, now)`: the course must be active, `inicio > now`, `plazas` in 1–200, initial state `Programada` or `Oculta` (default `Programada`).
- `Actualizar(inicio, plazas, estado, notas, ocupadas, confirmarCambioFecha, now)` returns `CambioEdicion(Publicada, Avisos)`:
  1. A `Cancelada` edition cannot change to another state → `estado_invalido`.
  2. A started edition (`Inicio <= now`) or a cancelled one only accepts `Notas`; any other change → `estado_invalido`.
  3. `plazas` out of 1–200 or a new `inicio` in the past → `DomainException` (400).
  4. `plazas < ocupadas` → `plazas_por_debajo_de_ocupadas` with `extensions.ocupadas`.
  5. `inicio` changed, `ocupadas > 0` and no confirmation → `cambio_fecha_requiere_confirmacion` with `extensions.reservas`.
  6. `Oculta → Programada` sets `Publicada`. Moving to `Cancelada` with `ocupadas > 0` adds the warning `Tiene X inscritos: avisa y reembolsa manualmente`.
- `EsReservable(cursoActivo, now, cierre)`: `Estado == Programada && cursoActivo && Inicio - now > cierre`. Visible on the web ⇔ bookable (even when full).

State machine of `Edicion.Estado` (only these transitions are allowed while the edition has not started):

| From \ To | Programada | Oculta | Cancelada |
| --- | --- | --- | --- |
| Programada | — | yes | yes |
| Oculta | yes (publishes event) | — | yes |
| Cancelada | `estado_invalido` | `estado_invalido` | — |

## Decisions

**D-01 `PlazasCalculator` is a static domain service.** `OcupaPlaza(estado, expiraEn, now)` (`Pagada`, or `Pendiente` with `ExpiraEn > now`), `Ocupadas(reservas, now)`, `Libres(plazas, ocupadas) = max(0, plazas − ocupadas)` and `EstadoPublico(libres, umbral)` (`Completo` if 0, `UltimasPlazas` if `≤ umbral`, else `Disponible`). `EstadoReserva` is added to Domain now because it is part of the contract and the rule needs it. *Alternative:* wait for F03 — rejected, the spec asks for the calculation designed and tested now.

**D-02 Occupancy is read through `IOcupacionReader` (Application).** `LeerAsync(edicionIds)` returns `Ocupacion(Pagadas, Pendientes)` per edition in one grouped query; `TieneReservasAsync(edicionId)` answers the delete rule. The F02 implementation (`OcupacionReader`, Application, over `IAppDbContext`) returns no bookings; F03 replaces its body with a `GroupBy` over `Reservas` using the same predicate as `PlazasCalculator.OcupaPlaza`. Listings call it once per request (never per edition), so there is no N+1. No counter is stored. Integration tests decorate it with a fake that lets a test declare occupied seats for its own editions. *Alternative:* create a minimal `Reserva` table now — rejected, F02 explicitly excludes it.

**D-03 Business rule violations.** Domain gets `BusinessRuleException(code, message, extensions)` and `ErrorCodes` (the contract's codes). `ConflictException` (Application) gains an optional `Code` and `Extensions`. `ExceptionHandlingMiddleware` maps both to `409` and writes `extensions.code` plus any extra extensions; `NotFoundException` writes `code = no_encontrado`. `DomainException` keeps mapping to `400`.

**D-04 Optimistic concurrency with `xmin`.** `Edicion.Version` (`uint`, `IsRowVersion()` → PostgreSQL `xmin`) is exposed as a string `version` in `EdicionAdminDto`. `PUT` compares the received `version` with the loaded one (stale form → `409 concurrencia`) and catches `DbUpdateConcurrencyException` (simultaneous writes → `409 concurrencia`). A non-numeric `version` is a validation error.

**D-05 Events after commit.** `EdicionPublicadaEvent(Guid EdicionId) : INotification` lives in `Application/Ediciones`. Handlers call `IPublisher.Publish` only after `SaveChangesAsync` succeeds. No handler exists in F02; MediatR ignores notifications without handlers.

**D-06 Settings.** `IReservasSettings` (Application) exposes `CierreAntesDeInicio` and `UmbralUltimasPlazas`; Infrastructure binds `Reservas:{CierreHorasAntes, UmbralUltimasPlazas}` (24, 3). `App:TimeZone` is added to configuration for the front and emails; the API keeps every date in UTC.

**D-07 Course seed.** `Infrastructure/Data/Seed/cursos.json` is an embedded resource read by `CursoSeeder` after `UserSeeder`. A course is inserted only if no course has that slug; existing courses are never updated. Texts are `[PENDIENTE]` placeholders. *Alternative:* `HasData` in a migration — rejected, every text change would need a migration and would overwrite the developer's edits.

**D-08 Public queries.** Visible editions are filtered in SQL (`Estado = Programada AND Curso.Activo AND Inicio > now + cierre`), ordered by `Inicio`, loaded in one query for all the courses of the response, then occupancy is read in one query. `proximaEdicion` is the first visible edition with free seats, else the first visible one, else `null`. `GET /api/cursos/{slug}/ediciones` sets `Cache-Control: no-store`. A missing or inactive course → `404 no_encontrado`.

**D-09 Admin listing order.** Upcoming editions (`Inicio > now`) ascending by `Inicio`; with `incluirPasadas=true`, past ones follow, most recent first.

**D-10 No new NuGet packages, no new project references.**

## Endpoints

| Method | Route | Auth | Request | Success | Errors |
| --- | --- | --- | --- | --- | --- |
| GET | `/api/cursos` | anonymous | — | `200 CursoResumenDto[]` | — |
| GET | `/api/cursos/{slug}` | anonymous | — | `200 CursoDetalleDto` | `404 no_encontrado` |
| GET | `/api/cursos/{slug}/ediciones` | anonymous | — | `200 EdicionPublicaDto[]`, `Cache-Control: no-store` | `404 no_encontrado` |
| GET | `/api/admin/cursos` | Backoffice | — | `200 {id, nombre, slug, activo}[]` | `401`, `403` |
| POST | `/api/admin/cursos` | SuperAdmin | `CursoBody` | `201 CursoAdminDto` | `400`, `401`, `403` |
| PUT | `/api/admin/cursos/{id}` | SuperAdmin | `CursoBody` | `200 {curso: CursoAdminDto, avisos: string[]}` | `400`, `404`, `409 curso_con_ediciones` |
| DELETE | `/api/admin/cursos/{id}` | SuperAdmin | — | `204` (deactivates) | `404`, `409 curso_con_ediciones` |
| GET | `/api/admin/ediciones?cursoId=&incluirPasadas=false` | Backoffice | — | `200 EdicionAdminDto[]` | `401`, `403` |
| GET | `/api/admin/ediciones/{id}` | Backoffice | — | `200 EdicionAdminDto` | `404` |
| POST | `/api/admin/ediciones` | Backoffice | `{cursoId, inicio, plazas, estado?, notas?}` | `201 EdicionAdminDto` | `400`, `404` (course) |
| PUT | `/api/admin/ediciones/{id}` | Backoffice | `{inicio, plazas, estado, notas?, version, confirmarCambioFecha?}` | `200 {edicion, avisos}` | `400`, `404`, `409 concurrencia / plazas_por_debajo_de_ocupadas / cambio_fecha_requiere_confirmacion / estado_invalido` |
| DELETE | `/api/admin/ediciones/{id}` | Backoffice | — | `204` | `404`, `409 edicion_con_reservas` |

`CursoBody` = `{nombre, slug, descripcionCorta, descripcion, precio, imagen, ubicacion?, duracionTexto?, activo, orden}`. `CursoAdminDto` = `CursoBody` + `id`. Changing the slug adds the warning `Has cambiado el slug: las URLs antiguas del curso dejarán de funcionar (afecta al SEO).`

`EdicionAdminDto` = `{id, cursoId, curso, inicio, plazas, pagadas, pendientes, libres, estado, reservable, notas, version}`. Public DTOs follow `Docs/00-contrato-y-convenciones.md` exactly.

Errors are `application/problem+json`. Business errors look like:

```json
{ "status": 409, "title": "Conflict", "detail": "No puedes dejar menos plazas que las ocupadas (5).",
  "code": "plazas_por_debajo_de_ocupadas", "ocupadas": 5 }
```

(ASP.NET Core serializes `ProblemDetails.Extensions` at the top level of the JSON object, which is what `extensions.code` means in the contract.)

## Risks / Trade-offs

- [Until F03 every edition shows all seats free] → expected by the spec; `IOcupacionReader` is the single place F03 changes, and its integration tests already exercise the 409 paths through a fake.
- [`xmin` changes on any row update, including future ones made by F03/F04 code paths that touch `Edicion`] → only the backoffice edit compares it; other writers do not need the version.
- [The visible/bookable rule is written twice: `Edicion.EsReservable` (in memory) and the SQL filter] → both are covered by tests (unit for the method, integration for the endpoints).
- [Placeholder course texts go live if the client does not send them] → marked `[PENDIENTE]`; the SuperAdmin edits them through the API.

## Migration Plan

New migration `CursosYEdiciones` (tables `Cursos`, `Ediciones`). Applied at startup; the seed inserts the 4 courses on the first start.

## Open Questions

- VAT included in `Precio` and course location (`Ubicacion`) — open questions of the PRD; the fields exist and are editable.
