# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

Zoi Nail Academy API: a .NET 10 REST API (Clean Architecture) for a course-selling website: 4 courses with dated editions and limited seats, bookings paid by card (Stripe) or bank transfer/Bizum, a waiting list, and a backoffice for the client. The backoffice is closed: two users (`Admin` = the client, `SuperAdmin` = the developer) are seeded from environment variables, and there is no registration. Product docs live in `Docs/`: `PRD.md`, `00-contrato-y-convenciones.md` (routes, DTOs, error codes, events — **it wins over the feature specs**), and `features/Fxx-*.md` (one spec per feature, implemented in order F01 → F11). The `Notes` example feature of the template was removed in F01; it is preserved in the first commit as a pattern reference.

## Commands

```bash
dotnet build                                  # build the whole solution
dotnet test                                   # all tests (unit + integration)
dotnet test tests/UnitTests                   # unit tests only
dotnet test tests/IntegrationTests            # integration only (requires Docker: uses Testcontainers)
dotnet test --filter "FullyQualifiedName~ResultTests"          # a single test class
dotnet test --filter "FullyQualifiedName~ResultTests.Success"  # a single test
dotnet run --project src/ZoiNailAcademy.Host       # run the API (Swagger UI in development)
```

EF Core migrations (there is an `IDesignTimeDbContextFactory` that reads `CONNECTION_STRING` or falls back to localhost, so no startup project is needed):

```bash
dotnet ef migrations add <Name> --project src/ZoiNailAcademy.Infrastructure
dotnet ef migrations has-pending-model-changes --project src/ZoiNailAcademy.Infrastructure   # also run in CI
```

Migrations are applied automatically at startup (`Database.MigrateAsync()` in `Program.cs`).

## Architecture

Clean Architecture with 4 projects and a strict dependency flow (enforced by the OpenSpec specs):

- `src/ZoiNailAcademy.Domain` — entities, enums, and domain exceptions. **References no other layer.** Entities inherit from `BaseAuditableEntity` (Guid Id + CreatedAt/UpdatedAt), expose properties with `private set`, and are constructed only through `Create(...)` factory methods.
- `src/ZoiNailAcademy.Application` — use cases with MediatR (CQRS) + FluentValidation. Validation runs automatically in the pipeline (`ValidationBehavior`). Handlers return `Result`/`Result<T>` as the success envelope (the MediatR response is constrained to `Result`); expected failures that map to an HTTP status code are thrown as typed exceptions (`ConflictException` → 409, `UnauthorizedException` → 401, `ForbiddenException` → 403, `NotFoundException` → 404, `ValidationException` → 400) and translated centrally by `ExceptionHandlingMiddleware`. `DomainException` (invariant violations) → 400; `BusinessRuleException` (Domain, contract business rules) → 409. Business errors carry the contract code (`ErrorCodes`) in `ProblemDetails` `code` (plus extra extensions such as `ocupadas`); `NotFoundException` always adds `no_encontrado`. Data access goes through the `IAppDbContext` interface. Does not reference Infrastructure or Host.
- `src/ZoiNailAcademy.Infrastructure` — EF Core + Npgsql (PostgreSQL). `AppDbContext` implements `IAppDbContext`, manages audit timestamps automatically in `SaveChanges`, and entity configurations live in `Data/Configurations/`. Authentication services (BCrypt password hasher, JWT generator, SHA-256 refresh-token hasher) live in `Authentication/`.
- `src/ZoiNailAcademy.Host` — ASP.NET Core: controllers (every route is prefixed with `/api` by `ApiRoutePrefixConvention` — never write `api/` in a `[Route]`), OpenAPI/Swagger, CORS only in Development, JWT Bearer authentication with a global fallback policy (every endpoint requires auth unless marked `[AllowAnonymous]`), authorization policies `AuthorizationPolicies.Backoffice` (Admin or SuperAdmin) and `AuthorizationPolicies.SuperAdmin`, named per-IP rate-limit policies (`RateLimitPolicies.Auth`, `.Reservas`, `.Formularios`), `ForwardedHeaders` as the first middleware, and `ExceptionHandlingMiddleware` that translates exceptions into `ProblemDetails` responses (RFC 7807). Registers each layer's DI via its `AddApplication()` / `AddInfrastructure()` extensions.

`Directory.Build.props` centralizes `net10.0`, nullable, and implicit usings — do not redefine those properties in individual `.csproj` files.

The connection string is read from `ConnectionStrings:DefaultConnection` or, failing that, from the `DATABASE_URL` environment variable (URI format, converted into an Npgsql connection string). Deployment targets Railway via a multi-stage `Dockerfile`; Kestrel listens on `http://*:{PORT}` (default 8080; dual-stack IPv6 for Railway's private network, IPv4 fallback) and `GET /api/health` returns 200 without authentication. In production the browser only talks to Next.js, which rewrites `/api/*` to this API over the private network.

JWT signing is configured via the `Jwt` section (`SigningKey`, `Issuer`, `Audience`, `AccessTokenMinutes`, `RefreshTokenDays`), bound in Infrastructure as `JwtOptions`. On Railway these are supplied as environment variables (`Jwt__SigningKey`, …) — `Jwt__SigningKey` is a secret and must never be committed; `appsettings.Development.json` only holds non-production placeholders for local runs. The access token (claims `sub`, `email`, `role`) goes in the response body; the refresh token only travels in the `rt` cookie (`HttpOnly`, `Secure` via `Auth:CookieSecure`, `SameSite=Strict`, `Path=/api/auth`). Users are seeded by `UserSeeder` after migrations (`Admin:*`, `SuperAdmin:*`; startup fails in Production if they are missing). Every configuration key is listed in `.env.example`; a new key must be added there and to `appsettings.json` (without secrets).

Seats: free seats are never stored. `PlazasCalculator` (Domain) holds the rules (`Reserva.OcupaPlazaEn` is the same rule as an EF expression) and `IOcupacionReader` reads occupancy from `Reservas` for all the editions of a response in one grouped query. Every booking goes through `CrearReservaService`, which locks the edition row (`IAppDbContext.BeginTransactionAsync` + `BloquearEdicionAsync`, `SELECT … FOR UPDATE`) before counting seats: never insert a `Reserva` elsewhere outside tests. Stripe is reached only through `IStripeGateway` (faked in integration tests by `FakeStripeGateway`) and webhooks through `IStripeWebhookParser` (real signature check; tests sign payloads with the test secret). Domain events (`INotification`) are published after the commit with `IPublisher.PublicarTrasCommitAsync`, which logs a failing handler instead of failing the request; integration tests read them with `EventRecorder`. Backoffice actions on a booking require the `version` (`xmin`) the client read (`AccionesReserva.CargarAsync`) and lock the edition before any seat check on a booking that no longer holds a seat. The expiry job (`ExpirarReservasJob` → `ExpirarReservasCaducadasCommand`) runs behind a PostgreSQL advisory lock; integration tests remove it from the shared host and start it only through `CustomWebApplicationFactory.CreateHost(clock, conJob: true)`, which is also how tests that move time forward get their own host and clock.

Waiting list (F05): one `ListaEspera` row per course and email (unique index); signing up again updates it. The stored consent is always `ConsentimientoListaEspera.Vigente` (never the front's text). Accent-insensitive search uses the `Busqueda` column, kept normalized by the entity with `TextoBusqueda.Normalizar`, so queries are a plain `Contains` (no `unaccent`). CSV exports go through `Application/Common/Csv/CsvBuilder` (BOM, `;`, quoting, formula protection) — reuse it in F07.

Emails (F06): never send from a request. Event handlers in `Application/Emails/NotificarEventos` render a template (`Emails/Plantillas`, built with `EmailCuerpo` so HTML and text match and every value is encoded) and enqueue it through `BandejaSalida` (deduplicated by `(Tipo, ReferenciaId)`); `EnviarEmailsJob` sends the outbox through `IEmailSender` behind its own advisory lock. Without `Resend:ApiKey` or `Email:From` the `LogEmailSender` is used, so nothing fails while Resend is not set up. Integration tests remove the job from the shared host and drive `EnviarEmailsPendientesCommand` on their own host with `FakeEmailSender`.

Backoffice queries (F07): booking listings project with `Select` to `ReservaAdminFila` (`ReservaAdminConsulta.Proyeccion`, one query with the edition and course joined) and map to `ReservaAdminDto` in memory; never load bookings one by one. `ReservaAdminConsulta.Buscar` is the booking search: `CodigoReserva.TryNormalizar` reads a code however it is typed, `Reserva.Busqueda` (set in `Create`, like `ListaEspera.Busqueda`) covers name/email/phone without accents, and `TextoBusqueda.DigitosTelefono` matches phones by digits. "Today" is computed with `ZonaHoraria.FinDelDia` in `App:TimeZone`, never in UTC. The summary counters are global, so integration tests check them as differences before/after seeding.

New-dates notices (F11): `EdicionPublicadaEvent` only marks the course in `AvisosPendientes`; `AvisosNuevasEdicionesJob` (`EnviarAvisosNuevasEdicionesCommand`, own advisory lock) sends one email per subscribed waiting-list person with every bookable date not yet in `AvisosEnviados`, inserting those rows, the emails and the removal of the mark in one save (`AvisoPendiente.Publicaciones` is a concurrency token, so a date published meanwhile is picked up next cycle). Emails a person can unsubscribe from carry `EmailSaliente.BajaUrl`, which `ResendEmailSender` sends as `List-Unsubscribe` + one-click; `POST /api/lista-espera/baja` takes the token from the query, JSON or form and always answers 204. Integration tests remove the job from the shared host and run the command directly.

Time: never call `DateTime.UtcNow`/`DateTimeOffset.UtcNow`; inject `TimeProvider` (registered as `TimeProvider.System`; integration tests use `FakeTimeProvider`). Timestamps are `DateTimeOffset` (`timestamptz`, UTC).

Swagger UI and the OpenAPI document are mapped in Development, and can also be enabled temporarily in Production via the `Swagger__EnableInProduction=true` environment variable — unset (or `false`) by default, so `/swagger` and `/openapi/v1.json` stay unmapped and return `401` like any other unmatched route. When enabled, both endpoints bypass the global authentication fallback policy.

Front (F09): `web/` is the Next.js 16 app (App Router, TypeScript strict, Tailwind 4, shadcn-style components in `web/src/components/ui`); today it holds only the backoffice under `/admin`, and F08 adds the public site to the same project. The browser only calls relative `/api/*`, rewritten to `API_INTERNAL_URL`. Backoffice auth lives in `web/src/lib/admin/session.ts`: access token only in memory, single-flight refresh on 401 (serialized across tabs with a Web Lock, because the API revokes every session when a rotated refresh token is replayed), proactive refresh a minute before expiry, logout broadcast on `BroadcastChannel('auth')`. Dates are typed and shown in Madrid through `web/src/lib/fechas.ts` (`madridAIso`/`isoAMadrid`). DTO types are hand-written in `web/src/lib/admin/types.ts`; `npm run api:types` generates the Swagger ones to compare. Commands: `cd web && npm run typecheck && npm test && npm run build`; `npm run e2e` runs Playwright (phone and desktop) against a running API in Development plus `npm start`.

## Code conventions

- Controllers, not Minimal APIs.
- New use cases in Application are organized by feature, then by operation: `Application/<Feature>/<Operation>/` (e.g. `Authentication/Login/`), with each operation folder containing its command/query, handler, and validator. Do not create loose handlers outside a feature/operation folder.
- Commands/Queries/DTOs are positional sealed records (e.g. `public sealed record LoginCommand(string Email, string Password);`).
- Thin MediatR handlers — business rules live in the domain entities, not in the handler.
- Domain entities: `private set` properties, instantiation only through static `Create(...)` factory methods that enforce invariants. Handlers never construct entities via public setters.
- Backoffice controllers live under `admin/...` and carry `[Authorize(Policy = AuthorizationPolicies.Backoffice)]` at class level; SuperAdmin-only actions add `[Authorize(Policy = AuthorizationPolicies.SuperAdmin)]`. `AuthorizationTests` discovers every `/api/admin/**` route dynamically and fails if one is anonymous or lacks those policies.
- Public form endpoints use `[EnableRateLimiting(RateLimitPolicies.Reservas)]` or `RateLimitPolicies.Formularios`.
- Domain, routes and DTOs follow the language of the contract (`Curso`, `Edicion`, `Reserva`, JSON `camelCase`, enums as strings); technical code inherited from the template stays in English.
- `AppDbContext` is only accessible from Infrastructure; Application uses the `IAppDbContext` interface.
- Nullable enabled; no suppressions (`#pragma` / `!`) without a documented justification.

## Adding a new feature (checklist)

1. Domain: entity in `Domain/Entities/` + unit tests in `tests/UnitTests/Domain/Entities/`.
2. Application: `Application/<Feature>/<Operation>/` with command/query, handler, validator + handler/validator unit tests.
3. Infrastructure: `DbSet` in `IAppDbContext` and `AppDbContext`, configuration in `Data/Configurations/`, then `dotnet ef migrations add <Name> --project src/ZoiNailAcademy.Infrastructure`.
4. Host: controller in `Host/Controllers/`.
5. Integration tests in `tests/IntegrationTests/Controllers/`.

## Tests

- **UnitTests**: xUnit + FluentAssertions + Moq (+ MockQueryable for `DbSet` mocks). Reference only Domain and Application.
- **IntegrationTests**: xUnit + `WebApplicationFactory<Program>` + Testcontainers.PostgreSql (spins up a real `postgres:17-alpine`, requires Docker). Tests share the fixture via `IntegrationTestCollection` and inherit from `IntegrationTestBase`. `IntegrationTestBase.SendAsync` sends a fresh `X-Forwarded-For` per request (`NextTestClientIp()`) so the rate limiter does not throttle unrelated tests; use `CreateUserAsync(role)` / `LoginAsNewUserAsync(role)` to get users and tokens (there is no registration), and pass the `rt` cookie explicitly (the client does not keep cookies). Tests that move time forward or change configuration create their own host with `Factory.WithWebHostBuilder(...)`.
- Naming: `Method_Scenario_ExpectedResult` (e.g. `Handle_ReusingRotatedToken_RevokesEveryActiveTokenOfTheUser`).
- Domain tests are pure unit tests with no mocks (the domain has no dependencies). Application handler tests mock `IAppDbContext` and services with Moq.
- Prefer TDD for new use cases: failing test, minimal implementation, refactor.
- Never run migrations or schema experiments against the real production database — always use Testcontainers PostgreSQL.

## OpenSpec workflow

Changes are planned with OpenSpec (`openspec/` directory): current specs live in `openspec/specs/` and in-progress changes in `openspec/changes/` (archived under `openspec/changes/archive/`). Use the `/opsx:propose`, `/opsx:apply`, `/opsx:sync`, and `/opsx:archive` commands to propose, implement, and archive changes. When implementing, respect the requirements of the existing specs (`project-skeleton`, `shared-kernel`, `authentication`, and after F01 `user-seeding` and `network-edge`).

## Conventions

- Markdown documentation and OpenSpec artifacts are written in English.
- Code identifiers (classes, methods, properties) are in English, except business domain names, routes and DTO fields, which follow the Spanish API contract in `Docs/00-contrato-y-convenciones.md`; user-facing error and validation messages are in Spanish; commit messages may be in Spanish.
- MVP philosophy: keep scope minimal — nothing beyond the PRD.
