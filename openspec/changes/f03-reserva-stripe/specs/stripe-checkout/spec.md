## ADDED Requirements

### Requirement: Card booking starts a Stripe Checkout session
`POST /api/reservas/tarjeta` (anonymous, rate limit `reservas`) with `{edicionId, nombre, email, telefono, aceptaCondiciones, website}` SHALL validate the input (`aceptaCondiciones` MUST be `true`), create a `Stripe` booking through `CrearReservaService` with `ExpiraEn = now + StripeMinutosBloqueo + 5 min`, create a Checkout session (EUR, one line of `Importe × 100` cents named `"{Curso} — {fecha Madrid}"`, customer email, `client_reference_id` and metadata with the booking id and code, `ExpiresAt = now + StripeMinutosBloqueo`, success and cancel URLs from `App:BaseUrl`, locale `es`, idempotency key `checkout-{reservaId}`), store the session id and answer `200 {codigo, checkoutUrl}`.

#### Scenario: Booking with free seats
- **WHEN** a valid request is sent for a bookable edition with free seats
- **THEN** the response is `200` with a `CUR-` code and the checkout URL, and the booking is `Pendiente` with its `StripeSessionId`
- **[INTEGRATION]**

#### Scenario: Validation
- **WHEN** `aceptaCondiciones` is false, the email is invalid or the phone is invalid
- **THEN** the response is `400` with errors on those fields
- **[INTEGRATION]**

#### Scenario: Honeypot
- **WHEN** `website` is not empty
- **THEN** the response is `200` with an invented code and no booking is created
- **[INTEGRATION]**

#### Scenario: Stripe failure frees the seat
- **WHEN** the Checkout session cannot be created
- **THEN** the response is `502` `ProblemDetails` and the booking is `Cancelada`
- **[INTEGRATION]**

### Requirement: Verified and idempotent Stripe webhook
`POST /api/stripe/webhook` (anonymous, no rate limit) SHALL verify the `Stripe-Signature` header against the raw body with `Stripe:WebhookSecret` (invalid → `400`) and record each processed event id in `StripeEvento` in the same transaction as its effect; an event id already recorded MUST be answered `200` without changes. Unknown event types and sessions without a booking answer `200`. Unexpected errors answer `500` and roll back the event record.

#### Scenario: Invalid signature
- **WHEN** the signature does not match
- **THEN** the response is `400` and nothing changes
- **[INTEGRATION]**

#### Scenario: Duplicate event
- **WHEN** the same `checkout.session.completed` event is delivered twice
- **THEN** both answers are `200`, the booking is paid once and `ReservaPagadaEvent` is published exactly once
- **[INTEGRATION]**

### Requirement: Webhook effects on bookings
`checkout.session.completed` with `payment_status = paid` SHALL find the booking by session id (or `client_reference_id`) and: `Pendiente` → `Pagada` with `StripePaymentIntentId` and `ReservaPagadaEvent`; `Pagada` → no change; `Expirada`/`Cancelada` → lock the edition and pay it (with `ReservaPagadaEvent`) if a seat is free, otherwise keep the state, set `RequiereRevision` and publish `PagoTrasCaducidadSinPlazaEvent`. `checkout.session.expired` SHALL turn a `Pendiente` booking into `Expirada` and publish `ReservaExpiradaEvent`. Events are published after the commit.

#### Scenario: Completed payment
- **WHEN** a signed `completed` event arrives for a pending booking
- **THEN** the booking is `Pagada` and `ReservaPagadaEvent` is published once
- **[INTEGRATION]**

#### Scenario: Expired session
- **WHEN** a signed `expired` event arrives for a pending booking
- **THEN** the booking is `Expirada`, `ReservaExpiradaEvent` is published and the seat is free again
- **[INTEGRATION]**

#### Scenario: Late payment with a free seat
- **WHEN** `completed` arrives for an `Expirada` booking and the edition still has a seat
- **THEN** the booking becomes `Pagada`
- **[INTEGRATION]**

#### Scenario: Late payment without a seat
- **WHEN** `completed` arrives for an `Expirada` booking and the edition is full
- **THEN** the booking stays `Expirada` with `RequiereRevision = true` and `PagoTrasCaducidadSinPlazaEvent` is published
- **[INTEGRATION]**

#### Scenario: Handler rules with mocks
- **WHEN** the webhook handler receives a duplicate event, an unknown session or an unpaid `completed`
- **THEN** it changes nothing and publishes nothing
- **[UNIT]**
