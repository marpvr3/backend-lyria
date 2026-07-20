# ADR-019: Agregado EstablishmentCategory

## Estado

Aceptado

## Fecha

2026-07-14

## Contexto

`EstablishmentCategory` es el primer agregado de negocio implementado en Lyria. Representa las categorías de establecimientos gastronómicos (restaurante, cafetería, bar, etc.). Es un catálogo de referencia administrado dentro del módulo Establishments.

Las decisiones de diseño de este agregado establecen precedentes para los demás agregados de catálogo (`DietaryNeed`, `Service`) y sirven como primera validación de las abstracciones definidas en fases anteriores.

## Decisión

### Identidad

`EstablishmentCategoryId` se implementa como `readonly record struct` que implementa `IStronglyTypedId<Guid>`, ubicado en `Lyria.Domain.Establishments.Categories`.

### Diseño del agregado

`EstablishmentCategory` hereda de `AggregateRoot<EstablishmentCategoryId>` y se ubica en `Lyria.Domain.Establishments.Categories`.

**Propiedades:**

| Propiedad | Tipo | Restricciones |
|-----------|------|--------------|
| `Code` | `string` | 2–50 caracteres, `^[A-Z0-9](_?[A-Z0-9])*$`, normalizado a mayúsculas |
| `Name` | `string` | 2–100 caracteres, normalizado (trim + espacios reducidos) |
| `Description` | `string?` | Máximo 500 caracteres, nullable |
| `SortOrder` | `int` | ≥ 0 |
| `IsActive` | `bool` | `true` al crear |

**Factory method:** `EstablishmentCategory.Create(id, code, name, description, sortOrder)` — constructor privado, creación solo vía factory. Normaliza y valida todos los parámetros.

**Métodos de mutación:**
- `UpdateDetails(code, name, description)` — normaliza y valida.
- `ChangeSortOrder(sortOrder)` — valida ≥ 0.
- `Deactivate()` — lanza si ya está inactivo.
- `Reactivate()` — lanza si ya está activo.

**Validación:** las invariantes se protegen mediante `DomainException` (vía `EstablishmentCategoryException`). El código se valida con `[GeneratedRegex]` compilado.

### Repositorio

`IEstablishmentCategoryRepository` en `Application.Abstractions.Persistence`:
- `GetByIdAsync(EstablishmentCategoryId, CancellationToken)`
- `ExistsByCodeAsync(string normalizedCode, EstablishmentCategoryId? excludingId, CancellationToken)`
- `AddAsync(EstablishmentCategory, CancellationToken)`
- `SaveChangesAsync(CancellationToken)`

Es un repositorio específico (no genérico). `SaveChangesAsync` se incluye en el repositorio para esta fase inicial; se evaluará `IUnitOfWork` cuando se necesite coordinar múltiples agregados.

### Servicio de lectura

`IEstablishmentCategoryReadService` en `Application.Abstractions.Persistence`:
- `GetByIdAsync(Guid, CancellationToken)` — retorna `EstablishmentCategoryResponse?`.
- `ListActiveAsync(CancellationToken)` — retorna `IReadOnlyList<EstablishmentCategoryResponse>`.

Separar las consultas de lectura del repositorio de escritura es consistente con CQRS.

### Namespace

El agregado se ubica en `Lyria.Domain.Establishments.Categories`, dentro del módulo Establishments — no en un módulo genérico "Catalogs". Esto es consistente con ADR-006 (monolito modular) y con el mapa de módulos que asigna `EstablishmentCategory` a Establishments.

## Consecuencias

### Positivas

- Primera validación real de las abstracciones base (Entity, AggregateRoot, IStronglyTypedId, DomainException).
- Establece el patrón para catálogos de referencia (DietaryNeed, Service seguirán la misma estructura).
- Validación de dominio completa con regex compilado.
- Repositorio específico evita abstracción prematura.

### Negativas

- `SaveChangesAsync` en el repositorio en lugar de `IUnitOfWork` es una decisión temporal que deberá revisarse.
- La normalización de código (uppercase) se realiza tanto en el dominio como en el handler de creación — doble normalización intencional para seguridad defensiva.
