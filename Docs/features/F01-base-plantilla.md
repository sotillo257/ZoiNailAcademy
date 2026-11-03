# F01 — Base de la plantilla

**Revisión línea a línea:** Sí (auth y red).
**Depende de:** nada. Es la primera feature y bloquea todas las demás.

## Objetivo

Dejar la plantilla lista para producción en este proyecto: sin la feature de ejemplo, con la API bajo `/api`, registro cerrado, roles, usuarios sembrados, refresh token en cookie y la IP real del cliente detrás de los proxies de Railway y Next.

## Alcance

**Incluye**
1. Eliminar la feature Notes (pasos del README de la plantilla) y regenerar la migración `InitialCreate`.
2. Prefijo `/api` en todos los controllers.
3. Deshabilitar `/auth/register`.
4. Roles `Admin` y `SuperAdmin` en el usuario y en el JWT.
5. Seed de usuarios desde variables de entorno.
6. Refresh token en cookie `HttpOnly`; nuevos endpoints `logout`, `me` y `cambiar-password`.
7. `ForwardedHeaders` para la IP real.
8. Rate limiting por política (auth y formularios públicos), reutilizable por las demás features.
9. `TimeProvider` registrado en DI.
10. Revisar licencia de MediatR.
11. CORS solo en Development; Swagger solo en Development (salvo `Swagger__EnableInProduction`).

**No incluye**
- Recuperación de contraseña por email (fuera de v1; si la clienta olvida su contraseña, el SuperAdmin la resetea, ver más abajo).
- Gestión de usuarios desde la interfaz.

## Cambios en detalle

### 1. Quitar Notes
Seguir la sección "Eliminar la feature de ejemplo Notes" del README de la plantilla. Mantener la estructura de carpetas vacía de Application para que las siguientes features sigan el mismo patrón. Los tests de Notes se pueden conservar en una rama/commit como referencia de patrón para la IA antes de borrarlos.

### 2. Prefijo `/api`
- Opción recomendada: convención global (`IApplicationModelConvention` que antepone `api/` a todas las rutas de controllers), para no depender de que cada controller lo declare.
- `/health` pasa a `/api/health`.
- Test: no queda ningún endpoint fuera de `/api` (recorrer `EndpointDataSource` en un test de integración).

### 3. Registro cerrado
- Eliminar el endpoint y su command. No basta con ocultarlo de Swagger.
- Test: `POST /api/auth/register` → 404.

### 4. Roles
- Añadir `Rol` al usuario: enum `Admin | SuperAdmin`, guardado como string.
- Claim `role` en el access token.
- Políticas de autorización:
  - `"Backoffice"` → `RequireRole("Admin", "SuperAdmin")`.
  - `"SuperAdmin"` → `RequireRole("SuperAdmin")`.
- Todos los controllers bajo `/api/admin` llevan `[Authorize(Policy = "Backoffice")]` a nivel de clase; las acciones de SuperAdmin añaden su política.
- Test de integración "fail closed": recorrer todas las rutas `/api/admin/**` y comprobar 401 sin token. Este test protege las features futuras.

### 5. Seed de usuarios
- Al arrancar, después de migrar: si no existe un usuario con `Admin__SeedEmail`, crearlo con `Admin__SeedPassword`, rol `Admin`, nombre `Admin__SeedNombre`. Igual para `SuperAdmin__SeedEmail` / `SuperAdmin__SeedPassword`.
- Si el usuario ya existe, **no** se sobreescribe la contraseña (para que la clienta pueda cambiarla).
- Excepción explícita: si `SuperAdmin__ResetAdminPassword=true`, se resetea la contraseña del Admin a `Admin__SeedPassword` y se registra un warning en el log. Es el mecanismo de "olvidé mi contraseña" en v1. Volver a ponerla a `false` tras usarla.
- Si faltan las variables en Production → el arranque falla con un error claro.

### 6. Tokens
- **Login** devuelve `{accessToken, expiresIn, user}` en el body y el refresh token en cookie:
  - Nombre `rt`, `HttpOnly`, `Secure`, `SameSite=Strict`, `Path=/api/auth`, `Expires` = caducidad del refresh.
  - En Development (http://localhost) `Secure` puede ir a `false` vía configuración (`Auth__CookieSecure`).
- **Refresh** lee la cookie (no el body), rota el token (el hash SHA-256 ya lo hace la plantilla) y reescribe la cookie. Reutilizar un refresh ya rotado revoca toda la familia de tokens de ese usuario (detección de robo).
- **Logout** revoca el refresh actual y borra la cookie.
- **Me** devuelve el usuario del token.
- **Cambiar password**: valida la actual, mínimo 10 caracteres la nueva, revoca todos los refresh del usuario salvo el actual.
- Access token: 15 min (`Jwt__AccessTokenMinutes`). Refresh: 14 días (`Jwt__RefreshTokenDays`).
- El front guarda el access token **solo en memoria** (ver F09).

### 7. IP real (`ForwardedHeaders`)
Cadena real en producción: navegador → edge de Railway → servicio Next → rewrite por red privada → API.
- `ForwardedHeadersOptions`: `XForwardedFor | XForwardedProto`, `ForwardLimit = 2` (punto de partida).
- `KnownNetworks`: rangos privados de Railway (IPv4 y IPv6 de la red interna) o, si no se pueden fijar, limpiar `KnownNetworks/KnownProxies`. Esto último solo es aceptable **mientras la API no tenga dominio público** (ver F03 y F10).
- `app.UseForwardedHeaders()` es el primer middleware.
- **Verificación obligatoria en Railway:** endpoint temporal `GET /api/_diag/ip` (solo con `Diag__Enabled=true`) que devuelve `RemoteIpAddress` y las cabeceras `X-Forwarded-*`. Probar desde dos redes distintas; deben salir dos IPs públicas distintas. Después, desactivar.

### 8. Rate limiting
Políticas con `FixedWindowLimiter` particionadas por IP (`HttpContext.Connection.RemoteIpAddress` después de `ForwardedHeaders`):

| Política | Límite por defecto | Uso |
| --- | --- | --- |
| `auth` | 10 / 5 min | `/api/auth/*` (ya existía, se ajusta) |
| `reservas` | 10 / 10 min | F03, F04 |
| `formularios` | 5 / 10 min | F05, F06 contacto |

Valores en configuración (`RateLimit__Reservas__PermitLimit`, etc.). Respuesta 429 con `ProblemDetails` y cabecera `Retry-After`.

### 9. `TimeProvider`
`services.AddSingleton(TimeProvider.System)`. Tests de integración lo reemplazan por `FakeTimeProvider`.

### 10. MediatR
Comprobar la versión en el `.csproj`. Si es ≥ 13: registrar la licencia Community (`MediatR__LicenseKey` en variables de entorno) y comprobar que el warning desaparece de los logs. Si es 12.x: anotar en el README que no se debe subir de versión sin licencia.

## Configuración nueva

`Admin__SeedEmail`, `Admin__SeedPassword`, `Admin__SeedNombre`, `SuperAdmin__SeedEmail`, `SuperAdmin__SeedPassword`, `SuperAdmin__ResetAdminPassword`, `Jwt__RefreshTokenDays`, `Auth__CookieSecure`, `RateLimit__*`, `Diag__Enabled`, `MediatR__LicenseKey`.

## Tests

**Integración**
- Login correcto → 200, body con token, cabecera `Set-Cookie` con `rt; HttpOnly; Secure; SameSite=Strict; Path=/api/auth`.
- Login incorrecto → 401 (mismo mensaje para email inexistente y password incorrecta).
- Refresh con cookie válida → 200 y cookie nueva distinta; refresh con la cookie antigua → 401 y la nueva también queda revocada.
- Refresh sin cookie → 401.
- Logout → 204 y la cookie ya no sirve.
- `register` → 404.
- Todas las rutas `/api/admin/**` sin token → 401; con token de `Admin` en rutas de SuperAdmin → 403.
- Seed: arrancar dos veces no duplica usuarios ni cambia contraseñas; `ResetAdminPassword=true` sí la cambia.
- Rate limit: la petición N+1 de la política `auth` → 429.
- Todas las rutas empiezan por `/api`.

**Unitarios**
- Validadores de login y cambio de contraseña.

## Criterios de aceptación

- [ ] No queda rastro de Notes (código, tablas, tests, migraciones).
- [ ] `dotnet test` en verde (unitarios e integración).
- [ ] Swagger muestra solo rutas `/api/...` y ninguna de registro.
- [ ] Login → refresh → logout funcionando desde el navegador a través del rewrite de Next (se comprueba de verdad en F09/F10).
- [ ] En Railway, el endpoint de diagnóstico muestra la IP pública real del cliente; después queda desactivado.
- [ ] Licencia de MediatR resuelta y documentada.

## Qué revisar a mano

- Atributos de la cookie (en especial `Path` y `SameSite`).
- Que el test "fail closed" de `/api/admin/**` existe y recorre las rutas dinámicamente (no una lista escrita a mano).
- Configuración de `ForwardedHeaders` y orden de middlewares: `ForwardedHeaders` → `ExceptionHandler` → `RateLimiter` → `Authentication` → `Authorization`.
