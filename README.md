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

## Endpoints

### Auth y sistema (F01)

| Método | Ruta | Auth | Descripción |
| --- | --- | --- | --- |
| GET | `/api/health` | No | Health check |
| POST | `/api/auth/login` | No | `{email, password}` → `{accessToken, expiresIn, user}` + cookie `rt` |
| POST | `/api/auth/refresh` | Cookie `rt` | Rota la cookie y devuelve lo mismo que login |
| POST | `/api/auth/logout` | Cookie `rt` | 204, revoca y borra la cookie |
| GET | `/api/auth/me` | Bearer | Usuario actual `{id, email, nombre, rol}` |
| POST | `/api/auth/cambiar-password` | Bearer | `{passwordActual, passwordNueva}` → 204; cierra las demás sesiones |
| GET | `/api/_diag/ip` | No | Solo con `Diag__Enabled=true`: IP vista por la API y cabeceras `X-Forwarded-*` |

### Cursos y ediciones (F02)

| Método | Ruta | Auth | Descripción |
| --- | --- | --- | --- |
| GET | `/api/cursos` | No | Cursos activos por `orden`, con `proximaEdicion` |
| GET | `/api/cursos/{slug}` | No | Detalle con las ediciones visibles; 404 si no existe o está inactivo |
| GET | `/api/cursos/{slug}/ediciones` | No | Calendario con plazas libres (`Cache-Control: no-store`) |
| GET | `/api/admin/cursos` | Backoffice | `{id, nombre, slug, activo}[]` para selectores |
| POST / PUT / DELETE | `/api/admin/cursos[/{id}]` | SuperAdmin | CRUD de cursos; `DELETE` desactiva (409 `curso_con_ediciones` si hay ediciones futuras programadas) |
| GET | `/api/admin/ediciones?cursoId=&incluirPasadas=` | Backoffice | Ediciones con pagadas, pendientes, libres, `reservable` y `version` |
| GET / POST / PUT / DELETE | `/api/admin/ediciones[/{id}]` | Backoffice | CRUD de ediciones; `PUT` exige el `version` leído (409 `concurrencia`) |

Los errores de negocio son `409` con `code` (y datos extra como `ocupadas` o `reservas`) en el `ProblemDetails`; ver la tabla de códigos en `Docs/00-contrato-y-convenciones.md`. Los 4 cursos iniciales se siembran al arrancar desde `src/ZoiNailAcademy.Infrastructure/Data/Seed/cursos.json` (solo si no existe el slug; nunca se sobrescriben). Hasta F03 no hay reservas, así que las plazas libres son las totales: el único punto que cambia F03 es `OcupacionReader`.

### Reservas con tarjeta (F03)

| Método | Ruta | Auth | Descripción |
| --- | --- | --- | --- |
| POST | `/api/reservas/tarjeta` | No (rate limit `reservas`) | `{edicionId, nombre, email, telefono, aceptaCondiciones, website}` → `{codigo, checkoutUrl}`; 409 `sin_plazas` / `edicion_no_reservable`, 429 `limite_pendientes`, 502 si falla Stripe |
| GET | `/api/reservas/{codigo}` | No | Estado público de la reserva, sin datos personales |
| POST | `/api/reservas/{codigo}/abandonar` | No (rate limit `reservas`) | 204; libera la plaza si el comprador vuelve del Checkout sin pagar |
| POST | `/api/stripe/webhook` | Firma `Stripe-Signature` | `checkout.session.completed` / `expired`; idempotente por id de evento |

Nunca se vende más de una plaza de las que hay: cada reserva bloquea la fila de la edición (`SELECT … FOR UPDATE`) antes de contar plazas. En Stripe hay que dar de alta el endpoint `https://<dominio>/api/stripe/webhook` con los eventos `checkout.session.completed` y `checkout.session.expired`, y copiar su secreto de firma en `Stripe__WebhookSecret`.

### Reservas por transferencia o Bizum (F04)

| Método | Ruta | Auth | Descripción |
| --- | --- | --- | --- |
| POST | `/api/reservas/manual` | No (rate limit `reservas`) | Igual que `tarjeta`; devuelve `datosPago`, `whatsappUrl` y `expiraEn` (48 h, recortadas al cierre; 409 si quedan < 2 h) |
| POST | `/api/admin/reservas/{id}/confirmar` | Backoffice | `{version, nota?}`: marca pagada una transferencia/Bizum |
| POST | `/api/admin/reservas/{id}/cancelar` | Backoffice | `{version, motivo?, confirmarPagada?}` |
| POST | `/api/admin/reservas/{id}/ampliar` | Backoffice | `{version, horas}` (1–168), recortado al cierre de la edición |

Las acciones responden `{reserva, avisos}` y exigen el `version` leído (409 `concurrencia` si el job u otra persona la cambió). Un job en segundo plano (`Reservas__JobIntervaloMinutos`) expira las pendientes vencidas con un advisory lock de PostgreSQL, así que es seguro con varias réplicas; aunque vaya con retraso, la plaza vuelve al calendario en cuanto pasa `ExpiraEn`. Cancelar una edición cancela sus reservas pendientes.

### Lista de espera (F05)

| Método | Ruta | Auth | Descripción |
| --- | --- | --- | --- |
| POST | `/api/lista-espera` | No (rate limit `formularios`) | `{cursoId, edicionPreferidaId?, fechaPreferida?, nombre, email, telefono, aceptaPrivacidad, website}` → 201 sin body |
| GET | `/api/admin/lista-espera?cursoId=&contactado=&q=&page=&pageSize=` | Backoffice | `{items, total, page, pageSize}` (50 por defecto, máximo 200); no contactados primero y, dentro, los que más esperan |
| PUT | `/api/admin/lista-espera/{id}/contactado` | Backoffice | `{contactado, notas?}` → `ListaEsperaAdminDto` (sin `notas`, se conservan) |
| GET | `/api/admin/lista-espera.csv?cursoId=` | Backoffice | CSV para Excel: UTF-8 con BOM, `;`, fechas en hora de Madrid, `lista-espera-{slug}-{yyyyMMdd}.csv` |

El mismo email en el mismo curso actualiza el registro (vuelve a "no contactado") y responde igual que un alta nueva. El texto de consentimiento que se guarda es la constante versionada del backend (`ConsentimientoListaEspera.V1`, también cubre el aviso por email de nuevas fechas de F11), no el que envía el front. `q` busca en nombre, email y teléfono sin distinguir mayúsculas ni acentos (`Jose` encuentra `José`). El CSV antepone `'` a los campos que empiezan por `=`, `+`, `-` o `@` para que Excel no los ejecute como fórmulas (por eso el teléfono sale como `'+34…`).

### Emails y contacto (F06)

| Método | Ruta | Auth | Descripción |
| --- | --- | --- | --- |
| POST | `/api/contacto` | No (rate limit `formularios`) | `{nombre, email, telefono?, mensaje, aceptaPrivacidad, website}` → 202; email a la clienta con `Reply-To` del remitente |
| GET | `/api/admin/emails/preview/{tipo}` | SuperAdmin | HTML del email con datos de ejemplo (`?formato=texto` para la versión de texto) |

Los eventos de dominio encolan los emails en `EmailsSalientes` (una sola vez por tipo y reserva aunque el evento se repita) y `EnviarEmailsJob` los envía cada 30 s, con reintentos a 1 min, 5 min, 15 min y 1 h, y `Fallido` tras 5 intentos. Un fallo de email nunca rompe una reserva ni un webhook. Los enviados se borran a los 90 días.

| Tipo | Cuándo | Para |
| --- | --- | --- |
| `ReservaPagada.Comprador` / `.Clienta` | Pago con tarjeta o transferencia confirmada | Comprador / clienta |
| `ReservaManual.Comprador` / `.Clienta` | Reserva por transferencia o Bizum | Comprador (datos de pago y WhatsApp) / clienta |
| `ReservaExpirada.Comprador` | Caduca una reserva **manual** (las de tarjeta no avisan) | Comprador |
| `PagoSinPlaza.Clienta` | Pago tras caducidad sin plaza | Clienta |
| `ListaEspera.Clienta` | Alta o re-alta en la lista de espera | Clienta |
| `Contacto.Clienta` / `Contacto.Acuse` | Formulario de contacto (acuse solo con `Email__AcuseContacto=true`) | Clienta / remitente |

**Sin cuenta de Resend:** si falta `Resend__ApiKey` o `Email__From`, la API arranca igual, lo avisa en el log al arrancar y los emails solo se escriben en el log (quedan como enviados con `ProveedorId = log`). Si falta `Email__AvisosClienta`, los avisos a la clienta no se encolan. En `Development` el proveedor es `Log` y cada email se guarda también en `emails-dev/*.html` para verlo en el navegador. Los textos, colores y el logo (`{App__BaseUrl}/email/logo.png`) son provisionales hasta revisarlos con la clienta en el preview.

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
| `Reservas__CierreHorasAntes` | Horas antes del inicio en que se cierran las reservas y la edición deja de verse en la web (24) |
| `Reservas__UmbralUltimasPlazas` | Plazas libres a partir de las cuales se muestra «últimas plazas» (3) |
| `App__TimeZone` | Zona horaria de presentación (`Europe/Madrid`); la API guarda y devuelve UTC |
| `App__BaseUrl` | URL pública de la web, para las URLs de vuelta de Stripe Checkout |
| `Reservas__StripeMinutosBloqueo` | Duración de la sesión de Checkout en minutos (mínimo 30; la reserva dura 5 más) |
| `Reservas__MaxPendientesPorContacto` | Reservas pendientes vigentes permitidas por email o teléfono (2) |
| `Reservas__ManualHorasCaducidad` | Horas que una reserva por transferencia/Bizum bloquea la plaza (48) |
| `Reservas__JobIntervaloMinutos` | Cada cuántos minutos se expiran las pendientes vencidas (5) |
| `Pago__Titular`, `Pago__Iban`, `Pago__BizumTelefono` | Datos de pago que ve el comprador |
| `Pago__WhatsAppTelefono` | WhatsApp para el comprobante, internacional sin `+` (`34612345678`) |
| `Stripe__SecretKey`, `Stripe__WebhookSecret` | **Secretos** de Stripe (`sk_…` y `whsec_…` del endpoint del webhook) |
| `App__NombreAcademia`, `App__DireccionAcademia` | Nombre y dirección del pie de los emails |
| `Email__Provider` | `Resend` (por defecto) o `Log`; sin `Resend__ApiKey` o `Email__From` se usa `Log` |
| `Resend__ApiKey` | **Secreto** de Resend (`re_…`) |
| `Email__From` | Remitente con dominio verificado en Resend (`Zoi Nail Academy <reservas@dominio>`) |
| `Email__ReplyTo` | Email de la clienta: `Reply-To` de los emails a compradores |
| `Email__AvisosClienta` | Dónde recibe la clienta los avisos (vacío: no se encolan) |
| `Email__AcuseContacto` | Acuse de recibo al que usa el formulario de contacto (`false`) |
| `Email__DirectorioLog` | Solo desarrollo: carpeta donde el proveedor `Log` guarda los `.html` |
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
