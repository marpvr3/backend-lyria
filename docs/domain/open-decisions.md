# Decisiones Abiertas de Dominio — Lyria

> **Plataforma:** Lyria — Plataforma de Gastronomía
> **Última revisión:** 2026-07-13
> **Estado:** En análisis

Estas decisiones no están aún resueltas en el modelo de dominio. Deben ser cerradas antes de iniciar la fase indicada. Ninguna decisión crítica debe permanecer ambigua antes de la Fase 3.

---

## Tabla de resumen

| # | Decisión | Recomendación | Resolver antes de | Impacto |
|---|----------|---------------|-------------------|---------|
| 1 | Catálogo unificado o separado para necesidades alimentarias | Catálogo unificado con discriminador Type | Fase 4 | Alto |
| 2 | Estrategia de moderación de reseñas | Publicación inmediata con post-moderación por reportes | Fase 11 | Alto |
| 3 | Estrategia inicial de rating resumido | ReviewSummary como proyección separada | Fase 11 | Alto |
| 4 | Forma conceptual de asignaciones de roles con alcance | RoleAssignment como agregado independiente | Fase 8 | Alto |
| 5 | Quién puede crear establecimientos | Solo Admin (MVP) | Fase 5 | Alto |
| 6 | Quién puede solicitar administración de un establecimiento | Admin asigna managers manualmente (MVP) | Fase 8 | Medio |
| 7 | Requisitos para marcar contenido como verificado | Moderador marca verificado con notas opcionales | Fase 12 | Medio |
| 8 | Evidencia requerida para adecuación alimentaria | Evidencia requerida solo para niveles Verified/Certified | Fase 7 | Alto |
| 9 | Si las imágenes requieren aprobación | Publicación inmediata (MVP) | Fase 6 | Medio |
| 10 | Si ciudades y provincias deben ser catálogos | Texto libre para MVP, CountryCode como ISO | Fase 6 | Medio |
| 11 | Si Favorite necesita eliminación lógica | Eliminación física | Fase 10 | Bajo |
| 12 | Si ReviewReport pertenece a Reviews o Moderation | Módulo Moderation | Fase 11–12 | Medio |
| 13 | Si BranchImage pertenece a Branch o Media | Módulo Media separado con BranchMediaGallery como aggregate root | Fase 6 | Alto |
| 14 | Si los catálogos son agregados o datos de referencia administrables | Agregados ligeros con invariantes mínimas | Fase 4 | Alto |
| 15 | Si se utilizarán identificadores fuertemente tipados | Strongly-typed IDs (opción B o C) | **Fase 3** | Alto |

---

## Decisiones detalladas

---

### 1. Catálogo unificado o separado para necesidades alimentarias

**Contexto:** Las necesidades alimentarias de los usuarios abarcan distintas categorías: restricciones médicas, alergias, intolerancias, estilos de vida, restricciones religiosas y preferencias personales. Se debe decidir cómo modelar este catálogo en el dominio.

#### Opciones

**Opción A — Catálogo unificado con discriminador `Type`**

Existe una sola entidad `DietaryNeed` con un campo `Type` que actúa como discriminador (`MedicalRestriction`, `Allergy`, `Intolerance`, `Lifestyle`, `Religious`, `Preference`).

| Ventajas | Desventajas |
|----------|-------------|
| Modelo simplificado con una sola tabla y repositorio | El discriminador puede volverse complejo si los tipos tienen comportamientos muy distintos |
| Consultas consistentes sin joins entre tablas | Riesgo de atributos nulos por tipo que no los utilizan |
| Extensible: agregar un nuevo tipo no requiere nueva tabla | Validaciones específicas por tipo requieren lógica condicional |
| Facilita la búsqueda y filtrado multi-tipo | |

**Opción B — Catálogos separados por tipo**

Cada categoría tiene su propio catálogo: `Allergy`, `Intolerance`, `LifestyleChoice`, etc.

| Ventajas | Desventajas |
|----------|-------------|
| Cada tabla tiene solo los atributos relevantes a su tipo | Duplicación de estructura y lógica de administración |
| Validaciones específicas por tipo son más claras | Consultas que cruzan tipos requieren UNION o múltiples llamadas |
| Esquema más normalizado | Mayor cantidad de repositorios y casos de uso |
| | Extensibilidad costosa: agregar un tipo implica nueva tabla |

#### Recomendación

**Opción A — Catálogo unificado con discriminador `Type`.**
Más simple, extensible y consistente para consultas. Las diferencias entre tipos no justifican la fragmentación del modelo en el estado actual del producto.

#### Impacto

Alto. Afecta la estructura del catálogo de dominio, la forma en que los usuarios asocian necesidades alimentarias y cómo los establecimientos las declaran para filtrado.

#### Resolver antes de

**Fase 4.**

---

### 2. Estrategia de moderación de reseñas

**Contexto:** Las reseñas que los usuarios publican sobre establecimientos y platos pueden contener contenido inapropiado. Se debe decidir cuándo y cómo se modera ese contenido.

#### Opciones

**Opción A — Publicación inmediata (post-moderación pasiva)**

Las reseñas se publican sin revisión previa. La moderación es reactiva y no está definida formalmente.

| Ventajas | Desventajas |
|----------|-------------|
| Menor fricción para el usuario | Sin mecanismo para retirar contenido inapropiado |
| Implementación mínima | Riesgo reputacional sin herramientas de control |

**Opción B — Moderación previa a la publicación (pre-moderación)**

Las reseñas quedan en estado pendiente hasta ser aprobadas por un moderador.

| Ventajas | Desventajas |
|----------|-------------|
| Control total del contenido publicado | Alta fricción: el usuario no ve su reseña de inmediato |
| Menor riesgo de contenido inapropiado | Requiere capacidad operativa de moderación desde el inicio |
| | Escala mal con crecimiento de volumen |

**Opción C — Publicación inmediata con post-moderación por reportes**

Las reseñas se publican de inmediato. Los usuarios pueden reportar una reseña, y el reporte activa el flujo de moderación.

| Ventajas | Desventajas |
|----------|-------------|
| Baja fricción para el usuario | Contenido inapropiado puede estar visible hasta ser reportado |
| Moderación proporcional al volumen real de problemas | Requiere flujo de reportes y resolución (ReviewReport) |
| Escala bien: solo se modera lo que se reporta | |
| Equilibrio razonable para MVP | |

#### Recomendación

**Opción C para MVP** — publicación inmediata con post-moderación activada por reportes. Ofrece la menor fricción con un mecanismo de control proporcional a la escala inicial.

#### Impacto

Alto. Define el ciclo de vida del estado de una reseña (`Draft`, `Published`, `UnderReview`, `Removed`) y los eventos de dominio asociados.

#### Resolver antes de

**Fase 11.**

---

### 3. Estrategia inicial de rating resumido

**Contexto:** Los establecimientos y sus sucursales deben mostrar una calificación promedio y el conteo de reseñas. Se debe decidir cómo se calcula y actualiza ese valor.

#### Opciones

**Opción A — Cálculo en cada consulta (`COUNT`/`AVG`)**

El promedio y conteo se calculan en tiempo real con SQL cada vez que se consulta.

| Ventajas | Desventajas |
|----------|-------------|
| Siempre exacto | Costoso bajo alto volumen de reseñas |
| Sin estado adicional que mantener | Escala mal |

**Opción B — Proyección `ReviewSummary` actualizada por eventos de dominio**

Se mantiene una entidad o read model `ReviewSummary` que se actualiza cada vez que se publica, edita o elimina una reseña.

| Ventajas | Desventajas |
|----------|-------------|
| Consultas de lectura eficientes | Eventual consistency: puede quedar desactualizado si falla un evento |
| Desacopla la lógica de lectura de la de escritura | Requiere lógica de actualización en la capa de aplicación |
| Escala bien | |
| Compatible con arquitectura de eventos del dominio | |

**Opción C — Columnas cacheadas en `Branch` controladas por el sistema**

El promedio y conteo se almacenan directamente en la entidad `Branch` y se actualizan sincrónicamente.

| Ventajas | Desventajas |
|----------|-------------|
| Simple de consultar (un solo objeto) | Acoplamiento entre reseñas y la entidad Branch |
| Sin proyección adicional | Hace crecer el agregado Branch innecesariamente |
| | Problemas de concurrencia bajo escritura simultánea |

#### Recomendación

**Opción B — `ReviewSummary` como proyección separada.** Buen equilibrio entre rendimiento y corrección. Compatible con la arquitectura basada en eventos y no contamina el agregado principal.

#### Impacto

Alto. Afecta el diseño del módulo de reseñas, los eventos de dominio que se emiten y la estructura de la capa de lectura.

#### Resolver antes de

**Fase 11.**

---

### 4. Forma conceptual de asignaciones de roles con alcance

**Contexto:** Los usuarios pueden tener roles con alcance limitado a un establecimiento o sucursal específica (por ejemplo, `Manager` de un establecimiento). Se debe modelar cómo se representa y gestiona esa asignación.

#### Opciones

**Opción A — `RoleAssignment` como entidad dentro del agregado `UserAccount`**

La asignación de rol es una entidad hija del usuario.

| Ventajas | Desventajas |
|----------|-------------|
| Fácil acceso desde el usuario | El agregado `UserAccount` crece con lógica de autorización |
| Consistencia garantizada por el agregado raíz | Dificulta la administración de roles sin cargar el usuario completo |
| | Acoplamiento entre identidad y autorización |

**Opción B — `RoleAssignment` como agregado independiente**

La asignación de rol es un agregado autónomo con su propio ciclo de vida.

| Ventajas | Desventajas |
|----------|-------------|
| Gestión por Admins sin necesidad de cargar `UserAccount` | Requiere consistencia eventual entre usuario y asignación |
| Ciclo de vida propio: creación, revocación, expiración | Más entidades y repositorios |
| Desacoplamiento entre identidad y permisos | |
| Permite auditoría independiente de asignaciones | |

**Opción C — `RoleAssignment` como parte de un agregado `Authorization` separado**

Existe un agregado `Authorization` que agrupa todas las asignaciones y políticas de un usuario o contexto.

| Ventajas | Desventajas |
|----------|-------------|
| Separación clara de concerns de autorización | Mayor complejidad estructural |
| Extensible para políticas complejas | Difícil de justificar en MVP |

#### Recomendación

**Opción B — agregado independiente.** Permite administración de roles sin acoplar la carga del usuario. Más alineado con el principio de responsabilidad única.

#### Impacto

Alto. Afecta el módulo de autorización, los flujos de gestión de managers y la forma en que se verifican permisos en los controllers.

#### Resolver antes de

**Fase 8.**

---

### 5. Quién puede crear establecimientos

**Contexto:** Lyria necesita una política clara sobre quién está autorizado a dar de alta un nuevo establecimiento en la plataforma.

#### Opciones

**Opción A — Solo Admin**

Solo usuarios con rol `Admin` pueden crear establecimientos.

| Ventajas | Desventajas |
|----------|-------------|
| Control total sobre el crecimiento del catálogo | Requiere intervención manual del equipo operativo |
| Sin necesidad de flujo de aprobación | Frena el onboarding autónomo de propietarios |
| Más simple para MVP | |

**Opción B — Cualquier usuario autenticado (requiere moderación)**

Cualquier usuario puede crear un establecimiento; queda pendiente de aprobación.

| Ventajas | Desventajas |
|----------|-------------|
| Autoservicio para propietarios | Requiere flujo completo de aprobación desde el inicio |
| Escalable en crecimiento | Riesgo de datos incorrectos o spam |
| | Mayor complejidad operativa |

**Opción C — Admin crea, Manager actualiza los asignados**

El Admin crea el establecimiento, luego asigna un Manager que puede actualizar su información.

| Ventajas | Desventajas |
|----------|-------------|
| Delegación controlada | Aún requiere intervención de Admin para creación |
| Manager tiene autonomía sobre datos que controla | Dos roles con responsabilidades distintas que coordinar |

#### Recomendación

**Opción A para MVP.** Crecimiento controlado y menor complejidad inicial. La creación autónoma puede habilitarse en fases posteriores.

#### Impacto

Alto. Define las políticas de autorización del módulo de establecimientos y los casos de uso de creación.

#### Resolver antes de

**Fase 5.**

---

### 6. Quién puede solicitar administración de un establecimiento

**Contexto:** Una vez que un establecimiento existe en la plataforma, se debe definir cómo un propietario o representante puede obtener acceso para administrarlo.

#### Opciones

**Opción A — Admin asigna managers manualmente**

Un Admin con acceso al backoffice asigna el rol `Manager` a un usuario sobre un establecimiento específico.

| Ventajas | Desventajas |
|----------|-------------|
| Control total y verificación manual | No escala sin equipo de soporte |
| Sin flujo adicional de claim | Requiere canal externo de comunicación (email, soporte) |
| Suficiente para MVP | |

**Opción B — Usuarios pueden solicitar administración (claim flow)**

Los usuarios envían una solicitud para administrar un establecimiento. La solicitud queda pendiente de aprobación.

| Ventajas | Desventajas |
|----------|-------------|
| Autoservicio para propietarios | Requiere flujo de claim, evidencia, aprobación |
| Escalable | Complejo: ¿quién valida la titularidad? |
| | Mayor carga operativa y de dominio |

**Opción C — Híbrido**

Admins asignan por defecto; existe un formulario de solicitud que genera una tarea de revisión.

| Ventajas | Desventajas |
|----------|-------------|
| Combina control con autoservicio | Requiere ambos flujos desde el inicio |
| | Más complejo que cualquiera de las opciones puras |

#### Recomendación

**Opción A para MVP.** El flujo de claim es complejo y puede diferirse. La asignación manual es suficiente hasta que la escala lo exija.

#### Impacto

Medio. Afecta el módulo de autorización y los flujos de gestión de establecimientos.

#### Resolver antes de

**Fase 8.**

---

### 7. Requisitos para marcar contenido como verificado

**Contexto:** Lyria puede marcar ciertos contenidos (platos, declaraciones de adecuación alimentaria, establecimientos) como verificados. Se debe definir qué requiere ese estado.

#### Opciones

**Opción A — Moderador marca verificado con notas opcionales**

Un moderador con los permisos adecuados marca el contenido como verificado y puede dejar una nota interna.

| Ventajas | Desventajas |
|----------|-------------|
| Simple de implementar | Verificación subjetiva sin respaldo documental |
| Suficiente para MVP | Puede perder credibilidad si no hay evidencia |
| Notas permiten trazabilidad mínima | |

**Opción B — Requiere carga de evidencia**

El moderador debe adjuntar un documento o evidencia antes de marcar como verificado.

| Ventajas | Desventajas |
|----------|-------------|
| Mayor credibilidad del estado verificado | Requiere módulo de gestión de documentos |
| Auditoría más robusta | Mayor fricción operativa |

**Opción C — Requiere certificación externa**

Solo entidades con una certificación de tercero reconocido pueden ser marcadas como verificadas.

| Ventajas | Desventajas |
|----------|-------------|
| Máxima credibilidad | Altamente complejo, dependencia externa |
| | Inviable para MVP |

#### Recomendación

**Opción A para MVP** con campo de notas opcionales. La verificación basada en evidencia puede incorporarse en versiones posteriores.

#### Impacto

Medio. Afecta el flujo de moderación y el significado del estado `Verified` en el dominio.

#### Resolver antes de

**Fase 12.**

---

### 8. Evidencia requerida para adecuación alimentaria

**Contexto:** Los establecimientos pueden declarar que un plato o menú es apto para ciertos perfiles alimentarios. Se debe definir qué nivel de evidencia se requiere para cada nivel de declaración.

#### Opciones

**Opción A — Sin evidencia requerida (solo declaración)**

Cualquier establecimiento puede declarar adecuación alimentaria sin presentar documentación.

| Ventajas | Desventajas |
|----------|-------------|
| Máxima facilidad de onboarding | Sin garantía de veracidad |
| | Riesgo para usuarios con restricciones médicas reales |

**Opción B — Evidencia requerida solo para niveles `Verified`/`Certified`**

La declaración libre no requiere evidencia. Los niveles superiores (`Verified`, `Certified`) requieren documentación adjunta.

| Ventajas | Desventajas |
|----------|-------------|
| Equilibrio entre accesibilidad y confianza | Requiere manejo de documentos para niveles altos |
| La declaración libre es suficiente para MVP básico | Dos flujos distintos según el nivel |
| Incentiva a establecimientos a subir de nivel | |

**Opción C — Evidencia siempre requerida**

Toda declaración de adecuación alimentaria debe respaldarse con documentación.

| Ventajas | Desventajas |
|----------|-------------|
| Máxima confiabilidad | Alta fricción: bloquea onboarding inicial |
| | Inviable para MVP |

#### Recomendación

**Opción B** — evidencia requerida solo para `Verified`/`Certified`. La declaración libre queda abierta; la verificación requiere prueba. Modelo por niveles progresivos.

#### Impacto

Alto. Define la entidad `DietaryAdequacy`, sus niveles posibles y los requisitos de documentación asociados.

#### Resolver antes de

**Fase 7.**

---

### 9. Si las imágenes requieren aprobación

**Contexto:** Los usuarios o managers pueden subir imágenes de establecimientos, sucursales o platos. Se debe definir si esas imágenes requieren revisión antes de publicarse.

#### Opciones

**Opción A — Publicación inmediata**

Las imágenes se publican sin aprobación previa.

| Ventajas | Desventajas |
|----------|-------------|
| Menor fricción para el uploader | Riesgo de imágenes inapropiadas visibles hasta reporte |
| Implementación mínima | |
| Suficiente para MVP | |

**Opción B — Requiere aprobación de moderador**

Las imágenes quedan en estado `PendingReview` hasta ser aprobadas.

| Ventajas | Desventajas |
|----------|-------------|
| Control del contenido visual | Alta fricción; imágenes no aparecen hasta aprobación |
| | Requiere capacidad de moderación de imágenes |

**Opción C — Publicación inmediata con verificaciones automáticas**

Las imágenes se publican, pero pasan por checks automatizados (tamaño, formato, detección de contenido).

| Ventajas | Desventajas |
|----------|-------------|
| Equilibrio entre fricción y control | Requiere integración con servicios externos |
| Escalable | Mayor complejidad técnica |

#### Recomendación

**Opción A para MVP.** La moderación de imágenes es una mejora de segunda generación. El riesgo inicial es bajo y el costo de implementación de las otras opciones es alto.

#### Impacto

Medio. Afecta el ciclo de vida de `BranchImage` y la política de publicación en el módulo Media.

#### Resolver antes de

**Fase 6.**

---

### 10. Si ciudades y provincias deben ser catálogos

**Contexto:** Los establecimientos y sucursales tienen una dirección que incluye ciudad y provincia. Se debe decidir si estos campos son texto libre o catálogos estructurados.

#### Opciones

**Opción A — Texto libre**

Ciudad y provincia son campos de tipo `string` en el value object `Address`.

| Ventajas | Desventajas |
|----------|-------------|
| Implementación mínima | Sin normalización: "Buenos Aires" y "Bs. As." son distintos |
| Suficiente para MVP | Dificulta filtrado y agrupación geográfica |
| Sin costo de mantenimiento de catálogo | |

**Opción B — Catálogos estructurados (`City`, `Province`)**

Existen entidades `City` y `Province` como catálogos administrables.

| Ventajas | Desventajas |
|----------|-------------|
| Normalización de datos geográficos | Alta complejidad para MVP |
| Facilita búsqueda y filtrado | Requiere poblar y mantener catálogos por país |
| Estadísticas geográficas confiables | |

**Opción C — Híbrido: texto libre con normalización opcional**

Se almacena texto libre, pero con un campo de referencia opcional a un catálogo de ciudades.

| Ventajas | Desventajas |
|----------|-------------|
| Flexibilidad inicial | Dos representaciones del mismo dato |
| Migración futura posible | Inconsistencia hasta que se normalice |

#### Recomendación

**Opción A para MVP.** El catálogo de ciudades es complejo y no esencial en la fase inicial. El código de país (`CountryCode`) como código ISO es la única estructura necesaria por ahora.

#### Impacto

Medio. Afecta el value object `Address` y la capacidad de filtrado geográfico en el módulo de búsqueda.

#### Resolver antes de

**Fase 6.**

---

### 11. Si `Favorite` necesita eliminación lógica

**Contexto:** Los usuarios pueden marcar establecimientos o platos como favoritos y luego quitarlos. Se debe decidir si esa acción es una eliminación física o lógica.

#### Opciones

**Opción A — Eliminación física**

El registro `Favorite` se elimina de la base de datos al quitar el favorito.

| Ventajas | Desventajas |
|----------|-------------|
| Modelo simple, sin columnas de estado | Sin historial de favoritos anteriores |
| Sin acumulación de registros inactivos | No recuperable sin auditoría externa |

**Opción B — Eliminación lógica (soft delete)**

El registro se marca como inactivo (`IsDeleted`, `RemovedAt`) pero permanece en la base de datos.

| Ventajas | Desventajas |
|----------|-------------|
| Historial completo de favoritos | Mayor complejidad en queries (filtrar por activos) |
| Recuperable | Acumulación de registros inactivos |
| Permite análisis de comportamiento | Sin requisito funcional claro que lo justifique |

#### Recomendación

**Opción A — eliminación física.** No existe requisito de auditoría ni recuperación para favoritos. El modelo más simple es el correcto aquí.

#### Impacto

Bajo. Afecta únicamente la implementación del repositorio de `Favorite` y la operación de quitar favorito.

#### Resolver antes de

**Fase 10.**

---

### 12. Si `ReviewReport` pertenece a Reviews o Moderation

**Contexto:** Cuando un usuario reporta una reseña como inapropiada, se crea un `ReviewReport`. Se debe decidir en qué módulo del dominio reside esa entidad.

#### Opciones

**Opción A — Módulo Reviews**

`ReviewReport` se ubica dentro del módulo de reseñas, como una entidad relacionada con la reseña reportada.

| Ventajas | Desventajas |
|----------|-------------|
| Cohesión con el objeto reportado | Mezcla concerns de contenido y moderación |
| Acceso directo a la reseña | El módulo Reviews crece con lógica de workflow |

**Opción B — Módulo Moderation**

`ReviewReport` pertenece al módulo de moderación, que gestiona el flujo de resolución del reporte.

| Ventajas | Desventajas |
|----------|-------------|
| Separación clara de concerns | Referencia cruzada entre módulos (ID de reseña) |
| Moderation tiene su propio ciclo de vida | Requiere coordinación entre módulos |
| Permite extender moderación a otros tipos de contenido | |
| Reviews no necesita conocer lógica de moderación | |

#### Recomendación

**Opción B — módulo Moderation.** Los reportes son sobre el flujo de moderación, no sobre la creación o contenido de la reseña. Separar los módulos permite extensibilidad futura (reportar imágenes, platos, etc.).

#### Impacto

Medio. Afecta la estructura del módulo Reviews y la frontera entre Reviews y Moderation.

#### Resolver antes de

**Fase 11–12.**

---

### 13. Si `BranchImage` pertenece a `Branch` o Media

**Contexto:** Las sucursales pueden tener imágenes asociadas. Se debe decidir si `BranchImage` es una entidad dentro del agregado `Branch` o si vive en un módulo Media independiente.

#### Opciones

**Opción A — Entidad dentro del agregado `Branch`**

`BranchImage` es una entidad hija de `Branch`, cargada junto con el agregado.

| Ventajas | Desventajas |
|----------|-------------|
| Consistencia garantizada por el agregado | El agregado `Branch` crece con concerns de almacenamiento |
| Acceso directo desde la sucursal | Las imágenes siempre se cargan aunque no se necesiten |
| | Dificulta el manejo independiente del ciclo de vida de imágenes |

**Opción B — Módulo Media separado con `BranchMediaGallery` como aggregate root**

El módulo Media tiene su propio aggregate root `BranchMediaGallery` (uno por sede), que contiene las entidades `BranchImage` como elementos internos.

| Ventajas | Desventajas |
|----------|-------------|
| Ciclo de vida independiente del ciclo de vida de `Branch` | Requiere módulo y repositorio adicional |
| `Branch` no crece con concerns de almacenamiento | Consistencia eventual entre Branch e imágenes |
| `BranchMediaGallery` protege las invariantes colectivas: unicidad de imagen principal y unicidad de órdenes | |
| Extensible a otros tipos de media (DishImage, MenuImage) | |
| Las imágenes no se cargan innecesariamente con Branch | |

#### Recomendación

**Opción B — módulo Media separado con `BranchMediaGallery` como aggregate root.** Las invariantes de la galería (máximo una imagen principal activa, órdenes únicos) son colectivas y requieren un aggregate root propio que las proteja. `BranchImage` es una entidad interna de `BranchMediaGallery`. `Branch` no conoce las imágenes; solo referencia al módulo Media por `BranchId`.

#### Decisión adoptada

**Resuelta.** `BranchMediaGallery` es el aggregate root del módulo Media, identificado por `BranchId`. `BranchImage` es una entidad interna con propiedades: `BranchImageId`, `StorageKey`, `PublicUrl`, `FileName`, `MimeType`, `AlternativeText`, `IsPrimary`, `SortOrder`, `Status`, `CreatedAt`, `UpdatedAt`.

#### Impacto

Alto. Afecta la estructura del módulo Media, el repositorio (`IBranchMediaGalleryRepository`) y la forma en que se accede a imágenes desde la API.

#### Resolver antes de

**Fase 6.**

---

### 14. Si los catálogos son agregados o datos de referencia administrables

**Contexto:** Lyria tiene varios catálogos administrables: `DietaryNeed`, `Cuisine`, `Tag`, `Category`, etc. Se debe decidir cuánto comportamiento de dominio deben tener estas entidades.

#### Opciones

**Opción A — Aggregate roots completos con comportamiento de dominio**

Cada catálogo es un agregado raíz con invariantes, eventos de dominio y lógica compleja.

| Ventajas | Desventajas |
|----------|-------------|
| Máxima expresividad del dominio | Sobreingeniería para entidades que son esencialmente datos |
| | Complejidad innecesaria |

**Opción B — Datos de referencia simples administrados vía CRUD**

Los catálogos son tablas simples sin comportamiento de dominio.

| Ventajas | Desventajas |
|----------|-------------|
| Implementación mínima | Sin validaciones de unicidad ni control de estado activo/inactivo |
| Fácil de entender | Puede llevar a inconsistencias en el catálogo |

**Opción C — Agregados ligeros con invariantes mínimas**

Los catálogos son agregados con comportamiento reducido: unicidad de nombre y estado `Active`/`Inactive`.

| Ventajas | Desventajas |
|----------|-------------|
| Suficiente expresividad sin sobreingeniería | Requiere definir claramente qué es un "agregado ligero" |
| Validaciones básicas garantizadas | |
| Estado activo/inactivo controlado por el dominio | |
| Extensible si se necesita más comportamiento | |

#### Recomendación

**Opción C — agregados ligeros.** Los catálogos necesitan unicidad y control de estado, pero no comportamiento complejo. Es el balance correcto entre expresividad y pragmatismo.

#### Impacto

Alto. Afecta el diseño base de todos los catálogos del dominio y la estructura de sus repositorios.

#### Resolver antes de

**Fase 4.**

---

### 15. Si se utilizarán identificadores fuertemente tipados

**Contexto:** Los agregados y entidades del dominio necesitan identificadores únicos. Se debe decidir si usar `Guid` primitivo o strongly-typed IDs por tipo de entidad.

#### Opciones

**Opción A — Tipos primitivos (`Guid`)**

Todos los identificadores son `Guid` sin wrapping adicional.

| Ventajas | Desventajas |
|----------|-------------|
| Sin fricción adicional | Intercambio accidental de IDs de distintos tipos posible en tiempo de compilación |
| Familiar para cualquier desarrollador .NET | Menor expressividad: `Guid` no indica qué entidad identifica |

**Opción B — Strongly-typed IDs por entidad**

Cada entidad tiene su propio tipo de ID: `EstablishmentId`, `BranchId`, `UserId`, etc.

| Ventajas | Desventajas |
|----------|-------------|
| El compilador previene el intercambio accidental de IDs | Requiere serialización/deserialización customizada |
| Mayor legibilidad en firmas de métodos y constructores | Más tipos que mantener |
| Expresivo: el tipo comunica a qué entidad pertenece | Friction con EF Core (value converters necesarios) |

**Opción C — Strongly-typed IDs con tipo base compartido**

Similar a B, pero todos los IDs heredan de un tipo base genérico (`TypedId<T>`) para reducir boilerplate.

| Ventajas | Desventajas |
|----------|-------------|
| Todos los beneficios de B | Complejidad del tipo base genérico |
| Menor boilerplate por entidad | Puede ser difícil de entender para nuevos colaboradores |
| Conversión y serialización centralizadas | |

#### Recomendación

**Opción B o C — strongly-typed IDs.** La seguridad en tiempo de compilación justifica el overhead. Se debe decidir la implementación concreta (`record struct`, `class`, `readonly struct`) y cómo se manejan los value converters de EF Core antes de crear el primer agregado.

> **Nota crítica:** Esta decisión debe tomarse **antes de la Fase 3**, ya que impacta la firma de todos los constructores y métodos de dominio. Es la única decisión de esta lista con fecha límite en Fase 3.

#### Impacto

Alto. Afecta la firma de todos los agregados, entidades, repositorios y casos de uso del sistema.

#### Resolver antes de

**Fase 3** — ninguna entidad de dominio debe crearse antes de cerrar esta decisión.

---

## Prioridad de resolución

Las siguientes decisiones son bloqueantes antes de iniciar la Fase 3 y no deben permanecer ambiguas:

| Decisión | Motivo |
|----------|--------|
| **#15 — Strongly-typed IDs** | Define la firma base de todos los agregados y entidades del dominio |

Las demás decisiones tienen su plazo indicado en la columna "Resolver antes de" de la tabla de resumen. Se recomienda resolver las de impacto **Alto** con anticipación suficiente (mínimo una fase antes del límite) para evitar retrabajo estructural.
