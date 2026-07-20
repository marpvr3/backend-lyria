# Casos de uso — Lyria

Lyria es una plataforma gastronómica que conecta usuarios con establecimientos de alimentación. Este documento describe todos los casos de uso del sistema, agrupados por módulo, con referencia a las reglas de dominio definidas en `domain-rules.md`.

---

## Tabla resumen por módulo

| Módulo | Total | Commands | Queries | MVP esencial | MVP secundario |
|---|---|---|---|---|---|
| Search & Discovery | 9 | 0 | 9 | 9 | 0 |
| User Profiles | 3 | 2 | 1 | 3 | 0 |
| Favorites | 3 | 2 | 1 | 3 | 0 |
| Reviews | 5 | 3 | 1 | 4 | 0 |
| Moderation (usuario) | 2 | 2 | 0 | 1 | 1 |
| Establishments | 8 | 6 | 0 | 4 | 4 |
| Media | 4 | 4 | 0 | 4 | 0 |
| Moderation (moderador) | 13 | 9 | 4 | 0 | 13 |
| Administration | 12 | 10 | 0 | 6 | 6 |
| Identity & Access | 6 | 6 | 0 | 6 | 0 |
| **Total** | **65** | **44** | **21** | **40** | **24** |

> **Nota:** Los casos de uso 36–48 (Moderation) corresponden al actor Moderator y se contabilizan en el módulo Moderation (moderador). Los casos de uso 20–21 (ReportReview, ReportBranchInformation) se contabilizan en Moderation (usuario).

---

## Módulo: Search & Discovery

Casos de uso públicos disponibles para cualquier visitante sin autenticación.

---

### UC-01 — SearchEstablishments

| Campo | Valor |
|---|---|
| **Nombre funcional** | Buscar establecimientos |
| **Actor** | Visitor |
| **Objetivo** | Permitir que el visitante busque establecimientos gastronómicos aplicando filtros de texto, categoría y necesidades alimentarias. |
| **Módulo** | Search & Discovery |
| **Tipo** | Query |
| **Prioridad** | MVP esencial |

**Precondiciones**
- Ninguna. El acceso es público.

**Flujo principal**
1. El visitante ingresa términos de búsqueda y/o selecciona filtros (categoría, necesidad alimentaria).
2. El sistema valida los parámetros de paginación y filtros.
3. El sistema consulta únicamente establecimientos con al menos una sede publicada y activa.
4. El sistema devuelve una lista paginada con nombre, categoría principal, resumen de puntuación y cantidad de sedes.

**Reglas aplicables**
- Regla 7: una sede no publicada no aparece en resultados públicos.
- Regla 8: una sede suspendida no aparece públicamente.
- Regla 51: solo el contenido en estado publicado aparece en interfaces públicas.

**Resultado esperado**
Lista paginada de establecimientos que cumplen los criterios de búsqueda.

**Errores esperados**
- Parámetros de paginación inválidos (página o tamaño fuera de rango).
- Filtros con valores no reconocidos en el catálogo.

---

### UC-02 — SearchBranches

| Campo | Valor |
|---|---|
| **Nombre funcional** | Buscar sedes |
| **Actor** | Visitor |
| **Objetivo** | Permitir que el visitante busque sedes por nombre, ubicación, categoría, servicios o necesidades alimentarias. |
| **Módulo** | Search & Discovery |
| **Tipo** | Query |
| **Prioridad** | MVP esencial |

**Precondiciones**
- Ninguna. El acceso es público.

**Flujo principal**
1. El visitante ingresa criterios de búsqueda (texto, coordenadas de referencia, radio, filtros de servicio o alimentarios).
2. El sistema valida los parámetros.
3. El sistema consulta sedes publicadas y activas; excluye sedes suspendidas o no publicadas.
4. Si se proporcionan coordenadas, el sistema calcula distancia en tiempo de consulta sin persistirla.
5. El sistema devuelve la lista paginada, opcionalmente ordenada por distancia o puntuación.

**Reglas aplicables**
- Regla 7: sedes no publicadas excluidas de resultados públicos.
- Regla 8: sedes suspendidas excluidas de resultados públicos.
- Regla 9: la distancia se calcula en consulta; no se persiste.
- Regla 51: solo contenido publicado es visible públicamente.

**Resultado esperado**
Lista paginada de sedes que cumplen los criterios, con nombre, dirección, distancia (si aplica) y puntuación.

**Errores esperados**
- Coordenadas inválidas o fuera de rango geográfico.
- Radio de búsqueda negativo o excesivo.
- Filtros con valores no reconocidos.

---

### UC-03 — GetEstablishmentDetails

| Campo | Valor |
|---|---|
| **Nombre funcional** | Consultar detalle de establecimiento |
| **Actor** | Visitor |
| **Objetivo** | Mostrar la información completa y pública de un establecimiento: nombre, descripción, categorías, slug y listado de sedes activas. |
| **Módulo** | Search & Discovery |
| **Tipo** | Query |
| **Prioridad** | MVP esencial |

**Precondiciones**
- El establecimiento existe y tiene estado publicado.

**Flujo principal**
1. El visitante accede al detalle de un establecimiento mediante su identificador o slug.
2. El sistema verifica que el establecimiento esté publicado.
3. El sistema devuelve nombre, descripción, categorías (con indicación de cuál es la principal) y lista de sedes publicadas.

**Reglas aplicables**
- Regla 1: el nombre del establecimiento es válido y no nulo.
- Regla 2: el slug está normalizado y es único.
- Regla 3 y 4: se muestran todas las categorías; se identifica la principal.
- Regla 7 y 8: solo sedes publicadas y no suspendidas se incluyen en el listado.
- Regla 51: solo contenido publicado es visible.

**Resultado esperado**
Detalle completo del establecimiento con sus categorías y sedes activas.

**Errores esperados**
- Establecimiento no encontrado (identificador o slug inexistente).
- Establecimiento no publicado al intentar acceder directamente por identificador.

---

### UC-04 — GetBranchDetails

| Campo | Valor |
|---|---|
| **Nombre funcional** | Consultar detalle de sede |
| **Actor** | Visitor |
| **Objetivo** | Mostrar la información completa y pública de una sede: nombre, dirección, contacto, servicios, adecuación alimentaria publicada e imágenes activas. |
| **Módulo** | Search & Discovery |
| **Tipo** | Query |
| **Prioridad** | MVP esencial |

**Precondiciones**
- La sede existe, está publicada y no está suspendida.

**Flujo principal**
1. El visitante accede al detalle de una sede por su identificador.
2. El sistema verifica que la sede esté publicada y activa.
3. El sistema devuelve información de la sede, imágenes publicadas (en orden), resumen de adecuación alimentaria, resumen de puntuación y servicios.
4. La adecuación alimentaria diferencia visualmente la información declarada de la verificada.

**Reglas aplicables**
- Regla 5: la sede pertenece a un único establecimiento.
- Regla 7 y 8: sede publicada y no suspendida.
- Regla 15: información declarada se distingue de información verificada.
- Regla 17: certificaciones expiradas no se muestran como vigentes.
- Regla 51: solo contenido publicado visible.
- Regla 60 y 62: solo imágenes publicadas y no eliminadas lógicamente.

**Resultado esperado**
Detalle completo de la sede con información pública, imágenes, adecuación alimentaria y puntuación.

**Errores esperados**
- Sede no encontrada.
- Sede no publicada o suspendida.

---

### UC-05 — GetBranchOpeningHours

| Campo | Valor |
|---|---|
| **Nombre funcional** | Consultar horarios de atención de una sede |
| **Actor** | Visitor |
| **Objetivo** | Mostrar los horarios de atención vigentes de una sede, indicando si está abierta o cerrada en el momento de la consulta. |
| **Módulo** | Search & Discovery |
| **Tipo** | Query |
| **Prioridad** | MVP esencial |

**Precondiciones**
- La sede existe y está publicada.
- La sede tiene una zona horaria IANA registrada.

**Flujo principal**
1. El visitante consulta los horarios de una sede.
2. El sistema recupera los períodos de atención activos del horario semanal vigente.
3. El sistema interpreta los horarios según la zona horaria de la sede.
4. El sistema calcula el estado actual (abierto/cerrado) y lo incluye en la respuesta junto con los períodos del día.

**Reglas aplicables**
- Regla 10: la sede tiene zona horaria IANA válida.
- Regla 21: puede haber varios períodos por día.
- Regla 22: los períodos de un mismo día no se superponen.
- Regla 23: un período puede cruzar medianoche.
- Regla 24: la ausencia de períodos activos implica sede cerrada.
- Regla 25: los horarios se interpretan en la zona horaria de la sede.
- Regla 26: períodos eliminados lógicamente no se incluyen en el cálculo.
- Regla 27: solo existe una versión activa del horario semanal por sede.

**Resultado esperado**
Horario semanal de la sede con estado de apertura actual calculado en tiempo real.

**Errores esperados**
- Sede no encontrada o no publicada.
- Sede sin zona horaria configurada (datos incompletos).

---

### UC-06 — GetBranchReviews

| Campo | Valor |
|---|---|
| **Nombre funcional** | Consultar reseñas de una sede |
| **Actor** | Visitor |
| **Objetivo** | Listar las reseñas publicadas de una sede, con puntuación, comentario y fecha, para ayudar al visitante a tomar decisiones. |
| **Módulo** | Search & Discovery |
| **Tipo** | Query |
| **Prioridad** | MVP esencial |

**Precondiciones**
- La sede existe y está publicada.

**Flujo principal**
1. El visitante solicita las reseñas de una sede.
2. El sistema recupera únicamente las reseñas en estado publicado.
3. El sistema excluye reseñas ocultas, en revisión o eliminadas lógicamente.
4. El sistema devuelve la lista paginada con puntuación, comentario, fecha y promedio general de la sede.

**Reglas aplicables**
- Regla 32: solo reseñas publicadas afectan el promedio.
- Regla 36 y 37: promedio y contador son valores calculados.
- Regla 38: reseñas eliminadas lógicamente no se incluyen.
- Regla 51: solo contenido publicado visible.

**Resultado esperado**
Lista paginada de reseñas publicadas con promedio general de la sede.

**Errores esperados**
- Sede no encontrada o no publicada.

---

### UC-07 — GetDietaryNeeds

| Campo | Valor |
|---|---|
| **Nombre funcional** | Consultar catálogo de necesidades alimentarias |
| **Actor** | Visitor |
| **Objetivo** | Listar las necesidades alimentarias activas del catálogo para ser usadas como filtros de búsqueda o referencia. |
| **Módulo** | Search & Discovery |
| **Tipo** | Query |
| **Prioridad** | MVP esencial |

**Precondiciones**
- Ninguna. El acceso es público.

**Flujo principal**
1. El visitante solicita el catálogo de necesidades alimentarias.
2. El sistema devuelve únicamente los ítems activos del catálogo.

**Reglas aplicables**
- Regla 12: las necesidades alimentarias son conceptos estructurados del catálogo.

**Resultado esperado**
Lista de necesidades alimentarias activas con identificador y nombre.

**Errores esperados**
- Ninguno esperado en condiciones normales.

---

### UC-08 — GetEstablishmentCategories

| Campo | Valor |
|---|---|
| **Nombre funcional** | Consultar catálogo de categorías gastronómicas |
| **Actor** | Visitor |
| **Objetivo** | Listar las categorías gastronómicas activas del sistema para ser usadas como filtros o clasificación de establecimientos. |
| **Módulo** | Search & Discovery |
| **Tipo** | Query |
| **Prioridad** | MVP esencial |

**Precondiciones**
- Ninguna. El acceso es público.

**Flujo principal**
1. El visitante solicita el catálogo de categorías.
2. El sistema devuelve únicamente las categorías activas.

**Reglas aplicables**
- Regla 3 y 4: los establecimientos se asocian a categorías; existe una sola categoría principal.

**Resultado esperado**
Lista de categorías gastronómicas activas con identificador y nombre.

**Errores esperados**
- Ninguno esperado en condiciones normales.

---

### UC-09 — GetAvailableServices

| Campo | Valor |
|---|---|
| **Nombre funcional** | Consultar catálogo de servicios disponibles |
| **Actor** | Visitor |
| **Objetivo** | Listar los servicios activos del sistema (ej. entrega a domicilio, reservas, accesibilidad) para filtrar búsquedas. |
| **Módulo** | Search & Discovery |
| **Tipo** | Query |
| **Prioridad** | MVP esencial |

**Precondiciones**
- Ninguna. El acceso es público.

**Flujo principal**
1. El visitante solicita el catálogo de servicios.
2. El sistema devuelve únicamente los servicios activos.

**Reglas aplicables**
- Ninguna específica. Los servicios son datos de catálogo gestionados por el administrador.

**Resultado esperado**
Lista de servicios activos con identificador y nombre.

**Errores esperados**
- Ninguno esperado en condiciones normales.

---

## Módulo: User Profiles

Casos de uso para que el usuario registrado consulte y actualice su perfil.

---

### UC-10 — GetMyProfile

| Campo | Valor |
|---|---|
| **Nombre funcional** | Consultar mi perfil |
| **Actor** | Registered User |
| **Objetivo** | Permitir que el usuario autenticado consulte la información de su propio perfil. |
| **Módulo** | User Profiles |
| **Tipo** | Query |
| **Prioridad** | MVP esencial |

**Precondiciones**
- El usuario está autenticado con cuenta activa.

**Flujo principal**
1. El usuario solicita su información de perfil.
2. El sistema verifica que la cuenta esté activa.
3. El sistema devuelve nombre, correo, teléfono, foto y estado de la cuenta.

**Reglas aplicables**
- Regla 44: permisos validados en backend.
- Regla 66: la cuenta debe estar activa.
- Regla 67: cuenta suspendida no puede realizar operaciones.

**Resultado esperado**
Datos del perfil del usuario autenticado.

**Errores esperados**
- Token de autenticación inválido o expirado.
- Cuenta suspendida o eliminada.

---

### UC-11 — UpdateMyProfile

| Campo | Valor |
|---|---|
| **Nombre funcional** | Actualizar mi perfil |
| **Actor** | Registered User |
| **Objetivo** | Permitir que el usuario autenticado actualice su nombre, teléfono o foto de perfil. |
| **Módulo** | User Profiles |
| **Tipo** | Command |
| **Prioridad** | MVP esencial |

**Precondiciones**
- El usuario está autenticado con cuenta activa.

**Flujo principal**
1. El usuario envía los campos que desea actualizar (nombre, teléfono, foto).
2. El sistema valida el formato de los datos ingresados.
3. El sistema actualiza la información del perfil.
4. El sistema confirma la actualización.

**Reglas aplicables**
- Regla 44: permisos validados en backend antes de la escritura.
- Regla 66 y 67: cuenta debe estar activa.
- Regla 70: el correo electrónico no puede modificarse por este caso de uso; requiere flujo de verificación independiente.

**Resultado esperado**
Perfil actualizado correctamente.

**Errores esperados**
- Formato de nombre inválido (vacío, demasiado largo).
- Formato de teléfono inválido.
- Cuenta suspendida.

---

### UC-12 — UpdateMyDietaryNeeds

| Campo | Valor |
|---|---|
| **Nombre funcional** | Actualizar mis necesidades alimentarias |
| **Actor** | Registered User |
| **Objetivo** | Permitir que el usuario configure sus necesidades alimentarias personales para personalizar la experiencia en la plataforma. |
| **Módulo** | User Profiles |
| **Tipo** | Command |
| **Prioridad** | MVP esencial |

**Precondiciones**
- El usuario está autenticado con cuenta activa.
- Las necesidades alimentarias seleccionadas existen en el catálogo activo.

**Flujo principal**
1. El usuario selecciona o deselecciona necesidades alimentarias del catálogo.
2. El sistema valida que todos los identificadores correspondan a ítems activos del catálogo.
3. El sistema actualiza las necesidades alimentarias asociadas al perfil del usuario.
4. El sistema confirma la actualización.

**Reglas aplicables**
- Regla 12: las necesidades deben estar en el catálogo estructurado; no texto libre.
- Regla 44: permisos validados en backend.

**Resultado esperado**
Necesidades alimentarias del perfil actualizadas.

**Errores esperados**
- Identificador de necesidad alimentaria no encontrado o inactivo en el catálogo.
- Cuenta suspendida.

---

## Módulo: Favorites

Casos de uso para que el usuario registrado gestione sus sedes favoritas.

---

### UC-13 — AddFavorite

| Campo | Valor |
|---|---|
| **Nombre funcional** | Agregar sede a favoritos |
| **Actor** | Registered User |
| **Objetivo** | Permitir que el usuario marque una sede como favorita para acceder fácilmente a ella en el futuro. |
| **Módulo** | Favorites |
| **Tipo** | Command |
| **Prioridad** | MVP esencial |

**Precondiciones**
- El usuario está autenticado con cuenta activa.
- La sede existe y está publicada.
- La sede no ha sido previamente marcada como favorita por el mismo usuario.

**Flujo principal**
1. El usuario solicita agregar una sede a sus favoritos.
2. El sistema verifica que el usuario no tenga ya esa sede en sus favoritos.
3. El sistema verifica que la sede exista y esté publicada.
4. El sistema crea el vínculo favorito entre el usuario y la sede.
5. El sistema confirma la operación.

**Reglas aplicables**
- Regla 39: no pueden existir favoritos duplicados.
- Regla 40: solo usuarios autenticados pueden gestionar favoritos.
- Regla 41: un favorito pertenece a exactamente un usuario y una sede.

**Resultado esperado**
Favorito creado exitosamente.

**Errores esperados**
- La sede ya está en la lista de favoritos del usuario.
- Sede no encontrada o no publicada.
- Usuario no autenticado.

---

### UC-14 — RemoveFavorite

| Campo | Valor |
|---|---|
| **Nombre funcional** | Eliminar sede de favoritos |
| **Actor** | Registered User |
| **Objetivo** | Permitir que el usuario elimine una sede de su lista de favoritos. |
| **Módulo** | Favorites |
| **Tipo** | Command |
| **Prioridad** | MVP esencial |

**Precondiciones**
- El usuario está autenticado con cuenta activa.
- El favorito existe y pertenece al usuario.

**Flujo principal**
1. El usuario solicita eliminar una sede de sus favoritos.
2. El sistema verifica que el favorito exista y pertenezca al usuario autenticado.
3. El sistema elimina el favorito (lógica o físicamente según política).
4. El sistema confirma la operación.

**Reglas aplicables**
- Regla 40: solo usuarios autenticados gestionan favoritos.
- Regla 41: el favorito debe pertenecer al usuario.
- Regla 42: eliminar una sede no elimina físicamente los favoritos; se marcan como inactivos.

**Resultado esperado**
Favorito eliminado de la lista del usuario.

**Errores esperados**
- Favorito no encontrado o no pertenece al usuario.
- Usuario no autenticado.

---

### UC-15 — GetMyFavorites

| Campo | Valor |
|---|---|
| **Nombre funcional** | Consultar mis favoritos |
| **Actor** | Registered User |
| **Objetivo** | Listar las sedes que el usuario ha marcado como favoritas, con información básica de cada una. |
| **Módulo** | Favorites |
| **Tipo** | Query |
| **Prioridad** | MVP esencial |

**Precondiciones**
- El usuario está autenticado con cuenta activa.

**Flujo principal**
1. El usuario solicita su lista de favoritos.
2. El sistema recupera los favoritos activos del usuario.
3. El sistema devuelve información básica de cada sede favorita (nombre, dirección, puntuación).
4. Las sedes desactivadas o suspendidas se incluyen con indicación de estado, sin excluirse del historial.

**Reglas aplicables**
- Regla 40: solo usuarios autenticados acceden a sus favoritos.
- Regla 41: los favoritos pertenecen al usuario.
- Regla 42: sedes eliminadas no eliminan el historial de favoritos.

**Resultado esperado**
Lista paginada de sedes favoritas del usuario.

**Errores esperados**
- Usuario no autenticado.

---

## Módulo: Reviews

Casos de uso para que el usuario registrado gestione sus reseñas sobre sedes.

---

### UC-16 — CreateReview

| Campo | Valor |
|---|---|
| **Nombre funcional** | Crear reseña |
| **Actor** | Registered User |
| **Objetivo** | Permitir que el usuario publique una reseña con puntuación y comentario sobre una sede que haya visitado. |
| **Módulo** | Reviews |
| **Tipo** | Command |
| **Prioridad** | MVP esencial |

**Precondiciones**
- El usuario está autenticado con cuenta activa.
- La sede existe y está publicada.
- El usuario no tiene una reseña activa para esa sede.

**Flujo principal**
1. El usuario envía una puntuación (1-5) y un comentario sobre la sede.
2. El sistema valida que la puntuación esté en el rango permitido.
3. El sistema verifica que el usuario no tenga reseña activa para esa sede.
4. El sistema crea la reseña y actualiza el promedio calculado de la sede.
5. El sistema confirma la publicación.

**Reglas aplicables**
- Regla 28: el rating debe ser entero entre 1 y 5.
- Regla 29: solo una reseña activa por usuario por sede.
- Regla 32: solo reseñas publicadas afectan el promedio.
- Regla 36 y 37: promedio y contador son calculados, no editables manualmente.

**Resultado esperado**
Reseña creada y publicada; promedio de la sede recalculado.

**Errores esperados**
- El usuario ya tiene una reseña activa para esa sede.
- Puntuación fuera de rango (menor que 1 o mayor que 5).
- Comentario vacío o demasiado largo.
- Sede no encontrada o no publicada.

---

### UC-17 — UpdateMyReview

| Campo | Valor |
|---|---|
| **Nombre funcional** | Editar mi reseña |
| **Actor** | Registered User |
| **Objetivo** | Permitir que el usuario modifique la puntuación o el comentario de una reseña propia activa. |
| **Módulo** | Reviews |
| **Tipo** | Command |
| **Prioridad** | MVP esencial |

**Precondiciones**
- El usuario está autenticado con cuenta activa.
- La reseña existe, está activa y pertenece al usuario.

**Flujo principal**
1. El usuario envía los campos actualizados (puntuación, comentario o ambos).
2. El sistema verifica que la reseña pertenezca al usuario autenticado.
3. El sistema valida los nuevos valores.
4. El sistema actualiza la reseña y recalcula el promedio de la sede si cambió la puntuación.
5. El sistema confirma la actualización.

**Reglas aplicables**
- Regla 28: el nuevo rating debe ser entero entre 1 y 5.
- Regla 30: solo el propio usuario puede editar su reseña.
- Regla 33: el administrador no puede reescribir el contenido del usuario.
- Regla 36 y 37: promedio y contador son calculados.

**Resultado esperado**
Reseña actualizada; promedio de la sede recalculado si corresponde.

**Errores esperados**
- Reseña no encontrada o no pertenece al usuario.
- Nueva puntuación fuera de rango.
- Reseña eliminada lógicamente (no editable).

---

### UC-18 — DeleteMyReview

| Campo | Valor |
|---|---|
| **Nombre funcional** | Eliminar mi reseña |
| **Actor** | Registered User |
| **Objetivo** | Permitir que el usuario elimine lógicamente su propia reseña, retirándola de la vista pública y del cálculo del promedio. |
| **Módulo** | Reviews |
| **Tipo** | Command |
| **Prioridad** | MVP esencial |

**Precondiciones**
- El usuario está autenticado con cuenta activa.
- La reseña existe, está activa y pertenece al usuario.

**Flujo principal**
1. El usuario solicita eliminar su reseña.
2. El sistema verifica que la reseña pertenezca al usuario autenticado.
3. El sistema marca la reseña como eliminada lógicamente.
4. El sistema recalcula el promedio de la sede excluyendo la reseña eliminada.
5. El sistema confirma la eliminación.

**Reglas aplicables**
- Regla 31: solo eliminación lógica disponible para el usuario; no eliminación física.
- Regla 38: reseñas eliminadas lógicamente no se incluyen en el cálculo del promedio.
- Regla 36 y 37: promedio y contador son valores calculados.

**Resultado esperado**
Reseña marcada como eliminada; promedio de la sede recalculado.

**Errores esperados**
- Reseña no encontrada o no pertenece al usuario.
- Reseña ya eliminada lógicamente.

---

### UC-19 — GetMyReviewForBranch

| Campo | Valor |
|---|---|
| **Nombre funcional** | Consultar mi reseña sobre una sede |
| **Actor** | Registered User |
| **Objetivo** | Permitir que el usuario consulte la reseña que ha escrito sobre una sede específica, si existe. |
| **Módulo** | Reviews |
| **Tipo** | Query |
| **Prioridad** | MVP esencial |

**Precondiciones**
- El usuario está autenticado con cuenta activa.
- La sede existe.

**Flujo principal**
1. El usuario solicita su reseña para una sede específica.
2. El sistema busca la reseña activa del usuario para esa sede.
3. El sistema devuelve la reseña si existe, o indica que no hay reseña registrada.

**Reglas aplicables**
- Regla 29: existe como máximo una reseña activa por usuario por sede.
- Regla 30: el usuario accede únicamente a su propia reseña por este caso de uso.

**Resultado esperado**
Reseña del usuario para la sede solicitada, o indicación de que no existe.

**Errores esperados**
- Sede no encontrada.
- Usuario no autenticado.

---

## Módulo: Moderation (acciones del usuario)

Casos de uso mediante los cuales el usuario registrado puede reportar contenido inapropiado.

---

### UC-20 — ReportReview

| Campo | Valor |
|---|---|
| **Nombre funcional** | Reportar una reseña |
| **Actor** | Registered User |
| **Objetivo** | Permitir que el usuario reporte una reseña de otro usuario por contenido inapropiado, para que sea revisada por un moderador. |
| **Módulo** | Moderation |
| **Tipo** | Command |
| **Prioridad** | MVP esencial |

**Precondiciones**
- El usuario está autenticado con cuenta activa.
- La reseña existe y está publicada.
- El usuario no está reportando su propia reseña.

**Flujo principal**
1. El usuario selecciona la reseña y el motivo del reporte.
2. El sistema registra el reporte vinculado al usuario y a la reseña.
3. La reseña pasa a estado de revisión pendiente sin eliminarse automáticamente.
4. El sistema confirma el envío del reporte.

**Reglas aplicables**
- Regla 35: una reseña reportada pasa a revisión; no se elimina automáticamente.
- Regla 44: permisos validados en backend.
- Regla 55: las decisiones de moderación quedan registradas en auditoría.

**Resultado esperado**
Reporte registrado; la reseña queda en cola de moderación.

**Errores esperados**
- Reseña no encontrada o no publicada.
- El usuario intenta reportar su propia reseña.
- Motivo de reporte no válido o ausente.

---

### UC-21 — ReportBranchInformation

| Campo | Valor |
|---|---|
| **Nombre funcional** | Reportar información incorrecta de una sede |
| **Actor** | Registered User |
| **Objetivo** | Permitir que el usuario reporte información potencialmente incorrecta o desactualizada de una sede para que sea revisada. |
| **Módulo** | Moderation |
| **Tipo** | Command |
| **Prioridad** | MVP secundario |

**Precondiciones**
- El usuario está autenticado con cuenta activa.
- La sede existe y está publicada.

**Flujo principal**
1. El usuario selecciona la sede, el campo o sección con información incorrecta y describe el problema.
2. El sistema registra el reporte vinculado al usuario, la sede y el campo afectado.
3. El reporte queda disponible para revisión por moderadores o administradores.
4. El sistema confirma el envío del reporte.

**Reglas aplicables**
- Regla 44: permisos validados en backend.
- Regla 55: las acciones de moderación quedan en auditoría.

**Resultado esperado**
Reporte de información incorrecta registrado exitosamente.

**Errores esperados**
- Sede no encontrada o no publicada.
- Descripción del problema vacía o insuficiente.

---

## Módulo: Establishments

Casos de uso para la gestión de establecimientos y sedes por parte de administradores y responsables.

---

### UC-22 — CreateEstablishment

| Campo | Valor |
|---|---|
| **Nombre funcional** | Crear establecimiento |
| **Actor** | Administrator |
| **Objetivo** | Registrar un nuevo establecimiento gastronómico en la plataforma con su información básica y categorías. |
| **Módulo** | Establishments |
| **Tipo** | Command |
| **Prioridad** | MVP esencial |

**Precondiciones**
- El actor está autenticado con rol de administrador.
- El nombre del establecimiento es válido y el slug generado es único.

**Flujo principal**
1. El administrador envía los datos del nuevo establecimiento (nombre, descripción, categorías).
2. El sistema genera y valida el slug normalizado.
3. El sistema valida que exista exactamente una categoría principal entre las seleccionadas.
4. El sistema crea el establecimiento en estado no publicado (pendiente de completar información).
5. El sistema confirma la creación.

**Reglas aplicables**
- Regla 1: nombre válido y no nulo.
- Regla 2: slug normalizado y único en el sistema.
- Regla 3 y 4: puede tener varias categorías; exactamente una es la principal.
- Regla 44: permisos validados en backend.
- Regla 46: restricción de alcance de administrador.

**Resultado esperado**
Establecimiento creado en estado borrador o no publicado.

**Errores esperados**
- Nombre duplicado que genera slug ya existente.
- Más de una categoría principal seleccionada.
- Categorías no encontradas en el catálogo.
- Actor sin permisos suficientes.

---

### UC-23 — UpdateEstablishment

| Campo | Valor |
|---|---|
| **Nombre funcional** | Actualizar información del establecimiento |
| **Actor** | Establishment Manager / Administrator |
| **Objetivo** | Modificar los datos generales de un establecimiento (nombre, descripción, categorías) dentro del alcance autorizado. |
| **Módulo** | Establishments |
| **Tipo** | Command |
| **Prioridad** | MVP esencial |

**Precondiciones**
- El actor está autenticado con rol de manager (dentro de su alcance) o administrador.
- El establecimiento existe.

**Flujo principal**
1. El actor envía los campos a actualizar.
2. El sistema verifica que el actor tenga permisos sobre ese establecimiento.
3. El sistema valida los datos (nombre, categorías, slug si cambia).
4. El sistema actualiza la información del establecimiento.
5. El sistema confirma la actualización.

**Reglas aplicables**
- Regla 1 y 2: nombre y slug válidos.
- Regla 3 y 4: categorías válidas; una sola principal.
- Regla 44, 45 y 46: permisos y alcance validados.
- Regla 47: el responsable no puede aprobar su propio contenido.

**Resultado esperado**
Información del establecimiento actualizada.

**Errores esperados**
- Actor sin permisos sobre ese establecimiento.
- Nombre que genera slug duplicado.
- Configuración de categorías inválida.

---

### UC-24 — SubmitEstablishmentForReview

| Campo | Valor |
|---|---|
| **Nombre funcional** | Enviar establecimiento a revisión |
| **Actor** | Establishment Manager |
| **Objetivo** | Solicitar la revisión y aprobación de un establecimiento para que pueda publicarse en la plataforma. |
| **Módulo** | Establishments |
| **Tipo** | Command |
| **Prioridad** | MVP secundario |

**Precondiciones**
- El actor está autenticado con rol de manager para ese establecimiento.
- El establecimiento cumple los requisitos mínimos de información.
- El establecimiento no está ya en revisión o aprobado.

**Flujo principal**
1. El responsable solicita enviar el establecimiento a revisión.
2. El sistema verifica que el actor tenga permisos de alcance sobre ese establecimiento.
3. El sistema verifica que la información mínima requerida esté completa.
4. El sistema cambia el estado del establecimiento a "en revisión".
5. El sistema notifica a los moderadores.

**Reglas aplicables**
- Regla 44, 45 y 46: permisos y alcance.
- Regla 47: el responsable no puede aprobar su propio contenido.
- Regla 52: el flujo de moderación debe estar activo.
- Regla 57: el historial de intentos de revisión se conserva.

**Resultado esperado**
Establecimiento en cola de revisión por moderadores.

**Errores esperados**
- Actor sin permisos sobre ese establecimiento.
- Información mínima incompleta.
- El establecimiento ya está en revisión.

---

### UC-25 — CreateBranch

| Campo | Valor |
|---|---|
| **Nombre funcional** | Crear sede |
| **Actor** | Establishment Manager / Administrator |
| **Objetivo** | Registrar una nueva sede vinculada a un establecimiento existente. |
| **Módulo** | Establishments |
| **Tipo** | Command |
| **Prioridad** | MVP esencial |

**Precondiciones**
- El actor está autenticado con permisos sobre el establecimiento.
- El establecimiento al que pertenecerá la sede existe.
- El nombre de la sede es único dentro del establecimiento.

**Flujo principal**
1. El actor envía los datos de la nueva sede (nombre, ubicación, zona horaria).
2. El sistema verifica que el actor tenga permisos sobre el establecimiento.
3. El sistema valida que el nombre sea único dentro del establecimiento.
4. El sistema valida la zona horaria IANA proporcionada.
5. El sistema crea la sede en estado no publicado.

**Reglas aplicables**
- Regla 5: la sede pertenece a un único establecimiento (relación inmutable).
- Regla 10: zona horaria IANA requerida.
- Regla 11: nombre único dentro del mismo establecimiento.
- Regla 44, 45 y 46: permisos y alcance.

**Resultado esperado**
Sede creada en estado no publicado vinculada al establecimiento.

**Errores esperados**
- Nombre de sede duplicado dentro del mismo establecimiento.
- Zona horaria IANA inválida o no reconocida.
- Establecimiento no encontrado.
- Actor sin permisos.

---

### UC-26 — UpdateBranch

| Campo | Valor |
|---|---|
| **Nombre funcional** | Actualizar información general de la sede |
| **Actor** | Establishment Manager / Administrator |
| **Objetivo** | Modificar los datos generales de una sede (nombre, descripción) dentro del alcance autorizado. |
| **Módulo** | Establishments |
| **Tipo** | Command |
| **Prioridad** | MVP esencial |

**Precondiciones**
- El actor está autenticado con permisos sobre la sede o el establecimiento al que pertenece.
- La sede existe.

**Flujo principal**
1. El actor envía los campos a actualizar.
2. El sistema verifica los permisos de alcance del actor.
3. El sistema valida que el nuevo nombre sea único dentro del establecimiento.
4. El sistema actualiza la información de la sede.
5. El sistema confirma la actualización.

**Reglas aplicables**
- Regla 5: la pertenencia al establecimiento no puede cambiarse.
- Regla 11: nombre único dentro del establecimiento.
- Regla 44, 45 y 46: permisos y alcance.

**Resultado esperado**
Información general de la sede actualizada.

**Errores esperados**
- Nombre duplicado dentro del mismo establecimiento.
- Actor sin permisos sobre esa sede.

---

### UC-27 — UpdateBranchContactInformation

| Campo | Valor |
|---|---|
| **Nombre funcional** | Actualizar información de contacto de la sede |
| **Actor** | Establishment Manager / Administrator |
| **Objetivo** | Actualizar los datos de contacto de una sede: dirección, teléfono, correo de contacto, sitio web. |
| **Módulo** | Establishments |
| **Tipo** | Command |
| **Prioridad** | MVP esencial |

**Precondiciones**
- El actor está autenticado con permisos sobre la sede.
- La sede existe.

**Flujo principal**
1. El actor envía los datos de contacto actualizados.
2. El sistema verifica los permisos de alcance.
3. El sistema valida el formato de los datos (dirección, teléfono, correo).
4. El sistema actualiza los datos de contacto.
5. El sistema confirma la actualización.

**Reglas aplicables**
- Regla 6: la sede requiere ubicación válida para publicarse.
- Regla 44, 45 y 46: permisos y alcance.

**Resultado esperado**
Información de contacto de la sede actualizada.

**Errores esperados**
- Formato de teléfono o correo inválido.
- Actor sin permisos sobre esa sede.

---

### UC-28 — UpdateBranchOpeningHours

| Campo | Valor |
|---|---|
| **Nombre funcional** | Actualizar horarios de atención de la sede |
| **Actor** | Establishment Manager / Administrator |
| **Objetivo** | Definir o reemplazar los períodos de atención semanal de una sede. |
| **Módulo** | Establishments |
| **Tipo** | Command |
| **Prioridad** | MVP esencial |

**Precondiciones**
- El actor está autenticado con permisos sobre la sede.
- La sede tiene zona horaria IANA configurada.

**Flujo principal**
1. El actor envía la nueva configuración de períodos de atención por día.
2. El sistema verifica que la sede tenga zona horaria válida.
3. El sistema valida que no existan períodos superpuestos en el mismo día.
4. El sistema reemplaza el horario activo por el nuevo, desactivando el anterior.
5. El sistema confirma la actualización.

**Reglas aplicables**
- Regla 10: zona horaria IANA requerida.
- Regla 21: varios períodos por día permitidos.
- Regla 22: períodos del mismo día no pueden superponerse.
- Regla 23: un período puede cruzar medianoche.
- Regla 27: no pueden coexistir dos versiones activas del horario semanal.
- Regla 44, 45 y 46: permisos y alcance.

**Resultado esperado**
Horario semanal activo de la sede actualizado.

**Errores esperados**
- Períodos superpuestos en el mismo día.
- Sede sin zona horaria configurada.
- Actor sin permisos.

---

### UC-29 — UpdateBranchServices

| Campo | Valor |
|---|---|
| **Nombre funcional** | Actualizar servicios disponibles de la sede |
| **Actor** | Establishment Manager / Administrator |
| **Objetivo** | Configurar qué servicios del catálogo ofrece una sede (ej. reservas, delivery, accesibilidad). |
| **Módulo** | Establishments |
| **Tipo** | Command |
| **Prioridad** | MVP esencial |

**Precondiciones**
- El actor está autenticado con permisos sobre la sede.
- Los servicios seleccionados existen en el catálogo activo.

**Flujo principal**
1. El actor envía la lista de servicios que ofrece la sede.
2. El sistema verifica los permisos.
3. El sistema valida que los identificadores correspondan a servicios activos.
4. El sistema actualiza la lista de servicios de la sede.
5. El sistema confirma la actualización.

**Reglas aplicables**
- Regla 44, 45 y 46: permisos y alcance.

**Resultado esperado**
Servicios de la sede actualizados.

**Errores esperados**
- Servicio no encontrado o inactivo en el catálogo.
- Actor sin permisos.

---

### UC-30 — UpdateBranchDietarySuitability

| Campo | Valor |
|---|---|
| **Nombre funcional** | Actualizar adecuación alimentaria de la sede |
| **Actor** | Establishment Manager / Administrator |
| **Objetivo** | Declarar o actualizar la adecuación alimentaria de una sede para cada necesidad del catálogo, indicando el nivel y la fuente. |
| **Módulo** | Establishments |
| **Tipo** | Command |
| **Prioridad** | MVP secundario |

**Precondiciones**
- El actor está autenticado con permisos sobre la sede.
- Las necesidades alimentarias referenciadas existen en el catálogo activo.

**Flujo principal**
1. El actor envía la adecuación para cada necesidad alimentaria (nivel, fuente).
2. El sistema verifica los permisos de alcance.
3. El sistema valida que cada necesidad sea un concepto estructurado del catálogo.
4. El sistema valida que el nivel y la fuente sean valores definidos (total, parcial, no adecuado, desconocido; declarada, verificada, inferida).
5. El sistema actualiza la adecuación alimentaria de la sede.
6. El sistema confirma la actualización.

**Reglas aplicables**
- Regla 12: necesidades alimentarias del catálogo estructurado.
- Regla 13: nivel de adecuación obligatorio.
- Regla 14: fuente de adecuación obligatoria.
- Regla 19: la adecuación es independiente por sede.
- Regla 44, 45 y 46: permisos y alcance.

**Resultado esperado**
Adecuación alimentaria de la sede actualizada con estado declarado.

**Errores esperados**
- Necesidad alimentaria no encontrada o inactiva.
- Nivel o fuente con valores no permitidos.
- Actor sin permisos.

---

### UC-35 — SubmitBranchForReview

| Campo | Valor |
|---|---|
| **Nombre funcional** | Enviar sede a revisión |
| **Actor** | Establishment Manager |
| **Objetivo** | Solicitar la revisión y aprobación de una sede para que pueda publicarse en la plataforma. |
| **Módulo** | Establishments |
| **Tipo** | Command |
| **Prioridad** | MVP secundario |

**Precondiciones**
- El actor está autenticado con permisos sobre la sede.
- La sede cumple los requisitos mínimos (información, ubicación, zona horaria).
- La sede no está ya en revisión o aprobada.

**Flujo principal**
1. El responsable solicita enviar la sede a revisión.
2. El sistema verifica los permisos de alcance del actor.
3. El sistema verifica que la sede tenga la información mínima requerida.
4. El sistema cambia el estado de la sede a "en revisión".
5. El sistema notifica a los moderadores.

**Reglas aplicables**
- Regla 6: ubicación válida requerida para publicarse.
- Regla 10: zona horaria IANA requerida.
- Regla 44, 45 y 46: permisos y alcance.
- Regla 47: el responsable no puede aprobar su propio contenido.
- Regla 52 y 57: flujo de moderación activo; historial de revisiones conservado.

**Resultado esperado**
Sede en cola de revisión por moderadores.

**Errores esperados**
- Actor sin permisos sobre la sede.
- Información mínima incompleta (sin ubicación, sin zona horaria).
- La sede ya está en revisión.

---

## Módulo: Media

Casos de uso para la gestión de imágenes de sedes.

---

### UC-31 — AddBranchImage

| Campo | Valor |
|---|---|
| **Nombre funcional** | Agregar imagen a la sede |
| **Actor** | Establishment Manager / Administrator |
| **Objetivo** | Cargar una nueva imagen para la galería de una sede, validando formato y tamaño antes de aceptarla. |
| **Módulo** | Media |
| **Tipo** | Command |
| **Prioridad** | MVP esencial |

**Nota conceptual:** Este caso de uso opera sobre el agregado `BranchMediaGallery` de la sede. La imagen se agrega como una nueva entidad `BranchImage` dentro de la galería, y la galería garantiza la unicidad del orden de clasificación.

**Precondiciones**
- El actor está autenticado con permisos de gestión de imágenes sobre la sede.
- La sede existe y tiene una `BranchMediaGallery` asociada.

**Flujo principal**
1. El actor envía el archivo de imagen y metadatos opcionales.
2. El sistema valida el formato (tipos permitidos) y el tamaño máximo del archivo.
3. El sistema almacena el binario en el servicio de almacenamiento externo.
4. El sistema persiste únicamente la URL o referencia, no el binario.
5. El sistema agrega la nueva `BranchImage` a la `BranchMediaGallery` de la sede, asignando un `SortOrder` único.
6. El sistema confirma la carga.

**Reglas aplicables**
- Regla 59: índice de orden único en la colección.
- Regla 61: los binarios no se almacenan en la base de datos relacional.
- Regla 63: formato y tamaño validados antes de aceptar la carga.
- Regla 44, 45 y 46: permisos y alcance.

**Resultado esperado**
Imagen cargada y registrada en la galería de la sede (`BranchMediaGallery`).

**Errores esperados**
- Formato de imagen no permitido.
- Tamaño de archivo excede el límite.
- Actor sin permisos.

---

### UC-32 — SetPrimaryBranchImage

| Campo | Valor |
|---|---|
| **Nombre funcional** | Establecer imagen principal de la sede |
| **Actor** | Establishment Manager / Administrator |
| **Objetivo** | Marcar una imagen de la galería como la imagen principal de la sede, desplazando a la anterior si existía. |
| **Módulo** | Media |
| **Tipo** | Command |
| **Prioridad** | MVP esencial |

**Nota conceptual:** Este caso de uso opera sobre el agregado `BranchMediaGallery`. La operación de designar imagen principal es una responsabilidad del agregado, que garantiza que en todo momento haya como máximo una `BranchImage` con `IsPrimary = true` dentro de la galería.

**Precondiciones**
- El actor está autenticado con permisos sobre la sede.
- La imagen (`BranchImage`) existe en la galería activa de la sede (`BranchMediaGallery`) y está publicada.

**Flujo principal**
1. El actor indica la imagen que desea establecer como principal.
2. El sistema verifica los permisos.
3. El sistema verifica que la imagen pertenezca a la galería activa de la sede.
4. El agregado `BranchMediaGallery` desmarca la imagen principal anterior (si existe).
5. El agregado `BranchMediaGallery` marca la nueva imagen como principal.
6. El sistema confirma el cambio.

**Reglas aplicables**
- Regla 58: como máximo una imagen principal activa en un momento dado.
- Regla 44, 45 y 46: permisos y alcance.

**Resultado esperado**
La imagen seleccionada queda como imagen principal de la galería de la sede.

**Errores esperados**
- Imagen no encontrada o no pertenece a la galería activa.
- Actor sin permisos.

---

### UC-33 — ReorderBranchImages

| Campo | Valor |
|---|---|
| **Nombre funcional** | Reordenar imágenes de la sede |
| **Actor** | Establishment Manager / Administrator |
| **Objetivo** | Modificar el orden de visualización de las imágenes de la galería de una sede. |
| **Módulo** | Media |
| **Tipo** | Command |
| **Prioridad** | MVP esencial |

**Nota conceptual:** Este caso de uso opera sobre el agregado `BranchMediaGallery`. El reordenamiento actualiza los valores de `SortOrder` de las entidades `BranchImage` internas, y el agregado garantiza la unicidad de los nuevos órdenes.

**Precondiciones**
- El actor está autenticado con permisos sobre la sede.
- La galería de la sede (`BranchMediaGallery`) tiene al menos dos imágenes activas.

**Flujo principal**
1. El actor envía la nueva secuencia de orden de las imágenes activas.
2. El sistema verifica los permisos.
3. El sistema valida que todos los identificadores correspondan a imágenes activas de la galería de la sede.
4. El agregado `BranchMediaGallery` valida que los índices de orden sean únicos y sin repetición.
5. El agregado actualiza el `SortOrder` de cada `BranchImage`.
6. El sistema confirma el reordenamiento.

**Reglas aplicables**
- Regla 59: el índice de orden debe ser único dentro de la colección.
- Regla 44, 45 y 46: permisos y alcance.

**Resultado esperado**
Imágenes de la galería de la sede reordenadas según la nueva secuencia.

**Errores esperados**
- Identificador de imagen no encontrado o no pertenece a la galería de la sede.
- Índices de orden duplicados en la nueva secuencia.
- Actor sin permisos.

---

### UC-34 — RemoveBranchImage

| Campo | Valor |
|---|---|
| **Nombre funcional** | Eliminar imagen de la sede |
| **Actor** | Establishment Manager / Administrator |
| **Objetivo** | Eliminar lógicamente una imagen de la galería de una sede para que deje de mostrarse en la vista pública. |
| **Módulo** | Media |
| **Tipo** | Command |
| **Prioridad** | MVP esencial |

**Nota conceptual:** Este caso de uso opera sobre el agregado `BranchMediaGallery`. La eliminación lógica de una `BranchImage` es gestionada por el agregado, que también se encarga de desmarcar el indicador de imagen principal si la imagen eliminada era la principal.

**Precondiciones**
- El actor está autenticado con permisos sobre la sede.
- La imagen (`BranchImage`) existe y está activa en la galería de la sede (`BranchMediaGallery`).

**Flujo principal**
1. El actor solicita eliminar una imagen.
2. El sistema verifica los permisos.
3. El agregado `BranchMediaGallery` verifica si la imagen es la imagen principal; de ser así, la desmarca antes de eliminarla.
4. El agregado marca la `BranchImage` como eliminada lógicamente.
5. El sistema confirma la eliminación.

**Reglas aplicables**
- Regla 58: la galería puede quedar sin imagen principal si se elimina la única existente.
- Regla 62: imágenes eliminadas lógicamente no se muestran ni cuentan en la colección activa.
- Regla 44, 45 y 46: permisos y alcance.

**Resultado esperado**
Imagen marcada como eliminada; deja de aparecer en la vista pública.

**Errores esperados**
- Imagen no encontrada o ya eliminada lógicamente.
- Actor sin permisos.

---

## Módulo: Moderation (moderador)

Casos de uso para que el moderador revise, apruebe o rechace contenido enviado a revisión.

---

### UC-36 — ReviewEstablishmentSubmission

| Campo | Valor |
|---|---|
| **Nombre funcional** | Revisar solicitud de publicación de establecimiento |
| **Actor** | Moderator |
| **Objetivo** | Consultar los detalles de un establecimiento enviado a revisión para tomar una decisión de aprobación o rechazo. |
| **Módulo** | Moderation |
| **Tipo** | Query |
| **Prioridad** | MVP secundario |

**Precondiciones**
- El moderador está autenticado con rol de moderador.
- El establecimiento está en estado "en revisión".

**Flujo principal**
1. El moderador accede a la cola de establecimientos en revisión.
2. El moderador selecciona un establecimiento para revisar.
3. El sistema devuelve el detalle completo del establecimiento y el historial de revisiones previas.

**Reglas aplicables**
- Regla 44: permisos validados en backend.
- Regla 57: el historial de intentos de revisión se conserva.

**Resultado esperado**
Detalle del establecimiento disponible para que el moderador tome una decisión.

**Errores esperados**
- Establecimiento no encontrado o no en estado de revisión.
- Actor sin permisos de moderación.

---

### UC-37 — ApproveEstablishment

| Campo | Valor |
|---|---|
| **Nombre funcional** | Aprobar establecimiento |
| **Actor** | Moderator |
| **Objetivo** | Aprobar la publicación de un establecimiento que ha cumplido los criterios de revisión. |
| **Módulo** | Moderation |
| **Tipo** | Command |
| **Prioridad** | MVP secundario |

**Precondiciones**
- El moderador está autenticado con rol de moderador.
- El establecimiento está en estado "en revisión".
- El moderador no es el responsable del establecimiento (separación de funciones).

**Flujo principal**
1. El moderador aprueba el establecimiento con una nota opcional.
2. El sistema verifica que el moderador no sea el responsable del contenido.
3. El sistema cambia el estado del establecimiento a "aprobado" y lo marca como publicado.
4. El sistema registra la decisión en el log de auditoría.
5. El sistema confirma la aprobación.

**Reglas aplicables**
- Regla 44: permisos validados.
- Regla 47: el responsable no puede aprobar su propio contenido.
- Regla 51: al publicarse, el establecimiento aparece en búsquedas públicas.
- Regla 52: el flujo de moderación controla la visibilidad.
- Regla 55: la decisión queda en el log de auditoría.

**Resultado esperado**
Establecimiento aprobado y publicado en la plataforma.

**Errores esperados**
- Establecimiento no en estado de revisión.
- Actor sin permisos de moderación o es el responsable del contenido.

---

### UC-38 — RejectEstablishment

| Campo | Valor |
|---|---|
| **Nombre funcional** | Rechazar establecimiento |
| **Actor** | Moderator |
| **Objetivo** | Rechazar la publicación de un establecimiento indicando los motivos para que el responsable pueda corregirlo y enviarlo de nuevo. |
| **Módulo** | Moderation |
| **Tipo** | Command |
| **Prioridad** | MVP secundario |

**Precondiciones**
- El moderador está autenticado con rol de moderador.
- El establecimiento está en estado "en revisión".

**Flujo principal**
1. El moderador rechaza el establecimiento indicando los motivos.
2. El sistema cambia el estado a "rechazado" y notifica al responsable.
3. El sistema registra la decisión y los motivos en el log de auditoría.
4. El sistema conserva el historial del intento.

**Reglas aplicables**
- Regla 44: permisos validados.
- Regla 55: la decisión queda en auditoría.
- Regla 57: el historial de intentos se conserva; el contenido puede corregirse y reenviarse.

**Resultado esperado**
Establecimiento rechazado; el responsable puede corregirlo y enviarlo nuevamente.

**Errores esperados**
- Motivo de rechazo ausente o insuficiente.
- Establecimiento no en estado de revisión.
- Actor sin permisos.

---

### UC-39 — ReviewBranchSubmission

| Campo | Valor |
|---|---|
| **Nombre funcional** | Revisar solicitud de publicación de sede |
| **Actor** | Moderator |
| **Objetivo** | Consultar los detalles de una sede enviada a revisión para tomar una decisión. |
| **Módulo** | Moderation |
| **Tipo** | Query |
| **Prioridad** | MVP secundario |

**Precondiciones**
- El moderador está autenticado.
- La sede está en estado "en revisión".

**Flujo principal**
1. El moderador accede a la cola de sedes en revisión.
2. El moderador selecciona una sede.
3. El sistema devuelve el detalle completo de la sede y el historial de revisiones.

**Reglas aplicables**
- Regla 44: permisos validados.
- Regla 57: historial de revisiones disponible.

**Resultado esperado**
Detalle de la sede disponible para decisión de moderación.

**Errores esperados**
- Sede no encontrada o no en revisión.
- Actor sin permisos.

---

### UC-40 — ApproveBranch

| Campo | Valor |
|---|---|
| **Nombre funcional** | Aprobar sede |
| **Actor** | Moderator |
| **Objetivo** | Aprobar la publicación de una sede que ha cumplido los criterios de revisión. |
| **Módulo** | Moderation |
| **Tipo** | Command |
| **Prioridad** | MVP secundario |

**Precondiciones**
- El moderador está autenticado.
- La sede está en estado "en revisión".
- El moderador no es el responsable de la sede.

**Flujo principal**
1. El moderador aprueba la sede con nota opcional.
2. El sistema verifica separación de funciones.
3. El sistema cambia el estado a "aprobada" y la marca como publicada.
4. El sistema registra la decisión en auditoría.

**Reglas aplicables**
- Regla 6: ubicación válida necesaria para publicarse.
- Regla 44: permisos validados.
- Regla 47: el responsable no puede aprobar su propio contenido.
- Regla 51 y 52: visibilidad pública controlada por moderación.
- Regla 55: decisión registrada en auditoría.

**Resultado esperado**
Sede aprobada y visible públicamente.

**Errores esperados**
- Sede sin información mínima suficiente.
- Actor es el responsable del contenido.
- Actor sin permisos.

---

### UC-41 — RejectBranch

| Campo | Valor |
|---|---|
| **Nombre funcional** | Rechazar sede |
| **Actor** | Moderator |
| **Objetivo** | Rechazar la publicación de una sede con los motivos correspondientes para que pueda corregirse. |
| **Módulo** | Moderation |
| **Tipo** | Command |
| **Prioridad** | MVP secundario |

**Precondiciones**
- El moderador está autenticado.
- La sede está en estado "en revisión".

**Flujo principal**
1. El moderador rechaza la sede con motivos.
2. El sistema cambia el estado a "rechazada" y notifica al responsable.
3. El sistema registra la decisión en auditoría.
4. El historial de la revisión se conserva.

**Reglas aplicables**
- Regla 44: permisos validados.
- Regla 55: decisión en auditoría.
- Regla 57: historial conservado; puede reenviarse corregida.

**Resultado esperado**
Sede rechazada; el responsable puede corregirla y reenviarla.

**Errores esperados**
- Motivo de rechazo ausente.
- Sede no en estado de revisión.
- Actor sin permisos.

---

### UC-42 — ReviewDietarySuitability

| Campo | Valor |
|---|---|
| **Nombre funcional** | Revisar declaración de adecuación alimentaria |
| **Actor** | Moderator |
| **Objetivo** | Consultar la información de adecuación alimentaria declarada por el responsable de una sede para verificarla o rechazarla. |
| **Módulo** | Moderation |
| **Tipo** | Query |
| **Prioridad** | MVP secundario |

**Precondiciones**
- El moderador está autenticado.
- Existe una declaración de adecuación alimentaria pendiente de verificación para una sede.

**Flujo principal**
1. El moderador accede a la cola de adecuaciones pendientes de verificación.
2. El moderador selecciona una sede.
3. El sistema devuelve la información declarada y la documentación de respaldo (si aplica).

**Reglas aplicables**
- Regla 13 y 14: nivel y fuente declarados.
- Regla 15: la información declarada se distingue de la verificada.
- Regla 18: afirmaciones médicas requieren evidencia estructurada.
- Regla 44: permisos validados.

**Resultado esperado**
Información de adecuación alimentaria disponible para decisión del moderador.

**Errores esperados**
- No hay declaraciones pendientes.
- Actor sin permisos.

---

### UC-43 — VerifyDietarySuitability

| Campo | Valor |
|---|---|
| **Nombre funcional** | Verificar adecuación alimentaria |
| **Actor** | Moderator |
| **Objetivo** | Marcar como verificada la información de adecuación alimentaria de una sede, cambiando su fuente de "declarada" a "verificada". |
| **Módulo** | Moderation |
| **Tipo** | Command |
| **Prioridad** | MVP secundario |

**Precondiciones**
- El moderador está autenticado.
- La adecuación existe y está pendiente de verificación.
- El moderador no es el responsable de la sede.

**Flujo principal**
1. El moderador aprueba la verificación con evidencia y nota opcional.
2. El sistema valida la separación de funciones.
3. El sistema actualiza la fuente de "declarada" a "verificada".
4. El sistema registra la decisión en auditoría.

**Reglas aplicables**
- Regla 14: la fuente cambia a "verificada por tercero".
- Regla 15: la vista pública reflejará el estado verificado.
- Regla 44 y 47: permisos y separación de funciones.
- Regla 55: decisión en auditoría.

**Resultado esperado**
Adecuación alimentaria marcada como verificada; visible con distinción en la vista pública.

**Errores esperados**
- Actor es el responsable del contenido.
- Adecuación no en estado pendiente.
- Actor sin permisos.

---

### UC-44 — RejectDietarySuitability

| Campo | Valor |
|---|---|
| **Nombre funcional** | Rechazar declaración de adecuación alimentaria |
| **Actor** | Moderator |
| **Objetivo** | Rechazar la declaración de adecuación alimentaria de una sede por información insuficiente o incorrecta. |
| **Módulo** | Moderation |
| **Tipo** | Command |
| **Prioridad** | MVP secundario |

**Precondiciones**
- El moderador está autenticado.
- La adecuación existe y está pendiente de verificación.

**Flujo principal**
1. El moderador rechaza la declaración con motivos.
2. El sistema devuelve el estado de la adecuación a "declarada no verificada" o la marca como rechazada.
3. El sistema notifica al responsable.
4. El sistema registra la decisión en auditoría.

**Reglas aplicables**
- Regla 18: afirmaciones sin evidencia no pueden presentarse como seguras.
- Regla 44: permisos validados.
- Regla 55: decisión en auditoría.

**Resultado esperado**
Declaración rechazada; el responsable debe corregirla.

**Errores esperados**
- Motivo de rechazo ausente.
- Actor sin permisos.

---

### UC-45 — ReviewReportedReview

| Campo | Valor |
|---|---|
| **Nombre funcional** | Revisar reseña reportada |
| **Actor** | Moderator |
| **Objetivo** | Consultar el contenido de una reseña reportada para decidir si debe ocultarse o restaurarse. |
| **Módulo** | Moderation |
| **Tipo** | Query |
| **Prioridad** | MVP secundario |

**Precondiciones**
- El moderador está autenticado.
- Existe al menos un reporte activo sobre la reseña.

**Flujo principal**
1. El moderador accede a la cola de reseñas reportadas.
2. El moderador selecciona una reseña.
3. El sistema devuelve el contenido de la reseña, el motivo del reporte, y el historial de moderación de esa reseña.

**Reglas aplicables**
- Regla 34: el moderador no puede cambiar autoría ni contenido original.
- Regla 35: la reseña reportada está en revisión, no eliminada automáticamente.
- Regla 44: permisos validados.

**Resultado esperado**
Contenido de la reseña y reportes disponibles para decisión.

**Errores esperados**
- Reseña no encontrada o sin reportes activos.
- Actor sin permisos.

---

### UC-46 — HideReview

| Campo | Valor |
|---|---|
| **Nombre funcional** | Ocultar reseña |
| **Actor** | Moderator |
| **Objetivo** | Ocultar una reseña que incumple las normas de la plataforma, retirándola de la vista pública sin eliminarla permanentemente. |
| **Módulo** | Moderation |
| **Tipo** | Command |
| **Prioridad** | MVP secundario |

**Precondiciones**
- El moderador está autenticado.
- La reseña existe y está en estado publicado o en revisión.

**Flujo principal**
1. El moderador oculta la reseña con un motivo.
2. El sistema cambia el estado de la reseña a "oculta".
3. El sistema recalcula el promedio de la sede excluyendo la reseña oculta.
4. El sistema registra la decisión en auditoría.

**Reglas aplicables**
- Regla 32: solo reseñas publicadas afectan el promedio; la oculta se excluye.
- Regla 34: el moderador no altera autoría ni contenido original.
- Regla 38: reseñas ocultas no se incluyen en el promedio.
- Regla 44: permisos validados.
- Regla 55: decisión en auditoría.

**Resultado esperado**
Reseña oculta y excluida del promedio de la sede.

**Errores esperados**
- Reseña no encontrada o ya oculta.
- Motivo de ocultación ausente.
- Actor sin permisos.

---

### UC-47 — RestoreReview

| Campo | Valor |
|---|---|
| **Nombre funcional** | Restaurar reseña |
| **Actor** | Moderator |
| **Objetivo** | Restaurar una reseña previamente oculta a su estado publicado, incluyéndola nuevamente en el promedio de la sede. |
| **Módulo** | Moderation |
| **Tipo** | Command |
| **Prioridad** | MVP secundario |

**Precondiciones**
- El moderador está autenticado.
- La reseña existe y está en estado "oculta".

**Flujo principal**
1. El moderador restaura la reseña con nota opcional.
2. El sistema cambia el estado de la reseña a "publicada".
3. El sistema recalcula el promedio de la sede incluyendo la reseña restaurada.
4. El sistema registra la decisión en auditoría.

**Reglas aplicables**
- Regla 32 y 36: al restaurarse, la reseña vuelve a afectar el promedio.
- Regla 34: el moderador no modifica contenido ni autoría.
- Regla 44: permisos validados.
- Regla 55: decisión en auditoría.

**Resultado esperado**
Reseña restaurada y visible públicamente; promedio de la sede recalculado.

**Errores esperados**
- Reseña no en estado oculta.
- Actor sin permisos.

---

### UC-48 — ResolveReviewReport

| Campo | Valor |
|---|---|
| **Nombre funcional** | Resolver reporte de reseña |
| **Actor** | Moderator |
| **Objetivo** | Marcar como resuelto el reporte de una reseña tras haber tomado una decisión (ocultar o no ocultar). |
| **Módulo** | Moderation |
| **Tipo** | Command |
| **Prioridad** | MVP secundario |

**Precondiciones**
- El moderador está autenticado.
- El reporte existe y está pendiente de resolución.

**Flujo principal**
1. El moderador marca el reporte como resuelto, indicando la acción tomada.
2. El sistema actualiza el estado del reporte.
3. El sistema registra la resolución en auditoría.

**Reglas aplicables**
- Regla 44: permisos validados.
- Regla 55: la resolución queda en auditoría.

**Resultado esperado**
Reporte cerrado con indicación de la acción tomada.

**Errores esperados**
- Reporte no encontrado o ya resuelto.
- Actor sin permisos.

---

## Módulo: Administration

Casos de uso para la gestión de catálogos del sistema por parte del administrador.

---

### UC-49 — CreateCategory

| Campo | Valor |
|---|---|
| **Nombre funcional** | Crear categoría gastronómica |
| **Actor** | Administrator |
| **Objetivo** | Agregar una nueva categoría gastronómica al catálogo del sistema. |
| **Módulo** | Administration |
| **Tipo** | Command |
| **Prioridad** | MVP secundario |

**Precondiciones**
- El administrador está autenticado.
- El nombre de la categoría no existe en el catálogo activo.

**Flujo principal**
1. El administrador envía el nombre de la nueva categoría.
2. El sistema valida que el nombre no esté duplicado.
3. El sistema crea la categoría en estado activo.
4. El sistema confirma la creación.

**Reglas aplicables**
- Regla 44: permisos validados.

**Resultado esperado**
Nueva categoría disponible en el catálogo.

**Errores esperados**
- Nombre de categoría duplicado.
- Actor sin permisos de administrador.

---

### UC-50 — UpdateCategory

| Campo | Valor |
|---|---|
| **Nombre funcional** | Actualizar categoría gastronómica |
| **Actor** | Administrator |
| **Objetivo** | Modificar el nombre o descripción de una categoría gastronómica existente. |
| **Módulo** | Administration |
| **Tipo** | Command |
| **Prioridad** | MVP secundario |

**Precondiciones**
- El administrador está autenticado.
- La categoría existe y está activa.

**Flujo principal**
1. El administrador envía los campos actualizados.
2. El sistema valida que el nuevo nombre no esté duplicado.
3. El sistema actualiza la categoría.
4. El sistema confirma la actualización.

**Reglas aplicables**
- Regla 44: permisos validados.

**Resultado esperado**
Categoría actualizada en el catálogo.

**Errores esperados**
- Nombre duplicado.
- Categoría no encontrada.
- Actor sin permisos.

---

### UC-51 — DeactivateCategory

| Campo | Valor |
|---|---|
| **Nombre funcional** | Desactivar categoría gastronómica |
| **Actor** | Administrator |
| **Objetivo** | Desactivar una categoría para que deje de estar disponible en el catálogo público, sin eliminarla físicamente. |
| **Módulo** | Administration |
| **Tipo** | Command |
| **Prioridad** | MVP secundario |

**Precondiciones**
- El administrador está autenticado.
- La categoría existe y está activa.

**Flujo principal**
1. El administrador solicita desactivar una categoría.
2. El sistema valida que la categoría no sea la única categoría principal de un establecimiento publicado (en ese caso se advierte).
3. El sistema marca la categoría como inactiva.
4. El sistema confirma la desactivación.

**Reglas aplicables**
- Regla 3 y 4: los establecimientos dependen de categorías; desactivar puede afectar la clasificación.
- Regla 44: permisos validados.

**Resultado esperado**
Categoría desactivada; deja de aparecer en el catálogo público y en filtros.

**Errores esperados**
- Categoría no encontrada o ya inactiva.
- Actor sin permisos.

---

### UC-52 — CreateDietaryNeed

| Campo | Valor |
|---|---|
| **Nombre funcional** | Crear necesidad alimentaria |
| **Actor** | Administrator |
| **Objetivo** | Agregar una nueva necesidad alimentaria al catálogo estructurado del sistema. |
| **Módulo** | Administration |
| **Tipo** | Command |
| **Prioridad** | MVP secundario |

**Precondiciones**
- El administrador está autenticado.
- El nombre de la necesidad no existe en el catálogo.

**Flujo principal**
1. El administrador envía el nombre y descripción de la nueva necesidad alimentaria.
2. El sistema valida la unicidad del nombre.
3. El sistema crea el ítem en estado activo.
4. El sistema confirma la creación.

**Reglas aplicables**
- Regla 12: las necesidades alimentarias deben ser conceptos estructurados del catálogo.
- Regla 44: permisos validados.

**Resultado esperado**
Nueva necesidad alimentaria disponible en el catálogo.

**Errores esperados**
- Nombre duplicado.
- Actor sin permisos.

---

### UC-53 — UpdateDietaryNeed

| Campo | Valor |
|---|---|
| **Nombre funcional** | Actualizar necesidad alimentaria |
| **Actor** | Administrator |
| **Objetivo** | Modificar el nombre o descripción de una necesidad alimentaria existente en el catálogo. |
| **Módulo** | Administration |
| **Tipo** | Command |
| **Prioridad** | MVP secundario |

**Precondiciones**
- El administrador está autenticado.
- La necesidad alimentaria existe y está activa.

**Flujo principal**
1. El administrador envía los campos actualizados.
2. El sistema valida la unicidad del nuevo nombre.
3. El sistema actualiza el ítem.
4. El sistema confirma la actualización.

**Reglas aplicables**
- Regla 44: permisos validados.

**Resultado esperado**
Necesidad alimentaria actualizada en el catálogo.

**Errores esperados**
- Nombre duplicado.
- Necesidad no encontrada.
- Actor sin permisos.

---

### UC-54 — DeactivateDietaryNeed

| Campo | Valor |
|---|---|
| **Nombre funcional** | Desactivar necesidad alimentaria |
| **Actor** | Administrator |
| **Objetivo** | Desactivar una necesidad alimentaria del catálogo para que deje de estar disponible, siempre que no tenga referencias activas. |
| **Módulo** | Administration |
| **Tipo** | Command |
| **Prioridad** | MVP secundario |

**Precondiciones**
- El administrador está autenticado.
- La necesidad alimentaria existe y está activa.

**Flujo principal**
1. El administrador solicita desactivar una necesidad alimentaria.
2. El sistema verifica que no esté referenciada en ninguna adecuación activa.
3. Si hay referencias activas, el sistema rechaza la desactivación.
4. Si no hay referencias activas, el sistema marca el ítem como inactivo.
5. El sistema confirma la desactivación.

**Reglas aplicables**
- Regla 20: una necesidad alimentaria no puede eliminarse si está referenciada en adecuaciones activas.
- Regla 44: permisos validados.

**Resultado esperado**
Necesidad alimentaria desactivada del catálogo.

**Errores esperados**
- La necesidad está referenciada en adecuaciones activas (no se puede desactivar).
- Necesidad no encontrada o ya inactiva.
- Actor sin permisos.

---

### UC-55 — CreateService

| Campo | Valor |
|---|---|
| **Nombre funcional** | Crear servicio disponible |
| **Actor** | Administrator |
| **Objetivo** | Agregar un nuevo tipo de servicio al catálogo del sistema. |
| **Módulo** | Administration |
| **Tipo** | Command |
| **Prioridad** | MVP secundario |

**Precondiciones**
- El administrador está autenticado.
- El nombre del servicio no existe en el catálogo.

**Flujo principal**
1. El administrador envía el nombre y descripción del nuevo servicio.
2. El sistema valida la unicidad del nombre.
3. El sistema crea el servicio en estado activo.
4. El sistema confirma la creación.

**Reglas aplicables**
- Regla 44: permisos validados.

**Resultado esperado**
Nuevo servicio disponible en el catálogo.

**Errores esperados**
- Nombre de servicio duplicado.
- Actor sin permisos.

---

### UC-56 — UpdateService

| Campo | Valor |
|---|---|
| **Nombre funcional** | Actualizar servicio disponible |
| **Actor** | Administrator |
| **Objetivo** | Modificar el nombre o descripción de un servicio existente en el catálogo. |
| **Módulo** | Administration |
| **Tipo** | Command |
| **Prioridad** | MVP secundario |

**Precondiciones**
- El administrador está autenticado.
- El servicio existe y está activo.

**Flujo principal**
1. El administrador envía los campos actualizados.
2. El sistema valida la unicidad del nuevo nombre.
3. El sistema actualiza el servicio.
4. El sistema confirma la actualización.

**Reglas aplicables**
- Regla 44: permisos validados.

**Resultado esperado**
Servicio actualizado en el catálogo.

**Errores esperados**
- Nombre duplicado.
- Servicio no encontrado.
- Actor sin permisos.

---

### UC-57 — DeactivateService

| Campo | Valor |
|---|---|
| **Nombre funcional** | Desactivar servicio disponible |
| **Actor** | Administrator |
| **Objetivo** | Desactivar un servicio del catálogo para que deje de estar disponible para nuevas asignaciones a sedes. |
| **Módulo** | Administration |
| **Tipo** | Command |
| **Prioridad** | MVP secundario |

**Precondiciones**
- El administrador está autenticado.
- El servicio existe y está activo.

**Flujo principal**
1. El administrador solicita desactivar un servicio.
2. El sistema marca el servicio como inactivo.
3. El sistema confirma la desactivación.

**Reglas aplicables**
- Regla 44: permisos validados.

**Resultado esperado**
Servicio desactivado; no disponible para nuevas asignaciones, pero conservado en sedes que ya lo tenían.

**Errores esperados**
- Servicio no encontrado o ya inactivo.
- Actor sin permisos.

---

## Módulo: Identity & Access

Casos de uso para la gestión de roles y permisos por parte del administrador.

---

### UC-58 — CreateRole

| Campo | Valor |
|---|---|
| **Nombre funcional** | Crear rol |
| **Actor** | Administrator |
| **Objetivo** | Definir un nuevo rol en el sistema de autorización con nombre y descripción. |
| **Módulo** | Identity & Access |
| **Tipo** | Command |
| **Prioridad** | MVP esencial |

**Precondiciones**
- El administrador está autenticado.
- El nombre del rol no existe en el sistema.

**Flujo principal**
1. El administrador envía el nombre y descripción del nuevo rol.
2. El sistema valida la unicidad del nombre.
3. El sistema crea el rol sin permisos asignados inicialmente.
4. El sistema confirma la creación.

**Reglas aplicables**
- Regla 43: el control de acceso vive en el backend.
- Regla 44: permisos validados.
- Regla 48: los permisos globales se distinguen de los permisos por establecimiento o sede.

**Resultado esperado**
Nuevo rol creado en el sistema, listo para recibir permisos.

**Errores esperados**
- Nombre de rol duplicado.
- Actor sin permisos de administrador.

---

### UC-59 — UpdateRole

| Campo | Valor |
|---|---|
| **Nombre funcional** | Actualizar rol |
| **Actor** | Administrator |
| **Objetivo** | Modificar el nombre o descripción de un rol existente en el sistema. |
| **Módulo** | Identity & Access |
| **Tipo** | Command |
| **Prioridad** | MVP esencial |

**Precondiciones**
- El administrador está autenticado.
- El rol existe.

**Flujo principal**
1. El administrador envía los campos actualizados del rol.
2. El sistema valida la unicidad del nuevo nombre.
3. El sistema actualiza el rol.
4. El sistema confirma la actualización.

**Reglas aplicables**
- Regla 44: permisos validados.

**Resultado esperado**
Rol actualizado correctamente.

**Errores esperados**
- Nombre de rol duplicado.
- Rol no encontrado.
- Actor sin permisos.

---

### UC-60 — AssignPermissionToRole

| Campo | Valor |
|---|---|
| **Nombre funcional** | Asignar permiso a rol |
| **Actor** | Administrator |
| **Objetivo** | Agregar un permiso específico a un rol para ampliar las capacidades de los usuarios que tengan ese rol asignado. |
| **Módulo** | Identity & Access |
| **Tipo** | Command |
| **Prioridad** | MVP esencial |

**Precondiciones**
- El administrador está autenticado.
- El rol existe.
- El permiso existe en el sistema.
- El rol no tiene ya ese permiso asignado.

**Flujo principal**
1. El administrador selecciona el rol y el permiso a asignar.
2. El sistema verifica que el permiso no esté ya asignado al rol.
3. El sistema crea la asociación rol-permiso.
4. El sistema confirma la asignación.

**Reglas aplicables**
- Regla 43: el control de acceso vive en el backend.
- Regla 44: permisos validados.
- Regla 48: permisos globales distinguidos de permisos de alcance.

**Resultado esperado**
Permiso asignado al rol; los usuarios con ese rol obtienen la capacidad correspondiente.

**Errores esperados**
- Rol o permiso no encontrado.
- Permiso ya asignado al rol.
- Actor sin permisos.

---

### UC-61 — RemovePermissionFromRole

| Campo | Valor |
|---|---|
| **Nombre funcional** | Remover permiso de rol |
| **Actor** | Administrator |
| **Objetivo** | Eliminar un permiso de un rol para restringir las capacidades de los usuarios con ese rol. |
| **Módulo** | Identity & Access |
| **Tipo** | Command |
| **Prioridad** | MVP esencial |

**Precondiciones**
- El administrador está autenticado.
- El rol existe y tiene el permiso asignado.

**Flujo principal**
1. El administrador selecciona el rol y el permiso a remover.
2. El sistema verifica que el permiso esté asignado al rol.
3. El sistema elimina la asociación rol-permiso.
4. El sistema confirma la remoción.

**Reglas aplicables**
- Regla 43: el control de acceso vive en el backend.
- Regla 44: permisos validados.

**Resultado esperado**
Permiso removido del rol; los usuarios con ese rol pierden la capacidad correspondiente.

**Errores esperados**
- Rol o permiso no encontrado.
- El permiso no está asignado al rol.
- Actor sin permisos.

---

### UC-62 — AssignRoleToUser

| Campo | Valor |
|---|---|
| **Nombre funcional** | Asignar rol a usuario |
| **Actor** | Administrator |
| **Objetivo** | Otorgar un rol a un usuario con el alcance correspondiente (global, por establecimiento o por sede), registrando quién realizó la asignación. |
| **Módulo** | Identity & Access |
| **Tipo** | Command |
| **Prioridad** | MVP esencial |

**Precondiciones**
- El administrador está autenticado.
- El usuario existe y tiene cuenta activa.
- El rol existe en el sistema.
- No existe ya la misma asignación (mismo usuario, rol y alcance).

**Flujo principal**
1. El administrador selecciona el usuario, el rol y el alcance (global, establecimiento específico o sede específica).
2. El sistema valida que el alcance sea coherente con el tipo de rol.
3. El sistema verifica que no exista una asignación duplicada.
4. El sistema crea la asignación registrando el otorgante y la fecha.
5. El sistema confirma la asignación.

**Reglas aplicables**
- Regla 43: el control de acceso vive en el backend.
- Regla 44: permisos validados.
- Regla 45: las asignaciones con alcance respetan su ámbito.
- Regla 48: alcance global distinto de alcance por establecimiento o sede.
- Regla 49: no pueden existir asignaciones duplicadas.
- Regla 50: el otorgante queda registrado en auditoría.

**Resultado esperado**
Rol asignado al usuario en el alcance especificado; el usuario obtiene los permisos correspondientes dentro de ese ámbito.

**Errores esperados**
- Usuario o rol no encontrado.
- Asignación duplicada (mismo usuario, rol y alcance).
- Alcance incoherente (establecimiento o sede no existen, o la sede no pertenece al establecimiento indicado).
- Actor sin permisos de administrador.

---

### UC-63 — RevokeRoleAssignment

| Campo | Valor |
|---|---|
| **Nombre funcional** | Revocar asignación de rol |
| **Actor** | Administrator |
| **Objetivo** | Retirar una asignación de rol a un usuario, eliminando los permisos correspondientes dentro del alcance en que fue asignado. |
| **Módulo** | Identity & Access |
| **Tipo** | Command |
| **Prioridad** | MVP esencial |

**Precondiciones**
- El administrador está autenticado.
- La asignación de rol existe y está activa.

**Flujo principal**
1. El administrador selecciona la asignación de rol a revocar.
2. El sistema verifica que la asignación exista y esté activa.
3. El sistema marca la asignación como revocada, registrando quién la revocó y la fecha.
4. El sistema confirma la revocación.

**Reglas aplicables**
- Regla 43: el control de acceso vive en el backend.
- Regla 44: permisos validados.
- Regla 50: la acción queda registrada en auditoría.

**Resultado esperado**
Asignación de rol revocada; el usuario pierde los permisos correspondientes en el alcance especificado.

**Errores esperados**
- Asignación no encontrada o ya revocada.
- Actor sin permisos de administrador.

---

## Notas generales

- Los números de regla referenciados corresponden al documento `domain-rules.md` ubicado en `docs/domain/`.
- Todos los casos de uso de tipo Command implican validación de permisos en el backend antes de ejecutar la operación (regla 44).
- Las queries que devuelven contenido público filtran automáticamente el contenido no publicado o suspendido (reglas 7, 8, 51).
- El cálculo de promedio de ratings y el conteo de reseñas son siempre derivados; no pueden establecerse manualmente (reglas 36, 37).
- Las eliminaciones son lógicas salvo indicación contraria; los datos auditables se conservan según la política de retención (regla 56, 69).
