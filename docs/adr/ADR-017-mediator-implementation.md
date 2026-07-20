# ADR-017: Implementación del mediator — martinothamar/Mediator

## Estado

Aceptado — Supersede ADR-005

## Fecha

2026-07-14

## Contexto

ADR-005 adoptó CQRS como patrón arquitectónico pero difirió la elección de librería mediator por cuestiones de licencia con MediatR (licencia dual desde v12). Se necesita resolver esta decisión para implementar la primera vertical slice (`EstablishmentCategory`).

Se evaluaron las siguientes opciones:

1. **MediatR** (Jimmy Bogard): la librería más popular del ecosistema .NET. Sin embargo, desde la versión 12, adoptó un modelo de licencia dual (MIT para uso no comercial, licencia comercial para uso comercial). Esto introduce incertidumbre legal.

2. **martinothamar/Mediator**: source generator que genera el mediator en tiempo de compilación. Licencia MIT sin restricciones. API compatible con MediatR. Soporte para `ICommand<TResponse>`, `IQuery<TResponse>`, `IPipelineBehavior`. Versión estable 3.0.x.

3. **Mediator casero**: descartado explícitamente en ADR-005 por complejidad de mantenimiento y riesgo de bugs.

4. **Invocación directa de handlers**: descartado porque eliminaría los beneficios del pipeline (validación, logging, etc.) y requeriría cableado manual.

## Decisión

Se adopta **martinothamar/Mediator** v3.0.x como implementación del patrón mediator:

- `Mediator.Abstractions` (3.0.2): paquete de abstracciones, referenciado por `Lyria.Application`.
- `Mediator.SourceGenerator` (3.0.2): generador de código, referenciado exclusivamente por `Lyria.Api` como `PrivateAssets=all`.

### Contratos CQRS propios

Se definen wrappers en `Lyria.Application.Abstractions.Messaging` para desacoplar el código de aplicación de los tipos del mediator:

| Contrato Lyria | Extiende |
|----------------|----------|
| `ICommand` | `Mediator.ICommand<Result>` |
| `ICommand<TResponse>` | `Mediator.ICommand<Result<TResponse>>` |
| `ICommandHandler<TCommand>` | `Mediator.ICommandHandler<TCommand, Result>` |
| `ICommandHandler<TCommand, TResponse>` | `Mediator.ICommandHandler<TCommand, Result<TResponse>>` |
| `IQuery<TResponse>` | `Mediator.IQuery<TResponse>` |
| `IQueryHandler<TQuery, TResponse>` | `Mediator.IQueryHandler<TQuery, TResponse>` |

Los handlers y comandos/queries referencian los contratos de Lyria, no los de Mediator directamente.

### Registro en DI

`services.AddMediator()` se invoca en `Lyria.Api` (método generado por el source generator). El source generator descubre automáticamente todos los handlers y los registra.

## Consecuencias

### Positivas

- Licencia MIT sin restricciones comerciales.
- Source generator: sin reflexión en runtime, mejor rendimiento y detección de errores en compilación.
- API compatible con MediatR, facilita migración futura si se desea.
- Los wrappers desacoplan el código de aplicación de la librería concreta.

### Negativas

- Menor comunidad y documentación que MediatR.
- El source generator requiere que `Mediator.SourceGenerator` esté en el proyecto de entrada (`Lyria.Api`).
- Si Mediator deja de mantenerse, sería necesario evaluar alternativas (el desacoplamiento via wrappers mitiga este riesgo).

### Relación con ADR-005

ADR-005 queda **superseded** por esta decisión. CQRS como patrón arquitectónico se mantiene vigente; la decisión diferida del mediator se resuelve aquí.
