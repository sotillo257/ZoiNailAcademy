## ADDED Requirements

### Requirement: Business error codes in ProblemDetails
Expected business failures SHALL carry a code from the contract table (`Docs/00-contrato-y-convenciones.md`) in `ProblemDetails.extensions.code`. The Domain layer SHALL provide `BusinessRuleException(code, message, extensions)` and `ErrorCodes`; `ConflictException` SHALL accept an optional code and extensions. The Host middleware MUST map `BusinessRuleException` and `ConflictException` to `409`, writing `code` and every extra extension at the top level of the `ProblemDetails` JSON, and MUST add `code = no_encontrado` to every `404` produced by `NotFoundException`. `DomainException` keeps mapping to `400` without a code.

#### Scenario: Business rule violation becomes a coded 409
- **WHEN** a handler lets a `BusinessRuleException("plazas_por_debajo_de_ocupadas", ..., {ocupadas: 5})` escape
- **THEN** the response is `409` `application/problem+json` with `code = plazas_por_debajo_de_ocupadas` and `ocupadas = 5`
- **[INTEGRATION]**

#### Scenario: Not found carries its code
- **WHEN** a handler throws `NotFoundException`
- **THEN** the response is `404` with `code = no_encontrado`
- **[INTEGRATION]**
