# ADR-009: Estrategia inicial de búsqueda

## Estado

Aceptado

## Contexto

Lyria necesita capacidades de búsqueda para que los usuarios encuentren establecimientos según criterios como nombre, ubicación, tipo de cocina y adecuación dietética. La decisión principal es si adoptar desde el inicio un motor de búsqueda especializado (como Elasticsearch o Azure Cognitive Search) o utilizar SQL Server como motor de búsqueda en la etapa de MVP.

Los motores de búsqueda especializados ofrecen mejor relevancia, búsqueda de texto completo avanzada y escalabilidad horizontal, pero introducen complejidad operacional significativa: un componente adicional para desplegar, monitorear, mantener en sincronía y operar.

## Decisión

**SQL Server es el motor de búsqueda inicial de Lyria.**

Las decisiones específicas son:

- Las búsquedas se implementan mediante queries SQL optimizados con proyecciones de lectura.
- La paginación es **obligatoria** en todas las consultas de búsqueda; no se permiten consultas sin límite.
- No se agrega Elasticsearch ni ningún motor de búsqueda especializado en el MVP.
- Se usan las funciones geográficas nativas de SQL Server (`geography`, `STDistance`) para el cálculo de distancia en búsquedas por proximidad.
- La evolución hacia un motor especializado dependerá de evidencia concreta de volumen, latencia y relevancia insuficiente.

## Razones

1. SQL Server ya es parte de la infraestructura de Lyria (ADR-004); no agregar un componente adicional reduce la complejidad operacional.
2. En la escala del MVP, SQL Server puede manejar los volúmenes de búsqueda esperados con índices adecuados.
3. Las funciones geográficas de SQL Server son suficientes para búsquedas por proximidad sin dependencias externas.
4. Posponer Elasticsearch elimina la necesidad de mantener sincronización entre la base de datos principal y el índice de búsqueda.
5. La complejidad de relevancia avanzada (scoring, facetas, sugerencias) no está justificada hasta demostrar que los usuarios la necesitan.

## Alternativas consideradas

- **Elasticsearch**: mayor relevancia y búsqueda de texto completo, pero requiere infraestructura adicional y sincronización de datos. Descartado para el MVP.
- **Azure Cognitive Search**: servicio gestionado que reduce la operación, pero introduce costo y acoplamiento a una plataforma específica. Descartado para el MVP.
- **Full-text search de SQL Server**: puede usarse como complemento si se requiere búsqueda de texto completo básica dentro de SQL Server.

## Restricciones

- Toda consulta de búsqueda debe incluir paginación obligatoria (`OFFSET`/`FETCH` o equivalente en LINQ).
- Prohibido ejecutar queries de búsqueda sin proyección: siempre seleccionar solo los campos necesarios.
- Los índices necesarios para las consultas de búsqueda deben crearse mediante migraciones de EF Core.
- La decisión de migrar a un motor especializado debe documentarse en un nuevo ADR con evidencia de métricas reales.

## Consecuencias

- Infraestructura más simple: un único componente de datos para operación y búsqueda.
- Sin costo operacional adicional por motor de búsqueda en el MVP.
- Búsqueda de texto completo y scoring de relevancia son limitados respecto a motores especializados.
- Las búsquedas muy complejas (sinónimos, corrección ortográfica, ranking semántico) no son viables con esta estrategia.
- Aceptable para la escala del MVP; la evolución queda abierta con base en evidencia.
