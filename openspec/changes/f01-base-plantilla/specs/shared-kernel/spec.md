## MODIFIED Requirements

### Requirement: Entidad base auditable
La capa Domain SHALL proveer `BaseAuditableEntity` con `Id` de tipo `Guid` y marcas de tiempo `CreatedAt` y `UpdatedAt` de tipo `DateTimeOffset` (UTC, columna `timestamptz`). Estas marcas MUST gestionarse automáticamente por la infraestructura de persistencia a partir del `TimeProvider` inyectado, y NO por los handlers. El código MUST NOT leer `DateTime.UtcNow`/`DateTimeOffset.UtcNow` directamente: el reloj se obtiene siempre de `TimeProvider`, registrado en DI como `TimeProvider.System`.

#### Scenario: Timestamps gestionados en persistencia
- **WHEN** una entidad que hereda de `BaseAuditableEntity` se persiste por primera vez
- **THEN** `CreatedAt` se asigna automáticamente en `SaveChanges` con la hora del `TimeProvider` sin que el handler la setee
- **[INTEGRATION]**

#### Scenario: UpdatedAt cambia al modificar
- **WHEN** una entidad existente se modifica y se persiste
- **THEN** `UpdatedAt` se actualiza automáticamente con la hora del `TimeProvider`
- **[INTEGRATION]**

#### Scenario: El reloj es sustituible en tests
- **WHEN** los tests de integración registran un `FakeTimeProvider`
- **THEN** las marcas de auditoría y las caducidades de los refresh tokens usan la hora de ese `FakeTimeProvider`
- **[INTEGRATION]**
