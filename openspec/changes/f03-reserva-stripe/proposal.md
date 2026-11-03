## Why

Courses and editions exist (F02) but nobody can buy a seat. F03 lets a buyer pick an edition, pay by card in Stripe Checkout and end up enrolled, with two hard guarantees: a seat is never sold twice, and a payment is never processed twice. It also introduces the `Reserva` entity and the booking core (`CrearReservaService`) that the bank-transfer flow (F04) reuses (see `Docs/features/F03-reserva-stripe.md` and `Docs/00-contrato-y-convenciones.md`).

## What Changes

- New `Reserva` aggregate (code `CUR-XXXXXX`, method, state machine `Pendiente → Pagada | Expirada | Cancelada` plus the late-payment transitions, `xmin`) and `StripeEvento` (processed webhook ids).
- `CrearReservaService` (shared with F04): normalizes email and phone (E.164 with `libphonenumber-csharp`), locks the edition row with `SELECT … FOR UPDATE` inside a transaction, checks bookable / per-contact pending limit / free seats, and inserts a `Pendiente` booking with a unique random code and the course price.
- `POST /api/reservas/tarjeta` (rate limit `reservas`, honeypot `website`, `aceptaCondiciones`): creates the booking and a Stripe Checkout session; Stripe failure cancels the booking and answers `502`.
- `POST /api/stripe/webhook`: signature verification over the raw body, idempotency through `StripeEvento`, `checkout.session.completed` (paid) and `checkout.session.expired`, including payments that arrive after the booking expired.
- `GET /api/reservas/{codigo}` (public state without personal data) and `POST /api/reservas/{codigo}/abandonar` (frees the seat when the buyer comes back without paying).
- Events `ReservaPagadaEvent`, `ReservaExpiradaEvent`, `PagoTrasCaducidadSinPlazaEvent`, published after the commit.
- `OcupacionReader` now counts real bookings (one grouped query); edition deletion is blocked by any booking.
- New error mappings: `429` with `code` (`limite_pendientes`) and `502` for payment-provider failures.
- New configuration: `Stripe:SecretKey`, `Stripe:WebhookSecret`, `Reservas:StripeMinutosBloqueo` (30), `Reservas:MaxPendientesPorContacto` (2), `App:BaseUrl`.

## Capabilities

### New Capabilities
- `reservas`: the booking aggregate, its invariants and transitions, the booking core and the public booking endpoints.
- `stripe-checkout`: card payment through Stripe Checkout and its webhook.

### Modified Capabilities
- `ediciones`: occupied seats come from real bookings; deleting an edition with bookings is rejected for real.
- `shared-kernel`: `429` and `502` error mappings.

## Non-goals

- Refunds (done by hand in the Stripe dashboard).
- Asynchronous payment methods (SEPA…): card and the synchronous wallets Checkout shows.
- Emails (F06): only events are published.
- Bank transfer / Bizum, the expiry job and the client's booking actions (F04).
- Verifying the signature through the deployed Next.js rewrite — `[MANUAL]` (F10).

## Impact

- **Domain**: `Reserva`, `StripeEvento`, `MetodoPago`, `CodigoReserva`.
- **Application**: `Reservas/*`, `IStripeGateway`, `IStripeWebhookParser`, `IAppSettings`, `TelefonoNormalizer`, `TooManyRequestsException`, `PaymentProviderException`; `IAppDbContext` gains `Reservas`, `StripeEventos`, `BeginTransactionAsync` and `BloquearEdicionAsync`; `IReservasSettings` gains two settings.
- **Infrastructure**: configurations, migration `Reservas`, Stripe gateway and webhook parser, `AppSettings`.
- **Host**: `ReservasController`, `StripeWebhookController`, middleware mappings.
- **New NuGet packages**: `Stripe.net` (Infrastructure; official SDK, required by the spec), `libphonenumber-csharp` (Application; E.164 normalization, required by the spec), `Microsoft.Extensions.Logging.Abstractions` (Application; warnings for the honeypot and unknown sessions).
- **Tests**: F02 integration tests now seed real bookings instead of the `TestOcupacionReader` fake, which is removed.
