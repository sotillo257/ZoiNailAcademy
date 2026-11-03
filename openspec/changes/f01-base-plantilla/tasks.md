## 1. Remove Notes

- [x] 1.1 Delete the Notes entity, Application folder, controller, configuration, `DbSet`, unit and integration tests and the `notes` spec; keep `dotnet build` and `dotnet test` green

## 2. Domain: clock-agnostic tokens and roles

- [x] 2.1 Test `RefreshToken.IsActiveAt(now)` / `Revoke(now, replacedBy)` with `DateTimeOffset`, then implement; `BaseAuditableEntity` timestamps become `DateTimeOffset`
- [x] 2.2 Test `User.Create(..., role)` exposes the role, then add the `UserRole` enum and the `Role` property

## 3. Application: auth use cases

- [x] 3.1 Add `IAuthTokenSettings` and `AuthTokenIssuer`; test that `LoginHandler` returns `AuthResult` (access token, `expiresIn`, refresh token + expiry, user with role), then implement
- [x] 3.2 Test `LoginValidator`, then implement
- [x] 3.3 Test `RefreshHandler` (rotation, unknown, expired, reuse of a rotated token revokes the family), then implement
- [x] 3.4 Test `LogoutHandler` (revokes the active token, no-op without a token), then implement
- [x] 3.5 Test `GetCurrentUserHandler` (returns the user, `401` if missing), then implement
- [x] 3.6 Test `ChangePasswordValidator` and `ChangePasswordHandler` (wrong current password → `ValidationException`, revokes the other sessions), then implement
- [x] 3.7 Delete `Register` (command, handler, validator, tests)

## 4. Infrastructure

- [x] 4.1 Register `TimeProvider.System`; `AppDbContext`, `JwtTokenGenerator` (`role` claim) and `AuthTokenSettings` use it; `Jwt:RefreshTokenDays`
- [x] 4.2 `UserConfiguration` stores `Role` as text; `RefreshTokenConfiguration` uses `xmin` as concurrency token
- [x] 4.3 `UserSeeder` with `Admin`/`SuperAdmin` options, reset flag and fail-fast in Production
- [x] 4.4 Regenerate the `InitialCreate` migration and check `has-pending-model-changes`

## 5. Host

- [x] 5.1 `ApiRoutePrefixConvention`, `HealthController` at `api/health`, Kestrel on all interfaces (`http://*:{PORT}`)
- [x] 5.2 JWT Bearer with `role`/`sub` claim types, `OnForbidden` ProblemDetails, policies `Backoffice` and `SuperAdmin`
- [x] 5.3 `AuthController`: login, refresh, logout, me and cambiar-password with the `rt` cookie (`Auth:CookieSecure`)
- [x] 5.4 `ForwardedHeaders` options from configuration, first in the pipeline; `DiagController` gated by `Diag:Enabled`
- [x] 5.5 Named rate-limit policies from `RateLimit:*` with `Retry-After`; CORS only in Development; middleware order
- [x] 5.6 Run the seeder after `MigrateAsync`; update `appsettings*.json` and add `.env.example`

## 6. Integration tests (Testcontainers)

- [x] 6.1 Test fixture: seed settings, `FakeTimeProvider`, test admin controller as application part, cookie helpers, test rate-limit endpoints
- [x] 6.2 Auth: login (body, cookie attributes, generic 401), refresh (rotation, reuse detection, no cookie, expiry, concurrency), logout, me, cambiar-password, register 404
- [x] 6.3 Routes: every route under `/api`; `/api/admin/**` fail-closed (metadata, 401, 403)
- [x] 6.4 Seed: no duplicates, password kept, reset flag, Production fail-fast
- [x] 6.5 Network: forwarded IP via the diag endpoint, diag disabled → 404, rate-limit N+1 → 429 with `Retry-After` for all three policies, CORS absent in Production
- [x] 6.6 Full `dotnet test` green

## 7. Docs

- [x] 7.1 Update README, CLAUDE.md and `openspec/config.yaml` for the product (routes, config, MediatR 12.x license note)
- [ ] 7.2 [MANUAL] On Railway, with `Diag__Enabled=true`, check `/api/_diag/ip` from two networks, tune `ForwardedHeaders__ForwardLimit`/`KnownNetworks`, then disable it (F10)
- [ ] 7.3 [MANUAL] Login → refresh → logout from the browser through the Next rewrite (F09/F10)
