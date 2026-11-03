## ADDED Requirements

### Requirement: Course invariants
A `Curso` SHALL be created only through `Curso.Create(...)`. It MUST reject a blank `Nombre`, `DescripcionCorta` or `Imagen`, a `Slug` that does not match `^[a-z0-9]+(-[a-z0-9]+)*$`, and a `Precio` that is not greater than zero, by throwing `DomainException`.

#### Scenario: Valid course is created
- **WHEN** `Curso.Create` is called with valid data
- **THEN** the course exposes the trimmed values and `Activo`/`Orden` as given
- **[UNIT]**

#### Scenario: Invalid slug or price is rejected
- **WHEN** `Curso.Create` is called with slug `Acrílico Pro` or with price `0`
- **THEN** a `DomainException` is thrown
- **[UNIT]**

### Requirement: Initial courses are seeded
At startup, after the users are seeded, the system SHALL insert every course of the embedded `cursos.json` whose slug does not exist yet. Existing courses MUST NOT be modified.

#### Scenario: The 4 courses are available after the first start
- **WHEN** the application has started
- **THEN** `GET /api/cursos` contains the 4 seeded slugs
- **[INTEGRATION]**

#### Scenario: Seeding again does not overwrite a course
- **WHEN** a seeded course is edited and the seeder runs again
- **THEN** the course keeps the edited values and no duplicate is created
- **[INTEGRATION]**

### Requirement: Public course catalogue
`GET /api/cursos` (anonymous) SHALL return the active courses ordered by `Orden` as `CursoResumenDto[]`, each with `proximaEdicion`: the first visible edition with free seats, otherwise the first visible edition, otherwise `null`. `GET /api/cursos/{slug}` SHALL return `CursoDetalleDto` with the visible editions ordered by `Inicio`; it MUST return `404` with `code = no_encontrado` when the course does not exist or is inactive.

#### Scenario: Only active courses are listed
- **WHEN** an active and an inactive course exist
- **THEN** `GET /api/cursos` includes the active one and not the inactive one
- **[INTEGRATION]**

#### Scenario: Next edition prefers one with free seats
- **WHEN** a course has a full visible edition followed by one with free seats
- **THEN** `proximaEdicion` is the second one; when every visible edition is full it is the first one; with no visible edition it is `null`
- **[INTEGRATION]**

#### Scenario: Unknown or inactive course
- **WHEN** `GET /api/cursos/{slug}` is called with a slug that does not exist or belongs to an inactive course
- **THEN** the response is `404` with `code = no_encontrado`
- **[INTEGRATION]**

### Requirement: Course selector for the backoffice
`GET /api/admin/cursos` (policy `Backoffice`) SHALL return `{id, nombre, slug, activo}` for every course, active or not, ordered by `Orden`.

#### Scenario: Admin lists courses including inactive ones
- **WHEN** an Admin calls `GET /api/admin/cursos`
- **THEN** the response is `200` and contains active and inactive courses
- **[INTEGRATION]**

### Requirement: Course management is SuperAdmin-only
`GET /api/admin/cursos/{id}`, `POST /api/admin/cursos`, `PUT /api/admin/cursos/{id}` and `DELETE /api/admin/cursos/{id}` SHALL require the `SuperAdmin` policy. `GET` returns `200 CursoAdminDto` with every field of the course, active or not (`404 no_encontrado` when it does not exist), so the edit screen can be filled. Input MUST be validated (field lengths of the data model, slug format, `precio > 0`, `orden >= 0`) with `400` and errors per field; a slug already used by another course MUST be a `400` error on `slug`. `POST` returns `201 CursoAdminDto`. `PUT` returns `200 {curso, avisos}`, and `avisos` MUST contain a warning when the slug changed. `DELETE` MUST deactivate the course (never delete the row) and return `204`.

#### Scenario: Admin cannot create courses
- **WHEN** an Admin calls `POST /api/admin/cursos` with a valid body
- **THEN** the response is `403`
- **[INTEGRATION]**

#### Scenario: SuperAdmin creates a course
- **WHEN** a SuperAdmin calls `POST /api/admin/cursos` with a valid body
- **THEN** the response is `201` and the course is listed by `GET /api/admin/cursos`
- **[INTEGRATION]**

#### Scenario: SuperAdmin reads a course to edit it
- **WHEN** a SuperAdmin calls `GET /api/admin/cursos/{id}` for an inactive course, an Admin calls it, and a SuperAdmin calls it for an unknown id
- **THEN** the responses are `200` with every field, `403` and `404 no_encontrado`
- **[INTEGRATION]** **[UNIT]**

#### Scenario: Duplicate slug is rejected
- **WHEN** a SuperAdmin creates a course with the slug of an existing course
- **THEN** the response is `400` with an error on `slug`
- **[INTEGRATION]**

#### Scenario: Changing the slug warns
- **WHEN** a SuperAdmin updates a course with a new slug
- **THEN** the response is `200` and `avisos` contains the slug warning
- **[INTEGRATION]**

#### Scenario: Invalid fields are reported per field
- **WHEN** the create or update validator receives an empty name, a malformed slug or a non-positive price
- **THEN** it reports an error on each of those fields
- **[UNIT]**

### Requirement: A course with future scheduled editions cannot be deactivated
Deactivating a course (through `DELETE` or `PUT` with `activo = false`) MUST fail with `409` and `code = curso_con_ediciones` while the course has editions with `Estado = Programada` and `Inicio` in the future. Otherwise the course becomes inactive and disappears from the public endpoints.

#### Scenario: Deactivate a course without future editions
- **WHEN** a SuperAdmin calls `DELETE /api/admin/cursos/{id}` for a course without future scheduled editions
- **THEN** the response is `204`, the course still exists with `activo = false`, and `GET /api/cursos/{slug}` returns `404`
- **[INTEGRATION]**

#### Scenario: Deactivate a course with future scheduled editions
- **WHEN** the course has a future `Programada` edition
- **THEN** the response is `409` with `code = curso_con_ediciones`
- **[INTEGRATION]**

#### Scenario: Domain rule
- **WHEN** `Curso.Desactivar(tieneEdicionesFuturasProgramadas: true)` is called
- **THEN** a `BusinessRuleException` with code `curso_con_ediciones` is thrown and the course stays active
- **[UNIT]**
