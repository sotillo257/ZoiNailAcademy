# F06 — Emails y formulario de contacto

**Revisión línea a línea:** No (revisar a mano los textos de los emails con la clienta).
**Depende de:** F03, F04, F05 (consume sus eventos).

## Objetivo

Enviar todos los emails transaccionales desde el dominio de la clienta con Resend, de forma fiable (con reintentos) y sin que un fallo de email rompa una reserva o un webhook. Incluye el formulario de contacto.

## Alcance

**Incluye**
- Abstracción `IEmailSender` con implementación Resend y una de desarrollo.
- Bandeja de salida persistida (`EmailSaliente`) con job de envío y reintentos.
- Handlers de los eventos de dominio → emails.
- Plantillas de los 9 emails del PRD.
- `POST /api/contacto`.

**No incluye**
- Configuración DNS del dominio en Resend (F10).
- Aviso de nuevas ediciones a la lista de espera (F11, reutiliza esta infraestructura).

## Arquitectura

```
Evento de dominio (post-commit)
   → Handler de F06 construye el email con la plantilla
   → INSERT en EmailSaliente (Pendiente)
EnviarEmailsJob (cada 30 s)
   → toma pendientes con ProximoIntentoEn <= ahora (máx. 20)
   → IEmailSender.SendAsync
   → Enviado | reintento con backoff | Fallido tras 5 intentos
```

Por qué una bandeja de salida: si Resend está caído o devuelve 429, el email no se pierde y el webhook de Stripe responde 200 igualmente.

### EmailSaliente
| Campo | Tipo |
| --- | --- |
| `Id` | Guid |
| `Para` | string(200) |
| `ReplyTo` | string(200)? |
| `Asunto` | string(300) |
| `Html`, `Texto` | text |
| `Tipo` | string(50) (p. ej. `ReservaPagada.Comprador`) |
| `ReferenciaId` | Guid? (reserva, lista de espera…) |
| `Estado` | `Pendiente`, `Enviado`, `Fallido` |
| `Intentos` | int |
| `ProximoIntentoEn` | DateTimeOffset |
| `UltimoError` | string(1000)? |
| `ProveedorId` | string(100)? (id de Resend) |
| `CreadoEn`, `EnviadoEn` | DateTimeOffset |

- Índice único `(Tipo, ReferenciaId)` donde `ReferenciaId` no es nulo → aunque un evento se publique dos veces, el email solo se encola una vez.
- Backoff: 1 min, 5 min, 15 min, 1 h, 4 h. Tras el 5.º fallo → `Fallido` y log de error.
- Resend admite cabecera `Idempotency-Key`: usar `EmailSaliente.Id`.
- Mismo advisory lock que el job de F04 si hubiera varias réplicas.

### `IEmailSender`
- `ResendEmailSender`: paquete NuGet oficial `Resend` (o `HttpClient` contra `POST https://api.resend.com/emails`). `From = Email__From` (p. ej. `Nombre Academia <reservas@dominio>`), `ReplyTo = Email__ReplyTo` (el email de la clienta).
- `LogEmailSender` (Development): escribe el email en el log y en `./emails-dev/{fecha}-{tipo}.html` para revisarlo en el navegador.
- Selección por `Email__Provider` (`Resend` | `Log`).

## Plantillas

- Clases C# por tipo (`ReservaPagadaCompradorEmail`, etc.) que reciben un modelo y devuelven `(asunto, html, texto)`.
- Un layout HTML común: logo (URL absoluta del front), color de marca, pie con nombre de la academia, dirección y enlaces legales. HTML de email "a la antigua": tablas, estilos inline, ancho máx. 600 px. Versión de texto plano siempre.
- Todos los datos del usuario escapados (`HtmlEncoder`).
- Fechas en hora de Madrid con formato largo: "sábado, 5 de diciembre de 2026, 10:00" (`CultureInfo("es-ES")`).
- Importes con `€` y coma decimal ("149,00 €").
- El código de reserva va en el asunto y destacado en el cuerpo.

| Tipo | Evento | Para | Asunto | Contenido |
| --- | --- | --- | --- | --- |
| `ReservaPagada.Comprador` | `ReservaPagadaEvent` | Comprador | `Reserva confirmada · {curso} · {codigo}` | Curso, fecha y hora, ubicación, importe, código, método, contacto de la clienta |
| `ReservaPagada.Clienta` | `ReservaPagadaEvent` | `Email__AvisosClienta` | `Nueva inscripción · {curso} {fecha corta} · {codigo}` | Nombre, teléfono, email, método, importe, plazas libres restantes, enlace al detalle de la edición en el backoffice |
| `ReservaManual.Comprador` | `ReservaManualCreadaEvent` | Comprador | `Completa tu pago · {codigo}` | Datos bancarios y Bizum, concepto = código, importe, fecha límite (`ExpiraEn`), botón de WhatsApp, enlace a `/reserva/{codigo}` |
| `ReservaManual.Clienta` | `ReservaManualCreadaEvent` | Clienta | `Reserva pendiente de pago · {codigo}` | Datos del comprador, caducidad, enlace a pendientes del backoffice |
| `ReservaExpirada.Comprador` | `ReservaExpiradaEvent` | Comprador | `Tu reserva ha caducado · {codigo}` | Solo para **método Manual** (en Stripe, el comprador simplemente no pagó: no enviar). Plaza liberada, enlace para reservar de nuevo, aviso: "si ya pagaste, contesta a este email o escríbenos por WhatsApp" |
| `PagoSinPlaza.Clienta` | `PagoTrasCaducidadSinPlazaEvent` | Clienta | `⚠ Pago recibido sin plaza · {codigo}` | Datos del comprador, importe, id del pago en Stripe, instrucción de reembolsar |
| `ListaEspera.Clienta` | `ListaEsperaAltaEvent` | Clienta | `Nuevo interesado · {curso}` | Datos del interesado y fecha preferida |
| `Contacto.Clienta` | `POST /api/contacto` | Clienta | `Mensaje de la web · {nombre}` | Mensaje, con `ReplyTo` = email del remitente |
| `Contacto.Acuse` | `POST /api/contacto` | Remitente | `Hemos recibido tu mensaje` | Opcional (`Email__AcuseContacto=true`). Por defecto desactivado para no ser usado como relé de spam |

`ReservaCanceladaEvent` no envía email en v1.

> Los textos finales se revisan con la clienta. Dejar un endpoint de previsualización solo para SuperAdmin: `GET /api/admin/emails/preview/{tipo}` que devuelve el HTML con datos de ejemplo. Útil para enseñarle los emails antes de lanzar.

## Formulario de contacto

### `POST /api/contacto`
```json
{ "nombre": "…", "email": "…", "telefono": "…", "mensaje": "…",
  "aceptaPrivacidad": true, "website": "" }
```
- Rate limit `formularios`, honeypot.
- `mensaje` 10–3000 caracteres; `telefono` opcional.
- No se guarda en base de datos como entidad de negocio: se encola directamente el email `Contacto.Clienta` (sí queda en `EmailSaliente`, que es temporal).
- → 202.

### Limpieza
`EmailSaliente` con estado `Enviado` y más de 90 días se borra en el mismo job (una vez al día). Reduce datos personales almacenados (RGPD).

## Configuración

`Email__Provider` (`Resend`/`Log`), `Resend__ApiKey`, `Email__From`, `Email__ReplyTo`, `Email__AvisosClienta`, `Email__AcuseContacto` (false), `App__BaseUrl`, `App__NombreAcademia`, `App__DireccionAcademia`.

## Tests

**Unitarios**
- Cada plantilla con datos de ejemplo: contiene código, fecha en español, importe con coma; HTML escapa `<script>` en el nombre.
- Backoff de reintentos.

**Integración**
- Pagar una reserva (evento) → 2 filas en `EmailSaliente` (comprador y clienta). Publicar el evento otra vez → siguen siendo 2.
- Expirar una reserva Stripe → 0 emails al comprador; manual → 1.
- Job con un `IEmailSender` falso que falla 2 veces → `Enviado` al tercer intento; que falla siempre → `Fallido` tras 5.
- Contacto → 202 y email encolado con `ReplyTo` del remitente.
- Preview solo accesible con SuperAdmin.

**Manual**
- Enviar cada tipo a Gmail y a Outlook y comprobar que llega a la bandeja de entrada (no a spam), se ve bien en móvil y los enlaces apuntan al dominio de producción.

## Criterios de aceptación

- [ ] Todos los emails de la tabla se envían en los momentos indicados y nunca duplicados.
- [ ] Un fallo de Resend no rompe reservas ni webhooks y se reintenta.
- [ ] La clienta ha revisado y aprobado los textos (preview).
- [ ] Los emails llegan a la bandeja de entrada desde el dominio verificado.
