# ADR-004: SQL Server con Entity Framework Core

## Estado

Aceptada

## Contexto

Se necesita seleccionar la base de datos y el mecanismo de acceso a datos para Lyria.

## Decisión

Se adopta **SQL Server** como base de datos y **Entity Framework Core 10** como ORM.

Paquetes seleccionados:
- `Microsoft.EntityFrameworkCore` 10.0.9
- `Microsoft.EntityFrameworkCore.SqlServer` 10.0.9
- `Microsoft.EntityFrameworkCore.Design` 10.0.9 (herramienta de migraciones)

## Razones

1. SQL Server es una base de datos relacional madura con soporte empresarial.
2. EF Core proporciona un ORM completo con migraciones, change tracking y LINQ.
3. La versión 10.x es compatible con el runtime .NET 10.
4. EF Core Design permite generar y aplicar migraciones desde la CLI.

## Restricciones

- EF Core solo se referencia en `Lyria.Infrastructure` y `Lyria.Api` (Design).
- `Lyria.Domain` y `Lyria.Application` no deben depender de EF Core.
- No se deben colocar cadenas de conexión reales en `appsettings.json`.
- Los secretos deben gestionarse con User Secrets o variables de entorno.

## Estado actual

En esta fase se incluyen los paquetes pero no se han creado:
- DbContext
- Configuraciones de entidades
- Migraciones
- Cadenas de conexión

Estos se implementarán en fases posteriores.
