# ADR-003: ASP.NET Core Controllers

## Estado

Aceptada

## Contexto

ASP.NET Core ofrece dos modelos para construir APIs HTTP: Controllers y Minimal APIs. Se necesita definir cuál se utilizará en Lyria.

## Decisión

Se utilizan exclusivamente **ASP.NET Core Controllers** para todos los endpoints HTTP.

Configuración:
- `AddControllers()` para el registro de servicios.
- `MapControllers()` para el mapeo de rutas.

Queda **prohibido** el uso de:
- `MapGet`, `MapPost`, `MapPut`, `MapDelete`, `MapGroup`
- Cualquier otro patrón de Minimal API

## Razones

1. Los controllers proporcionan una estructura organizada y convencional para APIs.
2. Facilitan la separación de responsabilidades con atributos como `[ApiController]`.
3. Ofrecen soporte maduro para filtros, model binding y validación.
4. El equipo tiene experiencia con el patrón de controllers.

## Complementos configurados

- **Problem Details**: manejo estandarizado de errores HTTP (RFC 9457).
- **OpenAPI**: documentación automática de la API.
- **Scalar**: interfaz visual para explorar la API.

## Consecuencias

- Todos los endpoints deben definirse en clases que hereden de `ControllerBase`.
- Los controllers deben residir en el proyecto `Lyria.Api`.
- AutoMapper está prohibido; se prefiere mapeo explícito.
