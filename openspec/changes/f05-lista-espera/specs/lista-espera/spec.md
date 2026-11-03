## ADDED Requirements

### Requirement: Waiting list entry
A `ListaEspera` SHALL be created only through `ListaEspera.Create(...)` with a normalized email (trimmed, lower-cased), an E.164 phone, `ConsentimientoEn` and `TextoConsentimiento`, and a random unique `BajaToken` (32 bytes, base64url). There MUST be at most one entry per `(CursoId, Email)`. A preferred edition MUST belong to the course.

#### Scenario: Valid entry
- **WHEN** `ListaEspera.Create` is called with valid data
- **THEN** the email is normalized, the entry is not contacted, not unsubscribed, and its token has 43 URL-safe characters
- **[UNIT]**

#### Scenario: Preferred edition of another course
- **WHEN** the preferred edition belongs to another course
- **THEN** a `DomainException` is thrown
- **[UNIT]**

### Requirement: Public sign-up
`POST /api/lista-espera` (anonymous, rate limit `formularios`) SHALL validate `cursoId`, `nombre`, `email`, `telefono` (normalized as in F03) and `aceptaPrivacidad = true` (400 per field otherwise), require an existing active course (404 `no_encontrado` otherwise) and a preferred edition of that course when sent (400 on `EdicionPreferidaId` otherwise). It SHALL store `ConsentimientoListaEspera.V1` as the consent text, ignoring the text the front sends; that text covers being contacted by phone, WhatsApp or email about new dates. It SHALL answer `201` without body and publish `ListaEsperaAltaEvent` after the commit. A filled honeypot answers `201` and stores nothing.

#### Scenario: New sign-up
- **WHEN** a valid sign-up is sent for an active course
- **THEN** the response is `201` with an empty body, the entry stores the backend consent text and one `ListaEsperaAltaEvent` is published
- **[INTEGRATION]**

#### Scenario: Same person and course
- **WHEN** the same email signs up again for the same course
- **THEN** the existing entry is updated (name, phone, preferred date, consent), `Contactado` becomes false, `BajaEn` null, the response is the same `201`, and the event is published again
- **[INTEGRATION]**

#### Scenario: Same email on another course
- **WHEN** the email is already on the list of another course
- **THEN** a new entry is created
- **[INTEGRATION]**

#### Scenario: Inactive course and honeypot
- **WHEN** the course is inactive or unknown; or the honeypot is filled
- **THEN** the response is `404 no_encontrado`; or `201` with nothing stored
- **[INTEGRATION]**

#### Scenario: Validation
- **WHEN** the phone is missing or invalid, the email is invalid or privacy is not accepted
- **THEN** the validator fails on that field
- **[UNIT]**

### Requirement: Backoffice listing
`GET /api/admin/lista-espera` (policy `Backoffice`) SHALL filter by `cursoId` and `contactado`, search `q` contained in name, email or phone ignoring case and accents, order not contacted first and then by `CreadoEn` ascending, and paginate (`pageSize` 50 by default, 200 max) as `{items, total, page, pageSize}` of `ListaEsperaAdminDto` (`edicionPreferida` formatted `dd/MM/yyyy HH:mm` in Madrid time).

#### Scenario: Filters and accent-insensitive search
- **WHEN** the list is requested with `q=Jose` for a course with `José Núñez`
- **THEN** `José Núñez` is found, entries of other courses are not, and not contacted entries come first
- **[INTEGRATION]**

#### Scenario: Without token
- **WHEN** any `/api/admin/lista-espera*` endpoint is called without a token
- **THEN** the response is `401`
- **[INTEGRATION]**

### Requirement: Contacted mark
`PUT /api/admin/lista-espera/{id}/contactado` (policy `Backoffice`) with `{contactado, notas?}` SHALL set `Contactado`, set `ContactadoEn` when first marked and clear it when unmarked, replace the notes only when `notas` is sent (≤ 500), and answer `200 ListaEsperaAdminDto`; an unknown id returns `404`.

#### Scenario: Mark and unmark
- **WHEN** an entry is marked with notes, marked again without notes, and unmarked with `notas: null`
- **THEN** `ContactadoEn` is set and kept, the notes are kept on the second call, and unmarking clears `ContactadoEn` and the notes
- **[INTEGRATION]**

### Requirement: CSV export
`GET /api/admin/lista-espera.csv?cursoId=` (policy `Backoffice`) SHALL return `text/csv; charset=utf-8` built with `CsvBuilder`, columns `Curso; Nombre; Teléfono; Email; Fecha preferida; Edición preferida; Contactado; Fecha de alta; Notas; Baja`, dates `dd/MM/yyyy HH:mm` in Madrid time, the listing's order, and the file name `lista-espera-{slug}-{yyyyMMdd}.csv`. An unknown course returns `404`.

#### Scenario: Export
- **WHEN** the CSV of a course is downloaded
- **THEN** it starts with the BOM, uses `;`, shows the sign-up date in Madrid time and prefixes formula-like fields with `'`
- **[INTEGRATION]**
