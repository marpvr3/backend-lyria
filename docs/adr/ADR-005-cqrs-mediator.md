# ADR-005: CQRS y MediatR

## Estado

Superseded por [ADR-017](ADR-017-mediator-implementation.md)

> CQRS como patrón arquitectónico se mantiene vigente. La decisión diferida del mediator se resolvió en ADR-017 con la adopción de martinothamar/Mediator.

## Contexto

La arquitectura de Lyria contempla el uso de CQRS (Command Query Responsibility Segregation) como patrón para organizar los casos de uso. MediatR es una librería popular en el ecosistema .NET para implementar este patrón mediante el patrón Mediator.

## Decisiones

### CQRS es una decisión arquitectónica

CQRS se adopta como patrón arquitectónico para separar las operaciones de lectura (queries) y escritura (commands). Los casos de uso se organizarán por vertical slices dentro de `Lyria.Application/Features`.

### MediatR es una posible implementación, no el núcleo

MediatR es una herramienta que facilita la implementación de CQRS mediante despacho de mensajes, pero no es el único mecanismo posible. La arquitectura de Lyria no debe acoplarse a MediatR como dependencia fundamental.

### Consideraciones de licencia

Las versiones recientes de MediatR han introducido cambios en su modelo de licencia. Antes de adoptar MediatR en Lyria, se debe:

1. Revisar la licencia vigente de MediatR.
2. Determinar si es compatible con los requisitos legales del proyecto.
3. Evaluar alternativas si la licencia no es aplicable.

### Instalación diferida

La instalación de MediatR queda **diferida** hasta que se defina la licencia aplicable a Lyria.

No se debe:
- Instalar versiones antiguas de MediatR sin soporte para evitar la decisión de licencia.
- Ocultar o suprimir advertencias de licencia.
- Crear un mediator casero como sustituto temporal.

## Alternativas consideradas

Si MediatR no resulta viable por licencia:
- Implementar un mediator propio mínimo.
- Usar invocación directa de handlers sin mediator.
- Evaluar otras librerías de mediator compatibles.

## Consecuencias

- La estructura de `Application/Features` queda preparada para CQRS.
- No se puede implementar el despacho de comandos/queries hasta resolver la decisión de MediatR.
- Esta decisión debe revisarse antes de implementar el primer módulo de negocio.
