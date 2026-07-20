# ADR-016: Building blocks del dominio

## Estado

Aceptado

## Fecha

2026-07-14

## Contexto

La Fase 3 del roadmap de implementación establece la necesidad de definir las abstracciones base del dominio que serán reutilizadas por todos los agregados de las fases siguientes. Estas abstracciones deben seguir los principios de Domain-Driven Design táctico y respetar las restricciones de Clean Architecture: sin dependencias externas en la capa de dominio.

Se requiere decidir sobre:
1. La semántica de igualdad de entidades, incluyendo el manejo de entidades transitorias.
2. La ubicación de la colección de eventos de dominio (Entity vs AggregateRoot).
3. La forma del contrato de eventos de dominio (interfaz con propiedades vs marker interface).
4. El contrato para identificadores fuertemente tipados.
5. Las abstracciones que explícitamente NO se incluyen en esta fase.

## Decisión

### Abstracciones incluidas

Se implementan seis abstracciones base en `Lyria.Domain`:

| Abstracción | Namespace | Responsabilidad |
|-------------|-----------|----------------|
| `Entity<TId>` | `Lyria.Domain.Abstractions` | Identidad tipada y igualdad por identidad |
| `AggregateRoot<TId>` | `Lyria.Domain.Abstractions` | Hereda Entity, agrega eventos de dominio |
| `ValueObject` | `Lyria.Domain.Abstractions` | Igualdad estructural por componentes |
| `IDomainEvent` | `Lyria.Domain.Abstractions` | Marker interface para eventos de dominio |
| `IStronglyTypedId<TValue>` | `Lyria.Domain.Abstractions` | Contrato para IDs fuertemente tipados |
| `DomainException` | `Lyria.Domain.Exceptions` | Excepción base para errores de dominio |

### Decisiones clave

**Igualdad de Entity:** las entidades se comparan por tipo e identidad. Entidades transitorias (con `Id` igual al valor por defecto del tipo) solo son iguales por referencia, nunca por identidad. Esto previene que dos entidades nuevas con `Guid.Empty` se consideren iguales.

**Eventos de dominio en AggregateRoot:** la colección de eventos de dominio se ubica en `AggregateRoot`, no en `Entity`. Solo los aggregate roots pueden publicar eventos, lo que es consistente con DDD: los eventos representan hechos de negocio que ocurren a nivel de agregado.

**IDomainEvent como marker interface:** no se incluye `OccurredOnUtc` ni ninguna otra propiedad. El timestamp es una preocupación de infraestructura que se asigna al despachar o persistir el evento. Esto evita acoplar el dominio a decisiones de infraestructura y mantiene la interfaz libre de dependencias a MediatR.

**IStronglyTypedId como contrato:** se define como interfaz con una propiedad `Value` covariante. Los identificadores concretos se implementarán como `record struct` en los módulos correspondientes, obteniendo igualdad por valor sin asignación en el heap.

**DomainException sin HTTP:** la excepción base no incluye status codes, ProblemDetails ni ningún concepto HTTP. La traducción a respuestas HTTP es responsabilidad de la capa API.

### Abstracciones explícitamente rechazadas

Las siguientes abstracciones fueron evaluadas y descartadas para esta fase:

| Abstracción | Razón de exclusión |
|-------------|-------------------|
| `Result<T>` | Añade complejidad sin caso de uso concreto que lo justifique. Se puede introducir cuando haya un patrón claro de uso en la capa de Application. |
| `AuditableEntity` | Los metadatos de auditoría (`CreatedAt`, `UpdatedAt`) son preocupación de infraestructura. EF Core puede manejarlos mediante shadow properties o interceptors. |
| Soft delete / `IsDeleted` | Decisión de persistencia, no de dominio. Si se necesita, se implementa en Infrastructure. |
| `IRepository<T>` genérico | Viola el principio de no crear abstracciones prematuras. Cada módulo definirá sus propios contratos de repositorio. |
| `IUnitOfWork` | Pertenece a la capa de Application, no a Domain. Se implementará cuando se desarrollen los casos de uso. |
| Guard library | Los constructores de entidades y value objects validan sus propias invariantes directamente. |
| `IDomainService` | Marker interface sin valor semántico. Los servicios de dominio se definen como clases concretas cuando se necesitan. |
| `IDateTimeProvider` | Pertenece a la capa de Application como abstracción de infraestructura. |

## Consecuencias

### Positivas

- Las abstracciones base son mínimas y sin dependencias externas, consistente con Clean Architecture.
- La semántica de igualdad cubre todos los escenarios de comparación de entidades, incluyendo el caso transitorio.
- Los eventos de dominio están correctamente ubicados en AggregateRoot, no en Entity.
- El contrato `IStronglyTypedId` permite IDs tipados sin acoplar a un tipo primitivo específico.
- Las abstracciones rechazadas reducen la complejidad accidental del dominio.

### Negativas

- Las entidades que no son aggregate roots no pueden publicar eventos de dominio directamente. Esto es intencional pero requiere que los aggregate roots coordinen la publicación de eventos de sus entidades internas.
- La ausencia de `Result<T>` significa que los errores de dominio se comunican mediante excepciones. Si esto se convierte en un problema de rendimiento o ergonomía, se reconsiderará en una fase futura.

### Riesgos

- Si la detección de entidades transitorias por valor por defecto resulta insuficiente para algún tipo de `TId`, se necesitará refactorizar la lógica de `IsTransient()`.
