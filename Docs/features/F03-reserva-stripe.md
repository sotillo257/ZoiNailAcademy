# F03 — Reserva con tarjeta (Stripe Checkout)

**Revisión línea a línea:** Sí. Es la feature con más riesgo: dinero, concurrencia e idempotencia.
**Depende de:** F01, F02.

## Objetivo

Que el comprador elija una edición, pague con tarjeta en Stripe Checkout y quede inscrito, sin que nunca se venda más de una plaza de las que hay y sin procesar dos veces el mismo pago.

## Alcance

**Incluye**
- Entidad `Reserva` (compartida con F04) y `StripeEvento`.
- Servicio `CrearReservaService`: la parte común a tarjeta y manual (bloqueo, comprobación de plazas, límite por contacto, código único).
- `POST /api/reservas/tarjeta`: crea reserva Pendiente + sesión de Checkout.
- `POST /api/stripe/webhook`: confirma o expira.
- `GET /api/reservas/{codigo}`: estado público para la página de éxito/instrucciones.
- `POST /api/reservas/{codigo}/abandonar`: libera la plaza si vuelve del Checkout sin pagar.

**No incluye**
- Reembolsos (manuales desde el panel de Stripe).
- Métodos de pago asíncronos (SEPA, etc.). Solo tarjeta (y wallets que Stripe muestre en Checkout, que son síncronos).
- Emails (F06; aquí solo se publican eventos).

## Modelo de datos

### Reserva
| Campo | Tipo | Reglas |
| --- | --- | --- |
| `Id` | Guid | PK |
| `Codigo` | string(10) | Único. Formato `CUR-XXXXXX` |
| `EdicionId` | Guid | FK, `Restrict` |
| `Metodo` | enum | `Stripe`, `Manual` |
| `Estado` | enum | `Pendiente`, `Pagada`, `Expirada`, `Cancelada` |
| `Nombre` | string(120) | Obligatorio |
| `Email` | string(200) | Obligatorio, normalizado a minúsculas |
| `Telefono` | string(20) | Obligatorio, normalizado E.164 (`+34612345678`) |
| `Importe` | decimal(10,2) | Copia de `Curso.Precio` al crear |
| `StripeSessionId` | string(100)? | Índice único (filtrado no nulo) |
| `StripePaymentIntentId` | string(100)? | Para localizar el pago en Stripe |
| `ExpiraEn` | DateTimeOffset | |
| `PagadaEn` | DateTimeOffset? | |
| `CanceladaEn`, `ExpiradaEn` | DateTimeOffset? | |
| `RequiereRevision` | bool | Pago recibido sin plaza disponible |
| `NotaInterna` | string(500)? | |
| `ConsentimientoEn` | DateTimeOffset | Aceptación de condiciones y privacidad |
| `CreadaEn`, `ActualizadaEn` | DateTimeOffset | |
| `xmin` | uint | Concurrencia |

Índices: `(EdicionId, Estado)`, `(Email, Estado)`, `(Telefono, Estado)`, `(Estado, ExpiraEn)` para el job de F04.

### StripeEvento
| Campo | Tipo |
| --- | --- |
| `EventId` | string(100), PK |
| `Tipo` | string(100) |
| `ProcesadoEn` | DateTimeOffset |

### Código de reserva
- `CUR-` + 6 caracteres del alfabeto `23456789ABCDEFGHJKMNPQRSTUVWXYZ` (sin 0/O, 1/I/L).
- Generado con `RandomNumberGenerator`. Reintentar si colisiona con el índice único (máximo 5 intentos).
- No es secuencial ni adivinable: es lo único que identifica la reserva en las URLs públicas.

## `CrearReservaService` (común con F04)

Entrada: `edicionId`, `nombre`, `email`, `telefono`, `metodo`, `expiraEn`.

1. Normalizar email (trim + minúsculas) y teléfono (`libphonenumber-csharp`, región por defecto `ES`; inválido → 400).
2. Abrir transacción.
3. **Bloquear la fila de la edición:** `SELECT ... FROM "Ediciones" WHERE "Id" = @id FOR UPDATE` (SQL crudo vía `FromSqlInterpolated` o `ExecuteSql`). Esto serializa las reservas concurrentes de la **misma** edición sin bloquear las demás.
4. Comprobar que es reservable (F02) → si no, 409 `edicion_no_reservable`.
5. Comprobar límite por contacto: pendientes vigentes (cualquier edición) con el mismo email **o** teléfono ≥ `Reservas__MaxPendientesPorContacto` → 429 `limite_pendientes`.
6. Calcular plazas libres con `PlazasCalculator` → si 0, 409 `sin_plazas`.
7. Insertar la reserva en `Pendiente` con código, importe y `ExpiraEn`.
8. Commit.

> Por qué bloqueo pesimista y no `xmin`: insertar una reserva no modifica la fila de la edición, así que la concurrencia optimista sobre `Edicion` no detecta el conflicto. `xmin` se sigue usando para las ediciones del backoffice (F02).

## Flujo de pago con tarjeta

### `POST /api/reservas/tarjeta`
Rate limit: política `reservas`.

Request:
```json
{ "edicionId": "…", "nombre": "…", "email": "…", "telefono": "…",
  "aceptaCondiciones": true, "website": "" }
```
- `website` es un **honeypot**: si viene relleno → responder 200 falso con un código inventado y no crear nada (y log de warning).
- `aceptaCondiciones` debe ser `true`.

Pasos:
1. `CrearReservaService` con `metodo = Stripe` y `ExpiraEn = ahora + Reservas__StripeMinutosBloqueo` (mínimo 30, Stripe no admite menos) **+ 5 min de margen** para que el webhook de expiración llegue antes que el job de F04.
2. Crear la sesión de Checkout (`Stripe.net`, `SessionService.CreateAsync`):
   - `Mode = "payment"`, `Currency = "eur"`.
   - `LineItems`: una línea con `PriceData` (`UnitAmount = Importe × 100`, `ProductData.Name = "{Curso} — {fecha y hora en Madrid}"`), cantidad 1.
   - `CustomerEmail = email`.
   - `ClientReferenceId = reserva.Id`.
   - `Metadata = { reservaId, codigo }` y `PaymentIntentData.Metadata` igual.
   - `ExpiresAt = ahora + StripeMinutosBloqueo` (sin el margen).
   - `SuccessUrl = {App__BaseUrl}/reserva/{codigo}?origen=stripe`.
   - `CancelUrl = {App__BaseUrl}/reservar/{slug}?abandonada={codigo}`.
   - `Locale = "es"`.
   - Cabecera de idempotencia de Stripe: `IdempotencyKey = "checkout-" + reserva.Id`.
3. Guardar `StripeSessionId` en la reserva.
4. Si Stripe falla → marcar la reserva `Cancelada` (libera la plaza) y responder 502 con `ProblemDetails` "No se pudo iniciar el pago, inténtalo de nuevo".
5. Responder `{ codigo, checkoutUrl }`. El front redirige a `checkoutUrl`.

### `POST /api/stripe/webhook`
- Sin auth, sin rate limit, excluido del antiforgery.
- Leer el **body crudo** (`Request.EnableBuffering()` + `StreamReader`) y verificar con `EventUtility.ConstructEvent(json, Request.Headers["Stripe-Signature"], Stripe__WebhookSecret)`. Firma inválida → 400.
- **Idempotencia:** en una transacción, insertar `StripeEvento(EventId)`. Si viola la PK → ya procesado → 200 sin hacer nada.
- Eventos tratados:

| Evento | Acción |
| --- | --- |
| `checkout.session.completed` con `payment_status == "paid"` | Buscar reserva por `StripeSessionId` (o `client_reference_id`). Ver tabla de estados abajo |
| `checkout.session.expired` | Si la reserva está `Pendiente` → `Expirada`, publicar `ReservaExpiradaEvent` |
| Cualquier otro | 200 y no hacer nada |

Al completar:

| Estado actual | Acción |
| --- | --- |
| `Pendiente` | → `Pagada`, `PagadaEn`, `StripePaymentIntentId`. Publicar `ReservaPagadaEvent` |
| `Pagada` | Nada (duplicado lógico) |
| `Expirada` o `Cancelada` | Bloquear la edición (`FOR UPDATE`) y comprobar plazas. Si hay → `Pagada` y `ReservaPagadaEvent`. Si no → queda `Expirada`/`Cancelada` con `RequiereRevision = true` y se publica `PagoTrasCaducidadSinPlazaEvent` (la clienta reembolsa desde Stripe) |

- Reserva no encontrada → log de error con el `session.id` y 200 (si devolvemos error, Stripe reintenta para siempre).
- Excepción inesperada → 500 para que Stripe reintente (la transacción revierte también el `StripeEvento`).
- Los eventos MediatR se publican **después** del commit; si un handler de email falla no afecta a la respuesta.

### `POST /api/reservas/{codigo}/abandonar`
- El front lo llama al volver a `cancel_url`.
- Si la reserva es `Stripe` y `Pendiente` → expirar la sesión en Stripe (`SessionService.ExpireAsync`) y marcar `Cancelada`. Si Stripe dice que la sesión ya está completada, no tocar nada (llegará el webhook).
- Cualquier otro estado → 204 sin cambios. Rate limit `reservas`.

### `GET /api/reservas/{codigo}`
- Devuelve `ReservaPublicaDto` (contrato). **Sin** nombre, email ni teléfono.
- 404 si no existe.
- La página de éxito lo consulta con reintentos cortos, porque el webhook puede llegar unos segundos después de la redirección (estado todavía `Pendiente` → mostrar "Confirmando tu pago…").

## Webhook a través del proxy de Next

El webhook entra por el dominio del front y Next lo reenvía a la API (`/api/*`). Hay que comprobar que la firma se verifica:
- El rewrite de Next no debe modificar el body. Probar con `stripe trigger checkout.session.completed` contra el entorno desplegado.
- Si falla, plan B (F10): dominio público del backend y webhook directo a `https://api.<dominio>/api/stripe/webhook`. En ese caso, revisar `ForwardedHeaders` de F01 (no confiar en cualquier `X-Forwarded-For`).

## Configuración

`Stripe__SecretKey`, `Stripe__WebhookSecret`, `Reservas__StripeMinutosBloqueo` (30), `Reservas__MaxPendientesPorContacto` (2), `App__BaseUrl`.

## Tests

**Unitarios**
- Generador de códigos: formato, alfabeto, sin caracteres ambiguos.
- Normalización de teléfono: `612 34 56 78`, `+34612345678`, `0034612345678` → `+34612345678`; inválidos → error.
- Transiciones de estado de `Reserva` (método de dominio por transición; transiciones no válidas lanzan excepción de dominio → 409 `estado_invalido`).

**Integración** (Stripe sustituido por un `IStripeGateway` falso; el webhook se prueba con payloads firmados con un secreto de test usando `EventUtility`/firma HMAC generada en el test)
- Crear reserva con plazas → 200 y reserva `Pendiente` con `StripeSessionId`.
- **Concurrencia:** edición con 1 plaza, 10 peticiones en paralelo → exactamente 1 reserva creada, 9 × 409 `sin_plazas`.
- Edición no reservable → 409. Honeypot relleno → no crea nada.
- Límite por contacto: tercera pendiente con el mismo teléfono → 429.
- Fallo de Stripe → reserva `Cancelada` y 502.
- Webhook con firma inválida → 400.
- `completed` → `Pagada` y evento publicado una sola vez; mismo evento reenviado → 200 y nada cambia.
- `expired` → `Expirada`; plaza liberada.
- `completed` sobre `Expirada` con plaza → `Pagada`; sin plaza → `RequiereRevision` y evento de revisión.
- `abandonar` sobre pendiente → `Cancelada`; sobre pagada → sin cambios.
- `GET /api/reservas/{codigo}` no expone datos personales.

**Manual (Stripe en modo test, ver F10)**
- Pago completo con `4242 4242 4242 4242`.
- Pago rechazado con `4000 0000 0000 0002` → la reserva sigue `Pendiente` y expira.
- Cerrar la pestaña del Checkout → expira a los 30 min y la plaza vuelve.
- `stripe trigger` a través del dominio del front.

## Criterios de aceptación

- [ ] Imposible sobrevender (test de concurrencia en verde).
- [ ] Cada pago genera exactamente un `ReservaPagadaEvent`.
- [ ] Una plaza abandonada vuelve al calendario como máximo a los 35 minutos (o al instante si el comprador pulsa "volver").
- [ ] La verificación de firma funciona en el entorno desplegado.
- [ ] Ni el código ni los logs guardan datos de tarjeta (Stripe Checkout los gestiona; nosotros nunca los vemos).
