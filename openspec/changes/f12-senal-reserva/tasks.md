## 1. Domain

- [x] 1.1 `Curso.PrecioReserva` (defaults to `Precio`, range check) with tests
- [x] 1.2 `Reserva.Importe` copies the deposit and `Reserva.PrecioCurso` the price, with tests

## 2. Application

- [x] 2.1 `precioReserva` in the create/edit commands, request and validator, with tests
- [x] 2.2 Public DTOs (`precioReserva`, `ubicacion`, `duracionTexto`, `ediciones` in the list) and `ReservaPublicaDto.precioCurso`
- [x] 2.3 Admin DTOs, projection and attendees CSV (`Resto en persona`), with tests
- [x] 2.4 Stripe concept `Reserva de plaza: …` when there is a deposit
- [x] 2.5 Email model and templates (deposit label and in-person row), preview data, with tests

## 3. Infrastructure

- [x] 3.1 Configurations, seed and migration `SenalReserva` (backfill `PrecioReserva = Precio`, `PrecioCurso = Importe`)

## 4. Integration tests

- [x] 4.1 Admin create with and without deposit; deposit above the price → 400
- [x] 4.2 Card and manual bookings charge the deposit
- [x] 4.3 Public list returns the deposit and every visible edition

## 5. Manual

- [ ] 5.1 Real deposit amounts per course from the client (set from the backoffice or `cursos.json` before the first deploy)
- [ ] 5.2 Stripe test-mode payment with a deposit shows the right amount and concept in the Checkout page
