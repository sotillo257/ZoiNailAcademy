# Zoi Nail Academy — API

API REST de la web de venta de cursos de Zoi Nail Academy (.NET 10, Clean Architecture, PostgreSQL). El producto está definido en [`Docs/`](Docs/README.md): PRD, contrato de la API (`Docs/00-contrato-y-convenciones.md`, que manda sobre las specs) y una spec por feature.

## Stack

- **.NET 10 / ASP.NET Core** con controllers; todas las rutas bajo `/api` (convención global)
- **Clean Architecture** en 4 capas: `Domain` → `Application` → `Infrastructure` → `Host`
- **MediatR 12.x** (CQRS) + **FluentValidation** en el pipeline
- **EF Core 10 + Npgsql** (PostgreSQL), migraciones y seed de usuarios al arrancar
- **Autenticación**: backoffice cerrado (sin registro) con usuarios `Admin` y `SuperAdmin` sembrados desde variables de entorno; access token JWT (HS256, 15 min) en el body y refresh token rotado en cookie `HttpOnly` (`rt`, 14 días) con detección de reutilización
- **Red**: `ForwardedHeaders` para la IP real tras el edge de Railway y el rewrite de Next; rate limiting por IP con políticas `auth`, `reservas` y `formularios`
- **Errores** como `ProblemDetails` (RFC 7807)
- **Tests**: xUnit + FluentAssertions + Moq (unitarios) y `WebApplicationFactory` + Testcontainers PostgreSQL + `FakeTimeProvider` (integración)
- **Despliegue**: Dockerfile multi-stage para Railway, CI con GitHub Actions

## Requisitos

- .NET 10 SDK
- Docker (para los tests de integración y para levantar PostgreSQL en local)

## Puesta en marcha

```bash
docker run -d --name zoinailacademy-postgres -e POSTGRES_PASSWORD=postgres -p 5432:5432 postgres:17-alpine
dotnet run --project src/ZoiNailAcademy.Host
```

En `Development` se siembran `admin@local.test` / `admin-dev-password` y `superadmin@local.test` / `superadmin-dev-password` (ver `appsettings.Development.json`), la cookie `rt` va sin `Secure` (http local) y Swagger UI queda en `/swagger`.

## Tests

```bash
dotnet test tests/UnitTests          # unitarios
dotnet test tests/IntegrationTests   # integración (requiere Docker)
```

## Endpoints (F01)

| Método | Ruta | Auth | Descripción |
| --- | --- | --- | --- |
| GET | `/api/health` | No | Health check |
| POST | `/api/auth/login` | No | `{email, password}` → `{accessToken, expiresIn, user}` + cookie `rt` |
| POST | `/api/auth/refresh` | Cookie `rt` | Rota la cookie y devuelve lo mismo que login |
| POST | `/api/auth/logout` | Cookie `rt` | 204, revoca y borra la cookie |
| GET | `/api/auth/me` | Bearer | Usuario actual `{id, email, nombre, rol}` |
| POST | `/api/auth/cambiar-password` | Bearer | `{passwordActual, passwordNueva}` → 204; cierra las demás sesiones |
| GET | `/api/_diag/ip` | No | Solo con `Diag__Enabled=true`: IP vista por la API y cabeceras `X-Forwarded-*` |

`/api/auth/register` no existe. Los controllers de `/api/admin/**` llevan `[Authorize(Policy = AuthorizationPolicies.Backoffice)]` a nivel de clase (y `SuperAdmin` en las acciones del desarrollador); un test de integración recorre esas rutas dinámicamente y falla si alguna queda abierta.

## Configuración

Lista completa con ejemplos en [`.env.example`](.env.example). Toda variable nueva se declara también en `appsettings.json` (sin secretos) y en `.env.example`.

| Clave / variable | Descripción |
| --- | --- |
| `ConnectionStrings__DefaultConnection` o `DATABASE_URL` | Cadena de conexión de PostgreSQL (`DATABASE_URL` en formato URI, como la da Railway) |
| `PORT` | Puerto HTTP (por defecto 8080; escucha en todas las interfaces, IPv6 incluida, para la red privada de Railway) |
| `Jwt__SigningKey` | **Secreto**: clave de firma HS256 (mínimo 32 caracteres) |
| `Jwt__Issuer`, `Jwt__Audience`, `Jwt__AccessTokenMinutes`, `Jwt__RefreshTokenDays` | Parámetros de los tokens (15 min / 14 días) |
| `Auth__CookieSecure` | Atributo `Secure` de la cookie `rt` (`true` salvo en local por http) |
| `Admin__SeedEmail`, `Admin__SeedPassword`, `Admin__SeedNombre` | Usuario de la clienta (obligatorio en Production; contraseña de 10+ caracteres) |
| `SuperAdmin__SeedEmail`, `SuperAdmin__SeedPassword`, `SuperAdmin__SeedNombre` | Usuario del desarrollador (obligatorio en Production) |
| `SuperAdmin__ResetAdminPassword` | `true` resetea la contraseña del Admin a `Admin__SeedPassword` al arrancar (y cierra sus sesiones). Volver a `false` después |
| `RateLimit__{Auth,Reservas,Formularios}__PermitLimit` / `__Window` | Límites por IP (por defecto 10/5 min, 10/10 min, 5/10 min) |
| `ForwardedHeaders__ForwardLimit`, `ForwardedHeaders__KnownNetworks__N` | Proxies de confianza (ver abajo) |
| `Diag__Enabled` | Activa temporalmente `/api/_diag/ip` |
| `Swagger__EnableInProduction` | `true` para exponer Swagger temporalmente en producción |

CORS solo se configura en `Development` (`Cors:AllowedOrigins`): en producción el navegador habla con Next y Next reenvía `/api/*` por la red privada, así que todo es mismo origen.

### Usuarios y contraseñas

No hay registro ni recuperación por email. Si la clienta olvida su contraseña: poner `SuperAdmin__ResetAdminPassword=true` y un `Admin__SeedPassword` nuevo en Railway, redesplegar, comunicarle la contraseña y volver a poner el flag a `false`. Desde el backoffice puede cambiarla con `cambiar-password`.

### IP real detrás de los proxies

Cadena en producción: navegador → edge de Railway → Next.js → red privada → API. `ForwardLimit` debe coincidir con el número de proxies que añaden entradas a `X-Forwarded-For`: si es mayor y `KnownNetworks` está vacío, un cliente puede falsear su IP (y saltarse el rate limiting). Con `KnownNetworks` vacío se confía en cualquier proxy, lo que solo es aceptable mientras la API no tenga dominio público. Para comprobarlo en Railway: `Diag__Enabled=true`, abrir `/api/_diag/ip` desde dos redes distintas (deben salir dos IPs públicas distintas), ajustar las variables y desactivar el endpoint.

### Front (F08/F09)

- Guardar el access token **solo en memoria** y llamar a `/api/auth/refresh` al cargar y antes de que caduque (`expiresIn`), con `credentials: 'include'`.
- Serializar las llamadas a refresh (una sola promesa en vuelo): reutilizar un refresh token ya rotado se trata como robo y cierra todas las sesiones del usuario.

## MediatR

Se usa MediatR **12.5.0**, la última versión con licencia Apache 2.0, así que no hace falta `MediatR__LicenseKey`. **No subir a 13 o superior** sin registrar antes la licencia (Community es gratuita por debajo de 5 M$ de facturación anual).

## Historial

La feature de ejemplo `Notes` de la plantilla se eliminó en F01; quedó en el primer commit ("Genera la API desde la plantilla backend") como referencia del patrón completo (entidad → command/query + handler + validator → controller → tests).
