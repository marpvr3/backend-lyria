# ADR-012: Asignaciones de roles con alcance

## Estado

Aceptado

## Contexto

Lyria requiere un sistema de autorización que distinga entre permisos globales (válidos para toda la plataforma) y permisos con alcance restringido (válidos únicamente para un establecimiento o una sede específica).

Un modelo de roles plano, sin distinción de alcance, no es suficiente: un gestor de establecimiento no debería poder operar sobre otros establecimientos, y un administrador de sede no debería poder actuar sobre otras sedes del mismo establecimiento.

## Decisión

Se adopta un sistema de autorización basado en **roles y permisos con alcance** (`RoleAssignment` con scope):

- Cada asignación de rol (`RoleAssignment`) incluye un alcance (`scope`) que puede ser:
  - `Global`: sin restricción de establecimiento ni sede.
  - `Establishment`: acotado a un establecimiento concreto (requiere `EstablishmentId`).
  - `Branch`: acotado a una sede concreta (requiere `BranchId`; el establecimiento debe ser coherente).
- Reglas de consistencia obligatorias:
  - Alcance `Global`: los campos `EstablishmentId` y `BranchId` deben ser nulos.
  - Alcance `Establishment`: `EstablishmentId` es obligatorio; `BranchId` debe ser nulo.
  - Alcance `Branch`: `BranchId` es obligatorio; el `EstablishmentId` asociado debe coincidir con el establecimiento al que pertenece la sede.
- No se permiten asignaciones duplicadas para el mismo usuario, rol y alcance.
- Se prohíbe la autoaprobación: un gestor no puede aprobar su propio contenido.
- Cada asignación registra quién la otorgó (`GrantedBy`).
- Las asignaciones pueden desactivarse sin eliminarse físicamente.
- La implementación física de este módulo se difiere a la Fase 8 del proyecto.

## Razones

1. El modelo de alcance permite reutilizar los mismos roles con diferentes granularidades sin multiplicar la cantidad de roles definidos.
2. La prohibición de autoaprobación reduce el riesgo de conflictos de interés en la moderación de contenido.
3. Registrar al otorgante de cada asignación proporciona trazabilidad para auditorías.
4. La desactivación lógica preserva el historial de permisos sin pérdida de información.

## Restricciones

- Toda lógica de verificación de permisos debe considerar el alcance de la asignación, no solo el rol.
- Prohibido aprobar, publicar o moderar contenido propio.
- La coherencia entre `BranchId` y `EstablishmentId` en asignaciones de alcance `Branch` debe validarse en el dominio.
- No se deben crear asignaciones duplicadas activas para la misma combinación de usuario, rol y alcance.

## Consecuencias

- Sistema de autorización flexible y granular que soporta múltiples niveles de administración.
- Mayor complejidad en la lógica de verificación de permisos: cada comprobación debe evaluar el alcance de la asignación.
- La implementación diferida a Fase 8 significa que las fases anteriores deben diseñarse anticipando este modelo sin implementarlo aún.
- La trazabilidad de asignaciones facilita auditorías y resolución de conflictos de acceso.
