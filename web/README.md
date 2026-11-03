# Zoi Nail Academy — Web (Next.js)

Front de la web: por ahora solo el **backoffice** (`/admin`, F09). La web pública (F08) vivirá en este mismo proyecto.

## Stack

- Next.js 16 (App Router, `output: 'standalone'`), React 19, TypeScript estricto
- Tailwind CSS 4 con componentes estilo shadcn/ui (`src/components/ui`, sobre Radix) y `sonner` para los toasts
- `date-fns` + `date-fns-tz`: todas las fechas se muestran y se introducen en `Europe/Madrid`
- Vitest + Testing Library (unitarios) y Playwright (E2E, móvil y escritorio)

Colores de marca (de zoinailartist.online) en `src/app/globals.css`: granate `#820101` para acciones, rojo `#fc3d39` solo para llamar la atención (contadores, foco) y rosa `#995151` de apoyo. El rojo sobre blanco no llega a AA como texto, así que va como relleno con texto blanco en negrita.

## Puesta en marcha

Con la API levantada en `http://localhost:8080` (ver el README de la raíz):

```bash
cd web
npm install
npm run dev        # http://localhost:3000/admin
```

En el navegador todas las llamadas son relativas (`/api/...`); Next las reenvía a `API_INTERNAL_URL` (rewrite). **Ojo:** Next evalúa los rewrites al compilar, así que `API_INTERNAL_URL` tiene que existir también durante `npm run build`. Variables en `.env.example`.

## Scripts

```bash
npm run typecheck
npm test           # unitarios (Vitest)
npm run build
npm run e2e        # E2E contra un stack levantado (API en Development + `npm start`)
npm run api:types  # genera src/lib/api/schema.d.ts desde el Swagger de la API
```

Los E2E usan por defecto los usuarios sembrados en Development (`admin@local.test`, `superadmin@local.test`); se cambian con `E2E_*` (ver `playwright.config.ts`). Cada test usa su propio `X-Forwarded-For` para no chocar con el rate limit por IP de `/api/auth/*`.

## Autenticación del backoffice

Está en `src/lib/admin/session.ts` (revisar a mano, F09):

- El access token vive **solo en memoria** (`SessionClient`), nunca en `localStorage`/`sessionStorage`/cookies. El refresh token es la cookie `rt` (HttpOnly) de la API.
- Al abrir `/admin` se pide `POST /api/auth/refresh`; si falla → `/admin/login?next=…` (solo rutas `/admin`, sin redirecciones abiertas).
- `session.fetch` añade el Bearer y, ante un 401, hace **un único refresh compartido** por todas las peticiones y reintenta una vez. La API rota el refresh token y trata uno reutilizado como robo (cierra todas las sesiones), así que entre pestañas los refresh se serializan con un Web Lock.
- Refresco proactivo un minuto antes de `expiresIn`; logout propagado a las demás pestañas con `BroadcastChannel('auth')`.
- `/admin/**` sale con `Cache-Control: no-store`, `X-Robots-Tag: noindex` y `robots: noindex`.

## Estructura

```
src/app/admin/login             Login
src/app/admin/(panel)/…         Pantallas con sesión (guardia + navegación en components/admin/panel-shell.tsx)
src/components/admin            Componentes del backoffice (tarjeta de reserva, diálogos, formularios)
src/components/ui               Primitivas (botón, diálogo, pestañas, interruptor…)
src/lib/admin                   Sesión, errores (ProblemDetails), tipos de la API, descargas
src/lib/fechas.ts               Conversión hora de Madrid ↔ ISO (cambios de hora incluidos)
e2e/                            Playwright
```
