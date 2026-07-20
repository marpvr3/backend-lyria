# ADR-001: Uso de .NET 10 LTS

## Estado

Aceptada

## Contexto

Se necesita seleccionar la plataforma y versión del runtime para el backend de Lyria.

## Decisión

Se adopta **.NET 10 LTS** como plataforma objetivo con las siguientes configuraciones:

- Target framework: `net10.0`
- Lenguaje: C# 14
- SDK: 10.0.103 con `rollForward: latestPatch`
- Nullable reference types habilitado
- Implicit usings habilitado
- Warnings tratados como errores

## Razones

1. .NET 10 es la versión LTS más reciente, con soporte extendido.
2. C# 14 proporciona las características más modernas del lenguaje.
3. El SDK estable 10.0.103 está disponible y verificado.
4. Fijar `rollForward: latestPatch` permite actualizaciones de seguridad sin cambiar la versión menor.

## Restricciones

- No se permiten versiones preview, RC, alpha o beta de ningún paquete.
- No se debe migrar a .NET 11 preview.
- Todos los paquetes Microsoft deben mantenerse en la línea 10.x compatible.

## Consecuencias

- El equipo debe mantener el SDK .NET 10 actualizado con parches de seguridad.
- Los paquetes de terceros deben ser compatibles con `net10.0`.
