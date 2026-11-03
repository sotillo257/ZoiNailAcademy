## 1. Domain

- [x] 1.1 Test `TextoBusqueda.Normalizar` (accents, case, blanks), then implement it
- [x] 1.2 Test `ListaEspera` (create and normalize, unsubscribe token, preferred edition of another course, required fields, re-sign-up resets contact and unsubscribe and keeps notes, contacted mark keeps the first moment, notes), then implement it

## 2. Application

- [x] 2.1 Test `CsvBuilder` (quotes, line breaks, `;` inside a field, formula prefixes, BOM, CRLF), then implement it
- [x] 2.2 Test `AltaListaEsperaValidator` (phone required and valid, email valid, privacy accepted), then implement it
- [x] 2.3 Test `AltaListaEsperaHandler` (new entry with backend consent and event, upsert, inactive/unknown course, edition of another course, honeypot), then implement it with `ConsentimientoListaEspera` and `ListaEsperaAltaEvent`
- [x] 2.4 Test the listing (order, paging, course/contacted filters, accent-insensitive search, phone digits), `MarcarContactado` and the CSV export, then implement them with `ListaEsperaConsulta` and `ListaEsperaAdminDto`; `PagedResult` as `{items, total, page, pageSize}`

## 3. Infrastructure and Host

- [x] 3.1 `ListaEsperaConfiguration` (unique `(CursoId, Email)` and `BajaToken`, FK course `Restrict`, preferred edition `SetNull`) and migration `ListaEspera`
- [x] 3.2 `ListaEsperaController` (`formularios`) and `AdminListaEsperaController` (Backoffice)

## 4. Integration tests (Testcontainers)

- [x] 4.1 Sign-up: 201 and event, upsert resets contacted, same email on another course, simultaneous sign-ups, inactive/unknown course 404, edition of another course 400, honeypot, validation
- [x] 4.2 Backoffice: filters, accent search (`Jose` finds `José`), order and paging, preferred edition in Madrid time, page size limit, mark/unmark contacted and notes, CSV (BOM, `;`, Madrid dates, formula protection, file name), unknown course, 401 without token

## 5. Manual

- [ ] 5.1 [MANUAL] Open the CSV in Excel (Spanish locale) and check that accents and columns are correct
