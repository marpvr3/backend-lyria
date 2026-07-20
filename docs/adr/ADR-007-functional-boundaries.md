# ADR-007: Límites funcionales entre módulos

## Estado

Aceptado

## Contexto

El monolito modular adoptado en ADR-006 requiere definir con precisión cuáles son los módulos del sistema, qué responsabilidades tiene cada uno y cómo se relacionan entre sí. Sin esta definición explícita, los límites se erosionan con el tiempo y el sistema degenera en un monolito acoplado.

## Decisión

Se definen **10 módulos funcionales** con responsabilidades y datos propios:

### 1. Identity & Access
- Responsabilidad: cuentas, credenciales, roles y permisos.
- Datos propios: usuarios autenticables, tokens, asignaciones de rol.

### 2. User Profiles
- Responsabilidad: información personal del usuario, preferencias alimentarias declaradas.
- Datos propios: perfil de usuario, configuración de preferencias dietéticas.

### 3. Establishments
- Responsabilidad: establecimientos, sucursales, direcciones, horarios y servicios.
- Datos propios: entidades de establecimiento y sucursal, información de contacto y operación. Incluye los catálogos de referencia `EstablishmentCategory` y `Service`, que son datos propios del módulo y no pertenecen a ningún módulo genérico de catálogos compartido.

### 4. Dietary Catalog
- Responsabilidad: definición de necesidades dietéticas y de adecuación alimentaria.
- Datos propios: `DietaryNeed` es el dato primario y autoritativo de este módulo — el catálogo centralizado de restricciones, alergias, intolerancias, estilos de vida y requerimientos religiosos relacionados con la alimentación que el sistema reconoce.

### 5. Search & Discovery
- Responsabilidad: consultas, filtros y proyecciones de búsqueda.
- Datos propios: ninguno. Este módulo consume datos de otros módulos mediante proyecciones de lectura.

### 6. Favorites
- Responsabilidad: marcadores de usuario sobre sucursales.
- Datos propios: relación usuario–sucursal con metadatos de favorito.

### 7. Reviews
- Responsabilidad: calificaciones, comentarios y ciclo de vida de reseñas.
- Datos propios: reseñas, calificaciones, estados de publicación.

### 8. Media
- Responsabilidad: metadatos de imágenes y referencias a almacenamiento externo.
- Datos propios: registros de media con identificadores de recurso y asociaciones.

### 9. Moderation
- Responsabilidad: reportes, aprobaciones y verificación de contenido.
- Datos propios: reportes de contenido, estados de moderación, historial de decisiones.

### 10. Administration
- Responsabilidad: superficie de administración sobre otros módulos.
- Datos propios: ninguno exclusivo. Administration es una superficie de aplicación, no un dominio independiente.

### Reglas de interacción entre módulos

- Las dependencias entre módulos están permitidas únicamente mediante **identificadores** (IDs).
- Prohibido compartir modelos de dominio internos entre módulos distintos.
- Si un módulo necesita datos de otro, debe usar una proyección de lectura o una interfaz de contrato explícita.
- Administration no tiene dominio propio; orquesta operaciones sobre los módulos que administra.

## Razones

1. La asignación explícita de responsabilidades evita ambigüedad sobre dónde vive cada concepto del dominio.
2. Limitar la comunicación a identificadores reduce el acoplamiento estructural entre módulos.
3. Separar Search & Discovery como módulo sin datos propios permite optimizar queries de lectura sin afectar los modelos de escritura.
4. Reconocer Administration como superficie y no como dominio evita duplicar conceptos ya definidos en otros módulos.

## Restricciones

- Ningún módulo puede referenciar repositorios ni entidades internas de otro módulo directamente.
- Los contratos entre módulos deben definirse en `Lyria.Application` como abstracciones, no en `Lyria.Infrastructure`.
- Cambiar los límites de un módulo o crear uno nuevo requiere actualizar este ADR.

## Consecuencias

- Propiedad clara de cada dato y cada regla de negocio.
- Acoplamiento reducido entre módulos.
- Las consultas que cruzan módulos (por ejemplo, búsqueda con filtros dietéticos y datos de sucursal) requieren un diseño cuidadoso de proyecciones de lectura.
- La incorporación de nuevos módulos en el futuro debe seguir las mismas reglas de propiedad e interacción.
