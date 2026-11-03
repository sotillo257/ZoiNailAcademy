## Why

The API was generated from the backend template, which is built for an open multi-user product: public self-registration, refresh tokens in the JSON body, routes at the root and an example `Notes` feature. Zoi Nail Academy needs a closed backoffice for two seeded users (the client, `Admin`, and the developer, `SuperAdmin`) that sits behind two proxies (Railway edge → Next.js rewrite). Every later feature (F02–F11) depends on this base, so it has to come first (see `Docs/features/F01-base-plantilla.md` and `Docs/00-contrato-y-convenciones.md`).

## What Changes

- **BREAKING** Remove the `Notes` example feature (code, tests, table, spec) and regenerate the `InitialCreate` migration.
- **BREAKING** Every controller route gets the `/api` prefix through a global convention; `/health` → `/api/health`.
- **BREAKING** Remove `POST /auth/register` along with its command, handler and validator.
- Add a `Role` (`Admin | SuperAdmin`, stored as text) to `User`, emit it as the `role` claim, and add the authorization policies `Backoffice` and `SuperAdmin`.
- Seed the `Admin` and `SuperAdmin` users from configuration at startup without overwriting existing passwords; add the explicit `SuperAdmin:ResetAdminPassword` escape hatch; startup fails in Production if the seed variables are missing.
- **BREAKING** The refresh token travels only in an `HttpOnly` cookie `rt` (`Secure`, `SameSite=Strict`, `Path=/api/auth`). Login and refresh return `{accessToken, expiresIn, user{id,email,nombre,rol}}`. Reusing a rotated refresh token revokes all of the user's sessions.
- New endpoints `POST /api/auth/logout`, `GET /api/auth/me` and `POST /api/auth/cambiar-password`.
- Configurable `ForwardedHeaders` (`XForwardedFor | XForwardedProto`, `ForwardLimit` 2) as the first middleware, plus a temporary diagnostic endpoint `GET /api/_diag/ip` gated by `Diag:Enabled`.
- Rate limiting by named policies (`auth`, `reservas`, `formularios`) with configurable limits, `429` `ProblemDetails` and a `Retry-After` header.
- Register `TimeProvider` in DI and use it for every timestamp (audit fields, refresh-token expiry, JWT expiry). Timestamps become `DateTimeOffset` (`timestamptz`).
- CORS only in Development. Kestrel listens on all interfaces including IPv6 (`http://*:{PORT}`) so the API is reachable over Railway's IPv6 private network.
- MediatR stays on 12.x (Apache 2.0, no license key); this is documented.

## Capabilities

### New Capabilities
- `user-seeding`: creation of the backoffice users from configuration at startup and the Admin password reset mechanism.
- `network-edge`: real client IP behind the proxies (`ForwardedHeaders`), the IP diagnostic endpoint, and the reusable named rate-limiting policies.

### Modified Capabilities
- `authentication`: registration removed; roles and authorization policies; refresh token in cookie with reuse detection; logout, me and change-password endpoints; auth rate limit changed to 10 / 5 min and moved to a named policy.
- `project-skeleton`: `/api` prefix on every route, `/api/health`, CORS only in Development.
- `shared-kernel`: audit timestamps use `DateTimeOffset` from the injected `TimeProvider`.
- `notes`: removed entirely.

## Non-goals

- Password recovery by email (the SuperAdmin resets the Admin password through configuration).
- User management from the UI or the API.
- Any business entity (courses, editions, bookings) — those start in F02.
- Railway, DNS or CI setup beyond what the code needs (F10). The real-IP verification on Railway is a manual task.

## Impact

- **Domain**: `User` (+`Role`), `RefreshToken` (`DateTimeOffset`, clock passed in), `BaseAuditableEntity` (`DateTimeOffset`), new `UserRole` enum, `Note` removed.
- **Application**: `Authentication/*` rewritten (Login, Refresh, Logout, GetCurrentUser, ChangePassword), `Register` and `Notes` removed, new `IAuthTokenSettings`.
- **Infrastructure**: `UserSeeder`, `AuthTokenSettings`, `TimeProvider` in `AppDbContext` and `JwtTokenGenerator`, xmin concurrency token on `RefreshToken`, regenerated migration.
- **Host**: route prefix convention, cookie handling, policies, rate-limit options, forwarded-headers options, diag controller, middleware order.
- **Tests**: new `Microsoft.Extensions.TimeProvider.Testing` package (for `FakeTimeProvider`) in both test projects.
- **Config**: new keys `Admin:*`, `SuperAdmin:*`, `Jwt:RefreshTokenDays`, `Auth:CookieSecure`, `RateLimit:*`, `ForwardedHeaders:*`, `Diag:Enabled`; new `.env.example`.
- **Front (F08/F09)**: must call `/api/auth/refresh` with `credentials: 'include'` and keep the access token in memory.
