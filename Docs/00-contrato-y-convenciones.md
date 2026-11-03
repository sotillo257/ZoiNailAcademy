# 00 — Contrato de la API y convenciones comunes

Este documento es la referencia compartida por las 11 features. Si una spec y este documento no coinciden, manda este documento: corrige la spec.

## Convenciones generales

- **Idioma:** dominio, rutas y DTOs en español (`Curso`, `Edicion`, `Reserva`, `ListaEspera`). Código técnico (infra, helpers) en inglés si la plantilla ya lo usa así.
- **Rutas:** todas bajo `/api`. Públicas en `/api/...`, backoffice en `/api/admin/...`.
- **JSON:** `camelCase`. Enums serializados como string (`JsonStringEnumConverter`).
- **Fechas:** se guardan en UTC (`DateTimeOffset`, columna `timestamptz`). La API devuelve ISO 8601 con offset (`2026-12-05T10:00:00+00:00`). El front las muestra en `Europe/Madrid`. El backoffice envía la fecha local de Madrid convertida a ISO con offset.
- **Dinero:** `decimal(10,2)` en EUR. En JSON, número (`149.00`). En Stripe, céntimos (`14900`).
- **Reloj:** usar `TimeProvider` inyectado (nunca `DateTime.UtcNow` directo) para poder testear caducidades con `FakeTimeProvider`.
- **Errores:** `ProblemDetails` (RFC 7807) como ya hace la plantilla. Códigos de negocio en `extensions.code` (ver tabla de abajo).
- **Validación:** FluentValidation en el pipeline de MediatR. Error de validación → 400 con `errors` por campo.
- **Patrón por feature (el de Notes):** `Domain/Entities/X.cs` · `Application/<Feature>/Commands|Queries/*` (request + handler + validator) · `Infrastructure/Data/Configurations/XConfiguration.cs` · `Host/Controllers/XController.cs` · tests unitarios + integración.
- **Eventos de dominio:** las features de negocio publican `INotification` de MediatR **después del commit** (ver lista abajo). La F06 añade los handlers que envían emails. Hasta entonces, los eventos no tienen handler y no pasa nada.

## Códigos de error de negocio (`extensions.code`)

| Código | HTTP | Cuándo |
| --- | --- | --- |
| `sin_plazas` | 409 | No quedan plazas libres al reservar |
| `edicion_no_reservable` | 409 | Edición oculta, cancelada, empezada o dentro del cierre |
| `limite_pendientes` | 429 | El contacto ya tiene el máximo de reservas pendientes |
| `plazas_por_debajo_de_ocupadas` | 409 | Se intenta bajar `plazas` por debajo de pagadas + pendientes |
| `edicion_con_reservas` | 409 | Borrar una edición con reservas |
| `cambio_fecha_requiere_confirmacion` | 409 | Cambiar fecha de una edición con reservas sin `confirmarCambioFecha: true` |
| `estado_invalido` | 409 | Transición de estado no permitida (p. ej. confirmar una cancelada) |
| `concurrencia` | 409 | `xmin` no coincide al editar (otro cambio a la vez) |
| `curso_con_ediciones` | 409 | Desactivar/borrar un curso con ediciones futuras |
| `no_encontrado` | 404 | Recurso inexistente |

## Endpoints

### Auth (F01)

| Método | Ruta | Auth | Body → Respuesta |
| --- | --- | --- | --- |
| POST | `/api/auth/login` | No | `{email, password}` → `{accessToken, expiresIn, user{id,email,nombre,rol}}` + cookie `rt` |
| POST | `/api/auth/refresh` | Cookie `rt` | — → igual que login, rota la cookie |
| POST | `/api/auth/logout` | Cookie `rt` | — → 204, revoca y borra la cookie |
| GET | `/api/auth/me` | Bearer | — → `user` |
| POST | `/api/auth/cambiar-password` | Bearer | `{passwordActual, passwordNueva}` → 204 |
| GET | `/api/health` | No | Health check |

`/api/auth/register` **no existe**.

### Público

| Método | Ruta | Feature | Respuesta |
| --- | --- | --- | --- |
| GET | `/api/cursos` | F02 | `CursoResumenDto[]` |
| GET | `/api/cursos/{slug}` | F02 | `CursoDetalleDto` (incluye `ediciones`) |
| GET | `/api/cursos/{slug}/ediciones` | F02 | `EdicionPublicaDto[]` (siempre fresco, para el calendario) |
| POST | `/api/reservas/tarjeta` | F03 | `{codigo, checkoutUrl}` |
| POST | `/api/reservas/manual` | F04 | `ReservaManualCreadaDto` |
| GET | `/api/reservas/{codigo}` | F03 | `ReservaPublicaDto` (sin datos personales) |
| POST | `/api/reservas/{codigo}/abandonar` | F03 | 204 — libera la plaza si el comprador vuelve del Checkout sin pagar |
| POST | `/api/stripe/webhook` | F03 | 200 |
| POST | `/api/lista-espera` | F05 | 201 |
| POST | `/api/lista-espera/baja` | F11 | 204 (token en body JSON o en query, admite one-click de Gmail) |
| POST | `/api/contacto` | F06 | 202 |

### Backoffice (rol `Admin` o `SuperAdmin`)

| Método | Ruta | Feature |
| --- | --- | --- |
| GET | `/api/admin/cursos` | F02 (solo lectura, para selectores) |
| GET | `/api/admin/ediciones?cursoId=&incluirPasadas=` | F02 |
| GET | `/api/admin/ediciones/{id}` | F02 |
| POST | `/api/admin/ediciones` | F02 |
| PUT | `/api/admin/ediciones/{id}` | F02 |
| DELETE | `/api/admin/ediciones/{id}` | F02 |
| GET | `/api/admin/ediciones/{id}/reservas?incluir=` | F07 |
| GET | `/api/admin/ediciones/{id}/reservas.csv` | F07 |
| GET | `/api/admin/reservas/pendientes?q=&metodo=` | F07 |
| GET | `/api/admin/reservas/{id}` | F07 |
| POST | `/api/admin/reservas/{id}/confirmar` | F04 |
| POST | `/api/admin/reservas/{id}/cancelar` | F04 |
| POST | `/api/admin/reservas/{id}/ampliar` | F04 |
| POST | `/api/admin/reservas/{id}/marcar-revisada` | F07 |
| GET | `/api/admin/lista-espera?cursoId=&contactado=&q=` | F05 |
| PUT | `/api/admin/lista-espera/{id}/contactado` | F05 |
| GET | `/api/admin/lista-espera.csv?cursoId=` | F05 |
| GET | `/api/admin/resumen` | F07 (contadores para la portada del backoffice) |

### Solo `SuperAdmin`

| Método | Ruta | Feature |
| --- | --- | --- |
| GET | `/api/admin/cursos/{id}` | F02 (`CursoAdminDto` con todos los campos, para la pantalla de editar curso) |
| POST | `/api/admin/cursos` | F02 |
| PUT | `/api/admin/cursos/{id}` | F02 |
| DELETE | `/api/admin/cursos/{id}` | F02 (desactiva, no borra) |
| GET | `/api/admin/emails/preview/{tipo}` | F06 (HTML de ejemplo de cada email) |

## DTOs compartidos

```ts
type EstadoEdicionPublico = 'Disponible' | 'UltimasPlazas' | 'Completo';
type EstadoReserva = 'Pendiente' | 'Pagada' | 'Expirada' | 'Cancelada';
type MetodoPago = 'Stripe' | 'Manual';

interface CursoResumenDto {
  id: string; slug: string; nombre: string; descripcionCorta: string;
  precio: number;                 // precio total del curso
  precioReserva: number;          // lo que se paga al reservar (señal); el resto, en persona. = precio si se paga entero
  imagen: string; orden: number;
  ubicacion: string | null; duracionTexto: string | null;
  proximaEdicion: EdicionPublicaDto | null;
  ediciones: EdicionPublicaDto[]; // solo reservables o completas, futuras (para el calendario de la web)
}
interface CursoDetalleDto extends CursoResumenDto {
  descripcion: string;            // markdown o HTML saneado
}
interface EdicionPublicaDto {
  id: string; inicio: string;     // ISO con offset
  plazasTotales: number; plazasLibres: number;
  estado: EstadoEdicionPublico;   // UltimasPlazas si libres <= Reservas__UmbralUltimasPlazas
}
interface ReservaPublicaDto {
  codigo: string; estado: EstadoReserva; metodo: MetodoPago;
  curso: string; inicio: string;
  importe: number;                 // lo pagado (o por pagar) online: la señal
  precioCurso: number;             // precio total; precioCurso - importe se paga en persona
  expiraEn: string | null;
  datosPago: DatosPagoDto | null;  // solo si metodo=Manual y estado=Pendiente
  whatsappUrl: string | null;      // ídem
}
interface DatosPagoDto { titular: string; iban: string; bizumTelefono: string; concepto: string; }
interface ReservaManualCreadaDto extends ReservaPublicaDto {}
```

DTOs de backoffice: ver F02, F05 y F07.

## Eventos de dominio (MediatR `INotification`)

| Evento | Lo publica | Lo consume |
| --- | --- | --- |
| `ReservaPagadaEvent(reservaId)` | F03, F04 | F06 (email comprador + aviso clienta) |
| `ReservaManualCreadaEvent(reservaId)` | F04 | F06 |
| `ReservaExpiradaEvent(reservaId)` | F03, F04 | F06 |
| `ReservaCanceladaEvent(reservaId)` | F04 | — (v1 sin email) |
| `PagoTrasCaducidadSinPlazaEvent(reservaId)` | F03 | F06 (aviso clienta) |
| `ListaEsperaAltaEvent(listaEsperaId)` | F05 | F06 |
| `ContactoRecibidoEvent(...)` | F06 | F06 |
| `EdicionPublicadaEvent(edicionId)` | F02 | F11 |

## Variables de entorno

Lista completa en F10. Toda variable nueva que añada una feature se declara también en `appsettings.json` (sin secretos) y en `.env.example`.
