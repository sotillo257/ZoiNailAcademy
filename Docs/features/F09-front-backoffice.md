# F09 — Front del backoffice

**Revisión línea a línea:** No (sí revisar a mano el manejo de tokens en el cliente).
**Depende de:** F01, F02, F04, F05, F07.

## Objetivo

Que la clienta gestione sus fechas, confirme pagos y consulte inscritos y lista de espera desde `/admin`, **con especial cuidado en móvil**: el comprobante le llega por WhatsApp, así que lo normal será que confirme el pago desde el teléfono.

## Principios

- Vive en el mismo proyecto Next, bajo `/admin`, con su propio layout (sin el estilo de la web pública; un diseño sobrio y funcional, no hace falta Figma).
- Componentes: `shadcn/ui` (tabla, diálogo, formulario, toast, badge) sobre Tailwind.
- **Mobile first.** Cada pantalla se diseña primero para 375 px. Las tablas se convierten en tarjetas en móvil.
- La seguridad está en la API. El front solo oculta lo que el rol no puede usar.
- Todo en español, fechas en hora de Madrid.

## Autenticación en el cliente

- `AuthProvider` (contexto React) guarda el access token **en memoria** (estado + ref), nunca en `localStorage`/`sessionStorage`.
- Al montar `/admin`: `POST /api/auth/refresh` (la cookie `rt` va sola). Si 401 → `/admin/login?next=…`.
- Cliente `apiFetch` del backoffice:
  - Añade `Authorization: Bearer`.
  - Ante un 401: un **único** refresh en vuelo compartido por todas las peticiones (single-flight), reintenta la petición original una vez; si el refresh falla → logout local y login.
  - Refresco proactivo un minuto antes de `expiresIn`.
- Logout: `POST /api/auth/logout`, limpiar estado, ir a login.
- Las rutas `/admin/**` llevan `robots: noindex` y `Cache-Control: no-store`.
- Sincronización entre pestañas: `BroadcastChannel('auth')` para propagar el logout.

## Pantallas

### `/admin/login`
Email, contraseña, mostrar/ocultar contraseña. Error genérico "Email o contraseña incorrectos". 429 → "Demasiados intentos".

### `/admin` — Portada (`GET /api/admin/resumen`)
- Buscador grande arriba: "Buscar reserva por código, nombre o teléfono" → `/admin/reservas?q=`. En móvil es lo primero que se ve.
- Tarjetas: pendientes de pago (y cuántas caducan hoy), requieren revisión (en rojo si > 0), interesados sin contactar.
- Próximas ediciones con barra de ocupación (pagadas / pendientes / libres).

### `/admin/reservas` — Pendientes de pago (`GET /api/admin/reservas/pendientes`)
- Lista ordenada como devuelve la API. Cada tarjeta: código (grande), nombre, curso y fecha, importe, método, "caduca en 5 h" (rojo si < 6 h), teléfono como enlace `tel:` y botón WhatsApp (`wa.me/{telefono}`).
- Acciones (con diálogo de confirmación que repite código e importe):
  - **Confirmar pago** (solo manuales). Tras confirmar: toast "Pago confirmado, se ha enviado el email" y desaparece de la lista.
  - **Ampliar plazo** (selector 12 h / 24 h / 48 h / personalizado).
  - **Cancelar** (motivo opcional).
- Expiradas recientes en una sección aparte: "Caducadas (últimos 7 días)" con "Confirmar igualmente" si `plazaDisponible`, deshabilitado con explicación si no.
- `RequiereRevision` arriba en rojo con "Marcar como revisada" + nota obligatoria.
- 409 `concurrencia` → toast "Esta reserva acaba de cambiar" y recarga.

### `/admin/ediciones` — Ediciones (`GET /api/admin/ediciones`)
- Filtro por curso, interruptor "Mostrar pasadas".
- Fila/tarjeta: curso, fecha y hora, barra de ocupación, estado (badge), acciones.
- Botón "Nueva fecha".

### `/admin/ediciones/nueva` y `/admin/ediciones/[id]/editar`
- Campos: curso (selector), fecha, hora, plazas, estado (Programada / Oculta), notas.
- Fecha y hora se introducen en hora de Madrid y se envían como ISO con offset correcto (cuidado con el cambio de horario: usar `date-fns-tz` `fromZonedTime`).
- Errores 409 de F02:
  - `plazas_por_debajo_de_ocupadas` → mensaje en el campo plazas con el mínimo permitido.
  - `cambio_fecha_requiere_confirmacion` → diálogo "Esta edición tiene X inscritos, tendrás que avisarles. ¿Cambiar la fecha igualmente?" → reenviar con `confirmarCambioFecha: true`.
  - `concurrencia` → "Alguien ha modificado esta fecha. Recarga para ver los cambios".
- Cancelar edición: botón separado (zona de peligro) con diálogo que indica cuántos pagados y pendientes tiene, y que las pendientes se cancelarán.
- Borrar: solo visible si no tiene reservas.

### `/admin/ediciones/[id]` — Detalle (`GET /api/admin/ediciones/{id}/reservas`)
- Cabecera con curso, fecha, ocupación, estado y botón "Editar".
- Pestañas: Inscritos (pagadas) · Pendientes · Otras (expiradas/canceladas, cargadas bajo demanda).
- Inscritos: nombre, teléfono (`tel:`), email (`mailto:`), método, fecha de pago, código.
- Botón "Descargar asistentes (CSV)": descarga con `fetch` + token → `Blob` → enlace temporal (un `<a href>` directo no lleva el Bearer).

### `/admin/lista-espera` (`GET /api/admin/lista-espera`)
- Filtros: curso, "solo sin contactar" (activo por defecto), búsqueda.
- Tarjeta: nombre, teléfono con `tel:` y WhatsApp, email, curso, fecha preferida, antigüedad ("hace 12 días"), casilla "Contactado" (cambio optimista con rollback si falla), notas editables.
- Indicador "Se dio de baja de avisos" cuando aplique (F11).
- CSV por curso.

### `/admin/cursos` (solo SuperAdmin)
- Visible en el menú solo si `user.rol === 'SuperAdmin'`.
- Tabla y formulario con todos los campos de `Curso` (F02). Vista previa del Markdown de la descripción.

### `/admin/cuenta`
- Cambiar contraseña (`POST /api/auth/cambiar-password`).

### Solo SuperAdmin: `/admin/emails`
- Lista de tipos de email con vista previa en un iframe (`GET /api/admin/emails/preview/{tipo}`, F06), para revisarlos con la clienta.

## Navegación

- Escritorio: barra lateral (Inicio, Reservas, Fechas, Lista de espera, [Cursos], [Emails], Cuenta, Salir).
- Móvil: barra inferior con Inicio, Reservas, Fechas, Lista de espera; el resto en un menú.
- Contador de pendientes en el icono de Reservas.

## Estados comunes

- Carga: skeletons, no spinners a pantalla completa.
- Vacío: mensajes útiles ("No hay pagos pendientes 🎉" está bien aquí: es un panel interno).
- Error de red: banner con "Reintentar".
- Toda acción destructiva o irreversible pide confirmación.

## Tests

- **Unitarios:** `apiFetch` con single-flight (3 peticiones con 401 simultáneas → 1 refresh), conversión fecha local ↔ ISO en los dos cambios de horario anuales, tarjetas de reserva según estado.
- **E2E (Playwright)**, viewport móvil y escritorio:
  1. Login → crear edición → aparece en la web pública.
  2. Reserva manual desde la web → aparece en pendientes → buscar por código en minúsculas y sin guion → confirmar → pasa a inscritos.
  3. Editar plazas por debajo de ocupadas → error en el campo.
  4. Cambiar fecha con inscritos → diálogo de confirmación.
  5. Descargar CSV.
  6. Recargar la página sigue logueado (refresh por cookie); logout en una pestaña cierra la otra.
  7. Admin no ve "Cursos" y si navega a `/admin/cursos` la API responde 403 y se muestra "Sin permiso".

## Criterios de aceptación

- [ ] La clienta puede confirmar un pago desde el móvil en menos de 30 segundos desde que recibe el WhatsApp.
- [ ] El access token nunca toca el almacenamiento del navegador (comprobar en DevTools).
- [ ] Todas las pantallas usables a 375 px sin scroll horizontal.
- [ ] Probado por la clienta antes de la entrega (sesión de 20 minutos guiada).
