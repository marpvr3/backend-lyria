# ADR-020: Convenciones de base de datos y EF Core

## Estado

Aceptado

## Contexto

Lyria necesita convenciones claras para el modelado físico de la base de datos (SQL Server) y la configuración de Entity Framework Core. El código fuente está en inglés, pero la base de datos servirá a un dominio cuyo contexto de negocio es hispanohablante, por lo que los nombres físicos deben reflejar el idioma del negocio.

## Decisión

### Idioma del modelo físico

- **Código técnico** (clases, propiedades, namespaces, interfaces): **inglés**.
- **Esquemas, tablas y columnas** en SQL Server: **español**.
- La configuración de EF Core mapea explícitamente cada propiedad a su nombre de columna en español.

### Esquemas

- Cada módulo funcional utiliza un esquema propio cuando aporta aislamiento semántico.
- El módulo Establishments utiliza el esquema `establecimientos`.
- La tabla de historial de migraciones reside en el esquema `infraestructura`.

### Configuración EF Core

- **Fluent API exclusivamente** (`IEntityTypeConfiguration<T>`). Prohibido usar data annotations de persistencia en Domain.
- Nombres explícitos para tablas, columnas, claves primarias, índices y restricciones.
- Los value converters para IDs fuertemente tipados se declaran explícitamente en la configuración.
- `ValueGeneratedNever` para identificadores asignados por el dominio.
- Las propiedades de domain events (`DomainEvents`) se ignoran en la configuración.

### Migraciones

- Las migraciones se generan dentro de `Lyria.Infrastructure/Persistence/Migrations`.
- No se ejecutan migraciones automáticamente al iniciar la aplicación.
- No se utiliza `EnsureCreated` en código productivo.
- La ejecución de migraciones se realizará mediante un proceso controlado de despliegue.

### Restricciones

- Los nombres de PK siguen el patrón `PK_NombreTabla`.
- Los nombres de índices únicos siguen el patrón `UX_NombreTabla_NombreColumna`.
- Se prohíbe lazy loading.

## Consecuencias

- Los desarrolladores deben especificar explícitamente cada columna en la Fluent API.
- Las migraciones reflejan los nombres en español del modelo entidad–relación.
- Las consultas SQL directas utilizan nombres en español.
- No se pueden utilizar convenciones automáticas de EF Core para nombres de columnas.

## Referencias

- [ADR-004: SQL Server y EF Core](ADR-004-sql-server-ef-core.md)
- [Documentación de persistencia de EstablishmentCategory](../persistence/establishment-category-persistence.md)
