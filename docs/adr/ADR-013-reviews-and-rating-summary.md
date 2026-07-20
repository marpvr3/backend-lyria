# ADR-013: Estrategia de reseñas y resumen de rating

## Estado

Aceptado

## Contexto

Lyria permite a los usuarios escribir reseñas sobre sedes, incluyendo una calificación numérica. Es necesario definir el ciclo de vida de las reseñas, la estrategia para calcular y mantener el resumen de rating, y el modelo de moderación aplicable en la fase MVP.

Las principales alternativas para el resumen de rating son: calcularlo en cada consulta (costoso a escala), mantener columnas actualizadas manualmente (propenso a inconsistencias) o mantener una proyección/modelo de lectura actualizada mediante eventos (consistente y eficiente).

## Decisión

Se adopta la siguiente estrategia para reseñas y resumen de rating:

- **Una reseña activa por usuario por sede**: un usuario solo puede tener una reseña activa para cada sede en un momento dado.
- **Calificación entera entre 1 y 5**: sin decimales ni medias estrellas.
- **Solo las reseñas publicadas afectan las estadísticas**: las reseñas pendientes, rechazadas, ocultas o eliminadas no se computan en el resumen.
- **`ReviewSummary` como proyección**: el promedio de calificación y el total de reseñas son datos derivados. La fuente de verdad son las reseñas publicadas. Se mantiene una proyección (`ReviewSummary`) actualizada cuando el estado de una reseña cambia, evitando cálculos en cada consulta.
- **Estados del ciclo de vida de una reseña**:
  - `Pending`: recién creada, pendiente de publicación.
  - `Published`: visible públicamente y computable en estadísticas.
  - `Rejected`: rechazada por moderación; no visible ni computable.
  - `Hidden`: ocultada por moderación tras reportes; no visible pero conservada.
  - `DeletedByAuthor`: eliminada por el propio autor; no visible ni computable.
- **Moderación en MVP**: publicación inmediata con moderación posterior activada por reportes. No se aplica pre-moderación automática en esta fase.
- Los administradores no pueden reescribir ni editar el contenido de las reseñas de los usuarios.
- Los moderadores pueden ocultar o rechazar reseñas, pero no pueden modificar la autoría ni el texto original.
- Una reseña reportada no se elimina automáticamente; requiere acción explícita de un moderador.

## Razones

1. La publicación inmediata mejora la experiencia del usuario y es aceptable a escala MVP donde el volumen de reseñas fraudulentas es bajo.
2. La proyección `ReviewSummary` equilibra consistencia y rendimiento: evita recalcular el promedio en cada consulta sin depender de datos inconsistentes.
3. Los estados bien definidos permiten auditar el historial de moderación sin perder información.
4. Prohibir la edición de reseñas por administradores protege la integridad y confianza de los usuarios en la plataforma.

## Restricciones

- Prohibido que un usuario tenga más de una reseña activa (`Published` o `Pending`) por sede.
- Prohibido incluir reseñas en estados distintos a `Published` en el cálculo del `ReviewSummary`.
- Prohibido que administradores o moderadores modifiquen el texto o la calificación de una reseña existente.
- La calificación debe ser un entero en el rango [1, 5]; valores fuera de ese rango deben rechazarse en el dominio.
- Cualquier cambio en la estrategia de moderación (por ejemplo, activar pre-moderación) debe documentarse en un nuevo ADR o en una actualización de este registro.

## Consecuencias

- Buena experiencia de usuario: las reseñas aparecen inmediatamente sin esperar aprobación manual.
- Riesgo aceptable en MVP: la moderación reactiva es suficiente para el volumen inicial de contenido.
- El `ReviewSummary` como proyección requiere que cualquier cambio de estado en una reseña desencadene su actualización; esto debe gestionarse mediante eventos de dominio o lógica de aplicación.
- La estrategia de datos derivados evita inconsistencias entre contadores manuales y la realidad de las reseñas publicadas.
- En fases posteriores, si el volumen de contenido inadecuado aumenta, se puede activar pre-moderación sin cambiar el modelo de estados existente.
