# ZoiNailAcademy backend

API REST en .NET 10 con Clean Architecture, autenticación JWT y PostgreSQL.

## Stack

- **.NET 10 / ASP.NET Core** con controllers
- **Clean Architecture** en 4 capas: `Domain` → `Application` → `Infrastructure` → `Host`
- **MediatR** (CQRS) + **FluentValidation** en el pipeline
- **EF Core 10 + Npgsql** (PostgreSQL), migraciones aplicadas al arrancar
- **Autenticación**: registro/login/refresh con JWT (HS256), refresh tokens rotados y hasheados con SHA-256, contraseñas con BCrypt, rate limiting por IP en `/auth/*`
- **Errores** como `ProblemDetails` (RFC 7807)
- **Tests**: xUnit + FluentAssertions + Moq (unitarios) y `WebApplicationFactory` + Testcontainers PostgreSQL (integración)
- **Despliegue**: Dockerfile multi-stage para Railway, CI con GitHub Actions

## Requisitos

- .NET 10 SDK
- Docker (para los tests de integración y para levantar PostgreSQL en local)

## Puesta en marcha

```bash
docker run -d --name zoinailacademy-postgres -e POSTGRES_PASSWORD=postgres -p 5432:5432 postgres:17-alpine
dotnet run --project src/ZoiNailAcademy.Host
```

Con el entorno `Development`, Swagger UI queda disponible en `/swagger`.

## Tests

```bash
dotnet test tests/UnitTests          # unitarios
dotnet test tests/IntegrationTests   # integración (requiere Docker)
```

## Endpoints incluidos

| Método | Ruta | Auth | Descripción |
| --- | --- | --- | --- |
| GET | `/health` | No | Health check |
| POST | `/auth/register` | No | Registro, devuelve tokens y usuario |
| POST | `/auth/login` | No | Login, devuelve tokens y usuario |
| POST | `/auth/refresh` | No | Rota el refresh token |
| GET | `/notes?page=&pageSize=` | Sí | Lista paginada de notas del usuario (ejemplo) |
| GET | `/notes/{id}` | Sí | Detalle de una nota (ejemplo) |
| POST | `/notes` | Sí | Crea una nota (ejemplo) |
| PUT | `/notes/{id}` | Sí | Edita una nota (ejemplo) |
| DELETE | `/notes/{id}` | Sí | Borra una nota (ejemplo) |

`Notes` es una feature de ejemplo para mostrar el patrón completo; bórrala cuando añadas las features reales (ver más abajo).

## Configuración

| Clave / variable | Descripción |
| --- | --- |
| `ConnectionStrings__DefaultConnection` o `DATABASE_URL` | Cadena de conexión de PostgreSQL (`DATABASE_URL` en formato URI, como la da Railway) |
| `PORT` | Puerto HTTP (por defecto 8080) |
| `Jwt__SigningKey` | **Secreto**: clave de firma HS256 (mínimo 32 caracteres). Nunca se sube al repo |
| `Jwt__Issuer`, `Jwt__Audience`, `Jwt__AccessTokenMinutes` | Parámetros del access token |
| `Cors__AllowedOrigins__0`, `__1`… | Orígenes permitidos del frontend |
| `Swagger__EnableInProduction` | `true` para exponer Swagger temporalmente en producción |

## Eliminar la feature de ejemplo `Notes`

1. Borra `src/ZoiNailAcademy.Domain/Entities/Note.cs`, `src/ZoiNailAcademy.Application/Notes/`, `src/ZoiNailAcademy.Host/Controllers/NotesController.cs` y `src/ZoiNailAcademy.Infrastructure/Data/Configurations/NoteConfiguration.cs`.
2. Quita el `DbSet<Note>` de `IAppDbContext` y `AppDbContext`.
3. Borra los tests de `Notes` (`tests/UnitTests/Domain/Entities/NoteTests.cs`, `tests/UnitTests/Application/Notes/`, `tests/IntegrationTests/Controllers/Note*`).
4. Borra `src/ZoiNailAcademy.Infrastructure/Data/Migrations/` y regenera la migración inicial: `dotnet ef migrations add InitialCreate --project src/ZoiNailAcademy.Infrastructure`.
