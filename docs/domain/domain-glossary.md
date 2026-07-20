# Glosario del dominio — Lyria

## Conceptos principales

| Término en español | Nombre técnico | Definición |
|-------------------|---------------|------------|
| Establecimiento | `Establishment` | Marca, comercio u organización gastronómica. Representa la identidad comercial, no la ubicación física. |
| Sede | `Branch` | Ubicación física concreta de un establecimiento donde se atiende al público. Las condiciones reales de atención, horarios, servicios, imágenes, reseñas y adecuación alimentaria pertenecen a la sede. |
| Categoría | `EstablishmentCategory` | Tipo de establecimiento gastronómico: restaurante, cafetería, bar, panadería, pastelería, heladería, casa de té, etc. Implementado en Phase 4A. Ver [documentación](establishment-category.md). |
| Necesidad alimentaria | `DietaryNeed` | Condición, restricción, alergia, intolerancia, estilo de vida o requerimiento religioso relacionado con la alimentación. |
| Adecuación alimentaria | `BranchDietarySuitability` | Relación entre una sede y una necesidad alimentaria, con nivel de adecuación, fuente de información y estado de verificación. |
| Nivel de adecuación | `SuitabilityLevel` | Grado en que una sede puede atender una necesidad alimentaria: opción disponible, varias opciones, preparación separada, especializado, completamente apto. |
| Fuente de información | `InformationSource` | Origen de la información de adecuación: declarada por el establecimiento, reportada por usuario, verificada por Lyria, certificada externamente. |
| Estado de verificación | `VerificationStatus` | Estado del proceso de verificación: no verificado, pendiente, verificado, rechazado, expirado. |
| Estado de publicación | `PublicationStatus` | Estado del ciclo de vida de un contenido publicable: borrador, en revisión, publicado, suspendido, archivado. |
| Servicio | `Service` | Característica o servicio ofrecido por una sede: delivery, terraza, estacionamiento, WiFi, acceso para personas con movilidad reducida, etc. |
| Período de apertura | `BranchOpeningPeriod` | Intervalo horario de atención de una sede en un día específico de la semana. |
| Galería de imágenes de sede | `BranchMediaGallery` | Aggregate root del módulo Media que agrupa y gestiona todas las imágenes de una sede. Existe exactamente una galería por sede, identificada por `BranchId`. Garantiza que haya como máximo una imagen principal activa y que los órdenes de visualización sean únicos. |
| Imagen de sede | `BranchImage` | Entidad interna de `BranchMediaGallery`. Metadatos de una imagen individual dentro de la galería de una sede, con referencia al almacenamiento externo. Propiedades: `BranchImageId`, `StorageKey`, `PublicUrl`, `FileName`, `MimeType`, `AlternativeText`, `IsPrimary`, `SortOrder`, `Status`, `CreatedAt`, `UpdatedAt`. |
| Reseña | `Review` | Valoración y comentario de un usuario sobre una sede, con rating entre 1 y 5. |
| Reporte de reseña | `ReviewReport` | Reporte de contenido inapropiado realizado por un usuario sobre una reseña. |
| Favorito | `Favorite` | Relación entre un usuario y una sede marcada como favorita. |
| Resumen de reseñas | `ReviewSummary` | Dato derivado que contiene el rating promedio y total de reseñas publicadas de una sede. |
| Cuenta de usuario | `UserAccount` | Identidad de acceso al sistema: email, credenciales, estado de la cuenta. Pertenece a Identity & Access. |
| Perfil de usuario | `UserProfile` | Información personal del usuario: nombre, apellido, teléfono, foto, fecha de nacimiento. Pertenece a User Profiles. |
| Rol | `Role` | Conjunto nombrado de permisos que puede asignarse a un usuario. Mantiene un conjunto controlado de `PermissionId` y es responsable de agregar y revocar referencias a permisos, evitando duplicados. |
| Permiso | `Permission` | Entidad de referencia independiente con identidad propia. Capacidad atómica de realizar una acción específica en el sistema, identificada por un código único (ej. `establishments.publish`). Puede ser referenciada por múltiples roles simultáneamente. Su administración es controlada: se define en código o seed, no se crea dinámicamente. |
| Asignación de rol | `RoleAssignment` | Aggregate root independiente. Vinculación de un rol con un usuario en un alcance determinado (global, establecimiento o sede). No es una entidad interna de `UserAccount`. |
| Alcance de rol | `RoleScope` | Ámbito en el que aplica una asignación de rol: global, por establecimiento o por sede. |
| Dirección | `Address` | Ubicación textual de una sede: dirección, barrio, ciudad, provincia, país. |
| Geolocalización | `GeoLocation` | Coordenadas geográficas (latitud, longitud) de una sede. |
| Slug | `Slug` | Identificador URL-friendly de un establecimiento, normalizado y único. |
| Zona horaria | `TimeZoneId` | Identificador IANA de la zona horaria de una sede. |

## Tipos de necesidades alimentarias

| Tipo | Nombre técnico | Ejemplos |
|------|---------------|----------|
| Restricción médica | `MedicalRestriction` | Celiaquía, fenilcetonuria |
| Alergia | `Allergy` | Alergia a frutos secos, a mariscos, a huevo |
| Intolerancia | `Intolerance` | Intolerancia a la lactosa, a la fructosa |
| Estilo de vida | `Lifestyle` | Vegano, vegetariano, crudivegano |
| Religioso | `Religious` | Halal, kosher |
| Preferencia | `Preference` | Sin azúcar añadida, bajo en sodio, orgánico |

## Niveles de adecuación alimentaria

| Nivel | Nombre técnico | Descripción |
|-------|---------------|-------------|
| Opción disponible | `OptionAvailable` | La sede tiene al menos una opción que cumple con la necesidad. |
| Varias opciones | `MultipleOptions` | La sede ofrece varias alternativas para la necesidad. |
| Preparación separada | `SeparatePreparation` | La sede prepara los alimentos de forma separada para evitar contaminación cruzada. |
| Especializado | `Specialized` | La sede está orientada o especializada en atender esta necesidad. |
| Completamente apto | `FullyCompliant` | Todo el menú de la sede cumple con la necesidad (ej. restaurante 100% vegano). |
