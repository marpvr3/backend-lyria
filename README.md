# Lyria

Backend de la plataforma Lyria, construido con .NET 10 y Clean Architecture.

## Requisitos

- .NET SDK 10.0.301 o superior
- SQL Server (configuración pendiente)

## Inicio rápido

```bash
# Restaurar dependencias
dotnet restore Lyria.slnx

# Compilar
dotnet build Lyria.slnx --configuration Release

# Ejecutar pruebas
dotnet test Lyria.slnx --configuration Release

# Ejecutar la API
dotnet run --project src/Lyria.Api
```

## Arquitectura

Lyria sigue Clean Architecture con cuatro capas:

| Capa | Proyecto | Responsabilidad |
|------|----------|-----------------|
| Domain | `Lyria.Domain` | Entidades, aggregate roots, value objects, eventos de dominio, IDs tipados, excepciones |
| Application | `Lyria.Application` | Casos de uso, contratos, lógica de aplicación |
| Infrastructure | `Lyria.Infrastructure` | EF Core, SQL Server, servicios externos |
| API | `Lyria.Api` | Controllers ASP.NET Core, configuración HTTP |

## Estructura del proyecto

```
src/
├── Lyria.Domain/           Capa de dominio (sin dependencias externas)
├── Lyria.Application/      Capa de aplicación
├── Lyria.Infrastructure/   Capa de infraestructura (EF Core, SQL Server)
└── Lyria.Api/              API Web (Controllers)

tests/
├── Lyria.Domain.UnitTests/
├── Lyria.Application.UnitTests/
├── Lyria.Infrastructure.IntegrationTests/
├── Lyria.Api.FunctionalTests/
└── Lyria.ArchitectureTests/

docs/
├── product/                Visión, alcance, actores, capacidades
├── domain/                 Glosario, contextos, modelo conceptual, reglas, casos de uso
├── architecture/           Arquitectura, módulos, requisitos, roadmap
├── persistence/            Documentación de persistencia por agregado
├── api/                    Documentación de endpoints API
└── adr/                    Registros de decisiones arquitectónicas (ADR-001 a ADR-021)
```

## Documentación

### Producto

- [Visión del producto](docs/product/product-vision.md)
- [Alcance del MVP](docs/product/mvp-scope.md)
- [Actores y permisos](docs/product/actors-and-permissions.md)
- [Capacidades de negocio](docs/product/business-capabilities.md)

### Dominio

- [Glosario del dominio](docs/domain/domain-glossary.md)
- [Bounded contexts](docs/domain/bounded-contexts.md)
- [Modelo conceptual](docs/domain/conceptual-model.md)
- [Límites de agregados](docs/domain/aggregate-boundaries.md)
- [Value objects](docs/domain/value-objects.md)
- [Reglas de dominio](docs/domain/domain-rules.md)
- [Casos de uso](docs/domain/use-cases.md)
- [Decisiones abiertas](docs/domain/open-decisions.md)

### Arquitectura

- [Visión general de arquitectura](docs/architecture/architecture-overview.md)
- [Mapa de módulos](docs/architecture/module-map.md)
- [Requisitos no funcionales](docs/architecture/non-functional-requirements.md)
- [Roadmap de implementación](docs/architecture/implementation-roadmap.md)
- [Reglas de dependencia](docs/architecture/dependency-rules.md)
- [Estrategia de pruebas](docs/architecture/testing-strategy.md)
- [CQRS y Mediator](docs/architecture/cqrs-and-mediator.md)
- [Building blocks del dominio](docs/architecture/domain-building-blocks.md)

### Dominio

- [EstablishmentCategory](docs/domain/establishment-category.md)

### Persistencia

- [EstablishmentCategory — Persistencia](docs/persistence/establishment-category-persistence.md)

### API

- [EstablishmentCategories — API](docs/api/establishment-categories-api.md)
- [Registro Móvil — API](docs/api/mobile-registrations-api.md)
- [Autenticación Móvil — API](docs/api/authentication-api.md)
- [Verificación de Correo — API](docs/api/email-verification-api.md)

### Decisiones arquitectónicas

- [ADR-001: .NET 10](docs/adr/ADR-001-dotnet-10.md)
- [ADR-002: Clean Architecture](docs/adr/ADR-002-clean-architecture.md)
- [ADR-003: Controllers](docs/adr/ADR-003-controllers.md)
- [ADR-004: SQL Server y EF Core](docs/adr/ADR-004-sql-server-ef-core.md)
- [ADR-005: CQRS y MediatR](docs/adr/ADR-005-cqrs-mediator.md)
- [ADR-006: Monolito modular](docs/adr/ADR-006-modular-monolith.md)
- [ADR-007: Límites funcionales](docs/adr/ADR-007-functional-boundaries.md)
- [ADR-008: Modelo de adecuación alimentaria](docs/adr/ADR-008-dietary-suitability-model.md)
- [ADR-009: Estrategia de búsqueda](docs/adr/ADR-009-search-strategy.md)
- [ADR-010: Establecimiento y sede como agregados](docs/adr/ADR-010-establishment-and-branch-aggregates.md)
- [ADR-011: Identidad y perfil separados](docs/adr/ADR-011-identity-and-user-profile.md)
- [ADR-012: Roles con alcance](docs/adr/ADR-012-scoped-role-assignments.md)
- [ADR-013: Reseñas y rating](docs/adr/ADR-013-reviews-and-rating-summary.md)
- [ADR-014: Galería de medios de sede](docs/adr/ADR-014-branch-media-gallery.md)
- [ADR-015: Consistencia entre agregados](docs/adr/ADR-015-cross-aggregate-consistency.md)
- [ADR-016: Building blocks del dominio](docs/adr/ADR-016-domain-building-blocks.md)
- [ADR-017: Implementación del mediator](docs/adr/ADR-017-mediator-implementation.md)
- [ADR-018: Patrón Result en Application](docs/adr/ADR-018-application-result-pattern.md)
- [ADR-019: Agregado EstablishmentCategory](docs/adr/ADR-019-establishment-category-aggregate.md)
- [ADR-020: Convenciones de base de datos y EF Core](docs/adr/ADR-020-database-naming-and-ef-conventions.md)
- [ADR-021: Endpoints públicos de consulta de catálogos](docs/adr/ADR-021-public-catalog-query-endpoints.md)
- [ADR-022: Campos de auditoría en entidades](docs/adr/ADR-022-entity-audit-fields.md)
- [ADR-023: Migraciones automáticas al iniciar la API](docs/adr/ADR-023-automatic-migrations-on-startup.md)

### Operaciones

- [Migraciones automáticas al iniciar la API](docs/operations/database-migrations-on-startup.md)

## Despliegue

La estructura de la base de datos se actualiza aplicando las migraciones EF Core pendientes durante el inicio de la API. No se restauran respaldos para publicar cambios de esquema.

| Variable de entorno | Valor | Efecto |
|---------------------|-------|--------|
| `Database__ApplyMigrationsOnStartup` | `true` | La API aplica las migraciones pendientes antes de aceptar solicitudes |
| (sin configurar) | `false` (predeterminado) | La API no modifica la base de datos al iniciar |

Se configura una sola vez en el servidor y requiere reiniciar la API para tomar efecto. Detalles, limitaciones y permisos SQL requeridos: [Migraciones automáticas al iniciar la API](docs/operations/database-migrations-on-startup.md).

### Autenticación móvil

La API valida access tokens JWT en los endpoints protegidos. La configuración se valida al arrancar: **sin `Jwt__SigningKey` la API no inicia**.

| Variable de entorno | Valor | Efecto |
|---------------------|-------|--------|
| `Jwt__Issuer` | `Lyria.Api` | Emisor de los access tokens |
| `Jwt__Audience` | `Lyria.Mobile` | Audiencia esperada |
| `Jwt__SigningKey` | *(secreto)* | Clave de firma HMAC. Mínimo 32 caracteres |
| `Jwt__AccessTokenMinutes` | `15` | Vigencia del access token |
| `Jwt__RefreshTokenDays` | `30` | Vigencia del refresh token |

La clave de firma se suministra **únicamente** por variable de entorno y nunca se versiona. Generarla con `openssl rand -base64 64`.

**Solo las cuentas `Active` pueden iniciar sesión.** Una cuenta recién registrada queda en `Unverified` hasta que confirma su correo.

### Verificación de correo

Todo usuario registrado desde la aplicación móvil recibe por correo un código numérico de **seis dígitos** con el que confirma su dirección. Al confirmarlo, `IsEmailVerified` pasa a `true` y el estado cambia de `Unverified` a `Active`, que es cuando la cuenta puede iniciar sesión.

| Regla | Valor |
|-------|-------|
| Longitud del código | 6 dígitos, puede empezar por cero |
| Vigencia | 15 minutos |
| Usos | Uno solo |
| Intentos fallidos | Máximo 5; al agotarlos hay que pedir uno nuevo |
| Reenvío | `POST /api/v1/auth/email-verification/resend`, con 60 s de intervalo mínimo |
| Confirmación | `POST /api/v1/auth/email-verification/confirm` |
| Almacenamiento | Solo el HMAC-SHA256 del código; nunca se guarda ni se registra en claro |

El envío inicial forma parte del registro móvil y no tiene endpoint propio. Las respuestas son **genéricas**: ni el reenvío ni la confirmación revelan si un correo está registrado o en qué estado se encuentra la cuenta. Un fallo del proveedor de correo no revierte el registro; el usuario queda creado como `Unverified` y puede solicitar un reenvío.

La configuración se valida al arrancar: **sin `EmailVerification__CodeSecret` la API no inicia**.

| Variable de entorno | Valor | Efecto |
|---------------------|-------|--------|
| `EmailVerification__CodeSecret` | *(secreto)* | Clave HMAC de los códigos. Mínimo 32 caracteres, **distinta** de `Jwt__SigningKey` |
| `EmailVerification__ExpirationMinutes` | `15` | Vigencia del código |
| `EmailVerification__MaximumFailedAttempts` | `5` | Intentos que invalidan un código |
| `EmailVerification__ResendCooldownSeconds` | `60` | Intervalo mínimo entre envíos |

### Correo saliente

El envío usa SMTP a través de MailKit. La configuración SMTP **no** se valida al arrancar: un ambiente que no envía correo no debe impedir el inicio de la API.

| Variable de entorno | Valor |
|---------------------|-------|
| `Email__SmtpHost` | Servidor SMTP |
| `Email__SmtpPort` | `587` |
| `Email__UseTls` | `true` |
| `Email__Username` | *(secreto)* |
| `Email__Password` | *(secreto)* |
| `Email__FromAddress` | `no-reply@dominio.com` |
| `Email__FromName` | `Lyria` |

Ningún secreto se versiona: `appsettings.json` solo contiene cadenas vacías. La estructura de la tabla `dbo.UsuarioVerificacionesCorreo` se aplica con la migración `AddUserEmailVerifications`.

> ⚠️ **HTTPS obligatorio.** Contraseñas, tokens y códigos de verificación no deben viajar por HTTP plano. El repositorio no incluye hoy configuración TLS (ni `UseHttpsRedirection`, ni `ForwardedHeaders`, ni certificados), y la API pública está expuesta solo sobre HTTP. **El despliegue de la autenticación y de la verificación de correo queda bloqueado hasta configurar HTTPS.** Detalles: [Autenticación Móvil — API](docs/api/authentication-api.md) y [Verificación de Correo — API](docs/api/email-verification-api.md).

## Convenciones

- **Código**: inglés (clases, interfaces, namespaces, variables)
- **Mensajes al usuario**: español
- **Documentación**: español
