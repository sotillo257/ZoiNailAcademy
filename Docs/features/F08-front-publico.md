# F08 — Front público

**Revisión línea a línea:** No (revisión visual contra el Figma en escritorio y móvil).
**Depende de:** contrato de la API (puede empezar en paralelo a F02–F06 con mocks generados desde el Swagger).

## Objetivo

Maquetar la web pública según el Figma (escritorio y móvil con layouts propios) y conectar el flujo completo: ver cursos → elegir fecha en el calendario → pagar con tarjeta o transferencia/Bizum → confirmación; más lista de espera y contacto.

## Stack

- Next.js (App Router, versión estable actual), TypeScript estricto, `output: 'standalone'`.
- Tailwind CSS con los tokens del Figma (colores, tipografías, radios, espaciados) en `tailwind.config` / variables CSS.
- Formularios: `react-hook-form` + `zod`.
- Tipos de la API generados con `openapi-typescript` desde el Swagger (`npm run api:types`), cliente fino con `fetch`.
- Fechas: `date-fns` + `date-fns-tz` (zona `Europe/Madrid`, locale `es`).
- Calendario: `FullCalendar` (vistas `dayGridMonth` en escritorio y `listMonth` en móvil, ambas MIT) **o** componente propio. Decidir según el diseño del Figma: si el diseño del calendario es muy particular, componente propio con `date-fns`.

## Páginas y rutas

> **Pregunta abierta para el diseñador:** el encargo son "2 vistas + contacto". Confirmar si el Figma incluye también las pantallas del flujo (calendario, formulario de reserva, instrucciones de pago, éxito, lista de espera). Si no vienen, se maquetan con el sistema visual del Figma (mismos componentes, colores y tipografías) y se enseñan a la clienta antes de cerrar.

| Ruta | Render | Contenido |
| --- | --- | --- |
| `/` | SSR con `revalidate: 60` | Vista 1 del Figma (portada con los 4 cursos y su próxima fecha) |
| `/cursos/[slug]` | SSR con `revalidate: 60` + `generateStaticParams` | Vista 2 del Figma (detalle del curso, botones "Reservar" y "Lista de espera") |
| `/reservar/[slug]` | SSR, datos `no-store` | Calendario de ediciones + selección + formulario + método de pago |
| `/reserva/[codigo]` | Cliente | Estado de la reserva: instrucciones de pago manual o confirmación de tarjeta |
| `/lista-espera?curso=&edicion=` | SSR | Formulario de lista de espera |
| `/contacto` | Estático | Formulario de contacto (vista del Figma) |
| `/baja?token=&curso=` | Cliente | Baja de avisos (F11); `curso` es el slug, para nombrar el curso en el texto |
| `/aviso-legal`, `/privacidad`, `/cookies`, `/condiciones` | Estático (MDX) | Textos que aporta la clienta |
| `not-found`, `error` | — | Páginas propias con el estilo del Figma |

Los datos de plazas **siempre** se piden con `cache: 'no-store'` (en `/reservar` y en el detalle del curso para el bloque de próximas fechas). El resto del contenido del curso puede cachear 60 s.

## Flujo de reserva (`/reservar/[slug]`)

1. **Calendario**
   - Escritorio: vista mensual. Los días con edición se marcan; al pulsar, se ve la hora y las plazas libres. Navegación entre meses limitada a meses con ediciones.
   - Móvil: lista cronológica de ediciones (tarjetas con fecha, hora, plazas libres y estado). Nada de calendario mensual en móvil.
   - Estados: `Disponible` (normal), `UltimasPlazas` ("¡Últimas X plazas!"), `Completo` (deshabilitado + botón "Avisarme" que lleva a `/lista-espera?curso=…&edicion=…`).
   - Sin ediciones: mensaje "No hay fechas abiertas ahora mismo" + CTA a la lista de espera.
   - Si llega con `?abandonada={codigo}` (volvió del Checkout sin pagar): llamar a `POST /api/reservas/{codigo}/abandonar` una vez y mostrar aviso "No se completó el pago; tu plaza se ha liberado".
2. **Formulario**: nombre, email, teléfono (placeholder español, admite internacional), casilla "Acepto las condiciones de compra y la política de privacidad" con enlaces, honeypot `website` oculto (fuera de pantalla, `tabIndex=-1`, `autocomplete=off`, sin `display:none`).
3. **Método de pago**: dos opciones claras, "Tarjeta" y "Transferencia o Bizum" (con nota: "Tu plaza queda reservada {X} horas mientras confirmamos el pago").
4. **Envío**:
   - Tarjeta → `POST /api/reservas/tarjeta` → `window.location.assign(checkoutUrl)`.
   - Manual → `POST /api/reservas/manual` → `router.push('/reserva/{codigo}')`.
   - Botón deshabilitado y con spinner mientras envía (evitar doble reserva).
5. **Errores** (por `extensions.code`):
   - `sin_plazas` → "Alguien ha reservado la última plaza justo ahora" + recargar ediciones + ofrecer lista de espera.
   - `edicion_no_reservable` → mensaje del backend + recargar.
   - `limite_pendientes` → "Ya tienes reservas pendientes de pago. Complétalas o escríbenos por WhatsApp".
   - 400 → errores por campo en el formulario.
   - 429 → "Demasiados intentos, espera unos minutos".
   - Otros → mensaje genérico + enlace a contacto.

## Página de reserva (`/reserva/[codigo]`)

- Consulta `GET /api/reservas/{codigo}`.
- **Manual + Pendiente:** importe, titular e IBAN (con botón copiar), teléfono de Bizum (copiar), concepto = código (copiar, destacado), fecha límite en hora de Madrid y cuenta atrás, botón grande "Enviar comprobante por WhatsApp" (`whatsappUrl`), aviso "Te hemos enviado estos datos por email".
- **Stripe + Pendiente** (llega desde Checkout antes que el webhook): "Confirmando tu pago…" y reconsultar cada 2 s durante 30 s; si sigue pendiente, "Tu pago se está procesando, recibirás un email de confirmación".
- **Pagada:** confirmación con curso, fecha, código y "Te hemos enviado un email".
- **Expirada / Cancelada:** "Esta reserva ya no está activa" + volver a reservar.
- 404 → página de reserva no encontrada.
- `noindex` en esta ruta.

## Lista de espera y contacto

- Lista de espera: selector de curso (preseleccionado desde la URL), fecha preferida (texto libre; si viene `edicion`, mostrarla como contexto), nombre, teléfono, email, casilla de consentimiento con el texto de F05 (constante `CONSENTIMIENTO_LISTA_ESPERA_V1` en el front, idéntica a la constante versionada del backend; si se cambia una, se cambian las dos y se sube la versión) y honeypot. Éxito: "Te avisaremos cuando haya nuevas fechas".
- Contacto: según Figma, con los campos de F06.

## SEO y metadatos

- `generateMetadata` por página: título, descripción, Open Graph con la imagen del curso.
- `sitemap.ts` (portada, cursos, contacto, legales) y `robots.ts` (bloquear `/reserva/`, `/admin`, `/baja`).
- Datos estructurados `Course` + `CourseInstance` (JSON-LD) en el detalle del curso con las próximas ediciones.
- URLs canónicas con `NEXT_PUBLIC_SITE_URL`.

## Calidad

- **Responsive:** breakpoints del Figma. Donde escritorio y móvil cambian de estructura, componentes distintos (`hidden md:block` / `md:hidden`), sin detectar el dispositivo en servidor.
- **Accesibilidad:** contraste AA, foco visible, labels en todos los campos, errores asociados con `aria-describedby`, calendario navegable con teclado, `alt` en imágenes.
- **Rendimiento:** `next/image` con tamaños correctos (en `standalone` la optimización funciona con `sharp`), fuentes con `next/font`, Lighthouse móvil ≥ 90 en rendimiento, accesibilidad y SEO en portada y detalle.
- **Cookies:** sin analítica no hace falta banner. Si se añade analítica, banner con rechazo tan fácil como aceptar y sin cargar nada antes del consentimiento.
- **Configuración:** `API_INTERNAL_URL` (SSR y rewrite), `NEXT_PUBLIC_SITE_URL`. En el navegador, todas las llamadas son relativas (`/api/...`).

## `next.config`

- `output: 'standalone'`.
- `rewrites`: `/api/:path*` → `${API_INTERNAL_URL}/api/:path*`.
- `images.remotePatterns` vacío (imágenes locales).
- Cabeceras de seguridad: `X-Content-Type-Options`, `Referrer-Policy: strict-origin-when-cross-origin`, `X-Frame-Options: DENY` (o CSP `frame-ancestors 'none'`), `Permissions-Policy` restrictiva.

## Tests

- **Unitarios (Vitest + Testing Library):** formateo de fechas en Madrid (incluido el cambio de hora de octubre/marzo), mapeo de errores por `code`, validaciones zod, construcción de estados del calendario.
- **E2E (Playwright)** contra el entorno con Stripe en modo test:
  1. Reserva con tarjeta `4242…` hasta la página de confirmación.
  2. Reserva manual hasta instrucciones; el botón de WhatsApp lleva el código.
  3. Edición completa → botón de lista de espera → alta correcta.
  4. Contacto.
  5. Los mismos flujos 1 y 2 en viewport móvil (iPhone 13).

## Criterios de aceptación

- [ ] Las vistas del Figma coinciden en escritorio y móvil (revisión visual con el diseñador o la clienta).
- [ ] Los flujos E2E pasan.
- [ ] Ningún dato de plazas sale cacheado.
- [ ] Lighthouse móvil ≥ 90 en portada y detalle.
- [ ] Páginas legales enlazadas en el pie y en los formularios.
