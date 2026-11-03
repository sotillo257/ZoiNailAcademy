## Why

The approved design of the public web (F08) shows "Precio total" and "Reserva de plaza" for each course: the client wants buyers to pay only a seat deposit online (card or transfer/Bizum) and the rest in person, outside the web. Until now a booking charged the whole course price.

## What Changes

- `Curso.PrecioReserva` (decimal, `0 < PrecioReserva <= Precio`). Optional in the create/edit API (`precioReserva`); when omitted it equals `Precio`, so a course can still be paid in full.
- `Reserva.Importe` is now a copy of `PrecioReserva` (what is charged online: Stripe amount, transfer/Bizum amount, WhatsApp message) and the new `Reserva.PrecioCurso` keeps a copy of `Precio`. `PrecioCurso - Importe` is paid in person; the web does not track it.
- Public DTOs: `CursoResumenDto`/`CursoDetalleDto` add `precioReserva`, `ubicacion`, `duracionTexto`, and `CursoResumenDto` now carries `ediciones` (the public calendar table lists every date of every course from one call). `ReservaPublicaDto` adds `precioCurso`.
- Backoffice: `CursoAdminDto` and `ReservaAdminDto` add `precioReserva`/`precioCurso`; the attendees CSV adds the column `Resto en persona`.
- Emails label the amount "Reserva de plaza" and add a "Resto a pagar en persona" row when there is a deposit. The Stripe line item reads `Reserva de plaza: {curso} — {fecha}`.
- Migration `SenalReserva`: existing courses get `PrecioReserva = Precio`, existing bookings `PrecioCurso = Importe` (nothing changes for them).

## Capabilities

### New Capabilities
- `senal-reserva`: booking charges a per-course seat deposit; the rest is paid in person.

### Modified Capabilities
- None beyond the DTO fields listed above (additive).

## Non-goals

- Collecting the rest through the web (second payment link, reminders, tracking whether it was paid).
- A global or percentage deposit.

## Impact

- **Domain**: `Curso.PrecioReserva`, `Reserva.PrecioCurso`.
- **Application**: course commands/validator/DTOs, public and admin booking DTOs, Stripe concept, attendees CSV, email model and templates.
- **Infrastructure**: configurations, seed (`precioReserva` in `cursos.json`), migration `SenalReserva`.
- **Web (F09)**: course form field and booking card show the deposit and the rest.
