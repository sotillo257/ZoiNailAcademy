## Why

Many visitors want a course that has no dates yet or whose edition is full. F05 captures them so the client can call them when she opens new dates, and gives her a per-course list with a "contacted" mark and an Excel export (see `Docs/features/F05-lista-espera.md` and `Docs/00-contrato-y-convenciones.md`). The stored consent already covers the email about new dates, so F11 will not need to ask again.

## What Changes

- Entity `ListaEspera` (table `ListaEspera`, unique `(CursoId, Email)`, unique `BajaToken`) and migration `ListaEspera`.
- `POST /api/lista-espera` (anonymous, rate limit `formularios`, honeypot `website`, `aceptaPrivacidad`): creates the entry, or updates the existing one for the same course and email (back to not contacted and subscribed); always `201` without body. The consent text stored is the backend constant `ConsentimientoListaEspera.V1`; the front's `textoConsentimiento` is ignored. Publishes `ListaEsperaAltaEvent` after the commit in both cases.
- `GET /api/admin/lista-espera` (Backoffice): filters `cursoId`, `contactado`, `q` (name, email or phone, case and accent insensitive), not contacted first then oldest first, paginated `{items, total, page, pageSize}` (50 by default, 200 max).
- `PUT /api/admin/lista-espera/{id}/contactado` (Backoffice): `{contactado, notas?}` → `ListaEsperaAdminDto`.
- `GET /api/admin/lista-espera.csv` (Backoffice): CSV for Excel in Spanish, built with the new shared `CsvBuilder` (BOM, `;`, quoting, formula protection) that F07 reuses.
- `PagedResult<T>` now serializes as `{items, total, page, pageSize}` (it had no users yet).

## Capabilities

### New Capabilities
- `lista-espera`: the public sign-up and the backoffice listing, mark and export.
- `csv-export`: the shared CSV helper.

### Modified Capabilities
- None.

## Non-goals

- Automatic emails about new editions and the unsubscribe endpoint (F11); the model already stores `BajaToken`, `BajaEn` and the consent.
- The email to the client on each sign-up (F06 consumes `ListaEsperaAltaEvent`).

## Impact

- **Domain**: `ListaEspera` (`Create`, `ActualizarAlta`, `MarcarContactado`, `CambiarNotas`), `TextoBusqueda`.
- **Application**: `ListasEspera/AltaListaEspera`, `ListarListaEspera`, `MarcarContactado`, `ExportarListaEspera`; `ListaEsperaAdminDto`, `ListaEsperaAltaEvent`, `ConsentimientoListaEspera`; `Common/Csv/CsvBuilder`; `IAppDbContext.ListaEspera`.
- **Infrastructure**: `ListaEsperaConfiguration`, migration `ListaEspera`.
- **Host**: `ListaEsperaController`, `AdminListaEsperaController`.
- No new configuration keys and no new packages.
