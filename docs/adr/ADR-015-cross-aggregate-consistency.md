# ADR-015: Consistencia entre agregados

## Estado

Aceptado

## Contexto

La documentación anterior establecía una regla absoluta: "todas las relaciones entre agregados usan consistencia eventual y domain events". Esto es demasiado restrictivo para un monolito modular que comparte una única base de datos, y no refleja las necesidades reales de Lyria.

## Decisión

### Dentro de un agregado

- Consistencia inmediata.
- Invariantes protegidos por el aggregate root.

### Entre agregados

- Coordinación desde la capa Application.
- Relaciones mediante identificadores.
- Validaciones mediante ports o queries específicas.
- Se puede usar una única transacción cuando existe una necesidad real de atomicidad Y la infraestructura es compartida (misma base de datos).
- Ejemplo: la creación de un `Branch` puede validar que el `Establishment` existe en la misma transacción.

### Consistencia eventual

Se usa ÚNICAMENTE cuando:

- El procesamiento secundario puede diferirse.
- Existe tolerancia temporal.
- Existe una ventaja real de desacoplamiento.
- El evento representa un hecho de negocio.

Ejemplo recomendado:

```
ReviewPublished → actualizar ReviewSummary
```

### Domain events

- NO se usan automáticamente para todas las operaciones.
- Representan hechos de negocio significativos.
- Se usan cuando el procesamiento posterior es genuinamente asíncrono o cuando múltiples módulos necesitan reaccionar.
- NO deben usarse como mecanismo para evitar llamadas directas a servicios dentro de la misma unidad de despliegue cuando se necesita consistencia inmediata.

## Razones

1. Lyria es un monolito modular con base de datos compartida; la consistencia eventual entre módulos en el mismo proceso añade complejidad sin beneficio cuando la atomicidad es simple.
2. Los domain events deben representar hechos reales de negocio, no ser un patrón adoptado por inercia.
3. La capa Application puede coordinar múltiples agregados dentro de una única unidad de trabajo cuando está justificado.

## Restricciones

- La consistencia fuerte dentro de los agregados no es negociable.
- La consistencia eventual debe justificarse explícitamente, no ser el valor por defecto.
- Las transacciones entre agregados deben documentarse y justificarse.

## Consecuencias

- Implementación más simple para operaciones que genuinamente necesitan atomicidad.
- Domain events reservados para eventos reales de negocio (`ReviewPublished`, `BranchApproved`, etc.).
- La capa Application tiene responsabilidad clara sobre la coordinación entre agregados.
