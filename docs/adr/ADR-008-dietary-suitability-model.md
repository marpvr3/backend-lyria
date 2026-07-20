# ADR-008: Modelo de adecuación alimentaria

## Estado

Aceptado

## Contexto

El modelo original de la base de datos de referencia utilizaba una relación booleana simple (`SEDE_RESTRICCION`) para indicar si una sucursal admitía una restricción dietética. Este enfoque es insuficiente para la propuesta de valor de Lyria, que busca ser una fuente confiable y detallada de información alimentaria.

Un campo booleano no puede expresar:
- El grado real de adecuación (¿hay una opción o el menú es completamente apto?).
- El origen de la información (¿lo declaró el establecimiento o lo reportaron usuarios?).
- La confiabilidad de los datos (¿está verificado por Lyria o es una declaración no comprobada?).
- La variación entre sucursales del mismo establecimiento.

## Decisión

Se adopta el concepto de **`BranchDietarySuitability`** con tres dimensiones independientes:

### SuitabilityLevel — Nivel de adecuación

Describe qué tan apto es el establecimiento para la necesidad dietética:

| Valor | Significado |
|---|---|
| `OptionAvailable` | Existe al menos una opción apta, pero no es el foco del establecimiento. |
| `MultipleOptions` | Hay varias opciones disponibles. |
| `SeparatePreparation` | Los platos se preparan por separado para evitar contaminación cruzada. |
| `Specialized` | El establecimiento está especializado en esta necesidad dietética. |
| `FullyCompliant` | Todo el menú cumple con la necesidad dietética. |

### InformationSource — Origen de la información

Indica quién proveyó los datos sobre la adecuación:

| Valor | Significado |
|---|---|
| `EstablishmentDeclared` | El propio establecimiento lo declaró. |
| `UserReported` | Fue reportado por uno o más usuarios de Lyria. |
| `LyriaVerified` | Lyria verificó la información directamente. |
| `Certified` | Existe una certificación externa válida. |

### VerificationStatus — Estado de verificación

Refleja el estado actual del proceso de verificación:

| Valor | Significado |
|---|---|
| `Unverified` | Sin verificar. |
| `Pending` | Verificación en curso. |
| `Verified` | Verificado y vigente. |
| `Rejected` | La verificación fue rechazada. |
| `Expired` | La verificación venció y debe renovarse. |

### Reglas adicionales

- Las certificaciones pueden tener períodos de vigencia; deben expirar automáticamente al vencimiento.
- Las afirmaciones de seguridad médica (por ejemplo, apto para celíacos con diagnóstico) requieren evidencia verificable, no solo declaración del establecimiento.
- La adecuación se registra a nivel de **sucursal**, no de establecimiento, ya que puede variar entre locales del mismo negocio.
- No se usan booleanos simples como `IsVegan`, `IsGlutenFree` ni equivalentes.

## Razones

1. Los booleanos no expresan el grado de adecuación ni la confiabilidad de los datos, que son centrales para la propuesta de valor de Lyria.
2. Separar `SuitabilityLevel`, `InformationSource` y `VerificationStatus` permite consultar y filtrar por cada dimensión de forma independiente.
3. Modelar la verificación como un estado explícito permite gestionar el ciclo de vida de los datos y notificar vencimientos.
4. Registrar la adecuación por sucursal refleja la realidad operativa: no todos los locales de una cadena tienen las mismas condiciones.

## Restricciones

- Prohibido agregar columnas booleanas de tipo `IsX` para representar adecuación dietética.
- Cualquier afirmación de seguridad médica debe tener un `InformationSource` de `LyriaVerified` o `Certified` para ser presentada al usuario con ese nivel de confianza.
- Los registros con `VerificationStatus = Expired` no deben mostrarse como verificados en la interfaz.

## Consecuencias

- Información más rica y confiable para el usuario.
- Modelo de datos más complejo que requiere mayor cuidado en el diseño de consultas y proyecciones.
- La experiencia de usuario (UX) debe comunicar las tres dimensiones de forma comprensible sin abrumar al usuario.
- Se requiere un proceso operativo para gestionar verificaciones, vencimientos y actualizaciones de certificaciones.
