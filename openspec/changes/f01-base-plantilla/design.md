## Context

The solution was generated with `dotnet new plantilla-backend -n ZoiNailAcademy`. The template ships open registration, refresh tokens in the response body, routes at the root, a single hard-coded `auth` rate limit (5/min) and the `Notes` example. The product (see `Docs/PRD.md`, `Docs/00-contrato-y-convenciones.md`, `Docs/features/F01-base-plantilla.md`) runs on Railway as two services: Next.js (public) rewrites `/api/*` over the private network to this API, which has no public domain. The backoffice has exactly two users, created from environment variables. The contract document wins over the feature spec when the two disagree.

## Goals / Non-Goals

**Goals:**
- Leave a production-ready base that every later feature builds on: `/api` prefix, closed registration, roles and policies, cookie-based refresh, real client IP, reusable rate-limit policies, injected clock.
- Make "fail closed" verifiable: any future `/api/admin/**` route without the backoffice policy breaks a test.

**Non-Goals:**
- Password recovery by email, and user management.
- Business entities (F02+), domain events (F02+), emails (F06).
- Railway, DNS and CI configuration (F10).

## Aggregates and invariants

- **User** (aggregate root): `Email` (normalized, unique), `DisplayName`, `PasswordHash`, `Role` (`UserRole`: `Admin | SuperAdmin`, persisted as text). Created only through `User.Create(email, displayName, passwordHash, role)`. The password hash is changed only through `UpdatePasswordHash`.
- **RefreshToken** (its own aggregate, referencing `UserId`): `TokenHash` (SHA-256, unique lookup), `ExpiresAt`, `RevokedAt`, `ReplacedByTokenHash`. Invariant: once revoked it is never active again. Activity depends on time, so the clock is passed in (`IsActiveAt(now)`, `Revoke(now, replacedBy)`) to keep the domain free of `DateTime.UtcNow`.

## Decisions

**D-01 Global route prefix through `IApplicationModelConvention`.** `ApiRoutePrefixConvention` puts `api/` in front of every controller's attribute route. *Alternative:* write `[Route("api/...")]` on each controller — rejected because it is easy to forget. An integration test walks `EndpointDataSource` and asserts that every route starts with `api/`. The only exception is the OpenAPI document (`/openapi/v1.json`), which is development tooling and is never reachable through the Next rewrite.

**D-02 Registration is deleted, not hidden.** The controller action, command, handler, validator and tests are removed. Because of the global fallback authorization policy, an *anonymous* request to any unmatched route is answered with `401` (the same behavior the template documents for `/swagger` in Production). As a result, `POST /api/auth/register` returns `404` for an authenticated caller and `401` for an anonymous one. The test checks both and also asserts that the endpoint is absent from `EndpointDataSource`. *Alternative:* a catch-all 404 for anonymous unmatched routes — rejected because it changes the existing Swagger exposure requirement and helps route enumeration.

**D-03 Roles as an enum on `User`, emitted as the `role` claim.** `JwtBearer` runs with `MapInboundClaims = false`, `RoleClaimType = "role"` and `NameClaimType = "sub"`, so claim names match the token exactly. The policies `Backoffice` (`Admin`, `SuperAdmin`) and `SuperAdmin` (`SuperAdmin`) are defined in `AuthorizationPolicies`. Admin controllers carry `[Authorize(Policy = "Backoffice")]` at class level. Two integration tests protect future features: (a) every endpoint whose route starts with `api/admin` has no `IAllowAnonymous` and requires `Backoffice` or `SuperAdmin` (metadata check), and (b) every such endpoint, discovered dynamically with its route parameters filled in, answers `401` without a token, and every `SuperAdmin` endpoint answers `403` to an `Admin` token. F01 has no real admin endpoints yet, so the test assembly adds a test-only controller (`api/admin/__test`) as an application part to exercise both policies. `OnForbidden` returns `403` `ProblemDetails`.

**D-04 Seeding in Infrastructure (`UserSeeder`), run after `MigrateAsync`.** Options: `Admin:{SeedEmail, SeedPassword, SeedNombre}` and `SuperAdmin:{SeedEmail, SeedPassword, SeedNombre, ResetAdminPassword}`. Rules:
- A user is created only when no user with that (normalized) email exists. An existing user is never overwritten.
- If `SuperAdmin:ResetAdminPassword=true`, the Admin password is reset to `Admin:SeedPassword`, all of the Admin's refresh tokens are revoked, and a warning is logged.
- If an email or password is missing: in Production the startup throws `InvalidOperationException` naming the variable; in any other environment that user is skipped with a warning.
- A seed password shorter than 10 characters (the same rule as change-password) is rejected the same way.

*Alternative:* an Application command — rejected because seeding is a startup concern, not a use case exposed through MediatR.

**D-05 Refresh token only in a cookie.** `rt`; `HttpOnly`; `Secure` (`Auth:CookieSecure`, default `true`, `false` in `appsettings.Development.json`); `SameSite=Strict`; `Path=/api/auth`; `Expires` = refresh expiry. Handlers return an internal `AuthResult` (access token, `expiresIn` in seconds, raw refresh token, refresh expiry, user). The controller writes the cookie and returns only `{accessToken, expiresIn, user}`. `Path=/api/auth` also sends the cookie to `cambiar-password`, which needs it to know which session to keep.

**D-06 Reuse detection and concurrent rotation.**
- When a token that was already *rotated* (`ReplacedByTokenHash != null`) is presented, every active refresh token of that user is revoked and the request gets `401`.
- A token revoked by logout or change-password only gets `401`.
- `RefreshToken` uses PostgreSQL `xmin` as a concurrency token, so two simultaneous rotations of the same token cannot both succeed: the loser gets a `DbUpdateConcurrencyException`, which is translated to `401`.

**D-07 Wrong current password in change-password is a `400` validation error on `passwordActual`, not a `401`.** A `401` would make the front treat it as an expired session and log the user out.

**D-08 `IAuthTokenSettings` (Application) gives the access and refresh lifetimes.** It is implemented in Infrastructure from `JwtOptions` (`AccessTokenMinutes` = 15, new `RefreshTokenDays` = 14). `AuthTokenIssuer` (Application, scoped) centralizes token issuing for login and refresh. *Alternative:* `IOptions` in Application — rejected so that Application does not depend on the options package.

**D-09 Clock.** `TimeProvider.System` is registered as a singleton in `AddInfrastructure`. `AppDbContext`, `JwtTokenGenerator`, `UserSeeder` and the handlers inject it. Audit timestamps and refresh-token dates become `DateTimeOffset` (`timestamptz`), as the contract requires. Integration tests register a `FakeTimeProvider` initialized to the real "now", so JWT validation (which uses the real clock) still passes; tests that need to move time forward create their own factory.

**D-10 Forwarded headers.**
- Configuration: `ForwardedHeaders:ForwardLimit` (default 2) and `ForwardedHeaders:KnownNetworks` (CIDR list).
- An empty list clears `KnownIPNetworks`/`KnownProxies` (any proxy is trusted) and logs a startup warning. This is acceptable only while the API has no public domain.
- `UseForwardedHeaders()` is the first middleware. Middleware order: ForwardedHeaders → ExceptionHandling → (CORS in Development) → (Swagger) → RateLimiter → Authentication → Authorization.
- `GET /api/_diag/ip` is anonymous. It returns `404` unless `Diag:Enabled=true`, and returns `remoteIpAddress` plus the `X-Forwarded-*` / `X-Original-*` headers.

**D-11 Named rate-limit policies.**
- `RateLimitingOptions` holds the three policies, `Auth` (10 / 5 min), `Reservas` (10 / 10 min) and `Formularios` (5 / 10 min), bound from `RateLimit:{Policy}:{PermitLimit,Window}`.
- Each policy is a fixed window partitioned by `RemoteIpAddress`. The names are constants in `RateLimitPolicies`.
- Rejections return `429` `ProblemDetails` with `Retry-After` (seconds) taken from the lease metadata.
- `GET /api/auth/me` is excluded (`[DisableRateLimiting]`): it requires a valid token, so it is not a brute-force vector, and the front calls it on every load.

**D-12 CORS only in Development.** In production the browser talks to Next, which uses the same origin. Kestrel binds `http://*:{PORT}`: dual-stack IPv6 because Railway's private network is IPv6, with automatic IPv4 fallback on hosts without IPv6 (a literal `[::]` crashes there).

**D-13 MediatR stays at 12.5.0** (the last Apache-2.0 release). No license key is needed. The README warns not to upgrade to ≥13 without a license.

**D-14 New dependency:** `Microsoft.Extensions.TimeProvider.Testing` (test projects only), the official `FakeTimeProvider`. No other packages.

## Endpoints

| Method | Route | Auth | Request | Success | Errors |
| --- | --- | --- | --- | --- | --- |
| POST | `/api/auth/login` | anonymous, `auth` | `{email, password}` | `200 {accessToken, expiresIn, user{id,email,nombre,rol}}` + `Set-Cookie: rt` | `400` validation, `401` generic, `429` |
| POST | `/api/auth/refresh` | cookie `rt`, `auth` | — | `200` same as login, cookie rotated | `401`, `429` |
| POST | `/api/auth/logout` | cookie `rt`, `auth` | — | `204`, cookie deleted | `429` |
| GET | `/api/auth/me` | Bearer | — | `200 user` | `401` |
| POST | `/api/auth/cambiar-password` | Bearer, `auth` | `{passwordActual, passwordNueva}` | `204` | `400` (`errors.passwordActual` / `errors.passwordNueva`), `401`, `429` |
| GET | `/api/health` | anonymous | — | `200 {status}` | — |
| GET | `/api/_diag/ip` | anonymous | — | `200 {remoteIpAddress, headers}` | `404` when disabled |

All errors are `application/problem+json` (RFC 7807); validation errors are `ValidationProblemDetails` with `errors` per field.

## Risks / Trade-offs

- [Trusting every proxy while `KnownNetworks` is empty lets a client spoof `X-Forwarded-For` if `ForwardLimit` is larger than the number of proxies that actually append] → keep the API without a public domain; verify on Railway with `/api/_diag/ip` from two networks and set `ForwardLimit`/`KnownNetworks` to the observed chain; disable the endpoint afterwards.
- [Two browser tabs refreshing at the same time: the second one presents a rotated token and revokes the session] → the front (F09) must serialize refresh calls (single in-flight promise). This is documented as a requirement for F09.
- [10 requests / 5 min on `/api/auth/*` can throttle real users behind a shared NAT] → the limit is configurable (`RateLimit:Auth:*`).
- [`SameSite=Strict` cookie: the first navigation coming from an external site will not carry the cookie] → acceptable: the front calls refresh with `fetch` on the same origin after load.
- [The regenerated migration discards the template's history] → there is no deployed database yet.

## Migration Plan

Nothing is deployed yet: the `InitialCreate` migration is regenerated. Railway (F10) must define the new variables listed in `.env.example` before the first deploy, or the startup fails in Production.

## Open Questions

- The exact Railway private-network ranges (to fill `ForwardedHeaders:KnownNetworks`) — to be resolved with the diagnostic endpoint during F10.
