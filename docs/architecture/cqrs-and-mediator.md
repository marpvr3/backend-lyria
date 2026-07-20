# CQRS y Mediator — Arquitectura de Lyria

## Patrón CQRS

Lyria adopta CQRS (Command Query Responsibility Segregation) como patrón arquitectónico para organizar los casos de uso. Los comandos (operaciones de escritura) y las queries (operaciones de lectura) se separan en tipos distintos con handlers independientes.

### Organización

Los casos de uso se organizan en vertical slices dentro de `Lyria.Application/Features`:

```
Features/
└── EstablishmentCategories/
    ├── EstablishmentCategoryResponse.cs    # DTO de respuesta compartido
    ├── EstablishmentCategoryErrors.cs      # Errores específicos del feature
    ├── Create/
    │   ├── CreateEstablishmentCategoryCommand.cs
    │   ├── CreateEstablishmentCategoryCommandHandler.cs
    │   └── CreateEstablishmentCategoryCommandValidator.cs
    ├── Update/
    ├── Deactivate/
    ├── Reactivate/
    ├── GetById/
    └── ListActive/
```

## Librería Mediator

Se utiliza **martinothamar/Mediator** v3.0.x (licencia MIT), un source generator que produce el código del mediator en tiempo de compilación. Ver [ADR-017](../adr/ADR-017-mediator-implementation.md) para la justificación de la decisión.

### Paquetes

| Paquete | Proyecto | Propósito |
|---------|----------|-----------|
| `Mediator.Abstractions` | `Lyria.Application` | Interfaces base (`ICommand`, `IQuery`, etc.) |
| `Mediator.SourceGenerator` | `Lyria.Api` (PrivateAssets=all) | Genera el mediator en compilación |

### Registro

```csharp
// En Lyria.Api/Extensions/ServiceCollectionExtensions.cs
services.AddMediator();  // Método generado por el source generator
```

## Contratos CQRS de Lyria

Los tipos de negocio no referencian directamente los tipos de Mediator. Se definen wrappers en `Lyria.Application.Abstractions.Messaging`:

### Comandos

```csharp
// Comando sin valor de retorno → Result
public interface ICommand : Mediator.ICommand<Result>;

// Comando con valor de retorno → Result<TResponse>
public interface ICommand<TResponse> : Mediator.ICommand<Result<TResponse>>;

// Handlers
public interface ICommandHandler<in TCommand>
    : Mediator.ICommandHandler<TCommand, Result>
    where TCommand : ICommand;

public interface ICommandHandler<in TCommand, TResponse>
    : Mediator.ICommandHandler<TCommand, Result<TResponse>>
    where TCommand : ICommand<TResponse>;
```

### Queries

```csharp
public interface IQuery<out TResponse> : Mediator.IQuery<TResponse>;

public interface IQueryHandler<in TQuery, TResponse>
    : Mediator.IQueryHandler<TQuery, TResponse>
    where TQuery : IQuery<TResponse>;
```

## Patrón Result

Los comandos retornan `Result` o `Result<T>` (ubicados en `Application.Common.Results`). Esto permite comunicar éxito/fracaso sin excepciones para flujos esperados.

Ver [ADR-018](../adr/ADR-018-application-result-pattern.md) para detalles del patrón Result.

## Covarianza en los contratos

`IQuery<out TResponse>` utiliza covarianza (`out`) porque `TResponse` solo aparece en posición de salida en la interfaz base `Mediator.IQuery<TResponse>`. Esto permite asignar un `IQuery<Result<X>>` a una variable de tipo más general cuando sea necesario.

`ICommand<TResponse>` **no** utiliza covarianza porque:
1. La interfaz base `Mediator.ICommand<TResult>` no declara `TResult` como covariante.
2. El source generator de Mediator requiere tipos concretos para generar el código del mediator.
3. Los comandos son sealed records con un tipo de respuesta fijo, por lo que la covarianza no aporta beneficio práctico.

La decisión de mantener `ICommand<TResponse>` invariante es consistente con los contratos base de Mediator y no limita la funcionalidad del sistema.

## Pipeline de validación

Se utiliza FluentValidation integrado con el pipeline de Mediator mediante `ValidationBehavior<TMessage, TResponse>`:

1. Antes de ejecutar el handler, se ejecutan todos los validadores registrados para el tipo de mensaje.
2. Si hay errores de validación, se retorna `Result.Failure` con todos los errores concatenados (no se ejecuta el handler).
3. Si no hay errores, se ejecuta el handler normalmente.

### Construcción del resultado de validación

Para crear `Result.Failure<T>` sin conocer `T` en tiempo de compilación, `ValidationBehavior` utiliza un delegate compilado una única vez por tipo genérico mediante `Expression.Lambda`. Esto evita reflexión frágil (`MakeGenericMethod`, `MethodInfo.Invoke`) en cada invocación.

### Registro

```csharp
// En Lyria.Application/DependencyInjection.cs
services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);
services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

// En Lyria.Api/Extensions/ServiceCollectionExtensions.cs
services.AddMediator(options => options.ServiceLifetime = ServiceLifetime.Scoped);
```

Los handlers se registran como **Scoped** para ser compatibles con las dependencias de infraestructura (DbContext, repositorios), que también son scoped.

## Documentación relacionada

- [ADR-005: CQRS y MediatR](../adr/ADR-005-cqrs-mediator.md) (superseded por ADR-017)
- [ADR-017: Implementación del mediator](../adr/ADR-017-mediator-implementation.md)
- [ADR-018: Patrón Result en Application](../adr/ADR-018-application-result-pattern.md)
