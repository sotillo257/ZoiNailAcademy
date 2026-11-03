# shared-kernel Specification

## Purpose

Describe los bloques transversales compartidos entre capas: patrón Result, entidad base auditable, jerarquía de excepciones, validación por pipeline, abstracción de acceso a datos, contexto del usuario actual y registro de MediatR/FluentValidation.

## Requirements

### Requirement: Result pattern
La capa Application SHALL proveer un tipo `Result` y `Result<T>` para representar el éxito o el fallo esperado de una operación sin lanzar excepciones, con fábricas `Success`/`Failure` y acceso a si es exitoso, al valor y al error.

#### Scenario: Resultado exitoso expone el valor
- **WHEN** se crea un `Result<T>.Success(value)`
- **THEN** `IsSuccess` es verdadero y el valor es accesible

#### Scenario: Resultado fallido expone el error
- **WHEN** se crea un `Result<T>.Failure(error)`
- **THEN** `IsSuccess` es falso y el error es accesible, y el valor no se considera válido

### Requirement: Entidad base auditable
La capa Domain SHALL proveer `BaseAuditableEntity` con `Id` de tipo `Guid` y marcas de tiempo `CreatedAt` y `UpdatedAt`. Estas marcas MUST gestionarse automáticamente por la infraestructura de persistencia y NO por los handlers.

#### Scenario: Timestamps gestionados en persistencia
- **WHEN** una entidad que hereda de `BaseAuditableEntity` se persiste por primera vez
- **THEN** `CreatedAt` y `UpdatedAt` se asignan automáticamente en `SaveChanges` sin que el handler los setee

#### Scenario: UpdatedAt cambia al modificar
- **WHEN** una entidad existente se modifica y se persiste
- **THEN** `UpdatedAt` se actualiza automáticamente

### Requirement: Jerarquía de excepciones de aplicación
La capa Application SHALL definir excepciones tipadas para condiciones de error esperadas: `NotFoundException`, `ConflictException`, `UnauthorizedException`, `ForbiddenException` y `ValidationException`; y la capa Domain SHALL definir `DomainException` para violaciones de invariantes de negocio. El middleware del Host MUST mapear cada una a su código HTTP correspondiente (404, 409, 401, 403, 400).

#### Scenario: Cada excepción mapea a su código HTTP
- **WHEN** un handler lanza una de las excepciones de aplicación
- **THEN** el middleware del Host responde con el código HTTP correspondiente y un cuerpo `ProblemDetails` (RFC 7807); `ValidationException` usa `ValidationProblemDetails` con los errores por campo

### Requirement: Validación por pipeline
La capa Application SHALL ejecutar la validación de FluentValidation mediante un `ValidationBehavior` registrado como `IPipelineBehavior` de MediatR, de modo que cualquier request con un validador asociado se valide antes de llegar a su handler. Los mensajes de validación MUST estar en español.

#### Scenario: Request inválido se rechaza antes del handler
- **WHEN** se envía un request que incumple su validador de FluentValidation
- **THEN** el `ValidationBehavior` lanza `ValidationException` y el handler no se ejecuta

#### Scenario: Request válido llega al handler
- **WHEN** se envía un request que cumple su validador (o que no tiene validador)
- **THEN** el handler se ejecuta normalmente

### Requirement: Abstracción de acceso a datos
La capa Application SHALL definir `IAppDbContext` que exponga los `DbSet` de las entidades y `SaveChangesAsync`, y los handlers MUST depender de esta interfaz para acceder a datos. El sistema MUST NOT incluir un `IRepository<T>` genérico, domain events, outbox ni specification pattern.

#### Scenario: Los handlers dependen de la abstracción
- **WHEN** un handler necesita acceder a datos
- **THEN** lo hace a través de `IAppDbContext` inyectada, no de una implementación concreta ni de un repositorio genérico

#### Scenario: Infraestructura provee la implementación
- **WHEN** la aplicación arranca
- **THEN** `IAppDbContext` se resuelve al `AppDbContext` de EF Core sobre PostgreSQL registrado en la DI de Infrastructure

### Requirement: Contexto del usuario actual
La capa Application SHALL definir `ICurrentUserService` que exponga el identificador del usuario autenticado, y el Host MUST proveer una implementación que lo lea de los claims del principal de la petición, para soportar el aislamiento de datos por usuario.

#### Scenario: Devuelve el usuario autenticado
- **WHEN** hay una petición autenticada en curso
- **THEN** `ICurrentUserService` devuelve el identificador del usuario tomado de sus claims

### Requirement: Registro de MediatR y FluentValidation
La capa Application SHALL registrar MediatR (pineado en la versión 12.x, Apache 2.0), sus validadores de FluentValidation del ensamblado y el `ValidationBehavior` mediante un método de extensión de DI propio de la capa.

#### Scenario: MediatR y validadores quedan registrados
- **WHEN** se invoca el método de registro de DI de Application en el arranque
- **THEN** MediatR, los validadores del ensamblado y el `ValidationBehavior` quedan disponibles en el contenedor
