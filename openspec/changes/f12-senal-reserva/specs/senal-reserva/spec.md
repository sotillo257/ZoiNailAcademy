## ADDED Requirements

### Requirement: Per-course seat deposit
A course SHALL have a `PrecioReserva` greater than zero and not above `Precio`. When the create or edit request omits `precioReserva`, it SHALL equal `Precio`. An out-of-range value MUST be rejected with 400 on `PrecioReserva`.

#### Scenario: Deposit omitted
- **WHEN** a course is created or edited without `precioReserva`
- **THEN** `precioReserva` equals `precio`
- **[INTEGRATION]** **[UNIT]**

#### Scenario: Deposit above the price
- **WHEN** `precioReserva` is above `precio`, zero, negative or has more than 2 decimals
- **THEN** the API answers 400 with an error on `PrecioReserva`
- **[INTEGRATION]** **[UNIT]**

### Requirement: Bookings charge the deposit
A new booking SHALL copy `PrecioReserva` into `Importe` and `Precio` into `PrecioCurso`. The Stripe session amount, the transfer/Bizum amount and the WhatsApp message SHALL use `Importe`.

#### Scenario: Card booking with deposit
- **WHEN** a course costs 120 € with a 35,50 € deposit and someone books by card
- **THEN** the Checkout session is for 3550 cents and its concept starts with `Reserva de plaza:`
- **[INTEGRATION]** **[UNIT]**

#### Scenario: Manual booking with deposit
- **WHEN** someone books by transfer/Bizum a course with a 40 € deposit
- **THEN** the response has `importe = 40`, `precioCurso = 120` and the WhatsApp message says `Importe: 40,00 €`
- **[INTEGRATION]**

### Requirement: The rest is shown, not collected
Booking emails SHALL label `Importe` as "Reserva de plaza" and add "Resto a pagar en persona" when `PrecioCurso > Importe`; when the whole price is paid online they keep "Importe" and add nothing. The attendees CSV SHALL include `Resto en persona`.

#### Scenario: Email with deposit
- **WHEN** a booking of 149 € with a 50 € deposit is confirmed
- **THEN** the email shows "Reserva de plaza 50,00 €" and "Resto a pagar en persona 99,00 €" in HTML and text
- **[UNIT]**
