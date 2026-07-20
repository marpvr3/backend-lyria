# Reglas de dependencia — Lyria

## Regla general

Las dependencias siempre apuntan hacia el centro (Domain). Ninguna capa interna puede conocer a una capa externa.

## Dependencias entre proyectos

```
Domain          <- sin dependencias
Application     <- Domain
Infrastructure  <- Application, Domain
Api             <- Application, Infrastructure
```

## Dependencias prohibidas

| Origen | No puede depender de |
|--------|---------------------|
| Domain | Application, Infrastructure, Api |
| Application | Infrastructure, Api |
| Infrastructure | Api |

Estas reglas están validadas automáticamente por pruebas arquitectónicas en `Lyria.ArchitectureTests`.

## Restricciones de paquetes por capa

### Domain

No debe depender de:
- Entity Framework Core (`Microsoft.EntityFrameworkCore`)
- ASP.NET Core (`Microsoft.AspNetCore`)
- MediatR
- FluentValidation
- Ningún paquete externo

### Application

No debe depender de:
- Entity Framework Core
- ASP.NET Core
- SQL Server
- HttpContext

Puede depender de:
- `Microsoft.Extensions.DependencyInjection.Abstractions`

### Infrastructure

Puede depender de:
- Entity Framework Core
- SQL Server provider
- Paquetes de servicios externos

### Api

Puede depender de:
- ASP.NET Core
- OpenAPI
- Scalar
- EF Core Design (herramienta de migraciones)

## Validación

Las pruebas en `Lyria.ArchitectureTests` verifican estas reglas usando ArchUnitNET:

1. Domain no depende de Application, Infrastructure ni Api.
2. Application no depende de Infrastructure ni Api.
3. Infrastructure no depende de Api.
4. Controllers residen en Api.
5. Domain no depende de EF Core, ASP.NET Core ni MediatR.
