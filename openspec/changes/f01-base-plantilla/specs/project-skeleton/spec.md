## MODIFIED Requirements

### Requirement: Arranque del Host
The `ZoiNailAcademy.Host` SHALL configure the ASP.NET Core pipeline in `Program.cs`: controllers, Swagger/OpenAPI, an exception-handling middleware that translates Application exceptions into HTTP responses, and DI registration for the Application and Infrastructure layers. Kestrel SHALL listen on all interfaces (`http://*:{PORT}`, default port 8080), dual-stack IPv6 when available and IPv4 otherwise. Every controller route SHALL be served under the `/api` prefix through a global convention, so no controller declares the prefix itself. A CORS policy allowing the origins in `Cors:AllowedOrigins` SHALL be applied only in the Development environment; in any other environment no CORS headers are emitted (production traffic is same-origin through the Next.js rewrite). Swagger UI and the OpenAPI document SHALL be mapped when the host environment is Development, OR when the configuration flag `Swagger:EnableInProduction` is set to `true`; in either case, those endpoints SHALL be reachable without authentication (bypassing the global authorization fallback policy). When neither condition holds, Swagger UI and the OpenAPI document MUST NOT be mapped.

#### Scenario: La API arranca
- **WHEN** se ejecuta `dotnet run --project src/ZoiNailAcademy.Host`
- **THEN** la aplicación arranca y expone la UI de Swagger en desarrollo
- **[INTEGRATION]**

#### Scenario: Every route lives under /api
- **WHEN** the registered endpoints are enumerated from `EndpointDataSource`
- **THEN** every route pattern starts with `api/`, except the OpenAPI document route
- **[INTEGRATION]**

#### Scenario: CORS permite el origen del frontend en Development
- **WHEN** the host runs in Development and a configured frontend origin sends a request
- **THEN** the response includes the CORS headers authorizing that origin
- **[INTEGRATION]**

#### Scenario: No CORS outside Development
- **WHEN** the host runs in Production and a request carries an `Origin` header
- **THEN** the response has no `Access-Control-Allow-Origin` header
- **[INTEGRATION]**

#### Scenario: El middleware traduce excepciones
- **WHEN** un handler lanza una `NotFoundException`
- **THEN** el middleware responde con HTTP 404 y un cuerpo `ProblemDetails` (RFC 7807) sin filtrar el stack trace
- **[INTEGRATION]**

#### Scenario: Swagger disabled in Production by default
- **WHEN** the host runs with a non-Development environment and the `Swagger:EnableInProduction` configuration value is unset or `false`
- **THEN** `GET /openapi/v1.json` and `GET /swagger` are not mapped, and requests to them return `401 Unauthorized` via the global authorization fallback policy, exactly as any other unmatched route does
- **[INTEGRATION]**

#### Scenario: Swagger enabled in Production via configuration flag
- **WHEN** the host runs with a non-Development environment and the `Swagger:EnableInProduction` configuration value is `true`
- **THEN** `GET /openapi/v1.json` returns the OpenAPI document with HTTP 200 without an `Authorization` header, and `GET /swagger` serves the Swagger UI with HTTP 200 without an `Authorization` header
- **[INTEGRATION]**

#### Scenario: OpenAPI document exposes a bearer token security scheme
- **WHEN** `GET /openapi/v1.json` is requested while Swagger is mapped (Development, or Production with the flag enabled)
- **THEN** the returned document declares an HTTP bearer security scheme named `Bearer` under `components.securitySchemes`, and every operation references it as a security requirement
- **[INTEGRATION]**

### Requirement: Endpoint de health check
El sistema SHALL exponer `GET /api/health` que responda HTTP 200 sin requerir autenticación, para las comprobaciones de salud de Railway y el ping anti-cold-start del frontend. `GET /health` (sin prefijo) MUST NOT existir.

#### Scenario: Health responde sin autenticación
- **WHEN** se hace `GET /api/health` sin token
- **THEN** la respuesta es HTTP 200
- **[INTEGRATION]**
