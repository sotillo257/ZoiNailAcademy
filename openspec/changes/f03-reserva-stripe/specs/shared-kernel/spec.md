## ADDED Requirements

### Requirement: Rate-limit and payment-provider error mappings
`TooManyRequestsException(code, message)` SHALL map to `429` with `code` in the `ProblemDetails`, and `PaymentProviderException` SHALL map to `502` with a Spanish `detail` and without leaking provider details.

#### Scenario: Coded 429
- **WHEN** a handler throws `TooManyRequestsException("limite_pendientes", …)`
- **THEN** the response is `429` `application/problem+json` with `code = limite_pendientes`
- **[INTEGRATION]**

#### Scenario: 502 on provider failure
- **WHEN** a handler throws `PaymentProviderException`
- **THEN** the response is `502` with detail `No se pudo iniciar el pago, inténtalo de nuevo`
- **[INTEGRATION]**
