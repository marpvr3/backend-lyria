# Actores y permisos — Lyria

## Actores del sistema

### Visitor (Visitante)

Usuario no autenticado que accede a la plataforma para consultar información pública.

**Puede:**
- Buscar establecimientos y sedes.
- Consultar detalle de establecimientos y sedes.
- Aplicar filtros por categoría, ubicación, necesidades alimentarias y servicios.
- Consultar horarios de atención.
- Consultar servicios disponibles.
- Consultar adecuación alimentaria publicada.
- Consultar reseñas publicadas y resumen de puntuación.

**No puede:**
- Guardar favoritos.
- Crear reseñas.
- Reportar contenido.
- Administrar ninguna información.
- Acceder a endpoints administrativos.

### Registered User (Usuario registrado)

Usuario autenticado con cuenta activa.

**Puede:**
- Todo lo que puede un Visitor.
- Gestionar su perfil (nombre, teléfono, foto).
- Configurar sus necesidades alimentarias.
- Guardar y eliminar favoritos.
- Crear una reseña por sede (máximo una activa).
- Editar su propia reseña.
- Eliminar lógicamente su propia reseña.
- Reportar reseñas de otros usuarios.
- Reportar información incorrecta de una sede.

**No puede:**
- Editar reseñas de otros usuarios.
- Administrar establecimientos.
- Moderar contenido.
- Acceder a funciones administrativas.

### Establishment Manager (Responsable de establecimiento)

Usuario autenticado con rol de gestión asignado a uno o más establecimientos o sedes.

**Puede (dentro de su alcance asignado):**
- Actualizar información comercial del establecimiento.
- Gestionar sedes asignadas.
- Gestionar horarios de atención.
- Gestionar servicios disponibles.
- Gestionar imágenes de sede.
- Proponer información de adecuación alimentaria.
- Enviar contenido a revisión.

**No puede:**
- Aprobar su propio contenido.
- Moderar reseñas.
- Gestionar establecimientos no asignados.
- Asignarse permisos o roles.
- Verificar adecuación alimentaria.
- Eliminar reseñas de usuarios.

### Moderator (Moderador)

Usuario autenticado con rol de moderación.

**Puede:**
- Revisar contenido enviado a revisión.
- Aprobar o rechazar publicaciones de establecimientos y sedes.
- Verificar información de adecuación alimentaria.
- Moderar reseñas (ocultar, restaurar).
- Resolver reportes de reseñas.
- Suspender contenido según autorización.

**No puede:**
- Reescribir comentarios de usuarios.
- Cambiar autoría de reseñas.
- Gestionar roles ni permisos.
- Gestionar catálogos del sistema.
- Crear o eliminar establecimientos.

### Administrator (Administrador)

Usuario autenticado con privilegios administrativos completos.

**Puede:**
- Gestionar usuarios (consultar, activar, desactivar).
- Gestionar roles y permisos.
- Asignar y revocar roles a usuarios.
- Gestionar establecimientos y sedes.
- Gestionar catálogos (categorías, necesidades alimentarias, servicios).
- Gestionar moderadores.
- Consultar auditoría de operaciones.
- Configurar parámetros permitidos del sistema.

**No puede:**
- Reescribir reseñas de usuarios.
- Editar el rating de una reseña.
- Establecer manualmente el rating promedio de una sede.

## Matriz de permisos

### Permisos candidatos por área funcional

#### Establecimientos y sedes

| Permiso | Visitor | User | Manager | Moderator | Admin |
|---------|---------|------|---------|-----------|-------|
| `establishments:read` | SI | SI | SI | SI | SI |
| `establishments:create` | - | - | - | - | SI |
| `establishments:update` | - | - | Alcance | - | SI |
| `establishments:submit-review` | - | - | Alcance | - | SI |
| `establishments:approve` | - | - | - | SI | SI |
| `establishments:reject` | - | - | - | SI | SI |
| `establishments:suspend` | - | - | - | SI | SI |
| `branches:read` | SI | SI | SI | SI | SI |
| `branches:create` | - | - | Alcance | - | SI |
| `branches:update` | - | - | Alcance | - | SI |
| `branches:submit-review` | - | - | Alcance | - | SI |
| `branches:approve` | - | - | - | SI | SI |
| `branches:reject` | - | - | - | SI | SI |
| `branches:manage-hours` | - | - | Alcance | - | SI |
| `branches:manage-services` | - | - | Alcance | - | SI |
| `branches:manage-images` | - | - | Alcance | - | SI |
| `branches:manage-dietary` | - | - | Alcance | - | SI |

#### Reseñas y favoritos

| Permiso | Visitor | User | Manager | Moderator | Admin |
|---------|---------|------|---------|-----------|-------|
| `reviews:read` | SI | SI | SI | SI | SI |
| `reviews:create` | - | SI | SI | - | - |
| `reviews:update-own` | - | SI | SI | - | - |
| `reviews:delete-own` | - | SI | SI | - | - |
| `reviews:moderate` | - | - | - | SI | SI |
| `reviews:report` | - | SI | SI | - | - |
| `favorites:manage` | - | SI | SI | - | SI |

#### Perfiles

| Permiso | Visitor | User | Manager | Moderator | Admin |
|---------|---------|------|---------|-----------|-------|
| `profiles:read-own` | - | SI | SI | SI | SI |
| `profiles:update-own` | - | SI | SI | SI | SI |
| `profiles:manage-dietary-needs` | - | SI | SI | SI | SI |

#### Moderación

| Permiso | Visitor | User | Manager | Moderator | Admin |
|---------|---------|------|---------|-----------|-------|
| `moderation:review-content` | - | - | - | SI | SI |
| `moderation:verify-dietary` | - | - | - | SI | SI |
| `moderation:resolve-reports` | - | - | - | SI | SI |

#### Administración

| Permiso | Visitor | User | Manager | Moderator | Admin |
|---------|---------|------|---------|-----------|-------|
| `admin:manage-users` | - | - | - | - | SI |
| `admin:manage-roles` | - | - | - | - | SI |
| `admin:manage-catalogs` | - | - | - | - | SI |
| `admin:view-audit` | - | - | - | - | SI |
| `admin:assign-roles` | - | - | - | - | SI |

## Reglas de alcance

- **Alcance global**: el permiso aplica a todos los recursos del tipo. Ejemplo: un administrador puede gestionar cualquier establecimiento.
- **Alcance de establecimiento**: el permiso aplica solo al establecimiento asignado y todas sus sedes. Ejemplo: un manager puede editar solo los establecimientos que tiene asignados.
- **Alcance de sede**: el permiso aplica solo a la sede específica asignada.

### Restricciones de alcance

1. Una asignación global no debe tener establecimiento ni sede asociados.
2. Una asignación por establecimiento debe tener establecimiento y no sede.
3. Una asignación por sede debe tener sede (y el establecimiento debe ser coherente).
4. No pueden existir asignaciones duplicadas (mismo usuario, rol, alcance).
5. Un responsable no puede aprobar su propio contenido (separación de funciones).
6. La asignación debe registrar quién la otorgó y cuándo.
