## REMOVED Requirements

### Requirement: User registration
**Reason**: The backoffice is closed. Users are created only by the seed (see `user-seeding`). The endpoint, command, handler and validator are deleted, not just hidden.
**Migration**: None. `POST /api/auth/register` answers `404` to an authenticated caller and `401` to an anonymous one (global fallback policy on unmatched routes).

### Requirement: Rate limiting of auth endpoints
**Reason**: Replaced by the named `auth` policy (10 / 5 min, configurable) described in `network-edge` and in "Auth endpoints are rate limited" below.
**Migration**: Configure `RateLimit:Auth:PermitLimit` / `RateLimit:Auth:Window`.

## MODIFIED Requirements

### Requirement: User login
The system SHALL expose `POST /api/auth/login` accepting `{ email, password }`. On valid credentials it SHALL return `200` with `{ accessToken, expiresIn, user: { id, email, nombre, rol } }`, where `expiresIn` is the access-token lifetime in seconds and `rol` is `Admin` or `SuperAdmin`, and SHALL set the refresh token in the `rt` cookie (see "Refresh token cookie"). The response body MUST NOT contain the refresh token. On invalid credentials the system MUST return `401` with a single generic message that does NOT reveal whether the email is unknown or the password is wrong. The email MUST be normalized (trimmed, lower-cased) before lookup. Both email and password are required.

#### Scenario: Successful login returns token, user and cookie
- **WHEN** `POST /api/auth/login` is called with the correct password for an existing email (in any casing)
- **THEN** the response is `200` with a non-empty `accessToken`, `expiresIn` equal to `Jwt:AccessTokenMinutes × 60`, and the matching `user` including `nombre` and `rol`
- **AND** the response has a `Set-Cookie` header for `rt` with `HttpOnly`, `Secure`, `SameSite=Strict` and `Path=/api/auth`, and the body has no `refreshToken`
- **[INTEGRATION]**

#### Scenario: Wrong password and unknown email return the same 401
- **WHEN** `POST /api/auth/login` is called with a wrong password, or with an unknown email
- **THEN** both responses are `401` `application/problem+json` with the same `detail`
- **[INTEGRATION]**

#### Scenario: Missing fields are rejected
- **WHEN** the login command has an empty email or an empty password
- **THEN** validation fails with an error on that field
- **[UNIT]**

### Requirement: Refresh token rotation
The system SHALL expose `POST /api/auth/refresh`, which reads the refresh token only from the `rt` cookie (never from the body). When the token is active it SHALL return `200` with the same body as login, revoke the presented token (recording its replacement), issue a new token and rewrite the cookie. Only the SHA-256 hash of a refresh token is persisted. A missing, unknown, expired or revoked token MUST return `401`. Presenting a token that was already rotated MUST also revoke every active refresh token of that user (theft detection). Two concurrent rotations of the same token MUST NOT both succeed.

#### Scenario: Valid refresh rotates the cookie
- **WHEN** `POST /api/auth/refresh` is called with a valid `rt` cookie
- **THEN** the response is `200` with a new `accessToken` and a `Set-Cookie: rt` whose value differs from the presented one
- **[INTEGRATION]**

#### Scenario: Reusing a rotated token revokes the whole family
- **WHEN** a client refreshes with cookie A (getting cookie B) and then refreshes again with cookie A
- **THEN** the second call returns `401`, and a subsequent refresh with cookie B also returns `401`
- **[INTEGRATION]**

#### Scenario: Refresh without cookie is rejected
- **WHEN** `POST /api/auth/refresh` is called without the `rt` cookie
- **THEN** the response is `401` in `application/problem+json`
- **[INTEGRATION]**

#### Scenario: Expired refresh token is rejected
- **WHEN** the refresh lifetime (`Jwt:RefreshTokenDays`) has elapsed according to the `TimeProvider`
- **THEN** `POST /api/auth/refresh` with that cookie returns `401`
- **[INTEGRATION]**

#### Scenario: Concurrent refreshes with the same token
- **WHEN** two refresh requests with the same cookie are sent at the same time
- **THEN** exactly one returns `200` and the other returns `401`
- **[INTEGRATION]**

#### Scenario: RefreshToken activity reflects revocation and expiry
- **WHEN** a `RefreshToken` is revoked, or `now` is past its `ExpiresAt`
- **THEN** `IsActiveAt(now)` is `false`; otherwise it is `true`
- **[UNIT]**

### Requirement: JWT access token issuance
The system SHALL issue HS256-signed JWT access tokens carrying the user's id as the `sub` claim, the user's role as the `role` claim, and an expiry of `Jwt:AccessTokenMinutes` (default 15) computed from the `TimeProvider`. The authenticated `UserId` MUST be taken from the token.

#### Scenario: Access token carries id and role
- **WHEN** an access token is issued for a user
- **THEN** its `sub` claim equals the user id, its `role` claim equals the user's role, and its expiry is `AccessTokenMinutes` after issuing
- **[INTEGRATION]**

#### Scenario: Authenticated request resolves UserId from the token
- **WHEN** a request is made to a protected endpoint with a valid Bearer access token
- **THEN** the resolved current `UserId` equals the `sub` claim of that token
- **[INTEGRATION]**

### Requirement: Bearer authentication enforced by default
The system SHALL require a valid Bearer access token on every endpoint except those explicitly marked anonymous (`/api/auth/login`, `/api/auth/refresh`, `/api/auth/logout`, `GET /api/health`, `GET /api/_diag/ip`). A request without a valid token to a protected endpoint MUST return `401` in `application/problem+json`. The authenticated user's identity MUST be derived solely from the token.

#### Scenario: Protected endpoint without a token is challenged
- **WHEN** a protected endpoint is called without an `Authorization: Bearer` header
- **THEN** the response is `401` in `application/problem+json`
- **[INTEGRATION]**

#### Scenario: Invalid token is rejected
- **WHEN** a protected endpoint is called with a malformed Bearer token
- **THEN** the response is `401` in `application/problem+json`
- **[INTEGRATION]**

#### Scenario: Register no longer exists
- **WHEN** `POST /api/auth/register` is called with a valid Bearer token
- **THEN** the response is `404`, and no registered endpoint has the route `api/auth/register`
- **[INTEGRATION]**

## ADDED Requirements

### Requirement: Roles and authorization policies
Every user SHALL have a role, `Admin` or `SuperAdmin`, persisted as text. The system SHALL define the policy `Backoffice` (role `Admin` or `SuperAdmin`) and the policy `SuperAdmin` (role `SuperAdmin`). Every endpoint under `/api/admin` MUST require `Backoffice` or `SuperAdmin` and MUST NOT allow anonymous access. A user without the required role MUST receive `403` in `application/problem+json`.

#### Scenario: Every admin route requires a backoffice policy
- **WHEN** the endpoints whose route starts with `api/admin` are enumerated dynamically
- **THEN** none allows anonymous access and each requires the `Backoffice` or `SuperAdmin` policy
- **[INTEGRATION]**

#### Scenario: Admin routes fail closed without a token
- **WHEN** each endpoint under `/api/admin` is called without a token
- **THEN** every response is `401`
- **[INTEGRATION]**

#### Scenario: Admin token on a SuperAdmin route is forbidden
- **WHEN** each endpoint requiring the `SuperAdmin` policy is called with an `Admin` token
- **THEN** every response is `403` `application/problem+json`, while a `SuperAdmin` token is allowed
- **[INTEGRATION]**

### Requirement: Refresh token cookie
The refresh token SHALL be sent only as the cookie `rt` with `HttpOnly`, `Secure` (configurable through `Auth:CookieSecure`, default `true`), `SameSite=Strict`, `Path=/api/auth` and `Expires` equal to the refresh token expiry (`Jwt:RefreshTokenDays`, default 14).

#### Scenario: Cookie attributes
- **WHEN** login or refresh succeeds
- **THEN** the `Set-Cookie` header for `rt` contains `httponly`, `secure`, `samesite=strict`, `path=/api/auth` and an `expires` about `RefreshTokenDays` ahead
- **[INTEGRATION]**

### Requirement: Logout
The system SHALL expose `POST /api/auth/logout` (anonymous). It revokes the refresh token in the `rt` cookie when present and active, deletes the cookie, and returns `204`. Without a cookie it still returns `204`.

#### Scenario: Logout invalidates the cookie
- **WHEN** a client logs out with its `rt` cookie and then calls refresh with that same cookie
- **THEN** logout returns `204` with a `Set-Cookie` that expires `rt`, and the refresh returns `401`
- **[INTEGRATION]**

### Requirement: Current user
The system SHALL expose `GET /api/auth/me` (Bearer) returning `{ id, email, nombre, rol }` of the token's user. If the user no longer exists, it returns `401`.

#### Scenario: Me returns the token's user
- **WHEN** `GET /api/auth/me` is called with a valid token
- **THEN** the response is `200` with that user's `id`, `email`, `nombre` and `rol`
- **[INTEGRATION]**

### Requirement: Change password
The system SHALL expose `POST /api/auth/cambiar-password` (Bearer) accepting `{ passwordActual, passwordNueva }`. `passwordActual` is required and `passwordNueva` MUST have at least 10 characters. A wrong current password MUST return `400` with an error on `passwordActual`. On success it stores the new hash, revokes every active refresh token of the user except the one in the request's `rt` cookie, and returns `204`.

#### Scenario: Password change keeps only the current session
- **WHEN** a user with two sessions (cookies A and B) changes the password sending cookie A
- **THEN** the response is `204`, the new password logs in, the old one returns `401`, refresh with A still works, and refresh with B returns `401`
- **[INTEGRATION]**

#### Scenario: Wrong current password
- **WHEN** `passwordActual` does not match
- **THEN** the response is `400` `ValidationProblemDetails` with an error on `passwordActual` and the password is unchanged
- **[INTEGRATION]**

#### Scenario: New password too short
- **WHEN** `passwordNueva` has fewer than 10 characters, or `passwordActual` is empty
- **THEN** validation fails with an error on that field
- **[UNIT]**

### Requirement: Auth endpoints are rate limited
`/api/auth/login`, `/api/auth/refresh`, `/api/auth/logout` and `/api/auth/cambiar-password` SHALL use the `auth` rate-limit policy. `GET /api/auth/me` is exempt because it requires a valid token.

#### Scenario: Login N+1 is throttled
- **WHEN** more than `RateLimit:Auth:PermitLimit` login requests come from the same client IP within the window
- **THEN** the excess request receives `429` with a `Retry-After` header
- **[INTEGRATION]**
