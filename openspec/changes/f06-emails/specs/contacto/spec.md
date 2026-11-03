## ADDED Requirements

### Requirement: Contact form
`POST /api/contacto` (anonymous, rate limit `formularios`) SHALL validate `nombre`, `email`, optional `telefono`, `mensaje` (10–3000 characters) and `aceptaPrivacidad = true`, enqueue `Contacto.Clienta` for `Email:AvisosClienta` with Reply-To = the sender, and answer `202`. Nothing else is stored. A filled honeypot answers `202` and enqueues nothing. The acknowledgement to the sender is sent only with `Email:AcuseContacto = true` and never repeats the message.

#### Scenario: Message sent
- **WHEN** a valid message is posted
- **THEN** the response is `202` and one `Contacto.Clienta` email is enqueued with Reply-To = the sender and the name HTML-encoded
- **[INTEGRATION]**

#### Scenario: Honeypot and validation
- **WHEN** the honeypot is filled; or the message is too short
- **THEN** the response is `202` with nothing enqueued; or `400` on `Mensaje`
- **[INTEGRATION]**
