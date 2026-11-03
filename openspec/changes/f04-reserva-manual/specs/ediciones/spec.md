## ADDED Requirements

### Requirement: Cancelling an edition cancels its pending bookings
When an edition moves to `Cancelada`, every `Pendiente` booking of that edition SHALL become `Cancelada` in the same save, its Stripe session (if any) MUST be expired after the commit, and one `ReservaCanceladaEvent` per booking MUST be published. Paid bookings MUST NOT change; the response warns `Tiene X inscritos: avisa y reembolsa manualmente` with X = paid bookings.

#### Scenario: Cascade
- **WHEN** an edition with one paid and two pending bookings is cancelled
- **THEN** both pending bookings are `Cancelada`, the paid one stays `Pagada`, and `avisos` contains `Tiene 1 inscritos: avisa y reembolsa manualmente`
- **[INTEGRATION]**
