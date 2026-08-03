# ADR-023: Aplicación automática de migraciones al iniciar la API

## Estado

Aceptado — Modifica ADR-020 (sección «Migraciones»)

## Fecha

2026-08-02

## Contexto

La estructura de la base de datos de producción se venía actualizando restaurando una copia de la base de desarrollo sobre producción. Ese procedimiento reemplaza la base completa y puede eliminar datos productivos recientes.

Lyria ya utiliza migraciones EF Core y cuenta con la tabla de historial `__EFMigrationsHistory`. Lo que faltaba era un mecanismo controlado para aplicar, durante un despliegue normal, únicamente las migraciones que todavía no fueron aplicadas.

ADR-020 estableció que «no se ejecutan migraciones automáticamente al iniciar la aplicación» y que la ejecución se realizaría «mediante un proceso controlado de despliegue». Este ADR define ese proceso controlado y ajusta esa restricción: la aplicación automática existe, pero está deshabilitada por defecto y solo se activa con una configuración explícita del servidor.

## Decisión

### Mecanismo

- La API aplica las migraciones EF Core pendientes durante el inicio, **después** de construir `WebApplication` y **antes** de configurar el pipeline HTTP.
- La lógica vive en `Lyria.Infrastructure.Persistence.DatabaseMigrator`, que crea un scope, resuelve `LyriaDbContext`, consulta `GetPendingMigrationsAsync()` y ejecuta `MigrateAsync()`.
- La API la invoca mediante la extensión `WebApplication.ApplyPendingMigrationsAsync()` en `Lyria.Api.Extensions.WebApplicationExtensions`.
- No existe endpoint HTTP, controlador ni operación administrativa para disparar migraciones.

### Configuración

- Opción `Database:ApplyMigrationsOnStartup`, tipada en `DatabaseStartupOptions`.
- Valor predeterminado: `false` en `appsettings.json`. Ningún ambiente la habilita implícitamente.
- Se habilita únicamente por variable de entorno del servidor: `Database__ApplyMigrationsOnStartup=true`.

### Manejo de errores

- Si falla la consulta de migraciones pendientes o la aplicación de una migración, el error se registra con nivel `Critical` y se relanza.
- La API no inicia con una estructura incompleta: la excepción se produce antes de configurar el pipeline HTTP.
- El bloque `catch` global de `Program.cs` registra el fallo con nivel `Fatal`, cierra Serilog en el `finally` y **relanza la excepción**, de modo que el proceso termina con un código de salida distinto de cero y el orquestador del servidor detecta el despliegue fallido.
- No se degrada el error a advertencia ni se hace fallback a `EnsureCreated`.

### Prohibiciones que se mantienen

- No se usa `EnsureCreated`, `EnsureCreatedAsync`, `EnsureDeleted` ni `EnsureDeletedAsync` en código productivo.
- No se ejecuta SQL manual para crear o reparar objetos.
- No se restauran respaldos como parte de una publicación.
- La fuente de verdad del estado del esquema es `__EFMigrationsHistory`.

## Consecuencias

- Una publicación normal actualiza el esquema sin reemplazar la base ni perder datos, salvo que una migración contenga operaciones destructivas (`DropColumn`, `DropTable`, cambios incompatibles). Revisar y probar cada migración antes de publicar sigue siendo obligatorio.
- El usuario SQL de la API necesita permisos para ejecutar las operaciones DDL incluidas en las migraciones.
- El mecanismo aplica migraciones pendientes; **no** repara objetos eliminados manualmente. Si una migración figura como aplicada y alguien elimina la tabla que creó, EF Core no la volverá a ejecutar.
- Con el valor predeterminado `false`, los comandos `dotnet ef` locales y las pruebas no modifican ningún esquema de forma accidental.
- Con varias instancias, EF Core (desde la versión 9, vigente en EF Core 10) toma un bloqueo de migraciones al ejecutar `MigrateAsync()`, por lo que dos instancias no aplican migraciones en simultáneo. No se implementa ningún bloqueo propio. El riesgo restante es de compatibilidad: mientras una instancia migra, las demás pueden seguir atendiendo solicitudes o ejecutar código que no coincide con la nueva estructura.

## Referencias

- [ADR-004: SQL Server y EF Core](ADR-004-sql-server-ef-core.md)
- [ADR-020: Convenciones de base de datos y EF Core](ADR-020-database-naming-and-ef-conventions.md)
- [Migraciones automáticas al iniciar la API](../operations/database-migrations-on-startup.md)
