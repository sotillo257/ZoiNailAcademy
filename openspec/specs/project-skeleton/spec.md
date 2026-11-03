# project-skeleton Specification

## Purpose

Describe la estructura base de la solución backend (proyectos, configuración de build, arranque del Host, health check y empaquetado para despliegue).

## Requirements

### Requirement: Estructura de solución de 4 capas
El sistema SHALL organizarse como una solución .NET 10 (`ZoiNailAcademy.slnx`) con cuatro proyectos que sigan el flujo de dependencias de Clean Architecture: `ZoiNailAcademy.Host` → `ZoiNailAcademy.Application` → `ZoiNailAcademy.Domain`, y `ZoiNailAcademy.Infrastructure` → `ZoiNailAcademy.Application`/`ZoiNailAcademy.Domain`. `ZoiNailAcademy.Domain` MUST NOT tener dependencias sobre las otras capas.

#### Scenario: La solución compila
- **WHEN** se ejecuta `dotnet build` en la raíz del repositorio
- **THEN** los cuatro proyectos de `src/` y los dos de `tests/` compilan sin errores

#### Scenario: Domain no referencia otras capas
- **WHEN** se inspeccionan las referencias de proyecto de `ZoiNailAcademy.Domain`
- **THEN** no existe referencia a `Application`, `Infrastructure` ni `Host`

#### Scenario: Application no referencia Infrastructure
- **WHEN** se inspeccionan las referencias de `ZoiNailAcademy.Application`
- **THEN** no existe referencia a `ZoiNailAcademy.Infrastructure` ni a `ZoiNailAcademy.Host`

### Requirement: Configuración de build centralizada
El sistema SHALL centralizar el target framework y las opciones del compilador en un `Directory.Build.props` en la raíz que fije `net10.0`, `Nullable` habilitado e `ImplicitUsings` habilitado, de modo que ningún `.csproj` individual redefina esas propiedades.

#### Scenario: Todos los proyectos heredan la configuración
- **WHEN** se compila cualquier proyecto de la solución
- **THEN** usa `net10.0` con nullable e implicit usings habilitados sin declararlo en su propio `.csproj`

### Requirement: Arranque del Host
The `ZoiNailAcademy.Host` SHALL configure the ASP.NET Core pipeline in `Program.cs`: controllers, Swagger/OpenAPI, a CORS policy that allows frontend origins (read from `Cors:AllowedOrigins`), an exception-handling middleware that translates Application exceptions into HTTP responses, and DI registration for the Application and Infrastructure layers. Swagger UI and the OpenAPI document SHALL be mapped when the host environment is Development, OR when the configuration flag `Swagger:EnableInProduction` is set to `true`; in either case, those endpoints SHALL be reachable without authentication (bypassing the global authorization fallback policy). When neither condition holds, Swagger UI and the OpenAPI document MUST NOT be mapped.

#### Scenario: La API arranca
- **WHEN** se ejecuta `dotnet run --project src/ZoiNailAcademy.Host`
- **THEN** la aplicación arranca y expone la UI de Swagger en desarrollo
- **[INTEGRATION]**

#### Scenario: CORS permite el origen del frontend
- **WHEN** el frontend en un origen configurado hace una petición a la API
- **THEN** la respuesta incluye las cabeceras CORS que autorizan ese origen
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
- **THEN** the returned document declares an HTTP bearer security scheme named `Bearer` under `components.securitySchemes`, and every operation references it as a security requirement, so Swagger UI renders an "Authorize" button that lets a developer supply an access token and call protected endpoints from the UI
- **[INTEGRATION]**

### Requirement: Endpoint de health check
El sistema SHALL exponer `GET /health` que responda HTTP 200 sin requerir autenticación, para las comprobaciones de salud de Railway y el ping anti-cold-start del frontend.

#### Scenario: Health responde sin autenticación
- **WHEN** se hace `GET /health` sin token
- **THEN** la respuesta es HTTP 200

### Requirement: Empaquetado para Railway
El sistema SHALL incluir un `Dockerfile` multi-stage que construya y publique `ZoiNailAcademy.Host`, más un `.dockerignore`, de forma que la imagen resultante arranque la API y lea la cadena de conexión de PostgreSQL y el resto de configuración desde variables de entorno.

#### Scenario: La imagen se construye
- **WHEN** se ejecuta `docker build` en la raíz del repositorio
- **THEN** la imagen se construye correctamente y su entrypoint lanza `ZoiNailAcademy.Host`
