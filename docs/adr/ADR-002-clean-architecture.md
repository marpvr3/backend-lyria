# ADR-002: Clean Architecture

## Estado

Aceptada

## Contexto

Se necesita definir la estructura arquitectónica del backend de Lyria para asegurar separación de responsabilidades, testabilidad y mantenibilidad.

## Decisión

Se adopta **Clean Architecture** con cuatro capas:

1. **Domain**: lógica de negocio pura, sin dependencias externas.
2. **Application**: casos de uso y orquestación, depende solo de Domain.
3. **Infrastructure**: implementaciones técnicas (EF Core, SQL Server), depende de Application y Domain.
4. **Api**: punto de entrada HTTP, depende de Application e Infrastructure.

Se complementa con **DDD táctico** en la capa de dominio: entidades, aggregate roots, value objects, eventos de dominio y excepciones de dominio.

## Razones

1. La Dependency Rule asegura que la lógica de negocio no dependa de detalles de infraestructura.
2. Facilita las pruebas unitarias del dominio sin configurar infraestructura.
3. Permite reemplazar implementaciones de infraestructura sin afectar el dominio.
4. Las reglas de dependencia se validan automáticamente con pruebas arquitectónicas.

## Consecuencias

- Cada funcionalidad debe respetar las reglas de dependencia entre capas.
- Las abstracciones se definen en Application; las implementaciones en Infrastructure.
- Las pruebas arquitectónicas fallarán si se violan las reglas de dependencia.
