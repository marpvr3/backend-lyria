using Lyria.Api.Extensions;
using Lyria.Application.Common.Results;
using Lyria.Application.Features.MobileRegistrations;
using Lyria.Application.Features.MobileRegistrations.Register;
using Mediator;
using Microsoft.AspNetCore.Mvc;

namespace Lyria.Api.Controllers.V1;

/// <summary>
/// Registro de usuarios desde la aplicación móvil.
/// </summary>
/// <remarks>
/// Endpoint dedicado al flujo móvil. El backend asigna automáticamente el rol base
/// con alcance global y el nivel de importancia inicial de las restricciones.
/// El flujo administrativo de creación de usuarios (POST /api/v1/users) es independiente
/// y conserva su manejo actual de roles.
/// </remarks>
[ApiController]
[Route("api/v1/mobile/registrations")]
[Produces("application/json")]
[Tags("Registro Móvil")]
public sealed class MobileRegistrationsController(IMediator mediator) : ControllerBase
{
    /// <summary>
    /// Registra un usuario desde la aplicación móvil.
    /// </summary>
    /// <param name="request">Datos personales, contraseña y restricciones alimenticias.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Datos públicos del usuario registrado.</returns>
    /// <remarks>
    /// El usuario, su asignación de rol y sus restricciones alimenticias se confirman
    /// en una única transacción. Si cualquiera de las tres escrituras falla, no queda
    /// ningún registro parcial.
    ///
    /// La solicitud no admite roleId, passwordHash ni importanceLevel: el rol base,
    /// el estado inicial, el hash de la contraseña y el nivel de importancia los
    /// determina exclusivamente el backend.
    /// </remarks>
    /// <response code="201">Usuario registrado correctamente.</response>
    /// <response code="400">Datos de entrada inválidos o restricciones duplicadas.</response>
    /// <response code="404">Una restricción enviada no existe o no está disponible.</response>
    /// <response code="409">Ya existe un usuario con el mismo correo electrónico.</response>
    /// <response code="500">
    /// El registro móvil no está disponible por un problema de configuración interna.
    /// No es atribuible al cliente y no expone detalles internos.
    /// </response>
    [HttpPost]
    [Consumes("application/json")]
    [ProducesResponseType<MobileRegistrationResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Register(
        [FromBody] MobileRegistrationRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var command = new RegisterMobileUserCommand(
            request.Name,
            request.LastName,
            request.Email,
            request.Password,
            request.Phone,
            request.BirthDate,
            request.PhotoUrl,
            request.RestrictionIds ?? []);

        Result<MobileRegistrationResponse> result =
            await mediator.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return result.ToProblemResult();
        }

        return Created($"/api/v1/users/{result.Value.UserId}", result.Value);
    }
}

/// <summary>
/// Datos para registrar un usuario desde la aplicación móvil.
/// </summary>
/// <param name="Name">Nombre del usuario.</param>
/// <param name="LastName">Apellido del usuario.</param>
/// <param name="Email">Correo electrónico del usuario.</param>
/// <param name="Password">Contraseña en claro. Nunca se almacena ni se registra en logs.</param>
/// <param name="Phone">Teléfono de contacto del usuario.</param>
/// <param name="BirthDate">Fecha de nacimiento del usuario.</param>
/// <param name="PhotoUrl">URL de la foto de perfil del usuario.</param>
/// <param name="RestrictionIds">
/// Restricciones alimenticias seleccionadas del catálogo. Puede omitirse o ir vacía.
/// No se admiten identificadores vacíos ni duplicados.
/// </param>
public sealed record MobileRegistrationRequest(
    string Name,
    string LastName,
    string Email,
    string Password,
    string? Phone,
    DateOnly? BirthDate,
    string? PhotoUrl,
    IReadOnlyList<Guid>? RestrictionIds);
