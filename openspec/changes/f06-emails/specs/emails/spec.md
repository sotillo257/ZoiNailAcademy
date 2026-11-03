## ADDED Requirements

### Requirement: Outbox without duplicates
Domain events SHALL enqueue emails in `EmailsSalientes` instead of sending them. An email with a reference MUST be enqueued at most once per `(Tipo, ReferenciaId)`, even if its event is published twice.

#### Scenario: Paid booking
- **WHEN** a manual booking is confirmed and `ReservaPagadaEvent` and `ReservaManualCreadaEvent` are published again
- **THEN** there are exactly two `ReservaManual.*` and two `ReservaPagada.*` emails for the booking (buyer and client)
- **[INTEGRATION]**

#### Scenario: Expired booking
- **WHEN** a Stripe booking and a manual booking expire
- **THEN** only the manual buyer gets `ReservaExpirada.Comprador`
- **[INTEGRATION]**

#### Scenario: Client address missing
- **WHEN** `Email:AvisosClienta` is empty
- **THEN** the client's notices are skipped and the buyer's emails are still enqueued
- **[UNIT]**

### Requirement: Sending with retries
`EnviarEmailsJob` SHALL send due pending emails (at most 20 per cycle, behind an advisory lock). A failure MUST schedule the next attempt after 1 min, 5 min, 15 min and 1 h; the 5th failure MUST mark the email `Fallido`. A failed email is never retried.

#### Scenario: Provider recovers
- **WHEN** the provider fails twice for an email
- **THEN** it is not retried before the backoff and is `Enviado` on the third attempt
- **[INTEGRATION]**

#### Scenario: Provider always fails
- **WHEN** the provider always fails
- **THEN** the email is `Fallido` after 5 attempts
- **[INTEGRATION]**

### Requirement: Running without Resend
With `Email:Provider = Resend` but no `Resend:ApiKey` or `Email:From`, the API SHALL start, log a warning naming the missing variables, and write emails to the log instead of sending them; no request or job may fail because of it.

#### Scenario: Provider selection
- **WHEN** the API key or the sender is missing, or the provider is unknown
- **THEN** the log provider is selected with a warning; with both set, Resend is selected
- **[INTEGRATION]**

### Requirement: Templates
Every template SHALL produce a subject, an HTML version (tables, inline styles, 600 px, common layout) and a text version, with user data HTML-encoded, dates in Madrid time in long Spanish format and amounts like `149,00 €`; booking emails show the code in the subject and highlighted in the body.

#### Scenario: Content
- **WHEN** each booking template is rendered with a sample booking
- **THEN** both versions contain the code and `sábado, 5 de diciembre de 2026, 10:00`, and a name `<script>` is escaped
- **[UNIT]**

### Requirement: Preview
`GET /api/admin/emails/preview/{tipo}` SHALL return the HTML of the template with sample data to SuperAdmin only.

#### Scenario: Access
- **WHEN** SuperAdmin, Admin, an anonymous user and SuperAdmin with an unknown type call it
- **THEN** the answers are `200 text/html`, `403`, `401` and `404`
- **[INTEGRATION]**

### Requirement: Retention
Sent emails older than 90 days SHALL be deleted once a day.

#### Scenario: Cleanup
- **WHEN** the cleanup runs with a sent email of 91 days and a recent one
- **THEN** only the old one is deleted
- **[INTEGRATION]**
