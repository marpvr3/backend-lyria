# Migraciones automáticas al iniciar la API

Procedimiento para actualizar la estructura de la base de datos durante una publicación normal, sin restaurar respaldos de desarrollo sobre producción.

Decisión de referencia: [ADR-023](../adr/ADR-023-automatic-migrations-on-startup.md).

## Configuración

| Elemento | Valor |
|----------|-------|
| Clave de configuración | `Database:ApplyMigrationsOnStartup` |
| Tipo | booleano |
| Valor predeterminado (`appsettings.json`) | `false` |
| Variable de entorno del servidor | `Database__ApplyMigrationsOnStartup=true` |

La opción se configura **una sola vez** en el servidor. A partir de ese momento, cada publicación que incluya migraciones nuevas las aplicará al iniciar la API. El cambio de la variable requiere reiniciar la API, porque se lee durante el arranque.

Ningún ambiente habilita la opción de forma implícita: ni `Development` ni `Production`. La única condición que la activa es la configuración explícita.

## Comportamiento

Con `Database__ApplyMigrationsOnStartup=false` (predeterminado):

- La API registra un mensaje informativo y continúa el inicio.
- No consulta ni modifica el esquema de la base de datos.

Con `Database__ApplyMigrationsOnStartup=true`:

1. La API construye el host y, **antes** de configurar el pipeline HTTP, consulta las migraciones pendientes con `GetPendingMigrationsAsync()`.
2. Si no hay pendientes, registra el hecho y continúa el inicio.
3. Si hay pendientes, registra sus identificadores y ejecuta `MigrateAsync()`.
4. Las migraciones aplicadas quedan registradas en `__EFMigrationsHistory` y no se vuelven a ejecutar en reinicios posteriores.
5. La API empieza a aceptar solicitudes solo después de que las migraciones terminan correctamente.

Si una migración falla (error de conexión, error SQL, migración inválida, permisos insuficientes):

- El error se registra con nivel `Critical` y, en el arranque, con nivel `Fatal`.
- La excepción se relanza y **la API no inicia**.
- El proceso termina con un **código de salida distinto de cero**, de modo que el servicio del servidor (systemd, IIS, contenedor, etc.) reporta el despliegue como fallido en lugar de darlo por exitoso.
- No se aplica ningún mecanismo de recuperación automático ni se degrada el error a advertencia.

## Lo que este mecanismo no hace

- No restaura respaldos ni reemplaza la base de datos.
- No crea la base desde cero (`EnsureCreated` está prohibido en código productivo).
- No ejecuta SQL manual para crear o reparar objetos.
- No repara objetos eliminados manualmente: si una migración figura como aplicada en `__EFMigrationsHistory` y alguien elimina la tabla que creó, EF Core seguirá considerándola aplicada y no la volverá a ejecutar. La corrección en ese caso es una migración nueva o una intervención manual documentada.
- No garantiza la conservación de datos si una migración contiene operaciones destructivas (`DropColumn`, `DropTable`, cambios de tipo incompatibles, SQL destructivo). Cada migración debe revisarse y probarse antes de publicar.

## Requisitos en SQL Server

El usuario configurado en `ConnectionStrings:LyriaDatabase` debe poder ejecutar las operaciones DDL incluidas en las migraciones publicadas, típicamente:

- `CREATE TABLE`, `ALTER TABLE`, `DROP TABLE`
- `CREATE INDEX`, `DROP INDEX`
- creación de claves foráneas y restricciones
- lectura y escritura sobre `__EFMigrationsHistory`

Los permisos se otorgan fuera del código; la aplicación nunca modifica usuarios ni permisos.

## Respaldos

Los respaldos siguen siendo el mecanismo de recuperación ante fallos, y conviene tomar uno antes de una publicación que incluya migraciones. Lo que ya no debe hacerse es **restaurar** un respaldo de desarrollo sobre producción como parte de una publicación normal.

## Instancias múltiples

EF Core 10 (comportamiento incorporado desde EF Core 9) toma un **bloqueo de migraciones** al ejecutar `MigrateAsync()`. Si varias instancias arrancan a la vez, solo una aplica las migraciones y las demás esperan a que el bloqueo se libere; luego encuentran el historial actualizado y continúan sin reaplicar nada. Por eso no se implementó ningún bloqueo propio.

El riesgo que permanece no es la concurrencia de la migración, sino la **compatibilidad durante el cambio de esquema**: mientras una instancia migra, las instancias ya en ejecución pueden seguir atendiendo solicitudes contra una estructura que está cambiando, o ejecutar código de la versión anterior que no coincide con la nueva estructura. Para migraciones que rompen compatibilidad conviene una ventana de despliegue, drenar el tráfico, o diseñar la migración en pasos compatibles hacia atrás.

Hoy el repositorio no define infraestructura de múltiples instancias: la API se publica como una sola instancia.

## Verificación después de publicar

1. Revisar los logs de arranque: deben aparecer los identificadores de las migraciones aplicadas o el mensaje de que no hay pendientes.
2. Confirmar en `__EFMigrationsHistory` que las migraciones publicadas figuran aplicadas.
3. Confirmar que la API responde en `/api/health`.
