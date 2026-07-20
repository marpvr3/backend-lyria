# ADR-006: Monolito modular

## Estado

Aceptado

## Contexto

Lyria necesita una arquitectura que equilibre la simplicidad operacional con límites claros entre dominios funcionales. Las alternativas principales son el monolito tradicional, el monolito modular y los microservicios.

Un monolito tradicional no ofrece suficiente separación entre contextos y acumula deuda técnica a medida que el sistema crece. Los microservicios, por otro lado, introducen complejidad operacional significativa (orquestación, comunicación entre servicios, trazabilidad distribuida, consistencia eventual) que no está justificada en la etapa inicial del proyecto.

## Decisión

Lyria comienza como un **monolito modular**:

- No se adoptan microservicios en ninguna forma (incluidos mini-servicios o backends for frontends independientes).
- Los módulos tienen límites explícitos y deben respetarse como si fueran servicios separados.
- Cada módulo es propietario de sus datos y sus reglas de negocio.
- La comunicación entre módulos se realiza exclusivamente mediante identificadores, no mediante modelos compartidos.
- La extracción futura a microservicios requiere evidencia técnica y operacional que lo justifique.

## Razones

1. El monolito modular permite avanzar con rapidez sin sacrificar los límites funcionales necesarios para una futura extracción.
2. La complejidad operacional de los microservicios no está justificada sin datos de carga real.
3. Los límites bien definidos desde el inicio facilitan la evolución arquitectónica sin reescritura.
4. Un único artefacto de despliegue simplifica la infraestructura en la etapa de MVP.

## Restricciones

- Prohibido cruzar límites de módulo accediendo directamente a entidades o repositorios de otro módulo.
- Prohibido compartir modelos de dominio entre módulos distintos.
- Cualquier comunicación entre módulos debe ser explícita y controlada.
- La decisión de extraer un módulo a microservicio debe documentarse en un ADR y estar respaldada por evidencia.

## Consecuencias

- Despliegue más simple: un único proceso y una única base de datos.
- Infraestructura compartida: logging, autenticación, configuración centralizados.
- Se requiere disciplina de equipo para respetar los límites entre módulos sin enforcement técnico total.
- La refactorización hacia microservicios en el futuro es posible si se mantienen los límites desde el inicio.
