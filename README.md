# Lyria

Backend de la plataforma Lyria, construido con .NET 10 y Clean Architecture.

## Requisitos

- .NET SDK 10.0.301 o superior
- SQL Server (configuración pendiente)

## Inicio rápido

```bash
# Restaurar dependencias
dotnet restore Lyria.slnx

# Compilar
dotnet build Lyria.slnx --configuration Release

# Ejecutar pruebas
dotnet test Lyria.slnx --configuration Release

# Ejecutar la API
dotnet run --project src/Lyria.Api
```

## Arquitectura

Lyria sigue Clean Architecture con cuatro capas:

| Capa | Proyecto | Responsabilidad |
|------|----------|-----------------|
| Domain | `Lyria.Domain` | Entidades, aggregate roots, value objects, eventos de dominio, IDs tipados, excepciones |
| Application | `Lyria.Application` | Casos de uso, contratos, lógica de aplicación |
| Infrastructure | `Lyria.Infrastructure` | EF Core, SQL Server, servicios externos |
| API | `Lyria.Api` | Controllers ASP.NET Core, configuración HTTP |

## Estructura del proyecto

```
src/
├── Lyria.Domain/           Capa de dominio (sin dependencias externas)
├── Lyria.Application/      Capa de aplicación
├── Lyria.Infrastructure/   Capa de infraestructura (EF Core, SQL Server)
└── Lyria.Api/              API Web (Controllers)

tests/
├── Lyria.Domain.UnitTests/
├── Lyria.Application.UnitTests/
├── Lyria.Infrastructure.IntegrationTests/
├── Lyria.Api.FunctionalTests/
└── Lyria.ArchitectureTests/

docs/
├── product/                Visión, alcance, actores, capacidades
├── domain/                 Glosario, contextos, modelo conceptual, reglas, casos de uso
├── architecture/           Arquitectura, módulos, requisitos, roadmap
├── persistence/            Documentación de persistencia por agregado
├── api/                    Documentación de endpoints API
└── adr/                    Registros de decisiones arquitectónicas (ADR-001 a ADR-021)
```

## Documentación

### Producto

- [Visión del producto](docs/product/product-vision.md)
- [Alcance del MVP](docs/product/mvp-scope.md)
- [Actores y permisos](docs/product/actors-and-permissions.md)
- [Capacidades de negocio](docs/product/business-capabilities.md)

### Dominio

- [Glosario del dominio](docs/domain/domain-glossary.md)
- [Bounded contexts](docs/domain/bounded-contexts.md)
- [Modelo conceptual](docs/domain/conceptual-model.md)
- [Límites de agregados](docs/domain/aggregate-boundaries.md)
- [Value objects](docs/domain/value-objects.md)
- [Reglas de dominio](docs/domain/domain-rules.md)
- [Casos de uso](docs/domain/use-cases.md)
- [Decisiones abiertas](docs/domain/open-decisions.md)

### Arquitectura

- [Visión general de arquitectura](docs/architecture/architecture-overview.md)
- [Mapa de módulos](docs/architecture/module-map.md)
- [Requisitos no funcionales](docs/architecture/non-functional-requirements.md)
- [Roadmap de implementación](docs/architecture/implementation-roadmap.md)
- [Reglas de dependencia](docs/architecture/dependency-rules.md)
- [Estrategia de pruebas](docs/architecture/testing-strategy.md)
- [CQRS y Mediator](docs/architecture/cqrs-and-mediator.md)
- [Building blocks del dominio](docs/architecture/domain-building-blocks.md)

### Dominio

- [EstablishmentCategory](docs/domain/establishment-category.md)

### Persistencia

- [EstablishmentCategory — Persistencia](docs/persistence/establishment-category-persistence.md)

### API

- [EstablishmentCategories — API](docs/api/establishment-categories-api.md)

### Decisiones arquitectónicas

- [ADR-001: .NET 10](docs/adr/ADR-001-dotnet-10.md)
- [ADR-002: Clean Architecture](docs/adr/ADR-002-clean-architecture.md)
- [ADR-003: Controllers](docs/adr/ADR-003-controllers.md)
- [ADR-004: SQL Server y EF Core](docs/adr/ADR-004-sql-server-ef-core.md)
- [ADR-005: CQRS y MediatR](docs/adr/ADR-005-cqrs-mediator.md)
- [ADR-006: Monolito modular](docs/adr/ADR-006-modular-monolith.md)
- [ADR-007: Límites funcionales](docs/adr/ADR-007-functional-boundaries.md)
- [ADR-008: Modelo de adecuación alimentaria](docs/adr/ADR-008-dietary-suitability-model.md)
- [ADR-009: Estrategia de búsqueda](docs/adr/ADR-009-search-strategy.md)
- [ADR-010: Establecimiento y sede como agregados](docs/adr/ADR-010-establishment-and-branch-aggregates.md)
- [ADR-011: Identidad y perfil separados](docs/adr/ADR-011-identity-and-user-profile.md)
- [ADR-012: Roles con alcance](docs/adr/ADR-012-scoped-role-assignments.md)
- [ADR-013: Reseñas y rating](docs/adr/ADR-013-reviews-and-rating-summary.md)
- [ADR-014: Galería de medios de sede](docs/adr/ADR-014-branch-media-gallery.md)
- [ADR-015: Consistencia entre agregados](docs/adr/ADR-015-cross-aggregate-consistency.md)
- [ADR-016: Building blocks del dominio](docs/adr/ADR-016-domain-building-blocks.md)
- [ADR-017: Implementación del mediator](docs/adr/ADR-017-mediator-implementation.md)
- [ADR-018: Patrón Result en Application](docs/adr/ADR-018-application-result-pattern.md)
- [ADR-019: Agregado EstablishmentCategory](docs/adr/ADR-019-establishment-category-aggregate.md)
- [ADR-020: Convenciones de base de datos y EF Core](docs/adr/ADR-020-database-naming-and-ef-conventions.md)
- [ADR-021: Endpoints públicos de consulta de catálogos](docs/adr/ADR-021-public-catalog-query-endpoints.md)

## Convenciones

- **Código**: inglés (clases, interfaces, namespaces, variables)
- **Mensajes al usuario**: español
- **Documentación**: español
