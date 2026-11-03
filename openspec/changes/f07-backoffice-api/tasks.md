## 1. Domain

- [x] 1.1 Test `CodigoReserva.TryNormalizar` (`CUR-7K4Q2M`, `cur 7k4q2m`, `7K4Q2M`, blanks; not codes: wrong length, ambiguous characters, emails, phones), then implement it
- [x] 1.2 Test `TextoBusqueda.DigitosTelefono` (spaces, dashes, prefix; text that is not a phone), then implement it and reuse it in `ListaEsperaConsulta`
- [x] 1.3 Test `Reserva.Busqueda` and `Reserva.MarcarRevisada` (clears the mark and appends the note; no-op without the mark), then implement them

## 2. Application

- [x] 2.1 Test `ZonaHoraria.FinDelDia` (winter, summer, DST change day), then implement it
- [x] 2.2 Test `ObtenerResumenHandler` (each counter, expiring today at 23:30 and 00:30 Madrid, next 5 editions without cancelled ones), then implement it with `ResumenDto`
- [x] 2.3 Test `ListarReservasEdicionHandler` and its validator (default paid + pending in order, `incluir`, unknown edition, invalid values), then implement them with `ReservaAdminConsulta`/`ReservaAdminFila`
- [x] 2.4 Test `ExportarAsistentesHandler` (only paid, BOM, Madrid dates, decimal comma, file name, unknown edition), then implement it; move `ArchivoCsv` to `Common/Csv`
- [x] 2.5 Test `ListarPendientesHandler` (set and order, 7-day window, card expired excluded, code in any spelling, phone digits, accents, method filter, `plazaDisponible` in one grouped query), then implement it
- [x] 2.6 Test `ObtenerReservaAdminHandler`, `MarcarRevisadaHandler` and its validator (stale version → conflict), then implement them; `ReservaAdminDto.PlazaDisponible` optional and omitted when null

## 3. Infrastructure and Host

- [x] 3.1 `Reservas.Busqueda` in `ReservaConfiguration` and migration `ReservaBusqueda` (fills existing rows)
- [x] 3.2 `AdminResumenController`; `ediciones/{id}/reservas` and `reservas.csv` in `AdminEdicionesController`; `reservas/pendientes`, `reservas/{id}` and `marcar-revisada` in `AdminReservasController` (Backoffice policy at class level)

## 4. Integration tests (Testcontainers)

- [x] 4.1 Summary: counters as differences, expiring today around Madrid midnight on its own host and clock, next 5 editions
- [x] 4.2 Edition bookings (default, `incluir`, 400, 404) and attendee CSV (only paid, BOM, `;`, file name, 404)
- [x] 4.3 Pending: code in three spellings, phone without prefix and with spaces, 3 vs 10 days and card, `plazaDisponible`, order
- [x] 4.4 Detail of a cancelled booking and 404; `marcar-revisada` clears the mark, stores the note, lowers the counter, stale version → 409; 401 without token (covered by `AuthorizationTests` for every `/api/admin/**` route)

## 5. Manual

- [ ] 5.1 [MANUAL] Open the attendee CSV in Excel (Spanish locale) and check accents, amounts and columns
