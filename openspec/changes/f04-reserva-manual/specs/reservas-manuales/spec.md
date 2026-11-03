## ADDED Requirements

### Requirement: Manual booking expiry
A manual booking SHALL expire at `min(now + Reservas:ManualHorasCaducidad, Inicio − Reservas:CierreHorasAntes)`. When fewer than 2 hours remain until the closing time, the booking MUST be rejected with `409 edicion_no_reservable` and the message `Para esta fecha solo se puede pagar con tarjeta`.

#### Scenario: Expiry clipped to the closing time
- **WHEN** the closing time is 10 hours away and the manual lifetime is 48 hours
- **THEN** `ExpiraEn` equals the closing time; with the closing time 100 hours away it is `now + 48 h`
- **[UNIT]**

#### Scenario: Too close to the closing time
- **WHEN** the closing time is 1 hour away
- **THEN** a `BusinessRuleException(edicion_no_reservable)` is thrown
- **[UNIT]**

### Requirement: Manual booking endpoint
`POST /api/reservas/manual` (anonymous, rate limit `reservas`) with the same body as the card booking SHALL create a `Manual` `Pendiente` booking through `CrearReservaService`, publish `ReservaManualCreadaEvent` after the commit and answer `200 ReservaManualCreadaDto` with `datosPago` (`titular`, `iban`, `bizumTelefono` from `Pago:*`, `concepto` = booking code), `whatsappUrl` and `expiraEn`. A filled honeypot answers `200` and stores nothing.

#### Scenario: Create a manual booking
- **WHEN** a valid manual booking is sent for an edition with free seats
- **THEN** the response is `200` with `estado = Pendiente`, `metodo = Manual`, `datosPago` and `whatsappUrl`, and one `ReservaManualCreadaEvent` is published
- **[INTEGRATION]**

#### Scenario: Public status keeps the payment details while pending
- **WHEN** `GET /api/reservas/{codigo}` is called for that booking
- **THEN** it returns the same `datosPago` and `whatsappUrl`; once paid, both are `null`
- **[INTEGRATION]**

#### Scenario: Edition closing soon
- **WHEN** the edition closes in less than 2 hours
- **THEN** the response is `409` with `code = edicion_no_reservable`
- **[INTEGRATION]**

#### Scenario: Honeypot
- **WHEN** `website` is filled
- **THEN** the response is `200` and no booking is stored
- **[INTEGRATION]**

### Requirement: WhatsApp link
`whatsappUrl` SHALL be `https://wa.me/{Pago:WhatsAppTelefono}?text=` followed by the URL-encoded message `Hola, envío el comprobante de la reserva {codigo} ({curso}, {dd/MM/yyyy HH:mm in Europe/Madrid}). Importe: {importe} €.`

#### Scenario: Encoding of accents, euro sign and spaces
- **WHEN** the link is built for course `Acrílico avanzado`, 149.90 € and a December start at 09:00 UTC
- **THEN** the text decodes back to the exact message with `10:00` and `149,90 €`, and the encoded URL contains no raw spaces, accents or `€`
- **[UNIT]**
