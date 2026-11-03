## Context

F02 built editions and `EdicionAdminDto`, F03/F04 the bookings, the `ReservaAdminDto` shape and the backoffice actions (with the `version` check of `AccionesReserva.CargarAsync`), F05 the `CsvBuilder` and the accent-insensitive `Busqueda` column pattern. F07 adds read-only queries plus one small action (`marcar-revisada`).

## Goals / Non-Goals

**Goals:** the queries of the backoffice home page, edition attendees and pending bookings; finding a booking by its code however it is typed; no N+1.

**Non-Goals:** statistics, pagination, actions other than `marcar-revisada`.

## Decisions

**D-01 Projection to DTO.** Booking listings project with `Select` to an internal row (`ReservaAdminFila`, with the `xmin` as `uint` and the edition seats) that is mapped to `ReservaAdminDto` in memory, so each listing is one SQL query with joins. Occupancy for `EdicionAdminDto` and `plazaDisponible` comes from `IOcupacionReader` in one grouped query for every edition involved.

**D-02 `incluir`.** Comma-separated, case-insensitive values `pagadas`, `pendientes`, `expiradas`, `canceladas`; empty means `pagadas,pendientes`; any other value → 400 on `Incluir`. `pendientes` means state `Pendiente` (a pending booking past its expiry is shown until the job expires it). Order: paid by `PagadaEn`, pending by `ExpiraEn`, expired by `ExpiradaEn`, cancelled by `CanceladaEn` (a `CASE` in SQL), then `CreatedAt`, `Id`. Unknown edition → 404.

**D-03 Attendee CSV.** Only `Pagada`, ordered by `PagadaEn`. Columns `Código; Nombre; Teléfono; Email; Método; Importe; Fecha de pago`; method `Tarjeta` or `Transferencia o Bizum`; amount with decimal comma (`149,00`, Excel in Spanish; formatted without relying on ICU cultures); payment date `dd/MM/yyyy HH:mm` in `App:TimeZone`. File `asistentes-{slug}-{yyyyMMdd-HHmm}.csv` with the edition start in `App:TimeZone`. `ArchivoCsv` moves to `Application/Common/Csv` since it is now shared.

**D-04 Pending set.** `RequiereRevision`, or `Pendiente` with `ExpiraEn > now`, or `Expirada` + `Manual` with `ExpiradaEn ≥ now − 7 days`. Optional `metodo` filter. Order: needs review first (oldest first), then pending by `ExpiraEn` ascending, then expired by `ExpiradaEn` descending (most recent first), then `Id`. Not paginated.

**D-05 Search by code.** `CodigoReserva.TryNormalizar` removes blanks and dashes, upper-cases, drops an optional `CUR` prefix and accepts exactly 6 characters of the alphabet, returning `CUR-XXXXXX`. When `q` looks like a code the filter is `Codigo = normalized` **or** the text search below: names such as `Carmen` are also 6 letters of the alphabet, and a code never matches a name by accident, so the OR keeps both cases working.

**D-06 Text search.** `Reserva.Busqueda` = `TextoBusqueda.Normalizar("{Nombre} {Email} {Telefono}")`, set in `Create` (those fields never change). `q` is normalized the same way and matched with `Contains`. When `q` is only digits and phone separators (`612 34 56 78`, `+34 612…`), its digits are also matched against `Telefono`; the digit extraction moves to `TextoBusqueda.DigitosTelefono` and the waiting list reuses it. The migration fills existing rows with `lower()` plus `translate()` of Spanish accents, which matches `Normalizar` for the data we have.

**D-07 `plazaDisponible`.** Set only for `Expirada` bookings: whether their edition has a free seat now (the condition `confirmar` and `ampliar` check). Optional property of `ReservaAdminDto`, omitted from JSON when null, so the F04 action responses keep their shape.

**D-08 Home counters.** `pendientesDePago`: `Manual`, `Pendiente`, `ExpiraEn > now`. `pendientesQueCaducanHoy`: those with `ExpiraEn` before the next midnight in `App:TimeZone` (`ZonaHoraria.FinDelDia`). `requierenRevision`: `RequiereRevision = true` (clearing the flag is what resolves it). `listaEsperaSinContactar`: not contacted and not unsubscribed. `proximasEdiciones`: the 5 next editions that have not started and are not cancelled (hidden ones included: it is the backoffice), soonest first.

**D-09 `marcar-revisada`.** `{version, nota?}` (`nota` ≤ 500): `CargarAsync` checks the version (409 `concurrencia`), `Reserva.MarcarRevisada(nota)` clears the flag and appends the note like the other actions (no-op when the flag is already clear); answers `200 {reserva, avisos: []}` like the F04 actions.

## Endpoints

| Method | Route | Request | Success | Errors |
| --- | --- | --- | --- | --- |
| GET | `/api/admin/resumen` | — | `200 ResumenDto` | — |
| GET | `/api/admin/ediciones/{id}/reservas` | `?incluir=` | `200 {edicion, reservas}` | `400`, `404` |
| GET | `/api/admin/ediciones/{id}/reservas.csv` | — | `200 text/csv; charset=utf-8` | `404` |
| GET | `/api/admin/reservas/pendientes` | `?q=&metodo=` | `200 ReservaAdminDto[]` | `400` |
| GET | `/api/admin/reservas/{id}` | — | `200 ReservaAdminDto` | `404` |
| POST | `/api/admin/reservas/{id}/marcar-revisada` | `{version, nota?}` | `200 {reserva, avisos}` | `400`, `404`, `409` |

All with policy `Backoffice`.

## Risks / Trade-offs

- [The code/name OR may return a name match next to the code] → only when someone's name or email contains those 6 letters; the exact code is still in the result.
- [`LIKE '%…%'` without index] → hundreds of bookings per year.

## Migration Plan

Migration `ReservaBusqueda`: adds `Reservas.Busqueda` (`varchar(342)`, not null), fills it for existing rows, applied at startup. No configuration changes.
