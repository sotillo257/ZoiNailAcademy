## ADDED Requirements

### Requirement: Real client IP behind the proxies
The system SHALL process `X-Forwarded-For` and `X-Forwarded-Proto` as the first middleware of the pipeline, with `ForwardLimit` taken from `ForwardedHeaders:ForwardLimit` (default `2`) and the trusted proxy networks from `ForwardedHeaders:KnownNetworks` (CIDR list). When the list is empty, every proxy is trusted and a warning is logged at startup. Rate limiting and any other consumer MUST read the client IP from `HttpContext.Connection.RemoteIpAddress` after this middleware.

#### Scenario: Forwarded client IP becomes the remote IP
- **WHEN** a request arrives with `X-Forwarded-For: 198.51.100.7` and the diagnostic endpoint is enabled
- **THEN** `GET /api/_diag/ip` reports `remoteIpAddress` `198.51.100.7`
- **[INTEGRATION]**

### Requirement: Temporary IP diagnostic endpoint
The system SHALL expose `GET /api/_diag/ip` without authentication only when `Diag:Enabled` is `true`, returning `remoteIpAddress` and the `X-Forwarded-*` and `X-Original-*` request headers. When the flag is unset or `false`, the endpoint MUST answer `404`.

#### Scenario: Disabled by default
- **WHEN** `GET /api/_diag/ip` is called with `Diag:Enabled` unset
- **THEN** the response is `404`
- **[INTEGRATION]**

### Requirement: Named per-IP rate-limit policies
The system SHALL register the fixed-window rate-limit policies `auth` (default 10 requests / 5 minutes), `reservas` (10 / 10 minutes) and `formularios` (5 / 10 minutes), partitioned by client IP, with limits configurable through `RateLimit:{Auth|Reservas|Formularios}:{PermitLimit|Window}`. A rejected request MUST receive `429` in `application/problem+json` with a `Retry-After` header in seconds.

#### Scenario: Request N+1 within the window is throttled
- **WHEN** a client IP sends `PermitLimit + 1` requests to an endpoint under a policy within its window
- **THEN** the last response is `429` `application/problem+json` with a `Retry-After` header
- **[INTEGRATION]**

#### Scenario: Different client IPs are counted separately
- **WHEN** `PermitLimit + 1` requests come from distinct forwarded IPs
- **THEN** none of them is throttled
- **[INTEGRATION]**

#### Scenario: Every named policy is registered
- **WHEN** an endpoint uses the `reservas` or `formularios` policy
- **THEN** it is throttled after that policy's configured `PermitLimit`
- **[INTEGRATION]**
