# Hoja de ruta de implementación — Lyria

## Propósito

Este documento describe las fases de implementación planificadas para la plataforma Lyria. Cada fase tiene un objetivo claro, dependencias explícitas, entregables concretos, riesgos identificados y criterios de finalización verificables.

Las fases anteriores (1 y 2) corresponden a la inicialización del proyecto y la infraestructura base, ya completadas. Este documento cubre las fases 3 en adelante.

---

## Phase 3 — Building blocks del dominio ✔

**Estado:** Completada

### Objetivo

Establecer las abstracciones base del dominio que serán reutilizadas por todos los agregados de las fases siguientes.

### Entregables completados

- `Entity<TId>`, `AggregateRoot<TId>`, `ValueObject`, `IDomainEvent`, `IStronglyTypedId<TValue>`, `DomainException`.
- 48 pruebas (30 unitarias + 18 de arquitectura).
- ADR-016 documentado.

### Decisiones clave

Documentadas en [ADR-016](../adr/ADR-016-domain-building-blocks.md).

---

## Phase 4 — Catálogos fundacionales (en progreso)

### Objetivo

Implementar los catálogos de datos de referencia que serán utilizados por establecimientos, sucursales y perfiles de usuario.

### Sub-fases

#### Phase 4A — EstablishmentCategory (Domain + Application) ✔

**Estado:** Completada

Entregables:
- Agregado `EstablishmentCategory` con id tipado, factory, validación con regex compilado, normalización.
- Contratos CQRS (`ICommand`, `IQuery`, handlers) con martinothamar/Mediator ([ADR-017](../adr/ADR-017-mediator-implementation.md)).
- Patrón Result (`Result`, `Result<T>`, `Error`, `ErrorType`) en Application ([ADR-018](../adr/ADR-018-application-result-pattern.md)).
- 6 casos de uso: Create, Update, Deactivate, Reactivate, GetById, ListActive.
- Pipeline de validación con FluentValidation + `ValidationBehavior`.
- Repositorio específico `IEstablishmentCategoryRepository` y servicio de lectura `IEstablishmentCategoryReadService`.
- 145 pruebas (75 Domain + 46 Application + 22 Architecture + 1 Infrastructure + 1 Functional).
- ADR-017, ADR-018, ADR-019 documentados.

#### Phase 4B — EstablishmentCategory (Infrastructure + API) ✔

**Estado:** Completada

Entregables:
- `LyriaDbContext` con configuración explícita Fluent API y convenciones SQL en español ([ADR-020](../adr/ADR-020-database-naming-and-ef-conventions.md)).
- `EstablishmentCategoryConfiguration` con esquema `establecimientos`, tabla `CategoriasEstablecimiento`, value converter para ID tipado, índice único.
- `EstablishmentCategoryRepository` y `EstablishmentCategoryReadService` concretos.
- Migración inicial `InitialEstablishmentCategories` (no aplicada a base real).
- `EstablishmentCategoriesController` con dos endpoints GET públicos ([ADR-021](../adr/ADR-021-public-catalog-query-endpoints.md)).
- `ResultExtensions` para traducción de `Result` a Problem Details.
- Corrección de `ValidationBehavior`: delegate compilado reemplaza reflexión frágil.
- Handlers de Mediator registrados como Scoped.
- 214 pruebas (75 Domain + 53 Application + 38 Infrastructure + 36 Architecture + 12 Functional).
- ADR-020, ADR-021 documentados.

#### Phase 4C — DietaryNeed y Service — Pendiente

- Agregado `DietaryNeed` (Dietary Catalog).
- Agregado `Service` (Establishments).
- Misma estructura que `EstablishmentCategory`.

### Dependencias

- Phase 3 (abstracciones base del dominio).

### Criterios de finalización

- Los tres catálogos se pueden crear, editar y cambiar de estado mediante la API.
- Se impide la creación de duplicados.
- Los endpoints de listado paginan correctamente.
- Las pruebas pasan en CI.

---

## Phase 5 — Establishments

### Objetivo

Implementar el agregado central de establecimiento, que representa a la entidad propietaria o responsable de uno o más locales dentro de la plataforma.

### Dependencias

- Phase 4 (catálogos fundacionales).

### Entregables

- Agregado `Establishment` con nombre, descripción y estado de publicación.
- Asignación de una o más categorías del catálogo al establecimiento.
- Ciclo de vida del estado de publicación: borrador, publicado, suspendido.
- Casos de uso esenciales: crear establecimiento, editar información básica, publicar, suspender.
- Endpoints administrativos protegidos por scope `Establishment`.
- Pruebas unitarias de lógica de dominio y pruebas de integración.

### Riesgos

- Diseñar el agregado con demasiados atributos desde el inicio, antes de conocer los requisitos reales de los usuarios propietarios.
- Confundir el establecimiento (entidad legal o marca) con la sucursal (local físico).

### Criterios de finalización

- Un establecimiento puede crearse, editarse y publicarse mediante la API.
- Las categorías asignadas pertenecen al catálogo vigente.
- El estado de publicación sigue el ciclo de vida definido.
- Las pruebas pasan en CI.

---

## Phase 6 — Branches

### Objetivo

Implementar el agregado de sucursal, que representa un local físico asociado a un establecimiento. La sucursal es la unidad operativa principal de la plataforma.

### Dependencias

- Phase 5 (Establishments).

### Entregables

- Agregado `Branch` asociado a un `Establishment`.
- Value object `Address` (calle, número, ciudad, provincia, país por código ISO).
- Value object `GeoLocation` (latitud y longitud).
- Value object `ContactInformation` (teléfono, email, sitio web).
- Modelo de períodos de apertura (`OpeningPeriod`): día de la semana, hora de apertura, hora de cierre.
- Asignación de servicios del catálogo a la sucursal.
- Casos de uso: crear sucursal, editar información, gestionar períodos de apertura, asignar servicios.
- Pruebas unitarias y de integración.

### Riesgos

- Complejidad del modelo de períodos de apertura (manejo de excepciones, horarios especiales, días feriados) que puede extenderse innecesariamente en esta fase.
- Validación de coordenadas geográficas sin tener requisitos de precisión definidos.

### Criterios de finalización

- Una sucursal puede crearse con dirección, geolocalización y contacto.
- Los períodos de apertura se pueden agregar, modificar y eliminar.
- Los servicios se asignan desde el catálogo vigente.
- Las pruebas pasan en CI.

---

## Phase 7 — Dietary suitability

### Objetivo

Implementar el mecanismo mediante el cual una sucursal declara y verifica su aptitud para atender necesidades dietéticas específicas. Es un diferenciador central de la propuesta de valor de Lyria.

### Dependencias

- Phase 6 (Branches).

### Entregables

- Entidad `BranchDietarySuitability` que asocia una sucursal con una necesidad dietética del catálogo.
- Estado de verificación: declarado, con evidencia presentada, verificado, rechazado.
- Soporte para adjuntar evidencia documental (referencias a archivos externos).
- Reglas de publicación: una sucursal solo puede mostrar aptitudes verificadas o en proceso declarado, según configuración.
- Flujo administrativo de revisión de evidencia.
- Pruebas unitarias y de integración.

### Riesgos

- Definir un proceso de verificación demasiado complejo para la fase inicial sin tener un equipo de moderación real.
- Acoplar la lógica de publicación a estados demasiado granulares que dificulten la evolución.

### Criterios de finalización

- Una sucursal puede declarar aptitud para una necesidad dietética.
- El estado de verificación sigue el flujo definido.
- Las reglas de publicación se aplican correctamente.
- Las pruebas pasan en CI.

---

## Phase 8 — Identity & Access

### Objetivo

Implementar la gestión de identidad de usuarios, roles, permisos y scopes de autorización. Esta fase habilita la autenticación y el control de acceso en toda la plataforma.

### Dependencias

- Phase 3 (abstracciones base del dominio).
- Puede desarrollarse en paralelo con las fases 5 a 7.

### Entregables

- Agregado `UserAccount` con credenciales, estado (activo, suspendido, pendiente de verificación).
- Modelo de `Role` asignable a usuarios.
- Modelo de `Permission` con scope (`Global`, `Establishment`, `Branch`).
- Asignación de permisos a roles y de roles a usuarios.
- Autenticación mediante JWT (emisión y validación de tokens).
- Middleware de autorización basado en permisos y scopes.
- Endpoints de autenticación: registro, inicio de sesión, renovación de token.
- Pruebas unitarias, de integración y funcionales del flujo de autenticación.

### Riesgos

- Complejidad del modelo de permisos con scopes puede llevar a una implementación difícil de mantener si no se diseña con claridad.
- La gestión de tokens (expiración, revocación, renovación) puede requerir infraestructura adicional no planificada.
- Dependencia de decisiones de licencia o paquetes para la gestión de identidad.

### Criterios de finalización

- Un usuario puede registrarse, iniciar sesión y recibir un JWT válido.
- Los endpoints protegidos rechazan solicitudes sin token o con permisos insuficientes.
- El scope de autorización se aplica correctamente por recurso.
- Las pruebas pasan en CI.

---

## Phase 9 — User Profiles

### Objetivo

Implementar el perfil de usuario, que incluye información pública visible y preferencias personales privadas, entre ellas las necesidades dietéticas del propio usuario.

### Dependencias

- Phase 8 (Identity & Access).

### Entregables

- Agregado `UserProfile` vinculado a `UserAccount`.
- Sección pública del perfil: nombre visible, foto de perfil, descripción breve.
- Sección privada: fecha de nacimiento, necesidades dietéticas personales.
- Endpoints separados para consulta pública y gestión privada del perfil.
- Validación de que los datos privados no se exponen en endpoints públicos.
- Pruebas unitarias y funcionales.

### Riesgos

- Mezclar datos públicos y privados en el mismo modelo de respuesta por error.
- Acumular atributos de perfil sin un caso de uso que los justifique.

### Criterios de finalización

- El endpoint público de perfil no expone datos privados.
- El usuario autenticado puede actualizar sus preferencias dietéticas.
- Las pruebas pasan en CI.

---

## Phase 10 — Favorites

### Objetivo

Permitir a los usuarios autenticados marcar sucursales como favoritas para acceder rápidamente a ellas desde su perfil.

### Dependencias

- Phase 6 (Branches).
- Phase 8 (Identity & Access).

### Entregables

- Módulo de favoritos: agregar sucursal a favoritos, quitar sucursal de favoritos.
- Listado paginado de sucursales favoritas del usuario autenticado.
- Validación de que solo se pueden marcar como favoritas sucursales publicadas.
- Pruebas unitarias y funcionales.

### Riesgos

- Diseñar la funcionalidad de favoritos con más complejidad de la necesaria (por ejemplo, listas múltiples, compartir favoritos) sin demanda validada.

### Criterios de finalización

- Un usuario autenticado puede agregar y quitar sucursales de sus favoritos.
- El listado de favoritos está paginado.
- Las pruebas pasan en CI.

---

## Phase 11 — Reviews

### Objetivo

Implementar el sistema de reseñas y calificaciones de sucursales por parte de usuarios autenticados, incluyendo el mantenimiento de un resumen de calificación por sucursal.

### Dependencias

- Phase 6 (Branches).
- Phase 8 (Identity & Access).

### Entregables

- Agregado `Review` con calificación numérica y comentario de texto.
- Reglas de negocio: un usuario solo puede publicar una reseña por sucursal; la reseña puede editarse dentro de un período definido.
- Entidad `ReviewSummary` por sucursal: promedio de calificaciones y cantidad de reseñas.
- Actualización del `ReviewSummary` al publicar, editar o eliminar una reseña.
- Endpoints públicos de listado de reseñas por sucursal (paginados).
- Endpoints autenticados para publicar y editar reseña propia.
- Pruebas unitarias y de integración.

### Riesgos

- Condiciones de carrera en la actualización del `ReviewSummary` bajo carga concurrente.
- Definir reglas de edición o eliminación de reseñas demasiado restrictivas o permisivas sin validación con usuarios reales.

### Criterios de finalización

- Un usuario autenticado puede publicar una reseña por sucursal.
- El `ReviewSummary` refleja correctamente el promedio y la cantidad.
- Las reseñas se listan paginadas en el endpoint público.
- Las pruebas pasan en CI.

---

## Phase 12 — Moderation

### Objetivo

Implementar los flujos de moderación de contenido generado por usuarios, incluyendo reportes, revisión de reseñas y verificación de aptitud dietética.

### Dependencias

- Phase 11 (Reviews).

### Entregables

- Mecanismo de reporte de reseñas por usuarios.
- Flujo de revisión administrativa de reportes: pendiente, en revisión, resuelto, desestimado.
- Acciones de moderación: ocultar reseña, restaurar reseña, suspender usuario por abuso.
- Integración con el flujo de verificación de aptitud dietética de la Fase 7.
- Trazabilidad de todas las acciones de moderación con auditoría de quién ejecutó cada acción y cuándo.
- Endpoints administrativos protegidos por permisos de moderación.
- Pruebas unitarias y funcionales del flujo de moderación.

### Riesgos

- Definir flujos de moderación demasiado elaborados sin un equipo de moderación real que los opere.
- Diseñar la trazabilidad de auditoría de forma que genere un volumen de datos difícil de gestionar.

### Criterios de finalización

- Un usuario puede reportar una reseña.
- Un moderador puede revisar reportes pendientes y tomar acciones.
- Todas las acciones quedan registradas en el log de auditoría.
- Las pruebas pasan en CI.

---

## Phase 13 — Search & Discovery

### Objetivo

Implementar el motor de búsqueda y descubrimiento de sucursales, incluyendo filtros por categoría, necesidades dietéticas, servicios y distancia geográfica aproximada.

### Dependencias

- Phase 4 (Catálogos fundacionales).
- Phase 5 (Establishments).
- Phase 6 (Branches).
- Phase 7 (Dietary suitability).
- Phase 8 (Identity & Access).
- Phase 11 (Reviews) — para ordenamiento por calificación.

### Entregables

- Endpoint de búsqueda de sucursales con filtros combinables: categoría, necesidad dietética, servicios, ubicación aproximada.
- Ordenamiento por: relevancia, calificación promedio, distancia.
- Cálculo de distancia aproximada sobre SQL Server (sin motor geoespacial externo en esta fase).
- Proyecciones de lectura específicas para los resultados de búsqueda (sin cargar agregados completos).
- Paginación obligatoria en todos los resultados de búsqueda.
- Índices de base de datos para los campos de filtrado más utilizados.
- Pruebas funcionales de los principales escenarios de búsqueda.

### Riesgos

- Rendimiento insuficiente de las búsquedas geográficas sobre SQL Server a medida que crece el volumen de datos.
- Complejidad de los filtros combinados puede generar consultas lentas si los índices no están bien diseñados.
- Presión por incorporar un motor de búsqueda externo antes de validar que SQL Server no es suficiente.

### Criterios de finalización

- La búsqueda retorna resultados correctos para las combinaciones de filtros implementadas.
- Los resultados están paginados.
- Las proyecciones de lectura no cargan datos innecesarios.
- Los índices críticos están creados en migraciones.
- Las pruebas funcionales pasan en CI.
