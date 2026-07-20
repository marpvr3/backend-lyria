# Persistencia de EstablishmentCategory

## Modelo físico

### Esquema y tabla

| Concepto | Valor |
|----------|-------|
| Esquema | `establecimientos` |
| Tabla | `CategoriasEstablecimiento` |
| Clave primaria | `PK_CategoriasEstablecimiento` |
| Índice único | `UX_CategoriasEstablecimiento_Codigo` |

### Columnas

| Propiedad C# | Columna SQL | Tipo SQL | Nullable | Restricciones |
|--------------|-------------|----------|----------|---------------|
| `Id` | `CategoriaEstablecimientoId` | `uniqueidentifier` | No | PK, `ValueGeneratedNever` |
| `Code` | `Codigo` | `varchar(50)` | No | Índice único |
| `Name` | `Nombre` | `nvarchar(100)` | No | — |
| `Description` | `Descripcion` | `nvarchar(500)` | Sí | — |
| `SortOrder` | `Orden` | `int` | No | — |
| `IsActive` | `Activo` | `bit` | No | — |

### Propiedades ignoradas

- `DomainEvents` — no se persisten; se gestionan en memoria.

## DbContext

`LyriaDbContext` (interno, en `Lyria.Infrastructure.Persistence`):
- Hereda de `DbContext`.
- Aplica configuraciones desde el assembly de Infrastructure.
- No expone `DbSet` públicos; los repositorios usan `Set<T>()`.
- No despacha eventos de dominio.
- No sobrescribe `SaveChanges` para auditoría.
- No implementa soft delete global.
- No utiliza lazy loading.

### Tabla de historial de migraciones

```
infraestructura.__EFMigrationsHistory
```

## Configuración EF Core

`EstablishmentCategoryConfiguration` implementa `IEntityTypeConfiguration<EstablishmentCategory>`:
- Mapeo explícito de tabla, esquema y columnas mediante Fluent API.
- Value converter para `EstablishmentCategoryId ↔ Guid`.
- Índice único sobre `Codigo` para garantizar unicidad a nivel de base de datos.

## Repositorio

`EstablishmentCategoryRepository` implementa `IEstablishmentCategoryRepository`:

| Método | Comportamiento |
|--------|---------------|
| `GetByIdAsync` | Busca con tracking (para Commands). Retorna `null` si no existe. |
| `ExistsByCodeAsync` | Consulta eficiente con `AnyAsync`. Permite excluir un ID. No carga entidades. |
| `AddAsync` | Agrega al DbContext sin ejecutar `SaveChanges`. |
| `SaveChangesAsync` | Delega en `DbContext.SaveChangesAsync`. |

### Deuda técnica documentada

La violación del índice único de `Codigo` al persistir dos categorías con el mismo código normalizado produce un `DbUpdateException`. La traducción de este error a un `Conflict` de Application se implementará antes de exponer los Commands mediante HTTP. En la fase actual, la protección la proporciona la validación de Application (`ExistsByCodeAsync`), y el índice único es la garantía final frente a condiciones de carrera.

## Servicio de lectura

`EstablishmentCategoryReadService` implementa `IEstablishmentCategoryReadService`:

| Método | Comportamiento |
|--------|---------------|
| `GetActiveByIdAsync` | `AsNoTracking`. Filtra por ID e `IsActive`. Proyecta directamente a `EstablishmentCategoryResponse`. Retorna `null` para inactivas e inexistentes. |
| `ListActiveAsync` | `AsNoTracking`. Filtra por `IsActive`. Ordena por `SortOrder`, desempata por `Name`. Retorna colección vacía si no hay datos. |

## Migración

- **Nombre:** `InitialEstablishmentCategories`
- **Operaciones:** Crea esquema `establecimientos`, tabla `CategoriasEstablecimiento`, PK, índice único.
- **No se aplicó** a ninguna base de datos real.
- **Ejecución controlada** prevista para el proceso de despliegue.

## Referencias

- [ADR-020: Convenciones de base de datos](../adr/ADR-020-database-naming-and-ef-conventions.md)
- [EstablishmentCategory — Dominio](../domain/establishment-category.md)
