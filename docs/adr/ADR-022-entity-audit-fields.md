# ADR-022: Campos de auditoría en entidades

## Estado

Aceptado

## Contexto

Lyria necesita registrar cuándo se crean y modifican las entidades para fines de trazabilidad, depuración y auditoría operativa. Actualmente las tablas `CategoriasEstablecimiento` y `Establecimientos` no cuentan con campos temporales de auditoría.

## Decisión

### Campos de auditoría

Se agregan dos campos a todas las entidades auditables:

| Propiedad C#    | Columna SQL          | Tipo        | Nullable |
|-----------------|----------------------|-------------|----------|
| `CreatedAtUtc`  | `FechaCreacion`      | `datetime2` | NO       |
| `UpdatedAtUtc`  | `FechaActualizacion` | `datetime2` | SÍ       |

### Contrato auditable

Se define la interfaz `IAuditableEntity` en `Lyria.Domain.Abstractions`. Las entidades `EstablishmentCategory` y `Establishment` la implementan directamente, sin crear una clase base adicional.

### Asignación automática

Las fechas se asignan automáticamente en `LyriaDbContext.SaveChangesAsync`:

- **Creación** (`EntityState.Added`): `CreatedAtUtc = UTC actual`, `UpdatedAtUtc = null`.
- **Modificación** (`EntityState.Modified`): `CreatedAtUtc` permanece sin cambios (se marca como no modificado), `UpdatedAtUtc = UTC actual`.

### Fuente de reloj

Se utiliza `TimeProvider` (BCL) registrado en DI como `TimeProvider.System`. Las pruebas utilizan `FakeTimeProvider` del paquete `Microsoft.Extensions.TimeProvider.Testing`.

### Exposición en API

- `EstablishmentResponse` (detalle) incluye `createdAtUtc` y `updatedAtUtc`.
- `EstablishmentListItemResponse` (listado) no incluye auditoría para mantener el payload liviano.
- `EstablishmentCategoryResponse` no incluye auditoría porque actualmente solo se expone en endpoints públicos de consulta donde las fechas de auditoría no aportan valor al consumidor. Cuando existan endpoints administrativos de categorías, se podrá crear un `EstablishmentCategoryAdminResponse` con los campos de auditoría.
- Los campos de auditoría nunca se aceptan en requests de creación, actualización ni cambio de estado.

## Deuda técnica

Cuando el módulo de Identity & Access esté implementado (ADR-011), se deben agregar:

- `CreatedByUserId` / `CreadoPorUsuarioId`
- `UpdatedByUserId` / `ActualizadoPorUsuarioId`

Estos campos requieren un usuario autenticado confiable. No se utilizarán valores ficticios como `"system"` o `"admin"`.

## Consecuencias

- Todas las entidades que implementen `IAuditableEntity` obtienen auditoría temporal automática.
- Los controllers y commands no manejan fechas de auditoría.
- Las pruebas de auditoría son independientes del reloj real gracias a `TimeProvider`.
