# F07 — API del backoffice (consultas)

**Revisión línea a línea:** No.
**Depende de:** F02, F03, F04, F05.

## Objetivo

Dar a la clienta lo que necesita consultar en su día a día: cuántos inscritos tiene cada edición y quiénes son, qué reservas están pendientes de pago (buscándolas por el código que le llega por WhatsApp), y una portada con lo urgente.

## Alcance

**Incluye**
- Portada con contadores (`/api/admin/resumen`).
- Reservas de una edición + CSV.
- Listado y búsqueda de reservas pendientes.
- Detalle de una reserva.

**No incluye**
- Las acciones sobre reservas (confirmar, cancelar, ampliar: F04) ni el CRUD de ediciones (F02) ni la lista de espera (F05).
- Estadísticas de ventas o gráficos (fuera de v1).

## Endpoints

Todos con política `Backoffice`.

### `GET /api/admin/resumen`
```ts
interface ResumenDto {
  pendientesDePago: number;          // manuales Pendiente vigentes
  pendientesQueCaducanHoy: number;   // ExpiraEn antes de las 23:59 de hoy (Madrid)
  requierenRevision: number;         // RequiereRevision = true y sin resolver
  listaEsperaSinContactar: number;
  proximasEdiciones: EdicionAdminDto[]; // las 5 siguientes (F02)
}
```

### `GET /api/admin/ediciones/{id}/reservas?incluir=pagadas,pendientes`
```ts
interface ReservaAdminDto {
  id: string; codigo: string; edicionId: string; curso: string; inicio: string;
  metodo: 'Stripe' | 'Manual'; estado: EstadoReserva;
  nombre: string; email: string; telefono: string;
  importe: number; creadaEn: string; pagadaEn: string | null; expiraEn: string;
  requiereRevision: boolean; notaInterna: string | null;
  stripePaymentIntentId: string | null;
  version: string;
}
```
- Por defecto `pagadas,pendientes` (las que ocupan plaza). Admite también `expiradas,canceladas`.
- Orden: pagadas por `PagadaEn`, luego pendientes por `ExpiraEn`.
- Respuesta `{ edicion: EdicionAdminDto, reservas: ReservaAdminDto[] }` (sin paginar: máx. 200 plazas por edición).

### `GET /api/admin/ediciones/{id}/reservas.csv`
- Solo `Pagada` (es la lista de asistentes).
- Helper CSV de F05 (BOM, `;`, anti-fórmulas).
- Columnas: Código; Nombre; Teléfono; Email; Método; Importe; Fecha de pago.
- Archivo: `asistentes-{slug}-{yyyyMMdd-HHmm de la edición}.csv`.

### `GET /api/admin/reservas/pendientes?q=&metodo=`
- Reservas `Pendiente` vigentes **y** las `Expirada` de método Manual de los últimos 7 días (para cuando el comprador manda el comprobante tarde y la clienta necesita encontrarla y confirmarla si queda plaza), más las que tienen `RequiereRevision`.
- `q`:
  - Si parece un código (`CUR-` opcional + 6 caracteres del alfabeto), búsqueda exacta por código normalizado (mayúsculas, sin espacios, admite que escriban `cur7k4q2m` o `7K4Q2M`).
  - Si no, contiene en nombre, email o teléfono (teléfono comparando solo dígitos, para que `612345678` encuentre `+34612345678`).
- Orden: `RequiereRevision` primero, luego pendientes por `ExpiraEn` ascendente (las que caducan antes, arriba), luego expiradas recientes.
- Respuesta `ReservaAdminDto[]` con un campo extra `plazaDisponible: boolean` para las expiradas (si se podría confirmar todavía).

### `GET /api/admin/reservas/{id}`
- `ReservaAdminDto` de cualquier estado.
- Para la búsqueda directa por código desde la portada: `GET /api/admin/reservas/pendientes?q={codigo}` cubre el caso habitual; este endpoint sirve para el detalle.

### Resolver revisión
`POST /api/admin/reservas/{id}/marcar-revisada` body `{ version, nota }` → `RequiereRevision = false` y guarda la nota (p. ej. "Reembolsado el 05/12").

## Reglas

- Todas las fechas en la respuesta en ISO con offset; el front formatea en Madrid.
- Los listados nunca hacen N+1 (proyección directa a DTO con `Select`).
- El cálculo de "caducan hoy" usa la zona `App__TimeZone`, no UTC.

## Tests

**Integración**
- Resumen con datos sembrados: cada contador correcto (incluido "caducan hoy" alrededor de la medianoche de Madrid, con `FakeTimeProvider` a las 23:30 y a las 00:30).
- Reservas de una edición: por defecto solo pagadas y pendientes; con `incluir` las demás.
- CSV solo con pagadas, BOM y `;`.
- Búsqueda por código con distintas escrituras (`CUR-7K4Q2M`, `cur 7k4q2m`, `7K4Q2M`) → la misma reserva.
- Búsqueda por teléfono sin prefijo y con espacios.
- Expirada manual de hace 3 días aparece; de hace 10 días no; de Stripe no.
- `plazaDisponible` correcto.
- `marcar-revisada` limpia la marca.
- Sin token → 401.

## Criterios de aceptación

- [ ] La clienta pega el código que le llega por WhatsApp y encuentra la reserva al primer intento, escriba como lo escriba.
- [ ] Puede descargar la lista de asistentes de cada edición.
- [ ] La portada le dice qué tiene que hacer hoy (pendientes, revisiones, interesados sin llamar).
