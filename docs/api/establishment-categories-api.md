# API de EstablishmentCategories

## Ruta base

```
/api/v1/establishment-categories
```

## Endpoints

### Listar categorías activas

```http
GET /api/v1/establishment-categories
```

**Respuesta exitosa (200 OK):**

```json
[
  {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "code": "RESTAURANT",
    "name": "Restaurante",
    "description": "Establecimiento dedicado a la preparación y servicio de alimentos",
    "sortOrder": 1,
    "isActive": true
  }
]
```

- Retorna solo categorías activas.
- Ordenadas por `sortOrder` ascendente, desempate por `name` ascendente.
- Colección vacía (`[]`) si no hay datos.

### Obtener categoría por ID

```http
GET /api/v1/establishment-categories/{id}
```

**Parámetros:**

| Parámetro | Tipo | Ubicación | Descripción |
|-----------|------|-----------|-------------|
| `id` | `Guid` | Ruta | Identificador de la categoría |

**Respuesta exitosa (200 OK):**

```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "code": "RESTAURANT",
  "name": "Restaurante",
  "description": "Establecimiento dedicado a la preparación y servicio de alimentos",
  "sortOrder": 1,
  "isActive": true
}
```

- Solo retorna categorías activas.
- Una categoría inactiva se comporta públicamente como inexistente (404).

**Error — Categoría no encontrada (404 Not Found):**

```json
{
  "status": 404,
  "title": "Recurso no encontrado",
  "detail": "No se encontró la categoría de establecimiento con ID '...'.",
  "code": "EstablishmentCategories.NotFound"
}
```

**Error — GUID inválido (400 Bad Request):**

Retornado automáticamente por el model binding de `[ApiController]` cuando el formato del ID no es un GUID válido. La respuesta utiliza Validation Problem Details con el error asociado al parámetro `id`.

La ruta no utiliza la restricción `:guid` porque esta convertiría el formato inválido en una ruta no encontrada (404) en lugar del 400 esperado. Sin restricción de ruta, ASP.NET Core acepta la ruta, intenta el model binding a `Guid` y devuelve 400 automáticamente si el formato es inválido.

## Endpoints no disponibles

Los siguientes métodos HTTP **no están implementados** para este recurso:

- `POST` — Crear categoría
- `PUT` — Actualizar categoría
- `PATCH` — Actualización parcial
- `DELETE` — Eliminar/desactivar categoría

Estos endpoints se implementarán cuando exista Identity & Access (Phase 8). Los Commands correspondientes existen en Application pero no se exponen públicamente.

## Esquema de respuesta

### EstablishmentCategoryResponse

| Campo | Tipo | Nullable | Descripción |
|-------|------|----------|-------------|
| `id` | `string (uuid)` | No | Identificador único |
| `code` | `string` | No | Código normalizado en mayúsculas |
| `name` | `string` | No | Nombre de la categoría |
| `description` | `string` | Sí | Descripción opcional |
| `sortOrder` | `integer` | No | Orden de presentación |
| `isActive` | `boolean` | No | Estado de la categoría |

### ProblemDetails (errores)

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `status` | `integer` | Código HTTP |
| `title` | `string` | Título descriptivo del error |
| `detail` | `string` | Mensaje de detalle |
| `code` | `string` | Código técnico del error (en `extensions`) |

## Traducción de errores

| ErrorType | HTTP Status | Título |
|-----------|-------------|--------|
| Validation | 400 | Error de validación |
| NotFound | 404 | Recurso no encontrado |
| Conflict | 409 | Conflicto |
| Forbidden | 403 | Acceso denegado |
| Failure | 500 | Error interno del servidor |

## Controller

`EstablishmentCategoriesController` (en `Lyria.Api.Controllers.V1`):
- Usa `[ApiController]` y controllers tradicionales.
- Inyecta `IMediator` (no repositorios ni DbContext).
- Propaga `CancellationToken`.
- Traduce `Result` a respuestas HTTP mediante `ResultExtensions`.

## Referencias

- [ADR-021: Endpoints públicos de consulta de catálogos](../adr/ADR-021-public-catalog-query-endpoints.md)
- [ADR-003: ASP.NET Core Controllers](../adr/ADR-003-controllers.md)
- [Persistencia de EstablishmentCategory](../persistence/establishment-category-persistence.md)
