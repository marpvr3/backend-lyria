# ADR-010: Establecimiento y sede como agregados separados

## Estado

Aceptado

## Contexto

El dominio de Lyria distingue entre dos conceptos relacionados pero distintos: el **Establecimiento** (entidad de marca u organización, por ejemplo "Restaurante La Palma") y la **Sede** (ubicación física concreta de ese establecimiento).

La decisión de modelarlos como un único agregado o como agregados independientes tiene consecuencias directas sobre el rendimiento, la escalabilidad y la coherencia del modelo de dominio.

Un único agregado implicaría cargar toda la información del Establecimiento —incluyendo todas sus Sedes— cada vez que se necesita operar sobre una Sede individual, lo cual resulta costoso y viola el principio de raíz de agregado mínima.

## Decisión

Se modelan **Establecimiento** (`Establishment`) y **Sede** (`Branch`) como agregados separados:

- `Establishment` y `Branch` son raíces de agregado independientes.
- `Branch` referencia a `Establishment` mediante el identificador `EstablishmentId`, nunca mediante navegación directa al objeto.
- Las operaciones sobre una `Branch` no deben cargar el agregado `Establishment` completo.
- Los horarios de atención, servicios ofrecidos, aptitudes dietéticas e imágenes pertenecen a `Branch` o a módulos relacionados con la sede.
- Un `Establishment` puede tener una o muchas `Branch`.
- Las condiciones de operación (horarios, servicios, precios) pueden variar entre sedes del mismo establecimiento.

## Razones

1. Cada concepto tiene un ciclo de vida independiente: el establecimiento puede existir sin sedes operativas y viceversa.
2. Mantenerlos separados evita agregados demasiado grandes que impacten el rendimiento.
3. La referencia por identificador es la práctica estándar entre agregados en diseño orientado al dominio.
4. Las variaciones por sede son una realidad del negocio y requieren un modelo propio, no derivado del establecimiento padre.

## Restricciones

- Prohibido navegar desde `Branch` al objeto `Establishment` directamente; usar `EstablishmentId`.
- Prohibido incluir colecciones de `Branch` dentro del agregado `Establishment`.
- La consistencia entre agregados debe garantizarse mediante eventos de dominio o validaciones a nivel de aplicación, no mediante transacciones distribuidas.
- Cualquier cambio en esta relación debe documentarse en un nuevo ADR o en una actualización de este registro.

## Consecuencias

- Gestión de ciclo de vida independiente para cada agregado.
- Mejor rendimiento: las operaciones sobre una sede no cargan datos del establecimiento completo.
- Consultas que requieran datos combinados de `Establishment` y `Branch` deben resolverse a nivel de aplicación o mediante modelos de lectura dedicados.
- La consistencia entre agregados requiere mayor disciplina: no existe enforcement automático por transacciones de base de datos entre raíces de agregado.
