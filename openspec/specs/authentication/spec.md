# authentication Specification

## Purpose

Describes account registration, credential-based login, JWT access-token issuance, refresh-token rotation and invalidation, global Bearer enforcement, and per-IP rate limiting of the auth endpoints.

## Requirements

### Requirement: User registration

The system SHALL expose `POST /auth/register` accepting `{ email, password, displayName }` and, on success, create a new user and return `201` with `{ accessToken, refreshToken, user: { id, email, displayName } }`. Passwords MUST be stored only as a bcrypt hash, never in plaintext. Email MUST be normalized (trimmed, lower-cased) before the uniqueness check and before persistence. Password MUST be at least 8 characters; this is the only password rule.

#### Scenario: Successful registration returns tokens and user
- **WHEN** `POST /auth/register` is called with a well-formed, not-yet-registered email, a password of 8+ characters, and a display name
- **THEN** the response is `201` with a non-empty `accessToken`, a non-empty `refreshToken`, and `user` echoing the persisted `id`, normalized `email`, and `displayName`
- **AND** a `User` row and a `RefreshToken` row are persisted, with the stored password differing from the submitted plaintext
- **[INTEGRATION]**

#### Scenario: Duplicate email is rejected with 409
- **WHEN** `POST /auth/register` is called with an email that already exists (ignoring case and surrounding whitespace)
- **THEN** the response is `409` in `application/problem+json` and no second user is created
- **[INTEGRATION]**

#### Scenario: Password shorter than 8 characters is rejected
- **WHEN** `POST /auth/register` is called with a password of fewer than 8 characters
- **THEN** the response is `400` `ValidationProblemDetails` with an error entry for the password field and no user is created
- **[UNIT]**

#### Scenario: Malformed email is rejected
- **WHEN** `POST /auth/register` is called with a value that is not a valid email address
- **THEN** the response is `400` `ValidationProblemDetails` with an error entry for the email field
- **[UNIT]**

### Requirement: User login

The system SHALL expose `POST /auth/login` accepting `{ email, password }` and, on valid credentials, return `200` with the same shape as registration (`{ accessToken, refreshToken, user }`). On invalid credentials the system MUST return `401` with a single generic message that does NOT reveal whether the email is unknown or the password is wrong. Email MUST be normalized identically to registration before lookup.

#### Scenario: Successful login returns fresh tokens
- **WHEN** `POST /auth/login` is called with the correct password for an existing email (in any casing)
- **THEN** the response is `200` with a non-empty `accessToken`, a non-empty `refreshToken`, and the matching `user`
- **AND** a new `RefreshToken` row is persisted for that user
- **[INTEGRATION]**

#### Scenario: Wrong password returns generic 401
- **WHEN** `POST /auth/login` is called with an existing email but an incorrect password
- **THEN** the response is `401` in `application/problem+json` with a generic message
- **[INTEGRATION]**

#### Scenario: Unknown email returns the same generic 401
- **WHEN** `POST /auth/login` is called with an email that does not exist
- **THEN** the response is `401` in `application/problem+json` with the same generic message as the wrong-password case
- **[INTEGRATION]**

### Requirement: Refresh token rotation

The system SHALL expose `POST /auth/refresh` accepting `{ refreshToken }` and, when the presented token is active, return `200` with a new `{ accessToken, refreshToken }` pair (no user object). The refresh token issued to the client MUST be an opaque high-entropy secret; only its SHA-256 hash is persisted. On refresh the presented token MUST be revoked (recording the replacement) and a new refresh token issued. An expired, revoked, or unknown token MUST return `401`.

#### Scenario: Valid refresh rotates the token
- **WHEN** `POST /auth/refresh` is called with an active refresh token
- **THEN** the response is `200` with a new `accessToken` and a new `refreshToken` different from the one presented
- **AND** the presented token's stored record is marked revoked with its replacement recorded, and a new `RefreshToken` row exists
- **[INTEGRATION]**

#### Scenario: Reusing an already-rotated token is rejected
- **WHEN** `POST /auth/refresh` is called with a refresh token that was already used to rotate (now revoked)
- **THEN** the response is `401` in `application/problem+json` and no new token is issued
- **[INTEGRATION]**

#### Scenario: Expired refresh token is rejected
- **WHEN** `POST /auth/refresh` is called with a refresh token whose expiry is in the past
- **THEN** the response is `401` in `application/problem+json`
- **[INTEGRATION]**

#### Scenario: Unknown refresh token is rejected
- **WHEN** `POST /auth/refresh` is called with a token that matches no stored hash
- **THEN** the response is `401` in `application/problem+json`
- **[INTEGRATION]**

#### Scenario: RefreshToken.IsActive reflects revocation and expiry
- **WHEN** a `RefreshToken` is revoked, or its `ExpiresAt` is in the past
- **THEN** its `IsActive` is `false`; a token that is neither revoked nor expired reports `IsActive` `true`
- **[UNIT]**

### Requirement: JWT access token issuance

The system SHALL issue HS256-signed JWT access tokens carrying the user's id as the `sub` claim and an expiry of approximately 15 minutes, signed with a configured secret key. The token MUST be consumable by the existing current-user resolution so that the authenticated `UserId` is taken from the token.

#### Scenario: Access token carries the user id as subject
- **WHEN** an access token is issued for a user during registration or login
- **THEN** the token's `sub` claim equals that user's id and its expiry is approximately 15 minutes ahead
- **[UNIT]**

#### Scenario: Authenticated request resolves UserId from the token
- **WHEN** a request is made to a protected endpoint with a valid Bearer access token
- **THEN** the resolved current `UserId` equals the `sub` claim of that token
- **[INTEGRATION]**

### Requirement: Bearer authentication enforced by default

The system SHALL require a valid Bearer access token on every endpoint except those explicitly marked anonymous (`/auth/*` and `GET /health`). A request without a valid token to a protected endpoint MUST return `401` in `application/problem+json`. The authenticated user's identity MUST be derived solely from the token, never from the request body or query.

#### Scenario: Protected endpoint without a token is challenged
- **WHEN** a protected endpoint is called without an `Authorization: Bearer` header
- **THEN** the response is `401` in `application/problem+json`
- **[INTEGRATION]**

#### Scenario: Anonymous endpoints remain reachable without a token
- **WHEN** `GET /health` or any `/auth/*` endpoint is called without a token
- **THEN** the request is not rejected by authentication (no `401` from the auth challenge)
- **[INTEGRATION]**

#### Scenario: Invalid or expired token is rejected
- **WHEN** a protected endpoint is called with a malformed or expired Bearer token
- **THEN** the response is `401` in `application/problem+json`
- **[INTEGRATION]**

### Requirement: Rate limiting of auth endpoints

The system SHALL limit `/auth/*` to 5 requests per minute per client IP address, resolving the real client IP from forwarded headers when running behind a reverse proxy. Requests exceeding the limit MUST return `429` in `application/problem+json`.

#### Scenario: Requests over the limit are throttled
- **WHEN** more than 5 requests within one minute originate from the same client IP against `/auth/*`
- **THEN** the excess requests receive `429` in `application/problem+json`
- **[INTEGRATION]**

#### Scenario: Client IP is taken from forwarded headers behind the proxy
- **WHEN** requests arrive with an `X-Forwarded-For` header set by the proxy
- **THEN** rate-limit partitioning uses the forwarded client IP, so distinct clients are counted separately rather than all as the proxy
- **[INTEGRATION]**
