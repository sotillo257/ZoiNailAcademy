# F11 — Extra: aviso de nuevas ediciones a la lista de espera

**Revisión línea a línea:** No.
**Depende de:** F02 (`EdicionPublicadaEvent`), F05 (lista de espera, consentimiento y `BajaToken`), F06 (bandeja de salida de emails).
**Prioridad:** solo si da tiempo. Se entrega como **regalo** y se comunica explícitamente a la clienta como tal.

## Objetivo

Cuando la clienta publica una nueva fecha de un curso, avisar por email automáticamente a quienes están en la lista de espera de ese curso, sin duplicados y con opción de darse de baja.

## Alcance

**Incluye**
- Handler de `EdicionPublicadaEvent` que encola los avisos.
- Tabla `AvisoEnviado` para no repetir.
- Plantilla de email del aviso.
- Baja de avisos: `POST /api/lista-espera/baja` + página `/baja` (F08).
- Indicador de baja en el backoffice (F09).

**No incluye**
- Avisos por WhatsApp o SMS.
- Programar el envío para más tarde o elegir destinatarios a mano.

## Modelo de datos

### AvisoEnviado
| Campo | Tipo |
| --- | --- |
| `ListaEsperaId` | Guid, FK |
| `EdicionId` | Guid, FK |
| `EncoladoEn` | DateTimeOffset |

PK compuesta `(ListaEsperaId, EdicionId)`.

## Reglas

- **Disparo:** `EdicionPublicadaEvent` (al crear una edición `Programada` o al pasarla de `Oculta` a `Programada`; F02).
- **Destinatarios:** registros de `ListaEspera` del mismo curso con `BajaEn == null` y `ConsentimientoEn != null`. Se incluyen también los ya contactados (puede que no les encajara la fecha anterior).
- **Sin duplicados:** se inserta en `AvisoEnviado` antes de encolar; si ya existe el par, se salta. Así, ocultar y volver a publicar la misma edición no reenvía.
- **Agrupación:** si la clienta publica varias fechas del mismo curso en poco tiempo, no mandar un email por fecha. El handler no encola en el momento: marca el curso como "pendiente de aviso" y un job (`AvisosNuevasEdicionesJob`, cada 10 min) envía **un único email por persona** con todas las ediciones publicadas de ese curso que aún no se le hayan avisado y sigan reservables.
- **Edición ya no reservable** cuando corre el job (cancelada, oculta, completa o dentro del cierre) → no se incluye. Si una persona se queda sin ninguna edición que avisar, no se le envía nada.
- **Envío:** a través de la bandeja de F06 (`Tipo = AvisoNuevasEdiciones`, `ReferenciaId` = id de la lista de espera + hash de las ediciones, para la idempotencia). En lotes de `Avisos__LoteTamano` con pausa `Avisos__PausaEntreLotesSegundos`, para no chocar con el rate limit de Resend.
- La clienta recibe un resumen: `Aviso enviado a {N} interesados · {curso}`.

## Email

- **Asunto:** `Nuevas fechas para {curso}`.
- **Cuerpo:** saludo con nombre, lista de fechas (día, hora, plazas libres en el momento del envío, aclarando que pueden agotarse), botón "Reservar mi plaza" → `/reservar/{slug}`, recordatorio de que se apuntó a la lista de espera de este curso.
- **Pie:** enlace de baja `{App__BaseUrl}/baja?token={BajaToken}&curso={slug}` (`curso` permite a la página nombrar el curso, porque el endpoint responde 204 sin cuerpo) y cabeceras `List-Unsubscribe: <{url de baja}>` y `List-Unsubscribe-Post: List-Unsubscribe=One-Click` (mejora la entregabilidad en Gmail).

## Baja

### `POST /api/lista-espera/baja`
Body `{ token }`. Rate limit `formularios`.
- Token válido → `BajaEn = ahora` (idempotente: si ya estaba de baja, igual 204).
- Token inválido → 204 igualmente (no revelar si existe).
- Afecta solo a **ese** registro (ese curso). El texto de la página lo dice: "Ya no te avisaremos de nuevas fechas de {curso}".

### One-click (`List-Unsubscribe-Post`)
Gmail hace `POST` a la URL de baja con body `List-Unsubscribe=One-Click`. La URL de la cabecera debe apuntar a un endpoint que acepte ese POST sin JavaScript: `POST /api/lista-espera/baja?token=…` (aceptar el token también por query y el body form-urlencoded).

### Página `/baja?token=` (F08)
Pide confirmación con un botón ("Darme de baja de los avisos") y luego llama al endpoint. No dar de baja con un simple `GET` (los antivirus de correo abren los enlaces y darían de baja a la gente sin querer).

### Backoffice (F09)
Marca "Se dio de baja de avisos" en la tarjeta. La clienta puede seguir llamando a esa persona (la baja es del email automático); si un nuevo alta en la lista de espera del mismo curso llega con el mismo email, F05 ya limpia `BajaEn`.

## Configuración

`Avisos__LoteTamano` (20), `Avisos__PausaEntreLotesSegundos` (2), `Avisos__JobIntervaloMinutos` (10).

## Tests

**Integración**
- Publicar una edición → tras el job, un email por interesado del curso, ninguno a otros cursos, ninguno a los dados de baja.
- Publicar 3 fechas del mismo curso antes del job → un único email por persona con las 3.
- Ocultar y volver a publicar → no reenvía.
- Edición cancelada antes de que corra el job → no se avisa.
- Baja con token válido e inválido → 204 en ambos; solo el válido cambia el registro.
- One-click con body form-urlencoded → baja correcta.
- Email contiene `List-Unsubscribe` y el enlace de baja.

## Criterios de aceptación

- [ ] Publicar una fecha avisa a la lista de espera de ese curso en menos de 15 minutos.
- [ ] Nadie recibe dos avisos por la misma fecha.
- [ ] La baja funciona desde el enlace y desde el botón nativo de Gmail.
- [ ] La clienta sabe que esta función es un regalo (dicho por escrito en la entrega).
