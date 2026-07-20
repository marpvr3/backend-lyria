# ADR-018: Patrón Result en la capa de Application

## Estado

Aceptado

## Fecha

2026-07-14

## Contexto

En ADR-016 se descartó `Result<T>` del dominio por no tener caso de uso concreto. Con la implementación de la primera vertical slice (`EstablishmentCategory`), surge la necesidad de comunicar éxito/fracaso desde los handlers de Application sin usar excepciones para flujos esperados (entidad no encontrada, código duplicado, validación fallida).

Se evaluaron las opciones:

1. **Excepciones para todo**: simple, pero usar excepciones para flujos esperados (NotFound, conflicto de unicidad) tiene costo de rendimiento y mezcla errores de programación con resultados de negocio.
2. **Librería externa (ErrorOr, FluentResults)**: añade una dependencia para un concepto simple. La API puede no ajustarse exactamente a las necesidades del proyecto.
3. **Result propio en Application**: implementación mínima (dos clases, un record, un enum), sin dependencias, ubicada donde se usa.

## Decisión

Se implementa un patrón Result propio en `Lyria.Application.Common`:

### Tipos

| Tipo | Namespace | Responsabilidad |
|------|-----------|----------------|
| `Result` | `Application.Common.Results` | Resultado sin valor de retorno. Propiedades: `IsSuccess`, `IsFailure`, `Error`. |
| `Result<TValue>` | `Application.Common.Results` | Resultado con valor de retorno. Hereda `Result`, agrega propiedad `Value`. |
| `Error` | `Application.Common.Errors` | Record sellado con `Code`, `Description`, `Type`. Métodos factory estáticos. |
| `ErrorType` | `Application.Common.Errors` | Enum: `Validation`, `NotFound`, `Conflict`, `Forbidden`, `Failure`. |

### Invariantes

- Un resultado exitoso no puede tener error (debe ser `Error.None`).
- Un resultado fallido debe tener error (no puede ser `Error.None`).
- Acceder a `Value` en un `Result<T>` fallido lanza `InvalidOperationException`.

### Ubicación

Exclusivamente en `Lyria.Application.Common`. **No en Domain** — el dominio sigue usando excepciones (`DomainException`) para proteger sus invariantes. Esto es intencional: las invariantes de dominio son incumplimientos de programación, no resultados esperados.

### Integración con CQRS

Los contratos CQRS envuelven `Result` automáticamente:

- `ICommand : Mediator.ICommand<Result>` — handlers de comando retornan `Result`.
- `ICommand<TResponse> : Mediator.ICommand<Result<TResponse>>` — handlers retornan `Result<TResponse>`.
- Queries retornan el tipo directamente (ej. `IQuery<Result<EstablishmentCategoryResponse>>`).

## Consecuencias

### Positivas

- Los handlers expresan éxito/fracaso sin excepciones para flujos esperados.
- `ErrorType` permite a la capa API mapear a HTTP status codes sin acoplar Application a HTTP.
- Sin dependencias externas.
- Invariantes protegidas en constructor previenen estados inválidos.

### Negativas

- Código propio que mantener (mínimo: 2 clases + 1 record + 1 enum).
- Los desarrolladores deben aprender a usar `Result` en lugar de excepciones para flujos esperados.
