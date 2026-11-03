# Pendientes

Lo que falta para terminar y poner en producción la API. Al cerrar un punto, márcalo aquí y también en el `tasks.md` del cambio de OpenSpec correspondiente (si viene de ahí).

Última revisión: 03/11/2026, tras implementar F08 (web pública) y F12 (señal de reserva).

## 1. Otras features

- [x] **F08 – front público:** página `/baja?token=&curso=` (confirma con botón y `POST`, bloqueada en `robots.ts`).
- [ ] **F08:** ajustar Formaciones presenciales con la exportación en alta resolución del Figma (textos de la cabecera, ventajas, subtítulos, chips y testimonios están escritos por mí: la captura recibida no se leía).
- [ ] **F08 – contenido de la clienta:** rellenar `web/src/lib/sitio/datos.ts` (dirección, WhatsApp, email, Instagram, horario, URL de la academia online, del spa y de reserva de citas, mapa, calendario descargable) y `web/src/lib/sitio/contenido.ts` (fotos en `web/public/fotos/` y testimonios). Mientras falten, se ven los huecos del diseño.
- [ ] **F08 – textos legales:** `/aviso-legal`, `/privacidad`, `/cookies`, `/condiciones` llevan textos provisionales marcados `[PENDIENTE]`.
- [ ] **F08:** `NEXT_PUBLIC_SITE_URL` en el build de producción (canónicas y sitemap; sin ella Lighthouse SEO da 92).
- [ ] **F12 – señal:** importe real de "Reserva de plaza" de cada curso (`cursos.json` lleva 30 € provisionales) y prueba en Stripe test de un pago con señal (importe y concepto en la página de Checkout).
- [x] **F09 – front del backoffice:** la pantalla de editar curso (solo SuperAdmin) carga los datos con `GET /api/admin/cursos/{id}` (`CursoAdminDto`) y guarda con `PUT`.
- [x] **F09 – front del backoffice:** indicador "Se dio de baja de avisos" en la tarjeta de la lista de espera, con el campo `baja` de `ListaEsperaAdminDto`.
- [ ] **F08/F09:** regenerar los tipos TypeScript del front desde Swagger (`cd web && npm run api:types`, tarea manual, F02 9.2). Los DTOs del backoffice están escritos a mano en `web/src/lib/admin/types.ts`: compararlos con el `schema.d.ts` generado.
- [ ] **F08/F09:** el front (`web/`, web pública + backoffice) necesita su `Dockerfile` y el servicio en Railway (F10).
- [ ] **F10 – infraestructura:** desplegar en Railway, dominio y DNS, Stripe, CI, variables y checklist de producción.

## 2. Configuración en producción (Railway)

- [ ] `src/ZoiNailAcademy.Infrastructure/Data/Seed/cursos.json` lleva textos de relleno marcados `[PENDIENTE]`, con slugs `curso-1` a `curso-4` y precio de 100 €. Hay que sustituirlos por los datos reales de la clienta antes del primer despliegue, o editarlos después desde la API (`PUT /api/admin/cursos/{id}`, solo SuperAdmin) (F02 9.1).
- [ ] `Pago__*` con los datos reales del banco y de Bizum. Comprobar en un móvil que el enlace de WhatsApp se abre con el mensaje (F04 5.1).
- [ ] Crear la cuenta de Resend, verificar el dominio y configurar `Resend__ApiKey`, `Email__From`, `Email__ReplyTo` y `Email__AvisosClienta` (F06 5.2).
- [ ] Revisar `Avisos__LoteTamano`, `Avisos__PausaEntreLotesSegundos` y `Avisos__JobIntervaloMinutos` según el plan de Resend (F11).
- [ ] Con `Diag__Enabled=true`, comprobar `/api/_diag/ip` desde dos redes y ajustar `ForwardedHeaders__ForwardLimit`/`KnownNetworks`. Después, desactivarlo (F01 7.2).

## 3. Pruebas manuales

- [ ] Login → refresh → logout desde el navegador, a través del rewrite de Next (F01 7.3).
- [ ] Comprobar el SQL en los logs de una ejecución real: cada listado debe hacer una consulta para cursos, una para ediciones y una para ocupación (tarea manual, F02 9.3).
- [ ] **Stripe en modo test** (tareas manuales, F03 7.1–7.4):
  - [ ] Pago completo con la tarjeta `4242 4242 4242 4242`.
  - [ ] Pago rechazado con `4000 0000 0000 0002`: la reserva sigue `Pendiente` y caduca.
  - [ ] Cerrar la pestaña del Checkout: la plaza vuelve en 30–35 min.
  - [ ] **Lo más importante:** lanzar `stripe trigger checkout.session.completed` a través del dominio del front, para comprobar que la firma del webhook se verifica pasando por el rewrite de Next.
- [ ] Abrir los CSV en un Excel en español y comprobar que los acentos y las columnas se ven bien: lista de espera (tarea 5.1 de F05) y asistentes, también los importes (tarea 5.1 de F07).
- [ ] Enviar todos los tipos de email a Gmail y Outlook: que lleguen a la bandeja de entrada (no a spam), que se vean bien en el móvil y que los enlaces apunten a producción (F06 5.3).
- [ ] Aviso de nuevas fechas: con Resend configurado, comprobar que en Gmail aparece el botón nativo "Cancelar suscripción" y que da de baja (F11 5.1).

- [ ] **Backoffice (F09):** en un móvil real, confirmar un pago desde que llega el WhatsApp en menos de 30 s; comprobar en DevTools que el access token no aparece en Application → Storage; probar la descarga de CSV en iOS Safari.
- [ ] **Backoffice (F09):** el refresh de la sesión cuenta para el rate limit `auth` (10 cada 5 min por IP). Recargar la página muchas veces seguidas da 429 ("Demasiados intentos"); si molesta a la clienta, subir `RateLimit__Auth__PermitLimit` o separar una política para `/api/auth/refresh`.

## 4. Con la clienta

- [ ] **Probar el backoffice (F09)** en una sesión guiada de 20 minutos antes de la entrega: confirmar un pago, ampliar plazo, crear y editar una fecha, lista de espera.

- [ ] **Revisar los emails.** Los textos, el color (`#b5838d`) y el logo (`{App__BaseUrl}/email/logo.png`) son provisionales (F06 5.1):
  - Puede verlos con `GET /api/admin/emails/preview/{tipo}` (solo SuperAdmin); con `?formato=texto` sale la versión de texto.
  - En desarrollo, cada email se guarda además como `.html` en `emails-dev/` (`Email__DirectorioLog`).
  - El color y la ruta del logo están en `src/ZoiNailAcademy.Application/Emails/Plantillas/EmailLayout.cs`.

- [ ] Responder las preguntas abiertas de `Docs/README.md` ("Antes de empezar") y de `Docs/PRD.md` ("Preguntas abiertas"): IVA y facturas, presencial u online, datos bancarios, horas de cierre, pantallas del Figma, textos legales e imágenes.
- [ ] Decirle por escrito que el aviso de nuevas fechas (F11) es un **regalo** (F11 5.2).

## 5. Mantenimiento de OpenSpec

- [ ] Sincronizar y archivar los cambios terminados `f01` … `f11` (`/opsx:sync`, `/opsx:archive`). Ahora `openspec/specs/` solo contiene las specs de la plantilla.
- [ ] Borrar `openspec/specs/notes`: la feature Notes se eliminó en F01.
