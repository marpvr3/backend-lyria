# Reglas de dominio — Lyria

Lyria es una plataforma gastronómica que conecta usuarios con establecimientos de alimentación, permite gestionar sedes, horarios, adecuación alimentaria, reseñas y favoritos.

Este documento clasifica las reglas de negocio del sistema según su naturaleza arquitectónica. Cada regla se asigna a un módulo del dominio y a una de las siguientes categorías:

| Clasificación | Descripción |
|---|---|
| **Invariante de dominio** | Condición que siempre debe ser verdadera en el modelo; el dominio la hace cumplir sin excepción. |
| **Regla de aplicación** | Lógica orquestada en la capa de aplicación; puede implicar servicios, repositorios o contexto externo. |
| **Política de autorización** | Determina quién puede ejecutar una acción; se valida en backend antes de cualquier operación. |
| **Regla de persistencia** | Dicta cómo o qué se almacena (o no) en la base de datos. |
| **Regla de presentación** | Controla qué información es visible para qué audiencia; no altera el estado. |
| **Política operativa** | Decisión de negocio o proceso que rige el comportamiento del sistema en operación. |

---

## Resumen de clasificaciones

| Clasificación | Cantidad |
|---|---|
| Invariante de dominio | 28 |
| Regla de aplicación | 8 |
| Política de autorización | 14 |
| Regla de persistencia | 4 |
| Regla de presentación | 9 |
| Política operativa | 7 |
| **Total** | **70** |

---

## 1. Establecimientos y sedes

| # | Regla | Clasificación | Módulo |
|---|---|---|---|
| 1 | Un establecimiento debe tener un nombre válido (no nulo, no vacío, longitud máxima definida). | Invariante de dominio | `Establishment` |
| 2 | El slug debe estar normalizado: minúsculas, sin espacios, sin caracteres especiales no ASCII, único en el sistema. | Invariante de dominio | `Establishment` |
| 3 | Un establecimiento puede estar asociado a varias categorías gastronómicas. | Invariante de dominio | `Establishment` |
| 4 | Solo puede existir una categoría principal por establecimiento. | Invariante de dominio | `Establishment` |
| 5 | Una sede pertenece a un único establecimiento; la relación no puede cambiarse una vez creada. | Invariante de dominio | `Branch` |
| 6 | Una sede necesita ubicación geográfica válida (coordenadas o dirección verificable) para poder publicarse. | Regla de aplicación | `Branch` |
| 7 | Una sede no publicada no aparece en resultados de búsqueda pública ni en listados abiertos. | Regla de presentación | `Branch` |
| 8 | Una sede suspendida no aparece públicamente, aunque esté marcada como publicada. | Regla de presentación | `Branch` |
| 9 | La distancia entre usuario y sede se calcula en tiempo de consulta; no se persiste como atributo fijo. | Regla de persistencia | `Branch` |
| 10 | La sede debe tener una zona horaria válida (identificador IANA) antes de poder definir horarios. | Invariante de dominio | `Branch` |
| 11 | El nombre de una sede debe ser único dentro del mismo establecimiento. | Invariante de dominio | `Branch` |

---

## 2. Adecuación alimentaria

| # | Regla | Clasificación | Módulo |
|---|---|---|---|
| 12 | Una necesidad alimentaria no puede representarse únicamente como texto libre; debe estar asociada a un concepto estructurado del catálogo. | Invariante de dominio | `FoodAdequacy` |
| 13 | La adecuación debe indicar nivel (total, parcial, no adecuado, desconocido). | Invariante de dominio | `FoodAdequacy` |
| 14 | La adecuación debe indicar fuente (declarada por el establecimiento, verificada por tercero, inferida). | Invariante de dominio | `FoodAdequacy` |
| 15 | La información declarada debe distinguirse visualmente de la información verificada al mostrarse al usuario. | Regla de presentación | `FoodAdequacy` |
| 16 | Una certificación puede tener vigencia; expirada la fecha, deja de considerarse activa. | Regla de aplicación | `Certification` |
| 17 | Una verificación expirada no debe mostrarse como vigente aunque siga almacenada en la base de datos. | Regla de presentación | `Certification` |
| 18 | Una afirmación médica (ej. "apto para celíacos certificados") no puede presentarse como segura sin evidencia estructurada y vigente. | Política operativa | `FoodAdequacy` |
| 19 | La adecuación alimentaria puede variar entre sedes del mismo establecimiento; cada sede gestiona la propia. | Invariante de dominio | `FoodAdequacy` |
| 20 | Una necesidad alimentaria del catálogo no puede eliminarse si está referenciada en al menos una adecuación activa. | Regla de persistencia | `FoodAdequacy` |

---

## 3. Horarios

| # | Regla | Clasificación | Módulo |
|---|---|---|---|
| 21 | Una sede puede tener varios períodos de atención por día de la semana. | Invariante de dominio | `Schedule` |
| 22 | Los períodos de un mismo día no pueden superponerse entre sí. | Invariante de dominio | `Schedule` |
| 23 | Un período puede cruzar medianoche (hora de inicio mayor a hora de cierre indica cruce de día). | Invariante de dominio | `Schedule` |
| 24 | La ausencia de períodos activos en el día actual implica que la sede está cerrada en ese momento. | Regla de presentación | `Schedule` |
| 25 | Los horarios se interpretan según la zona horaria de la sede, no la del servidor ni la del usuario. | Regla de aplicación | `Schedule` |
| 26 | Un período eliminado lógicamente no debe incluirse en el cálculo del estado de apertura. | Regla de aplicación | `Schedule` |
| 27 | No pueden coexistir dos versiones activas del mismo horario semanal para una sede. | Invariante de dominio | `Schedule` |

---

## 4. Reseñas

| # | Regla | Clasificación | Módulo |
|---|---|---|---|
| 28 | El rating debe ser un entero entre 1 y 5, inclusive. | Invariante de dominio | `Review` |
| 29 | Un usuario solo puede tener una reseña activa por sede; intentar crear una segunda debe rechazarse. | Invariante de dominio | `Review` |
| 30 | El usuario solo puede editar el contenido de su propia reseña. | Política de autorización | `Review` |
| 31 | El usuario solo puede eliminar lógicamente su propia reseña; la eliminación física no está disponible para el usuario. | Política de autorización | `Review` |
| 32 | Solo las reseñas en estado publicado afectan el cálculo del promedio de la sede. | Regla de aplicación | `Review` |
| 33 | El administrador no puede reescribir el contenido del comentario escrito por el usuario. | Política de autorización | `Review` |
| 34 | El moderador puede ocultar o rechazar una reseña, pero no puede cambiar su autoría ni su contenido original. | Política de autorización | `Review` |
| 35 | Una reseña reportada pasa a revisión; no se elimina automáticamente. | Política operativa | `Review` |
| 36 | El promedio de ratings de una sede es un valor calculado; no puede establecerse manualmente. | Invariante de dominio | `Review` |
| 37 | El contador de reseñas de una sede es un valor calculado; no puede establecerse manualmente. | Invariante de dominio | `Review` |
| 38 | Una reseña eliminada lógicamente no debe incluirse en el cálculo del promedio. | Regla de aplicación | `Review` |

---

## 5. Favoritos

| # | Regla | Clasificación | Módulo |
|---|---|---|---|
| 39 | No pueden existir favoritos duplicados: un mismo usuario no puede agregar la misma sede dos veces. | Invariante de dominio | `Favorite` |
| 40 | Solo usuarios autenticados pueden gestionar (agregar, eliminar, consultar) sus favoritos. | Política de autorización | `Favorite` |
| 41 | Un favorito siempre pertenece a exactamente un usuario y a exactamente una sede. | Invariante de dominio | `Favorite` |
| 42 | Eliminar una sede no debe eliminar físicamente los favoritos históricos; deben marcarse como inactivos o desvinculados. | Regla de persistencia | `Favorite` |

---

## 6. Seguridad y autorización

| # | Regla | Clasificación | Módulo |
|---|---|---|---|
| 43 | Los roles no deben codificarse exclusivamente en el frontend; el control de acceso vive en el backend. | Política operativa | `Authorization` |
| 44 | Los permisos deben validarse en el backend antes de ejecutar cualquier operación de escritura o lectura sensible. | Política de autorización | `Authorization` |
| 45 | Las asignaciones de rol con alcance (por establecimiento o sede) deben respetar su ámbito; un responsable de una sede no tiene permisos sobre otras. | Política de autorización | `Authorization` |
| 46 | Un responsable no puede administrar establecimientos que no le han sido asignados explícitamente. | Política de autorización | `Authorization` |
| 47 | Un responsable no puede aprobar su propio contenido (reseñas, verificaciones, publicaciones). | Política de autorización / Invariante de dominio | `Authorization` |
| 48 | Los permisos globales deben distinguirse de los permisos por establecimiento y por sede; no son intercambiables. | Política de autorización | `Authorization` |
| 49 | No pueden existir asignaciones de rol duplicadas: un mismo usuario no puede tener el mismo rol en el mismo ámbito dos veces. | Invariante de dominio | `Authorization` |
| 50 | El otorgante de una asignación de rol debe quedar registrado en el sistema de auditoría. | Regla de persistencia | `Authorization` |

---

## 7. Publicación y moderación

| # | Regla | Clasificación | Módulo |
|---|---|---|---|
| 51 | Solo el contenido en estado publicado aparece en las interfaces públicas. | Regla de presentación | `Publication` |
| 52 | Solo el contenido aprobado aparece cuando el flujo de moderación está activo para ese tipo de contenido. | Regla de aplicación | `Publication` |
| 53 | La verificación de información (ej. adecuación alimentaria verificada) no equivale a publicación; son estados independientes. | Invariante de dominio | `Publication` |
| 54 | La publicación de contenido no equivale a certificación; certificar requiere un proceso adicional explícito. | Política operativa | `Publication` |
| 55 | Las decisiones de moderación (aprobación, rechazo, ocultación) deben quedar registradas en el log de auditoría. | Regla de persistencia | `Moderation` |
| 56 | La eliminación física de información auditada requiere una política explícita aprobada; no puede hacerse arbitrariamente. | Política operativa | `Moderation` |
| 57 | Un contenido rechazado puede ser corregido y enviado nuevamente a revisión; el historial de intentos se conserva. | Política operativa | `Moderation` |

---

## 8. Imágenes

| # | Regla | Clasificación | Módulo |
|---|---|---|---|
| 58 | Una sede puede tener como máximo una imagen principal activa en un momento dado. | Invariante de dominio | `Image` |
| 59 | El orden de visualización de imágenes debe ser único dentro de la colección de una sede (no pueden dos imágenes compartir el mismo índice de orden). | Invariante de dominio | `Image` |
| 60 | Solo las imágenes en estado publicado se muestran en la vista pública de la sede. | Regla de presentación | `Image` |
| 61 | Los binarios de imágenes no se almacenan en la base de datos relacional; se persiste únicamente la URL o referencia al objeto en almacenamiento externo. | Regla de persistencia | `Image` |
| 62 | Una imagen eliminada lógicamente no debe mostrarse ni contarse en la colección activa. | Regla de presentación | `Image` |
| 63 | El formato y el tamaño máximo de una imagen deben validarse antes de aceptar la carga. | Regla de aplicación | `Image` |

---

## 9. Cuentas de usuario

| # | Regla | Clasificación | Módulo |
|---|---|---|---|
| 64 | El correo electrónico de un usuario debe ser único en el sistema. | Invariante de dominio | `UserAccount` |
| 65 | La contraseña nunca se almacena en texto plano; siempre se persiste su hash con sal. | Regla de persistencia | `UserAccount` |
| 66 | Una cuenta puede estar en los estados: activa, suspendida, pendiente de verificación o eliminada. | Invariante de dominio | `UserAccount` |
| 67 | Una cuenta suspendida no puede autenticarse ni realizar operaciones de escritura. | Regla de aplicación | `UserAccount` |
| 68 | Una cuenta pendiente de verificación de correo tiene acceso limitado hasta completar la verificación. | Regla de aplicación | `UserAccount` |
| 69 | La eliminación de una cuenta es lógica; los datos necesarios para auditoría se conservan según la política de retención. | Política operativa | `UserAccount` |
| 70 | El correo electrónico no puede modificarse sin pasar por un flujo de verificación del nuevo correo. | Regla de aplicación | `UserAccount` |

---

## Notas

- Las reglas marcadas como **Invariante de dominio** deben hacerse cumplir en las entidades o value objects del proyecto `Lyria.Domain`, sin depender de servicios externos.
- Las reglas de **autorización** se implementan en el pipeline de la capa `Lyria.Api` (filtros, atributos, policies de ASP.NET Core) y se verifican nuevamente en los casos de uso de `Lyria.Application` cuando el contexto lo requiere.
- Las reglas de **persistencia** guían las decisiones de mapeo en `Lyria.Infrastructure` (EF Core, configuraciones de `IEntityTypeConfiguration`).
- Las reglas de **presentación** se aplican en las consultas de la capa de aplicación antes de devolver datos al controlador; nunca se filtran exclusivamente en el frontend.
- Las **políticas operativas** deben reflejarse en procedimientos, documentación de soporte y, cuando sea posible, en restricciones técnicas del sistema.
