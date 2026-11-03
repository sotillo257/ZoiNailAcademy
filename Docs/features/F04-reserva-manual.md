# F04 — Reserva por transferencia o Bizum

**Revisión línea a línea:** Sí (concurrencia del job y de las acciones de la clienta).
**Depende de:** F03 (`Reserva`, `CrearReservaService`).

## Objetivo

Permitir pagar por transferencia o Bizum: la plaza queda bloqueada con un código único, el comprador envía el comprobante por WhatsApp y la clienta confirma el pago desde el backoffice. Si nadie confirma a tiempo, la plaza se libera sola.

## Alcance

**Incluye**
- `POST /api/reservas/manual`.
- Job en segundo plano que expira las reservas pendientes vencidas (manuales y, como red de seguridad, las de Stripe).
- Acciones de la clienta: confirmar pago, cancelar, ampliar caducidad.
- Cancelación en cascada de las pendientes cuando se cancela una edición (TODO dejado en F02).

**No incluye**
- Conciliación bancaria automática.
- Subida del comprobante a la web (va por WhatsApp).
- Listados del backoffice (F07).

## Flujo

### `POST /api/reservas/manual`
Rate limit: política `reservas`. Mismo request que tarjeta (incluye honeypot `website` y `aceptaCondiciones`).

1. `CrearReservaService` con `metodo = Manual` y `ExpiraEn = ahora + Reservas__ManualHorasCaducidad` horas.
   - Si con esa caducidad la reserva expiraría **después** del cierre de la edición (`Inicio - CierreHorasAntes`), `ExpiraEn` se recorta al momento del cierre. Si queda menos de 2 horas hasta el cierre → 409 `edicion_no_reservable` con el mensaje "Para esta fecha solo se puede pagar con tarjeta".
2. Publicar `ReservaManualCreadaEvent` (email al comprador con los datos de pago + aviso a la clienta; F06).
3. Responder `ReservaManualCreadaDto` (= `ReservaPublicaDto`) con:
   - `datosPago`: `{ titular: Pago__Titular, iban: Pago__Iban, bizumTelefono: Pago__BizumTelefono, concepto: codigo }`.
   - `whatsappUrl`: `https://wa.me/{Pago__WhatsAppTelefono}?text={urlencode(mensaje)}` con el mensaje:
     `Hola, envío el comprobante de la reserva {codigo} ({curso}, {fecha dd/MM/yyyy HH:mm}). Importe: {importe} €.`
   - `expiraEn`.

`GET /api/reservas/{codigo}` (F03) devuelve los mismos `datosPago` y `whatsappUrl` mientras la reserva siga `Pendiente`, para que la página de instrucciones se pueda recargar o abrir desde el email.

### Job de caducidad (`ExpirarReservasJob`)
- `BackgroundService` con `PeriodicTimer` cada `Reservas__JobIntervaloMinutos` (5).
- Cada ciclo crea un scope y envía `ExpirarReservasCaducadasCommand` por MediatR.
- El command:
  1. Selecciona hasta 100 reservas `Pendiente` con `ExpiraEn <= ahora`.
  2. Para cada una, en su propia transacción: si sigue `Pendiente` (comprobar con `xmin`) → `Expirada`, `ExpiradaEn = ahora`.
     - Si es de Stripe: llamar antes a `SessionService.ExpireAsync`. Si Stripe responde que la sesión ya está completada → **no** expirar (llegará el webhook de pago).
  3. Publicar `ReservaExpiradaEvent` por cada una expirada.
- Errores de una reserva no paran el lote (log y siguiente).
- Supuesto: **una sola réplica** de la API. Si algún día hay más, usar un advisory lock de Postgres (`pg_try_advisory_lock`) al inicio del ciclo. Dejarlo ya implementado cuesta poco: hacerlo.
- El cálculo de plazas libres ya ignora pendientes caducadas, así que aunque el job vaya con retraso, la plaza vuelve al calendario en cuanto pasa `ExpiraEn`. El job solo sirve para dejar el estado coherente y enviar el email.

### Acciones de la clienta (rol Backoffice)

`POST /api/admin/reservas/{id}/confirmar` body `{ version, nota? }`
| Estado actual | Resultado |
| --- | --- |
| `Pendiente` (Manual) | → `Pagada`, `PagadaEn`, publica `ReservaPagadaEvent` |
| `Expirada` (Manual) | Bloquear edición (`FOR UPDATE`). Si hay plaza → `Pagada` + evento. Si no → 409 `sin_plazas` con mensaje "La plaza ya se ha ocupado; habla con el comprador" |
| `Pendiente` o `Expirada` de **Stripe** | 409 `estado_invalido` ("Los pagos con tarjeta se confirman solos") |
| `Pagada` | 200 idempotente, sin evento |
| `Cancelada` | 409 `estado_invalido` |

`POST /api/admin/reservas/{id}/cancelar` body `{ version, motivo? }`
- `Pendiente` (cualquier método; si es Stripe, expirar también la sesión) o `Expirada` → `Cancelada`. Publica `ReservaCanceladaEvent`.
- `Pagada` → permitido solo con `confirmarPagada: true` (devolución gestionada fuera). Respuesta con aviso "Recuerda reembolsar el pago". Sin esa marca → 409 `estado_invalido`.

`POST /api/admin/reservas/{id}/ampliar` body `{ version, horas }`
- Solo `Pendiente` y `Manual`. `horas` entre 1 y 168.
- `ExpiraEn = max(ExpiraEn, ahora) + horas`, recortado al cierre de la edición.
- También permitido sobre `Expirada` manual si hay plaza (la reactiva a `Pendiente`; bloquear edición).

Todas las acciones usan `version` (`xmin`) → 409 `concurrencia` si otro proceso (el job) la cambió a la vez. El front recarga y muestra el estado actual.

### Cancelación de una edición (cierra el TODO de F02)
Al pasar una edición a `Cancelada`, todas sus reservas `Pendiente` pasan a `Cancelada` en la misma transacción (expirando sesiones de Stripe fuera de la transacción, después). Las `Pagada` no se tocan: la respuesta avisa de cuántas hay.

## Configuración

`Reservas__ManualHorasCaducidad` (48), `Reservas__JobIntervaloMinutos` (5), `Pago__Titular`, `Pago__Iban`, `Pago__BizumTelefono`, `Pago__WhatsAppTelefono` (formato internacional sin `+`, p. ej. `34612345678`).

Pregunta abierta del PRD: si la clienta quiere cambiar los datos bancarios ella misma, habría que moverlos a una tabla de configuración editable desde el backoffice. En v1, variables de entorno.

## Tests

**Unitarios**
- Construcción de `whatsappUrl` (codificación de acentos, `€` y espacios).
- Recorte de `ExpiraEn` al cierre de la edición; rechazo si quedan menos de 2 h.
- Matriz de transiciones de confirmar / cancelar / ampliar.

**Integración** (con `FakeTimeProvider`)
- Crear manual → `Pendiente`, `datosPago` y `whatsappUrl` presentes; `GET /api/reservas/{codigo}` los devuelve.
- Avanzar el reloj más allá de `ExpiraEn` → la plaza vuelve a contar como libre **antes** de que pase el job.
- Ejecutar el command del job → `Expirada` y un `ReservaExpiradaEvent` por reserva.
- Job y confirmación a la vez sobre la misma reserva → una gana y la otra recibe 409 `concurrencia`; nunca queda `Pagada` y `Expirada` a la vez ni dos eventos.
- Confirmar expirada con plaza → `Pagada`; sin plaza → 409.
- Confirmar una de Stripe → 409.
- Ampliar → `ExpiraEn` correcto y recortado al cierre.
- Cancelar edición → sus pendientes quedan `Cancelada`.
- Dos réplicas simuladas del job en paralelo → cada reserva se expira una sola vez (advisory lock).

## Criterios de aceptación

- [ ] El comprador ve los datos de pago, el código y el botón de WhatsApp con el mensaje ya escrito, y los recibe también por email.
- [ ] La clienta encuentra la reserva por código y la confirma en dos toques desde el móvil (UI en F09).
- [ ] Las reservas no pagadas liberan su plaza solas al caducar.
- [ ] Ninguna carrera entre el job y la clienta produce estados incoherentes o emails duplicados.
