## ADDED Requirements

### Requirement: Publishing marks the course
Handling `EdicionPublicadaEvent` SHALL only mark the edition's course as pending notice (`AvisosPendientes`); it MUST NOT enqueue any email.

#### Scenario: Publish
- **WHEN** an edition is created `Programada` or moves from `Oculta` to `Programada`
- **THEN** its course is marked and no notice is enqueued until the job runs
- **[INTEGRATION]** **[UNIT]**

### Requirement: Grouped notice per person
`AvisosNuevasEdicionesJob` (every `Avisos:JobIntervaloMinutos`, default 10) SHALL, for each marked course, enqueue one email (`Tipo = AvisoNuevasEdiciones`, subject `Nuevas fechas para {curso}`) per waiting-list entry of that course with `BajaEn == null` (contacted ones included) listing every edition that is bookable when the job runs (scheduled, course active, outside the closing window) and has free seats, and that was not notified to that person before. People with no such edition MUST NOT be emailed. The mark is removed in the same save.

#### Scenario: One date
- **WHEN** a date is published and the job runs
- **THEN** every subscribed person of that course gets one email; nobody of other courses and nobody unsubscribed gets one
- **[INTEGRATION]** **[UNIT]**

#### Scenario: Three dates before the job
- **WHEN** three dates of the same course are published before the job runs
- **THEN** each person gets a single email with the three
- **[INTEGRATION]**

#### Scenario: Not bookable any more
- **WHEN** the published edition is cancelled (or hidden, full or inside the closing window) before the job runs
- **THEN** it is not notified, and a person left without dates gets nothing
- **[INTEGRATION]** **[UNIT]**

### Requirement: No duplicates
Each notified `(ListaEsperaId, EdicionId)` SHALL be stored in `AvisosEnviados` in the same save as the email, and a pair already stored MUST never be notified again. `ReferenciaId` is a hash of the entry and the editions of the email.

#### Scenario: Hide and publish again
- **WHEN** a notified edition is hidden and published again and the job runs
- **THEN** nobody gets a second email for it
- **[INTEGRATION]**

### Requirement: Batches and summary
Notices SHALL be enqueued in batches of `Avisos:LoteTamano` (20), each batch due `Avisos:PausaEntreLotesSegundos` (2) after the previous one. When someone was emailed and `Email:AvisosClienta` is set, the client SHALL get `Aviso enviado a {N} interesados · {curso}` with the notified dates.

#### Scenario: Batches
- **WHEN** 5 people are notified with batches of 2
- **THEN** their emails are due at +0 s, +0 s, +2 s, +2 s, +4 s and the client's summary says 5
- **[UNIT]**

### Requirement: Email content and unsubscribe headers
The notice SHALL greet the person by name, list each date (day and time in `App:TimeZone`, free seats when enqueued, warning that they can run out), link "Reservar mi plaza" to `/reservar/{slug}`, remind that they joined that course's waiting list, and link in the footer to `{App:BaseUrl}/baja?token={BajaToken}&curso={slug}`. It SHALL be sent with `List-Unsubscribe: <{App:BaseUrl}/api/lista-espera/baja?token={BajaToken}>` and `List-Unsubscribe-Post: List-Unsubscribe=One-Click`.

#### Scenario: Links and headers
- **WHEN** a notice is enqueued and sent through Resend
- **THEN** it contains the unsubscribe link and the request to Resend carries both headers
- **[INTEGRATION]** **[UNIT]**

### Requirement: Unsubscribe endpoint
`POST /api/lista-espera/baja` (anonymous, rate limit `formularios`) SHALL accept the token in the query, a JSON body `{token}` or a form field `token`, set `BajaEn` of that entry only (keeping the first moment), and answer `204` whether the token is valid, invalid, missing or already used. A `GET` MUST NOT unsubscribe.

#### Scenario: Valid and invalid token
- **WHEN** the endpoint is called with a valid and with an invalid token
- **THEN** both answer 204 and only the valid one changes its entry (shown as `baja: true` in the backoffice)
- **[INTEGRATION]** **[UNIT]**

#### Scenario: Gmail one-click
- **WHEN** the `List-Unsubscribe` URL is posted with the form body `List-Unsubscribe=One-Click`
- **THEN** the entry is unsubscribed
- **[INTEGRATION]**
