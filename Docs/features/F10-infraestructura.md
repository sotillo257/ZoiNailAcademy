# F10 — Infraestructura y despliegue

**Revisión línea a línea:** No (pero se ejecuta a mano con checklist).
**Depende de:** F01 para el primer despliegue; se completa al final.

## Objetivo

Desplegar el monorepo en Railway con dos servicios (web y API) y PostgreSQL, dominio propio, emails verificados, Stripe conectado, backups y CI, con un entorno de pruebas antes de producción.

## Estructura del repo

```
/
├─ src/                      ← plantilla .NET 10
├─ tests/
├─ web/                      ← Next.js
│  ├─ Dockerfile
│  └─ .dockerignore
├─ Dockerfile                ← API (el de la plantilla)
├─ .dockerignore
├─ .env.example              ← todas las variables, sin valores secretos
├─ docs/                     ← PRD y specs
└─ .github/workflows/ci.yml
```

## Railway

### Proyecto y entornos
- Un proyecto con dos entornos: `staging` y `production`.
  - `staging`: Stripe en modo test, `Email__Provider=Resend` con el dominio de pruebas o `Log`, base de datos propia.
  - `production`: Stripe live, dominio real.
- Con el plan Hobby, comprobar que el consumo de los dos entornos cabe; si no, `staging` se apaga (`Remove`/pausa) cuando no se usa.

### Servicios
| Servicio | Origen | Root directory | Watch paths | Dominio |
| --- | --- | --- | --- | --- |
| `web` | `web/Dockerfile` | `web/` | `web/**` | Dominio de la clienta (`www` y raíz) |
| `api` | `Dockerfile` de la raíz | `/` | `src/**`, `tests/**`, `Dockerfile` | **Ninguno** (solo red privada) |
| `postgres` | Plantilla PostgreSQL de Railway | — | — | — |

- `api` escucha en `http://[::]:8080` (`ASPNETCORE_URLS` o `PORT=8080` + configuración de Kestrel), porque la red privada de Railway puede ir por IPv6.
- `web` usa `API_INTERNAL_URL=http://${{api.RAILWAY_PRIVATE_DOMAIN}}:8080`.
- `api` usa `DATABASE_URL=${{postgres.DATABASE_URL}}` (referencia de variable de Railway).
- Health checks: `api` → `/api/health`; `web` → `/` (o `/api/health` a través del rewrite, que valida los dos a la vez).
- Una sola réplica de `api` (los jobs de F04 y F06 asumen eso, aunque llevan advisory lock).
- "Wait for CI": activado en ambos servicios para que no se despliegue si el CI falla.

### `web/Dockerfile` (multi-stage)
```dockerfile
FROM node:22-alpine AS deps
WORKDIR /app
COPY package.json package-lock.json ./
RUN npm ci

FROM node:22-alpine AS build
WORKDIR /app
COPY --from=deps /app/node_modules ./node_modules
COPY . .
ENV NEXT_TELEMETRY_DISABLED=1
RUN npm run build

FROM node:22-alpine AS run
WORKDIR /app
ENV NODE_ENV=production NEXT_TELEMETRY_DISABLED=1 HOSTNAME=0.0.0.0 PORT=3000
RUN addgroup -S app && adduser -S app -G app
COPY --from=build /app/public ./public
COPY --from=build --chown=app:app /app/.next/standalone ./
COPY --from=build --chown=app:app /app/.next/static ./.next/static
USER app
EXPOSE 3000
CMD ["node", "server.js"]
```
- Ojo: los rewrites de `next.config` se evalúan en **build**. `API_INTERNAL_URL` tiene que estar disponible como variable de build en Railway (o leerse en runtime con un route handler proxy si da problemas). Verificarlo en el primer despliegue.

### `.dockerignore`
`node_modules`, `.next`, `out`, `bin`, `obj`, `.git`, `emails-dev`, `*.md` (excepto si se necesitan), `.env*`.

## Dominio y DNS

- Dominio **a nombre de la clienta** (si es `.es`, en un registrador que lo venda; Cloudflare Registrar no vende `.es`).
- DNS (en el registrador o en Cloudflare como DNS):
  - Raíz y `www` → Railway (CNAME/ALIAS que indique Railway). Redirección `www` → raíz o al revés (decidir una canónica).
  - **Resend:** registros SPF, DKIM y MX de retorno que indique Resend para el subdominio de envío (p. ej. `send.dominio`). Añadir **DMARC** (`_dmarc` con `p=none` al principio, con informes a un email de la clienta; subir a `quarantine` tras unas semanas sin problemas).
- HTTPS: certificado automático de Railway.

## Stripe

- Cuenta **a nombre de la clienta**. Ella completa la verificación de identidad y la cuenta bancaria; tú solo necesitas acceso de desarrollador (miembro del equipo con rol Developer) para las claves.
- Modo test para `staging`, live para `production`.
- Webhook (uno por entorno): `https://{dominio}/api/stripe/webhook`, eventos `checkout.session.completed` y `checkout.session.expired`. Copiar el `whsec_…` a `Stripe__WebhookSecret`.
- Activar en Checkout los métodos de pago síncronos que quiera la clienta (tarjeta, Apple Pay, Google Pay). **No** activar métodos asíncronos (SEPA Direct Debit, etc.): F03 no los soporta.
- Branding de Checkout (logo, colores) desde el panel de Stripe.
- Probar que la firma se verifica a través del rewrite de Next (`stripe trigger` o un pago de prueba). **Plan B** si no: dar a `api` un dominio público `api.{dominio}`, apuntar el webhook ahí, y en F01 restringir `KnownNetworks` de verdad (con dominio público ya no vale confiar en cualquier `X-Forwarded-For`).
- Facturación: pregunta abierta. Stripe envía recibo, no factura. Si la clienta necesita factura, o la emite su gestoría o se activa Stripe Invoicing (coste aparte).

## Resend

- Cuenta a nombre de la clienta (o tuya con el dominio de ella; mejor de ella).
- Verificar el dominio antes de cualquier prueba con emails reales.
- API key con permiso solo de envío y restringida al dominio.

## Base de datos

- Backups automáticos de Railway activados en `production` desde el primer día. Comprobar la retención del plan.
- Además, backup lógico propio semanal: GitHub Action programada con `pg_dump` (conexión pública temporal o vía `railway connect`) a un almacenamiento externo (p. ej. Cloudflare R2 o Google Drive de la clienta), cifrado. Si no da tiempo, mínimo documentar cómo restaurar desde el backup de Railway y probarlo una vez.
- Migraciones al arrancar (plantilla). Antes de un despliegue con migración destructiva, backup manual.

## CI (`.github/workflows/ci.yml`)

Trigger: `push` y `pull_request` a `main`.

Job `api`:
1. `actions/setup-dotnet` 10.x
2. `dotnet restore`, `dotnet build -warnaserror`
3. `dotnet test tests/UnitTests`
4. `dotnet test tests/IntegrationTests` (Docker disponible en `ubuntu-latest` para Testcontainers)

Job `web` (`working-directory: web`):
1. `actions/setup-node` 22 con caché de npm
2. `npm ci`
3. `npm run lint` y `npm run typecheck`
4. `npm run test` (Vitest)
5. `npm run build`
6. Comprobación de que los tipos de la API están al día: generar desde el `swagger.json` exportado en el job `api` (artefacto) y `git diff --exit-code`.

Job `e2e` (opcional, manual o nocturno): Playwright contra `staging`.

Ramas: `main` despliega a `staging` automáticamente. `production` se despliega desde la rama `production` (merge de `main` cuando staging está validado), o con promoción manual en Railway.

## Variables de entorno (lista completa)

### `api`
| Variable | Staging | Producción | Feature |
| --- | --- | --- | --- |
| `ASPNETCORE_ENVIRONMENT` | `Staging` | `Production` | F01 |
| `DATABASE_URL` | ref. Railway | ref. Railway | F01 |
| `Jwt__SigningKey` | secreto ≥ 32 | secreto ≥ 32 (distinto) | F01 |
| `Jwt__Issuer`, `Jwt__Audience` | dominio | dominio | F01 |
| `Jwt__AccessTokenMinutes`, `Jwt__RefreshTokenDays` | 15, 14 | 15, 14 | F01 |
| `Auth__CookieSecure` | true | true | F01 |
| `Admin__SeedEmail`, `Admin__SeedPassword`, `Admin__SeedNombre` | — | — | F01 |
| `SuperAdmin__SeedEmail`, `SuperAdmin__SeedPassword` | — | — | F01 |
| `SuperAdmin__ResetAdminPassword` | false | false | F01 |
| `RateLimit__*` | por defecto | por defecto | F01 |
| `Diag__Enabled` | true al principio | false | F01 |
| `MediatR__LicenseKey` | si aplica | si aplica | F01 |
| `Stripe__SecretKey` | `sk_test_…` | `sk_live_…` | F03 |
| `Stripe__WebhookSecret` | `whsec_…` | `whsec_…` | F03 |
| `Reservas__StripeMinutosBloqueo` | 30 | 30 | F03 |
| `Reservas__MaxPendientesPorContacto` | 2 | 2 | F03 |
| `Reservas__ManualHorasCaducidad` | 48 | 48 | F04 |
| `Reservas__JobIntervaloMinutos` | 5 | 5 | F04 |
| `Reservas__CierreHorasAntes` | 24 | por confirmar | F02 |
| `Reservas__UmbralUltimasPlazas` | 3 | 3 | F02 |
| `Pago__Titular`, `Pago__Iban`, `Pago__BizumTelefono`, `Pago__WhatsAppTelefono` | datos de prueba | datos reales | F04 |
| `Email__Provider` | `Resend` o `Log` | `Resend` | F06 |
| `Resend__ApiKey` | — | — | F06 |
| `Email__From`, `Email__ReplyTo`, `Email__AvisosClienta` | tu email en avisos | email de la clienta | F06 |
| `Email__AcuseContacto` | false | false | F06 |
| `Avisos__LoteTamano`, `Avisos__PausaEntreLotesSegundos`, `Avisos__JobIntervaloMinutos` | 20, 2, 10 | 20, 2, 10 | F11 |
| `App__BaseUrl` | URL de staging | `https://dominio` | F03 |
| `App__TimeZone` | Europe/Madrid | Europe/Madrid | F02 |
| `App__NombreAcademia`, `App__DireccionAcademia` | — | — | F06 |
| `Swagger__EnableInProduction` | true | false | F01 |

### `web`
| Variable | Staging | Producción |
| --- | --- | --- |
| `API_INTERNAL_URL` | `http://${{api.RAILWAY_PRIVATE_DOMAIN}}:8080` | igual |
| `NEXT_PUBLIC_SITE_URL` | URL de staging | `https://dominio` |

`.env.example` en la raíz con todas, valores vacíos o de ejemplo, y comentario de qué feature la usa.

## Coste y titularidad

- Railway en tu cuenta (plan Hobby con el cupón de 1 año). Vigilar el consumo en el panel durante el primer mes: `web` + `api` + `postgres` encendidos 24/7, y `staging` si está activo. Poner una alerta de uso.
- Stripe, dominio y Resend a nombre de la clienta.
- Documentar en `docs/operacion.md`: dónde está cada cuenta, quién tiene acceso, cómo restaurar un backup, cómo rotar `Jwt__SigningKey` y la clave de Stripe, y cómo resetear la contraseña de la clienta (F01).

## Checklist de puesta en producción

- [ ] CI en verde en `main`.
- [ ] `staging` desplegado y flujos E2E pasados con Stripe test.
- [ ] Diagnóstico de IP correcto (F01) y desactivado.
- [ ] Webhook de Stripe verificando firma a través del dominio del front.
- [ ] Dominio con HTTPS, `www` redirigiendo a la canónica.
- [ ] SPF, DKIM y DMARC correctos; email de prueba en bandeja de entrada de Gmail y Outlook.
- [ ] Backups activos y restauración probada una vez.
- [ ] Variables de producción revisadas (sobre todo `sk_live`, `whsec` de producción, datos bancarios reales).
- [ ] Usuarios sembrados y contraseña de la clienta cambiada por ella en su primer acceso.
- [ ] Swagger desactivado en producción.
- [ ] Un pago real de 1 € completado y reembolsado (curso de prueba creado por el SuperAdmin, activado solo durante la prueba y desactivado después).
- [ ] Textos legales publicados.
