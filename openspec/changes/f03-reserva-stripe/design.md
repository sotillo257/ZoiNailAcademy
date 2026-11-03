## Context

F02 left `Curso`/`Edicion`, `PlazasCalculator` and an `IOcupacionReader` that reported no bookings. F03 adds money: the code must never oversell an edition and never apply the same Stripe payment twice, while the buyer's card data never touches our system (Stripe Checkout hosts the payment page).

## Goals / Non-Goals

**Goals:** `Reserva` + `StripeEvento`; `CrearReservaService` reusable by F04; card checkout, webhook, public status and abandon endpoints; real occupancy.

**Non-Goals:** refunds, async payment methods, emails (F06), manual payments / expiry job / admin actions (F04).

## Aggregates and invariants

**`Reserva`** (aggregate root; references `Edicion` by `EdicionId`, `Restrict`).
- `Create(edicion, codigo, metodo, nombre, email, telefono, expiraEn, now)`: `codigo` matches `^CUR-[23456789ABCDEFGHJKMNPQRSTUVWXYZ]{6}$`, `nombre` not blank (≤120), email/phone not blank, `expiraEn > now`. `Importe` is copied from `edicion.Curso.Precio`; `ConsentimientoEn = now`; state `Pendiente`.
- `OcupaPlaza(now)` and the expression `OcupaPlazaEn(now)` delegate to the rule of `PlazasCalculator` (paid, or pending with `ExpiraEn > now`) so SQL and memory share one definition.
- Transitions (any other → `BusinessRuleException(estado_invalido)` → 409):

| Method | From | To | Side effects |
| --- | --- | --- | --- |
| `AsignarSesionStripe(id)` | `Pendiente` (Stripe) | — | `StripeSessionId` |
| `Pagar(now, paymentIntentId)` | `Pendiente` | `Pagada` | `PagadaEn`, `StripePaymentIntentId` |
| `PagarTrasCaducidad(now, paymentIntentId)` | `Expirada`, `Cancelada` | `Pagada` | `PagadaEn`, `RequiereRevision = false` |
| `RegistrarPagoSinPlaza(paymentIntentId)` | `Expirada`, `Cancelada` | (same) | `RequiereRevision = true` |
| `Expirar(now)` | `Pendiente` | `Expirada` | `ExpiradaEn` |
| `Cancelar(now)` | `Pendiente` | `Cancelada` | `CanceladaEn` |

**`StripeEvento`** (`EventId` PK, `Tipo`, `ProcesadoEn`): append-only record of processed webhook events.

## Decisions

**D-01 Pessimistic lock on the edition row.** Inserting a booking does not modify `Ediciones`, so the edition's `xmin` cannot detect two buyers taking the last seat. `CrearReservaService` opens a transaction (`IAppDbContext.BeginTransactionAsync`), runs `SELECT 1 FROM "Ediciones" WHERE "Id" = @id FOR UPDATE` (`BloquearEdicionAsync`), and only then counts occupied seats and inserts. Concurrent bookings of the same edition are serialized; other editions are not blocked. Under `READ COMMITTED` the second transaction sees the first one's committed booking once the lock is released. The same lock protects late payments in the webhook.

**D-02 The Stripe call happens after the commit.** The lock is not held during network calls. If the Checkout session cannot be created, the booking is cancelled (seat freed) and the request answers `502` (`PaymentProviderException`). The Checkout `ExpiresAt` is `now + StripeMinutosBloqueo` (min 30, Stripe's floor) while the booking's `ExpiraEn` adds 5 minutes, so Stripe's `expired` webhook normally arrives before the F04 job.

**D-03 Two Stripe abstractions.** `IStripeGateway` (create / expire session — network) is replaced by a fake in integration tests. `IStripeWebhookParser` (signature check with `EventUtility.ConstructEvent`, `throwOnApiVersionMismatch: false`, 300 s tolerance) stays real in tests, which sign payloads with HMAC-SHA256 and a test secret. Invalid signature → `ValidationException` → `400`.

**D-04 Webhook idempotency.** In one transaction: if a `StripeEvento` with the event id exists → `200` and nothing else; otherwise insert it, apply the change, commit. A concurrent duplicate blocks on the uncommitted primary key and then fails on `SaveChanges`; that failure is treated as "already processed". Any other exception → `500` and the whole transaction (including the `StripeEvento`) rolls back, so Stripe retries. A session without a matching booking is logged and answered `200` (otherwise Stripe would retry forever). Events are published after the commit and a failing notification handler is logged, never turned into an error response.

**D-05 Late payments.** `completed` on an `Expirada`/`Cancelada` booking locks the edition, recomputes occupancy (the booking itself does not occupy a seat) and either pays it or records `RequiereRevision` and publishes `PagoTrasCaducidadSinPlazaEvent` (the client refunds from Stripe).

**D-06 Booking code.** `CodigoReserva.Generar()` uses `RandomNumberGenerator.GetItems` over the unambiguous alphabet (31⁶ ≈ 8.9·10⁸ codes). The service draws a code not present in the table, up to 5 attempts; the unique index is the final guarantee. Retrying on the index violation itself would abort the PostgreSQL transaction, hence the pre-check.

**D-07 Per-contact limit.** Valid pending bookings (any edition) with the same normalized email **or** phone ≥ `Reservas:MaxPendientesPorContacto` → `TooManyRequestsException(limite_pendientes)` → `429`. Checked inside the locked transaction; two simultaneous requests for different editions from the same contact can exceed it by one, which is accepted (it only limits seat hoarding).

**D-08 Honeypot.** A filled `website` returns `200` with an invented code and the success URL, logs a warning and writes nothing. It is checked in the handler (after validation), so a bot cannot tell it apart from success.

**D-09 Phone normalization.** `TelefonoNormalizer` (Application, `libphonenumber-csharp`, default region `ES`) accepts national, `+34` and `0034` forms and returns E.164; the validator reports invalid phones on `telefono`.

**D-10 Abandon.** `POST /api/reservas/{codigo}/abandonar`: only a `Stripe` + `Pendiente` booking is touched. The session is expired in Stripe first; if Stripe reports it already completed, nothing changes (the webhook will pay it). A concurrent change (`xmin`) is ignored: the endpoint always answers `204`.

**D-11 Public status.** `GET /api/reservas/{codigo}` returns `ReservaPublicaDto` without name, email or phone. `expiraEn` is only filled while `Pendiente`; `datosPago`/`whatsappUrl` are `null` until F04.

**D-12 Settings.** `IReservasSettings` gains `StripeMinutosBloqueo` (clamped to ≥ 30) and `MaxPendientesPorContacto`; `IAppSettings` exposes `BaseUrl` and `TimeZone` (`App:TimeZone`, used for the line-item name `"{Curso} — dd/MM/yyyy HH:mm"` in Madrid time). `StripeOptions` (`SecretKey`, `WebhookSecret`) stays in Infrastructure.

**D-13 New packages.** `Stripe.net` 52.4.2 (Infrastructure), `libphonenumber-csharp` 9.0.41 (Application), `Microsoft.Extensions.Logging.Abstractions` 10.0.x (Application). No new project references.

## Endpoints

| Method | Route | Auth | Request | Success | Errors |
| --- | --- | --- | --- | --- | --- |
| POST | `/api/reservas/tarjeta` | anonymous, `reservas` | `{edicionId, nombre, email, telefono, aceptaCondiciones, website}` | `200 {codigo, checkoutUrl}` | `400`, `404 no_encontrado`, `409 edicion_no_reservable / sin_plazas`, `429 limite_pendientes`, `502` |
| POST | `/api/stripe/webhook` | anonymous, no rate limit | raw Stripe event + `Stripe-Signature` | `200` | `400` invalid signature, `500` (Stripe retries) |
| GET | `/api/reservas/{codigo}` | anonymous | — | `200 ReservaPublicaDto` | `404 no_encontrado` |
| POST | `/api/reservas/{codigo}/abandonar` | anonymous, `reservas` | — | `204` | `404 no_encontrado` |

`ReservaPublicaDto` = `{codigo, estado, metodo, curso, inicio, importe, expiraEn, datosPago, whatsappUrl}` (contract). Errors are `application/problem+json`; business errors carry `code`.

## Risks / Trade-offs

- [The Next.js rewrite could alter the raw body and break the signature] → `[MANUAL]` check with `stripe trigger` in the deployed environment; plan B in F10 (webhook directly to the API domain).
- [Row lock contention on a very popular edition] → only bookings of that edition queue, and each transaction is a few short statements.
- [Stripe succeeds but saving `StripeSessionId` fails] → the webhook falls back to `client_reference_id` (the booking id).
- [Booking abandoned while the payment is being completed] → abandon checks Stripe first; a late `completed` still goes through the late-payment path.

## Migration Plan

Migration `Reservas` (tables `Reservas`, `StripeEventos`). Before going live the Stripe test keys and the webhook secret are set on Railway (F10).

## Open Questions

- Whether prices include VAT (PRD) — affects only the amount shown, not the flow.
