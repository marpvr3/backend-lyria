# ADR-021: Endpoints públicos de consulta de catálogos

## Estado

Aceptado

## Contexto

Los catálogos de referencia (como EstablishmentCategory) necesitan ser consultados públicamente por los consumidores de la API. Sin embargo, las operaciones de escritura (crear, actualizar, desactivar, reactivar) son operaciones administrativas que requieren autenticación y autorización.

En la fase actual no existe un sistema de Identity & Access implementado, por lo que exponer endpoints de escritura sería inseguro.

## Decisión

### Endpoints públicos

- Solo se exponen endpoints de **lectura** (GET) para los catálogos de referencia.
- Los endpoints son públicos (no requieren autenticación).
- Dos endpoints por catálogo:
  - `GET /api/v1/{catalog}` — lista las entradas activas, ordenadas.
  - `GET /api/v1/{catalog}/{id}` — obtiene una entrada por ID.

### Endpoints de escritura

- Los Commands (Create, Update, Deactivate, Reactivate) permanecen implementados en Application.
- **No se exponen mediante HTTP** hasta que exista Identity & Access (Phase 8).
- No se crean endpoints POST, PUT, PATCH ni DELETE para catálogos en esta fase.

### Controllers

- Los controllers son delgados: delegan al mediator y traducen el resultado.
- Los controllers no inyectan repositorios ni DbContext directamente; solo `IMediator`.
- El patrón Result se traduce a Problem Details en la capa API.
- Domain y Application desconocen HTTP (no dependen de ASP.NET Core).

### Traducción de Result a HTTP

La extensión `ResultExtensions` en la capa API traduce:

| ErrorType  | HTTP Status |
| ---------- | ----------: |
| Validation | 400         |
| NotFound   | 404         |
| Conflict   | 409         |
| Forbidden  | 403         |
| Failure    | 500         |

Se utiliza `ProblemDetails` con `Status`, `Title`, `Detail` y `Extensions["code"]`.

### Validación de identificadores

- La ruta `GET /api/v1/{catalog}/{id}` no utiliza la restricción de ruta `:guid`.
- La restricción `:guid` convertiría un identificador con formato inválido en una ruta no encontrada (404), ocultando el error real de formato.
- Sin restricción, el model binding de `[ApiController]` valida el formato del `Guid` y devuelve automáticamente `400 Bad Request` con Validation Problem Details.
- Un GUID válido cuya categoría activa no existe devuelve `404 Not Found` con `EstablishmentCategories.NotFound`.

### Protección de categorías inactivas

- Las consultas públicas solo muestran categorías activas.
- `GET /api/v1/{catalog}/{id}` retorna 404 para categorías inactivas, igual que para inexistentes.
- El filtro de actividad se aplica en el servicio de lectura (Infrastructure), no en el Controller.
- No se reutilizan consultas públicas para administración; las consultas administrativas se diseñarán como casos de uso separados cuando se implemente Identity & Access.

## Consecuencias

- Los consumidores pueden consultar catálogos sin autenticación.
- Las categorías inactivas no son visibles ni distinguibles de las inexistentes en endpoints públicos.
- Las operaciones de escritura solo estarán disponibles cuando se implemente Identity & Access.
- Los controllers no contienen lógica de negocio.
- La traducción de errores es consistente y extensible.

## Referencias

- [ADR-003: ASP.NET Core Controllers](ADR-003-controllers.md)
- [ADR-018: Patrón Result en Application](ADR-018-application-result-pattern.md)
- [Documentación de API de EstablishmentCategories](../api/establishment-categories-api.md)
