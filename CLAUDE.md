# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

ZoiNailAcademy backend: a .NET 10 REST API built on Clean Architecture, with JWT authentication (register/login/refresh) and per-user data isolation. `Notes` is a minimal example feature that shows the full pattern end to end (entity → command/query + handler + validator → controller → unit and integration tests); replace it with the real features of the product. Product requirements and API contracts live in `docs/` once they exist.

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
- `src/ZoiNailAcademy.Application` — use cases with MediatR (CQRS) + FluentValidation. Validation runs automatically in the pipeline (`ValidationBehavior`). Handlers return `Result`/`Result<T>` as the success envelope (the MediatR response is constrained to `Result`); expected failures that map to an HTTP status code are thrown as typed exceptions (`ConflictException` → 409, `UnauthorizedException` → 401, `ForbiddenException` → 403, `NotFoundException` → 404, `ValidationException` → 400) and translated centrally by `ExceptionHandlingMiddleware`. `DomainException` (invariant violations) → 400. Data access goes through the `IAppDbContext` interface. Does not reference Infrastructure or Host.
- `src/ZoiNailAcademy.Infrastructure` — EF Core + Npgsql (PostgreSQL). `AppDbContext` implements `IAppDbContext`, manages audit timestamps automatically in `SaveChanges`, and entity configurations live in `Data/Configurations/`. Authentication services (BCrypt password hasher, JWT generator, SHA-256 refresh-token hasher) live in `Authentication/`.
- `src/ZoiNailAcademy.Host` — ASP.NET Core: controllers, OpenAPI/Swagger, CORS (origins in `Cors:AllowedOrigins`), JWT Bearer authentication with a global fallback policy (every endpoint requires auth unless marked `[AllowAnonymous]`), per-IP rate limiting on `/auth/*`, and `ExceptionHandlingMiddleware` that translates exceptions into `ProblemDetails` responses (RFC 7807). Registers each layer's DI via its `AddApplication()` / `AddInfrastructure()` extensions.

`Directory.Build.props` centralizes `net10.0`, nullable, and implicit usings — do not redefine those properties in individual `.csproj` files.

The connection string is read from `ConnectionStrings:DefaultConnection` or, failing that, from the `DATABASE_URL` environment variable (URI format, converted into an Npgsql connection string). Deployment targets Railway via a multi-stage `Dockerfile`; the port comes from the `PORT` variable (default 8080) and `GET /health` returns 200 without authentication.

JWT signing is configured via the `Jwt` section (`SigningKey`, `Issuer`, `Audience`, `AccessTokenMinutes`), bound in Infrastructure as `JwtOptions`. On Railway these are supplied as environment variables (`Jwt__SigningKey`, `Jwt__Issuer`, `Jwt__Audience`, `Jwt__AccessTokenMinutes`) — `Jwt__SigningKey` is a secret and must never be committed; `appsettings.Development.json` only holds a non-production placeholder key for local runs.

Swagger UI and the OpenAPI document are mapped in Development, and can also be enabled temporarily in Production via the `Swagger__EnableInProduction=true` environment variable — unset (or `false`) by default, so `/swagger` and `/openapi/v1.json` stay unmapped and return `401` like any other unmatched route. When enabled, both endpoints bypass the global authentication fallback policy.

## Code conventions

- Controllers, not Minimal APIs.
- New use cases in Application are organized by feature, then by operation: `Application/<Feature>/<Operation>/` (e.g. `Notes/CreateNote/`), with each operation folder containing its command/query, handler, and validator. Do not create loose handlers outside a feature/operation folder.
- Commands/Queries/DTOs are positional sealed records (e.g. `public sealed record CreateNoteCommand(string Title, string Content);`).
- Thin MediatR handlers — business rules live in the domain entities, not in the handler.
- Domain entities: `private set` properties, instantiation only through static `Create(...)` factory methods that enforce invariants. Handlers never construct entities via public setters.
- Per-user data: entities owned by a user carry `UserId`; handlers resolve it with `ICurrentUserService.GetUserId()` and filter by it. A resource owned by another user is reported as `404`, never `403`.
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
- **IntegrationTests**: xUnit + `WebApplicationFactory<Program>` + Testcontainers.PostgreSql (spins up a real `postgres:17-alpine`, requires Docker). Tests share the fixture via `IntegrationTestCollection` and inherit from `IntegrationTestBase`. Calls to `/auth/*` should use `PostAsJsonFromIpAsync` with `NextTestClientIp()` so the rate limiter does not throttle unrelated tests.
- Naming: `Method_Scenario_ExpectedResult` (e.g. `Handle_ForeignNote_ThrowsNotFoundException`).
- Domain tests are pure unit tests with no mocks (the domain has no dependencies). Application handler tests mock `IAppDbContext` and services with Moq.
- Prefer TDD for new use cases: failing test, minimal implementation, refactor.
- Never run migrations or schema experiments against the real production database — always use Testcontainers PostgreSQL.

## OpenSpec workflow

Changes are planned with OpenSpec (`openspec/` directory): current specs live in `openspec/specs/` and in-progress changes in `openspec/changes/` (archived under `openspec/changes/archive/`). Use the `/opsx:propose`, `/opsx:apply`, `/opsx:sync`, and `/opsx:archive` commands to propose, implement, and archive changes. When implementing, respect the requirements of the existing specs (`project-skeleton`, `shared-kernel`, `authentication`).

## Conventions

- Markdown documentation and OpenSpec artifacts are written in English.
- Code identifiers (classes, methods, properties) are in English; user-facing error and validation messages are in Spanish; commit messages may be in Spanish.
- MVP philosophy: keep scope minimal — nothing beyond the PRD.
