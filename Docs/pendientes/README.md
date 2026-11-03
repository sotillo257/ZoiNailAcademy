# Pendientes

Lo que falta para terminar y poner en producción la API. Al cerrar un punto, márcalo aquí y también en el `tasks.md` del cambio de OpenSpec correspondiente (si viene de ahí).

Última revisión: 03/11/2026, tras implementar F11 en la API.

## 1. Otras features

- [ ] **F08 – front público:** página `/baja?token=&curso=`. Pide confirmación con el botón "Darme de baja de los avisos", llama a `POST /api/lista-espera/baja` con `{ token }` y muestra "Ya no te avisaremos de nuevas fechas de {curso}" (el nombre se saca del slug con `GET /api/cursos/{slug}`). Nunca debe dar de baja con un simple `GET`. Hay que bloquear `/baja` en `robots.ts`.
- [ ] **F09 – front del backoffice:** indicador "Se dio de baja de avisos" en la tarjeta de la lista de espera, con el campo `baja` de `ListaEsperaAdminDto`.
- [ ] **F08/F09:** regenerar los tipos TypeScript desde Swagger (`openspec/changes/f02-cursos-y-ediciones` 9.2).
- [ ] **F10 – infraestructura:** desplegar en Railway, dominio y DNS, Stripe, CI, variables y checklist de producción.

## 2. Configuración en producción (Railway)

- [ ] Sustituir los textos `[PENDIENTE]`, precios y slugs de `cursos.json` por los datos de la clienta antes del primer despliegue (F02 9.1).
- [ ] `Pago__*` con los datos reales del banco y de Bizum. Comprobar en un móvil que el enlace de WhatsApp se abre con el mensaje (F04 5.1).
- [ ] Crear la cuenta de Resend, verificar el dominio y configurar `Resend__ApiKey`, `Email__From`, `Email__ReplyTo` y `Email__AvisosClienta` (F06 5.2).
- [ ] Revisar `Avisos__LoteTamano`, `Avisos__PausaEntreLotesSegundos` y `Avisos__JobIntervaloMinutos` según el plan de Resend (F11).
- [ ] Con `Diag__Enabled=true`, comprobar `/api/_diag/ip` desde dos redes y ajustar `ForwardedHeaders__ForwardLimit`/`KnownNetworks`. Después, desactivarlo (F01 7.2).

## 3. Pruebas manuales

- [ ] Login → refresh → logout desde el navegador, a través del rewrite de Next (F01 7.3).
- [ ] Revisar el SQL en los logs de una ejecución real: cada listado debe hacer una consulta para cursos, una para ediciones y una para ocupación (F02 9.3).
- [ ] Stripe: pago completo con `4242 4242 4242 4242`. Tarjeta rechazada `4000 0000 0000 0002`: la reserva sigue `Pendiente` y caduca. Cerrar la pestaña del Checkout: la plaza vuelve en 30–35 min. `stripe trigger checkout.session.completed` a través del dominio del front: la firma se verifica (F03 7.1–7.4).
- [ ] Abrir en Excel (configuración en español) el CSV de la lista de espera y el de asistentes, y comprobar acentos, importes y columnas (F05 5.1, F07 5.1).
- [ ] Emails: revisar textos, colores y logo con la clienta mediante `/api/admin/emails/preview/{tipo}` (F06 5.1).
- [ ] Enviar todos los tipos de email a Gmail y Outlook: que lleguen a la bandeja de entrada (no a spam), que se vean bien en el móvil y que los enlaces apunten a producción (F06 5.3).
- [ ] Aviso de nuevas fechas: con Resend configurado, comprobar que en Gmail aparece el botón nativo "Cancelar suscripción" y que da de baja (F11 5.1).

## 4. Con la clienta

- [ ] Responder las preguntas abiertas de `Docs/README.md` ("Antes de empezar") y de `Docs/PRD.md` ("Preguntas abiertas"): IVA y facturas, presencial u online, datos bancarios, horas de cierre, pantallas del Figma, textos legales e imágenes.
- [ ] Decirle por escrito que el aviso de nuevas fechas (F11) es un **regalo** (F11 5.2).

## 5. Mantenimiento de OpenSpec

- [ ] Sincronizar y archivar los cambios terminados `f01` … `f11` (`/opsx:sync`, `/opsx:archive`). Ahora `openspec/specs/` solo contiene las specs de la plantilla.
- [ ] Borrar `openspec/specs/notes`: la feature Notes se eliminó en F01.
