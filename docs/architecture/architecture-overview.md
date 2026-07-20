# Visión general de arquitectura — Lyria

## Introducción

Lyria es una aplicación backend construida con .NET 10 que sigue los principios de Clean Architecture y Domain-Driven Design (DDD) táctico.

## Capas

### Domain (`Lyria.Domain`)

Capa central que contiene la lógica de negocio pura. No depende de ninguna otra capa ni de paquetes externos.

Contiene:
- Entidades (`Entity<TId>`) — identidad tipada y igualdad por identidad con manejo de entidades transitorias
- Aggregate roots (`AggregateRoot<TId>`) — hereda Entity y gestiona eventos de dominio pendientes
- Value objects (`ValueObject`) — igualdad estructural por componentes
- Eventos de dominio (`IDomainEvent`) — interfaz marcadora sin propiedades
- Identificadores tipados (`IStronglyTypedId<TValue>`) — contrato para IDs fuertemente tipados
- Excepciones de dominio (`DomainException`) — clase base abstracta sin conceptos HTTP

### Application (`Lyria.Application`)

Capa de casos de uso que orquesta la lógica de dominio. Depende únicamente de Domain.

Organizada por vertical slices:
- `Abstractions/`: contratos CQRS (`Messaging/`) y puertos de persistencia (`Persistence/`)
- `Common/`: utilidades compartidas (`Results/`, `Errors/`, `Behaviors/`)
- `Features/`: funcionalidades organizadas por dominio (vertical slices)

### Infrastructure (`Lyria.Infrastructure`)

Implementaciones técnicas y acceso a sistemas externos. Depende de Application y Domain.

Responsabilidades:
- Entity Framework Core con SQL Server
- Implementaciones de repositorios
- Servicios externos
- Observabilidad

### API (`Lyria.Api`)

Punto de entrada HTTP. Depende de Application e Infrastructure.

Características:
- ASP.NET Core Controllers (Minimal APIs prohibidas)
- Problem Details para manejo de errores
- OpenAPI para documentación
- Scalar como UI de API

## Arquitectura modular

Lyria adopta una arquitectura de **monolito modular** ([ADR-006](../adr/ADR-006-modular-monolith.md)). El dominio se organiza en módulos con límites explícitos ([ADR-007](../adr/ADR-007-functional-boundaries.md)):

| Módulo | Responsabilidad |
|--------|----------------|
| Identity & Access | Cuentas, credenciales, roles, permisos, alcances |
| User Profiles | Perfil personal, necesidades alimentarias del usuario |
| Establishments | Establecimientos, sedes, categorías, servicios, horarios |
| Dietary Catalog | Necesidades alimentarias, niveles de adecuación |
| Search & Discovery | Búsquedas, filtros, proyecciones de lectura |
| Favorites | Sedes favoritas del usuario |
| Reviews | Reseñas, rating, resumen de puntuación |
| Media | Metadatos de imágenes |
| Moderation | Reportes, aprobaciones, verificación |
| Administration | Superficie de aplicación sobre otros módulos |

Para más detalle, consulta el [mapa de módulos](module-map.md) y los [límites funcionales](../domain/bounded-contexts.md).

## Principios

1. **Dependency Rule**: las dependencias apuntan hacia adentro (API -> Infrastructure -> Application -> Domain).
2. **Separación de responsabilidades**: cada capa tiene un propósito claro.
3. **Domain puro**: sin dependencias de infraestructura.
4. **CQRS por vertical slices**: separación de comandos y consultas con martinothamar/Mediator ([ADR-017](../adr/ADR-017-mediator-implementation.md)).
5. **Módulos con límites explícitos**: cada módulo es propietario de sus datos y reglas.
6. **Comunicación por identificadores**: los módulos se relacionan mediante IDs, no comparten modelos internos.

## Tecnologías

| Componente | Tecnología |
|-----------|-----------|
| Runtime | .NET 10 LTS |
| Lenguaje | C# 14 |
| API | ASP.NET Core Controllers |
| ORM | Entity Framework Core 10 |
| Base de datos | SQL Server |
| Pruebas | xUnit v3 |
| Documentación API | OpenAPI + Scalar |
| Mediator | martinothamar/Mediator 3.0.x (source generator) |
| Validación | FluentValidation 12.x |

## Documentación relacionada

- [CQRS y Mediator](cqrs-and-mediator.md)
- [Building blocks del dominio](domain-building-blocks.md)
- [Mapa de módulos](module-map.md)
- [Requisitos no funcionales](non-functional-requirements.md)
- [Roadmap de implementación](implementation-roadmap.md)
- [Reglas de dependencia](dependency-rules.md)
- [Estrategia de pruebas](testing-strategy.md)
- [Persistencia de EstablishmentCategory](../persistence/establishment-category-persistence.md)
- [API de EstablishmentCategories](../api/establishment-categories-api.md)
