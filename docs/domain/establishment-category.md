# EstablishmentCategory — Documentación de dominio

## Descripción

`EstablishmentCategory` es un agregado de referencia del módulo **Establishments** que representa una categoría de establecimiento gastronómico (restaurante, cafetería, bar, panadería, heladería, etc.). Es un catálogo administrado con escritura infrecuente y lectura frecuente.

## Ubicación

- **Namespace:** `Lyria.Domain.Establishments.Categories`
- **Módulo:** Establishments
- **Aggregate root:** `EstablishmentCategory`
- **Identificador:** `EstablishmentCategoryId` (`readonly record struct`, `IStronglyTypedId<Guid>`)

## Propiedades

| Propiedad | Tipo | Nullable | Restricciones |
|-----------|------|----------|--------------|
| `Id` | `EstablishmentCategoryId` | No | GUID, asignado en creación |
| `Code` | `string` | No | 2–50 caracteres, regex `^[A-Z0-9](_?[A-Z0-9])*$`, normalizado a mayúsculas |
| `Name` | `string` | No | 2–100 caracteres, normalizado (trim + reducción de espacios) |
| `Description` | `string?` | Sí | Máximo 500 caracteres, vacío se convierte a null |
| `SortOrder` | `int` | No | ≥ 0 |
| `IsActive` | `bool` | No | `true` al crear |

## Reglas de validación del código

El código se normaliza a mayúsculas y debe cumplir:
- Longitud entre 2 y 50 caracteres.
- Solo letras mayúsculas (A-Z), dígitos (0-9) y guiones bajos (_).
- No puede empezar ni terminar con guion bajo.
- No puede contener guiones bajos consecutivos.
- Ejemplos válidos: `RESTAURANT`, `FAST_FOOD`, `BAR_PUB`, `CAFE2GO`.
- Ejemplos inválidos: `_BAR`, `BAR_`, `BAR__PUB`, `bar`, `BAR PUB`.

## Ciclo de vida

```
Create() → IsActive = true
         │
         ├── UpdateDetails(code, name, description)
         ├── ChangeSortOrder(sortOrder)
         ├── Deactivate() → IsActive = false
         │                   │
         │                   └── Reactivate() → IsActive = true
         └── (puede desactivarse y reactivarse múltiples veces)
```

## Invariantes

1. El código debe ser único dentro del catálogo (validado en Application, no en Domain).
2. Una categoría activa no puede activarse nuevamente.
3. Una categoría inactiva no puede desactivarse nuevamente.
4. Todos los parámetros se normalizan antes de validar.
5. Las violaciones de invariantes lanzan `EstablishmentCategoryException` (hereda `DomainException`).

## Normalización

| Campo | Normalización aplicada |
|-------|----------------------|
| Code | `Trim()` + `ToUpperInvariant()` |
| Name | `Trim()` + reducción de espacios múltiples a uno |
| Description | `Trim()`, cadena vacía → `null` |

## Casos de uso (Application)

| Caso de uso | Tipo | Comando/Query |
|-------------|------|---------------|
| Crear categoría | Command | `CreateEstablishmentCategoryCommand` → `Result<EstablishmentCategoryId>` |
| Actualizar categoría | Command | `UpdateEstablishmentCategoryCommand` → `Result` |
| Desactivar categoría | Command | `DeactivateEstablishmentCategoryCommand` → `Result` |
| Reactivar categoría | Command | `ReactivateEstablishmentCategoryCommand` → `Result` |
| Obtener por ID | Query | `GetEstablishmentCategoryByIdQuery` → `Result<EstablishmentCategoryResponse>` |
| Listar activas | Query | `ListActiveEstablishmentCategoriesQuery` → `IReadOnlyList<EstablishmentCategoryResponse>` |

## Contratos de persistencia

- **Repositorio de escritura:** `IEstablishmentCategoryRepository` — gestiona el ciclo de vida del agregado.
- **Servicio de lectura:** `IEstablishmentCategoryReadService` — proyecciones de lectura para queries.

## Persistencia

Documentada en [Persistencia de EstablishmentCategory](../persistence/establishment-category-persistence.md).

- Esquema: `establecimientos`, tabla: `CategoriasEstablecimiento`.
- Configuración explícita con Fluent API, sin data annotations.
- Índice único sobre código normalizado como garantía final de unicidad.
- Value converter para `EstablishmentCategoryId ↔ Guid`.

## API pública

Documentada en [API de EstablishmentCategories](../api/establishment-categories-api.md).

- `GET /api/v1/establishment-categories` — lista activas.
- `GET /api/v1/establishment-categories/{id}` — obtiene por ID (solo activas; una inactiva devuelve 404).
- Las consultas públicas no permiten distinguir entre una categoría inactiva y una inexistente.
- Las consultas administrativas (activas e inactivas) se diseñarán posteriormente.
- Los Commands no se exponen sin autenticación.

## Referencias

- [ADR-019: Agregado EstablishmentCategory](../adr/ADR-019-establishment-category-aggregate.md)
- [ADR-020: Convenciones de base de datos](../adr/ADR-020-database-naming-and-ef-conventions.md)
- [ADR-021: Endpoints públicos de catálogos](../adr/ADR-021-public-catalog-query-endpoints.md)
- [Límites de agregados](aggregate-boundaries.md) — Sección 2.4b
- [Glosario del dominio](domain-glossary.md) — Término "Categoría"
