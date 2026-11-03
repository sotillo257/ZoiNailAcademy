# Zoi Nail Academy — Web (Next.js)

Front de la web: la **web pública** (F08) y el **backoffice** (`/admin`, F09).

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

## Web pública (F08)

- Diseño aprobado (Figma): tokens `--z-*` en `globals.css` (granate `#8b0b0a`, crema `#f6f2f1`, gris foto `#e8e0de`, verde `#176f3c`) y Montserrat autoalojada (`@fontsource-variable/montserrat` + `next/font/local`, el build no necesita red).
- **Datos que aporta la clienta:** `src/lib/sitio/datos.ts` (dirección, WhatsApp, email, Instagram, horario, URLs de la academia online y del sistema de citas del spa, mapa, calendario descargable) y `src/lib/sitio/contenido.ts` (fotos y testimonios). Lo que va entre corchetes son los huecos del diseño. Una foto a `null` pinta el hueco gris del diseño; para ponerla, se deja en `public/fotos/` y se escribe su ruta. Mientras una URL externa no está, el enlace lleva a `/contacto`.
- Rutas: `/` (Inicio), `/formaciones` (calendario, formaciones, opiniones, lista de espera), `/contacto`, `/reservar/[slug]` (paso 1), `/reserva/[codigo]` (paso 2 transferencia/Bizum, vuelta de Stripe, pagada, caducada), `/baja` (avisos de F11), legales, `not-found` y `error`.
- Las plazas nunca se cachean (`leerApi(..., { fresco: true })`, páginas `force-dynamic`). El formulario de la lista de espera se carga al acercarse a su sección (`lista-espera-diferida.tsx`) para que Formaciones pase de 90 en Lighthouse móvil.

## Estructura

```
src/app/admin/login             Login
src/app/admin/(panel)/…         Pantallas con sesión (guardia + navegación en components/admin/panel-shell.tsx)
src/app/(sitio), src/app/(reserva)  Web pública y flujo de reserva (F08)
src/components/sitio            Componentes de la web pública
src/lib/sitio                   Cliente de la API pública, esquemas de formularios, errores por code, datos y contenido
src/components/admin            Componentes del backoffice (tarjeta de reserva, diálogos, formularios)
src/components/ui               Primitivas (botón, diálogo, pestañas, interruptor…)
src/lib/admin                   Sesión, errores (ProblemDetails), tipos de la API, descargas
src/lib/fechas.ts               Conversión hora de Madrid ↔ ISO (cambios de hora incluidos)
e2e/                            Playwright
```
