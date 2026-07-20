# Requisitos No Funcionales — Lyria

## Propósito

Este documento registra los requisitos no funcionales que guían el diseño y la evolución de la plataforma Lyria. No describen qué hace el sistema, sino cómo debe comportarse en términos de seguridad, rendimiento, disponibilidad, privacidad, internacionalización y calidad.

---

## Seguridad

### Autenticación

- La autenticación mediante JWT está planificada pero aún no implementada.
- Ningún endpoint sensible debe quedar accesible de forma anónima una vez activada la autenticación.
- Los tokens deben tener tiempo de expiración acotado y soporte de renovación segura.

### Autorización

- El modelo de autorización es basado en permisos, no en roles directos.
- Cada operación protegida requiere un permiso explícito asignado al usuario.
- Los permisos tienen alcance (`Scope`) que puede ser:
  - **Global**: aplica a toda la plataforma (por ejemplo, administradores del sistema).
  - **Establishment**: aplica al establecimiento propio del usuario.
  - **Branch**: aplica a una sucursal específica del establecimiento.
- No se debe inferir permisos implícitos; toda acción privilegiada requiere permiso declarado.

### Protección de endpoints administrativos

- Los endpoints de administración deben estar separados lógicamente de los endpoints públicos.
- El acceso sin el permiso adecuado debe retornar `403 Forbidden`, nunca `404 Not Found`, para usuarios autenticados sin privilegios.

### Validación de entrada

- Toda entrada del usuario debe validarse en la capa de Application antes de procesarse.
- Las validaciones deben aplicarse en los comandos (commands) correspondientes.
- No se deben confiar en validaciones solo del lado del cliente.

### Rate limiting

- El rate limiting es una decisión futura y no está implementado en la fase inicial.
- Se debe diseñar la API de forma que sea posible agregarlo sin cambios estructurales.

### Protección de datos personales

- Los datos personales sensibles (fecha de nacimiento, preferencias dietéticas, historial) no deben exponerse en endpoints públicos.
- El acceso a datos personales requiere autenticación y permisos adecuados.

### Manejo seguro de imágenes

- Las imágenes subidas por usuarios deben validarse en tipo, tamaño y contenido antes de almacenarse.
- El almacenamiento de imágenes debe desvincularse del servidor de aplicación.

### Auditoría administrativa

- Las acciones administrativas críticas (activar/desactivar recursos, moderar contenido, asignar permisos) deben quedar registradas con el usuario responsable, la acción ejecutada y el momento exacto.

### Prevención de escalada de privilegios

- Un usuario no puede asignarse permisos a sí mismo.
- La asignación de permisos solo puede realizarse por actores con permiso explícito para ello.

### Prevención de acceso entre establecimientos

- Un usuario con permisos sobre un establecimiento no debe poder leer ni modificar datos de otro establecimiento.
- La autorización debe verificarse siempre contra el recurso solicitado, no solo contra el rol del usuario.

---

## Rendimiento

### Paginación obligatoria

- Todos los endpoints que retornan colecciones deben implementar paginación.
- No se permite retornar listas completas sin límite en ningún caso de uso público o administrativo.
- Los parámetros de paginación deben tener valores predeterminados razonables y límites máximos explícitos.

### Proyecciones directas para búsquedas

- Las consultas de búsqueda y listado deben utilizar proyecciones directas sobre la base de datos.
- No se deben cargar agregados completos para luego filtrar o mapear en memoria.
- Las proyecciones deben construirse como DTOs o modelos de lectura específicos para cada caso de uso.

### Carga de agregados

- Los agregados completos solo deben cargarse cuando la operación requiere ejecutar lógica de dominio sobre ellos.
- Las consultas de solo lectura deben evitar cargar navegaciones innecesarias.

### Índices (futuros)

- Se planifican índices sobre: estado de publicación, categoría, ubicación geográfica, relaciones entre entidades.
- Los índices deben crearse en migraciones dedicadas, no mezcladas con cambios de esquema.
- La decisión de cada índice debe estar justificada por un caso de uso de consulta concreto.

### Estrategia de búsqueda inicial

- La búsqueda inicial se implementará directamente sobre SQL Server.
- No se introducirá un motor de búsqueda externo (Elasticsearch, Azure Search, etc.) de forma prematura.
- La arquitectura debe permitir incorporar un motor externo en el futuro sin cambiar los contratos de la capa de Application.

### Cálculos geográficos

- Los cálculos de distancia geográfica se implementarán inicialmente sobre SQL Server (funciones espaciales o cálculo aproximado).
- No se adoptará un motor geoespacial externo hasta que el volumen de datos o los requisitos de precisión lo justifiquen.

### Resumen de reseñas

- Se evalúa la posibilidad de mantener un resumen materializado de calificaciones por sucursal (`ReviewSummary`).
- Este enfoque evitaría calcular promedios en tiempo real sobre grandes volúmenes de reseñas.
- La decisión se tomará cuando se implemente el módulo de reseñas (Fase 11).

---

## Disponibilidad y operación

### Health checks

- La plataforma expone endpoints de health check ya implementados.
- Deben incluir verificación de conectividad con la base de datos.
- Los health checks deben ser monitoreables por herramientas externas sin autenticación.

### Problem Details

- Todos los errores de la API se retornan en formato `ProblemDetails` (RFC 7807), ya configurado.
- Los errores de validación, errores de negocio y errores inesperados deben distinguirse mediante `type` o `status` apropiados.

### Logging estructurado

- Todos los logs deben emitirse en formato estructurado (JSON o equivalente) para facilitar consultas y alertas.
- Se debe registrar al menos: nivel, mensaje, timestamp, nombre del servicio, y contexto relevante de la operación.

### Correlation ID

- Cada solicitud HTTP debe recibir un `CorrelationId` (generado por el servidor si el cliente no lo provee).
- El `CorrelationId` debe propagarse en todos los logs relacionados con esa solicitud.
- El `CorrelationId` debe retornarse en los headers de la respuesta.

### Manejo centralizado de errores

- Las excepciones no controladas deben ser capturadas por middleware centralizado.
- El middleware debe registrar el error completo internamente y retornar una respuesta `ProblemDetails` sin exponer detalles de implementación.

### Métricas (futuro)

- La plataforma debe prepararse para exponer métricas operacionales (latencia, tasa de error, uso de recursos).
- Se evaluará la integración con Prometheus o Application Insights en fases posteriores.

### Trazabilidad de moderación

- Las acciones de moderación (reportes, suspensiones, verificaciones) deben quedar trazadas con suficiente detalle para auditoría posterior.

---

## Privacidad

### Minimización de datos

- Solo se deben recopilar y almacenar los datos estrictamente necesarios para las funcionalidades implementadas.
- No se deben crear campos "por si acaso" sin un caso de uso definido.

### Fecha de nacimiento

- La fecha de nacimiento del usuario no debe exponerse en ningún endpoint público.
- Su uso debe limitarse a validaciones internas (por ejemplo, restricciones de edad).

### Preferencias personales

- Las preferencias dietéticas y necesidades especiales del usuario no deben exponerse públicamente.
- Solo el propio usuario (autenticado) debe poder acceder a sus preferencias.

### Perfil público y privado

- El perfil de usuario debe tener una sección pública (nombre visible, foto) y una sección privada (datos personales, preferencias).
- Los endpoints deben retornar únicamente la sección correspondiente según el contexto de la solicitud.

### Eliminación o anonimización (futura)

- Se debe prever la posibilidad de eliminar o anonimizar datos de un usuario a solicitud del mismo.
- La arquitectura de datos no debe crear dependencias que impidan cumplir con solicitudes de supresión.

### Protección de credenciales

- Las contraseñas deben almacenarse siempre con hash seguro (bcrypt o equivalente).
- No se deben almacenar contraseñas en texto plano bajo ninguna circunstancia.

### Logs y datos sensibles

- Los logs no deben contener contraseñas, tokens de autenticación, ni datos personales sensibles.
- Se debe revisar activamente que los serializers de logs no exporten propiedades sensibles de los objetos.

---

## Internacionalización

### Idioma de la interfaz

- La interfaz y todos los mensajes funcionales al usuario se presentan inicialmente en español.
- No se implementará soporte multiidioma en las fases iniciales.

### Código técnico

- Los identificadores de código (clases, interfaces, métodos, variables, namespaces) se escriben en inglés.
- Esta convención es obligatoria en todas las capas del sistema.

### Representación de países

- Los países se representan mediante código ISO 3166-1 alpha-2 (por ejemplo, `AR`, `CL`, `UY`).
- No se almacenan nombres de país en texto libre.

### Representación de zonas horarias

- Las zonas horarias se representan mediante identificadores IANA (por ejemplo, `America/Argentina/Buenos_Aires`).
- No se usan offsets fijos como `UTC-3` para almacenamiento persistente.

### Soporte geográfico

- La arquitectura de datos debe soportar desde el inicio la existencia de diferentes ciudades, provincias y países.
- No se deben codificar valores geográficos de forma rígida en la lógica de negocio.

### Traducción de contenido

- No se implementará traducción de contenido generado por usuarios en ninguna fase actual.
- La arquitectura debe permitir incorporarlo en el futuro sin cambios disruptivos.

---

## Calidad

### Pruebas unitarias

- La lógica de dominio y de aplicación debe cubrirse con pruebas unitarias.
- Las pruebas unitarias no deben depender de base de datos, red ni sistema de archivos.

### Pruebas de integración

- Las interacciones con la base de datos y servicios externos deben cubrirse con pruebas de integración.
- Se utilizan bases de datos reales (o contenedores) en las pruebas de integración, no mocks de EF Core.

### Pruebas funcionales

- Los flujos completos de la API deben cubrirse con pruebas funcionales que ejerciten la pila completa (desde el controller hasta la base de datos).

### Pruebas de arquitectura

- Las reglas de dependencia entre capas se validan automáticamente mediante ArchUnitNET, ya implementado.
- Cualquier violación de las reglas de dependencia debe ser un error de build o test, no solo una advertencia.

### ADRs obligatorios

- Toda decisión arquitectónica significativa debe documentarse en un Architecture Decision Record (ADR).
- No se pueden cambiar decisiones registradas en ADRs sin crear o actualizar el registro correspondiente.

### Documentación de reglas

- Las reglas de negocio no triviales deben estar documentadas, ya sea en el código (comentarios de dominio) o en documentos de arquitectura.

### Lógica de dominio en controllers

- Los controllers no deben contener lógica de negocio ni de dominio.
- Los controllers solo deben encargarse de: recibir la solicitud, delegar a la capa de Application, y retornar la respuesta HTTP adecuada.
