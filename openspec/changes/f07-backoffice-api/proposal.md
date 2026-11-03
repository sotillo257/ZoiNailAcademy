## Why

The client needs to see her day at a glance: how many people are enrolled in each edition and who they are, which bookings are waiting for a transfer or Bizum (finding them by the code the buyer sends over WhatsApp), and what needs her attention today (see `Docs/features/F07-backoffice-api.md` and `Docs/00-contrato-y-convenciones.md`). F04 built the actions on a booking; F07 builds the queries the backoffice (F09) reads.

## What Changes

- `GET /api/admin/resumen`: counters for the home page (manual bookings waiting for payment, those that expire today in Madrid time, bookings that need review, waiting-list people not contacted yet) and the next 5 editions.
- `GET /api/admin/ediciones/{id}/reservas?incluir=`: the edition and its bookings (paid and pending by default; `expiradas`, `canceladas` on request), paid by `PagadaEn`, then pending by `ExpiraEn`.
- `GET /api/admin/ediciones/{id}/reservas.csv`: the attendee list (paid only) with the F05 `CsvBuilder`, file `asistentes-{slug}-{yyyyMMdd-HHmm}.csv`.
- `GET /api/admin/reservas/pendientes?q=&metodo=`: valid pending bookings, manual bookings expired in the last 7 days and bookings that need review; `q` finds a code however it is typed (`CUR-7K4Q2M`, `cur 7k4q2m`, `7K4Q2M`), or text in name, email or phone (digits only, so `612345678` finds `+34612345678`); expired bookings carry `plazaDisponible`.
- `GET /api/admin/reservas/{id}`: one booking in any state.
- `POST /api/admin/reservas/{id}/marcar-revisada` `{version, nota?}`: clears `RequiereRevision` and appends the note.
- `Reserva.Busqueda` (name, email and phone normalized without case or accents, as `ListaEspera.Busqueda`) and migration `ReservaBusqueda` that fills it for existing rows.
- `ReservaAdminDto` gains the optional `plazaDisponible` (omitted when null, so F04 responses do not change). `ArchivoCsv` moves to `Application/Common/Csv`.

## Capabilities

### New Capabilities
- `backoffice-consultas`: home counters, bookings of an edition and attendee CSV, pending bookings and search by code, booking detail, "reviewed" mark.

### Modified Capabilities
- None.

## Non-goals

- Sales statistics or charts (out of v1).
- Pagination of the booking lists (an edition has at most 200 seats; pending bookings are a handful).

## Impact

- **Domain**: `Reserva.Busqueda`, `Reserva.MarcarRevisada`, `CodigoReserva.TryNormalizar`, `TextoBusqueda.DigitosTelefono`.
- **Application**: `Resumen/ObtenerResumen`, `Reservas/ListarReservasEdicion`, `Reservas/ExportarAsistentes`, `Reservas/ListarPendientes`, `Reservas/ObtenerReservaAdmin`, `Reservas/MarcarRevisada`, `Reservas/ReservaAdminConsulta`, `Common/ZonaHoraria`.
- **Infrastructure**: `ReservaConfiguration` (`Busqueda`), migration `ReservaBusqueda`.
- **Host**: `AdminResumenController`; new actions in `AdminEdicionesController` and `AdminReservasController`.
- No new configuration keys (`App:TimeZone` already exists) and no new packages.
