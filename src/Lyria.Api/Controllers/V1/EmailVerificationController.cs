using Lyria.Api.Extensions;
using Lyria.Application.Common.Results;
using Lyria.Application.Features.EmailVerifications;
using Lyria.Application.Features.EmailVerifications.Confirm;
using Lyria.Application.Features.EmailVerifications.Resend;
using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Lyria.Api.Controllers.V1;

/// <summary>
/// Verificación del correo electrónico de las cuentas registradas desde la aplicación móvil.
/// </summary>
/// <remarks>
/// Los dos endpoints son anónimos por definición: la cuenta aún no puede iniciar sesión
/// mientras su correo no esté verificado, de modo que el cliente no dispone de un access
/// token cuando los invoca. Ambos están sujetos a limitación de solicitudes.
///
/// El envío inicial del código forma parte del registro móvil
/// (<c>POST /api/v1/mobile/registrations</c>) y no tiene endpoint propio.
///
/// El código viaja en el cuerpo de la solicitud, por lo que la API debe exponerse siempre
/// sobre HTTPS.
/// </remarks>
[ApiController]
[Route("api/v1/auth/email-verification")]
[Produces("application/json")]
[Tags("Verificación de Correo")]
[AllowAnonymous]
public sealed class EmailVerificationController(IMediator mediator) : ControllerBase
{
    /// <summary>
    /// Solicita un nuevo código de verificación.
    /// </summary>
    /// <param name="request">Correo electrónico de la cuenta.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Mensaje genérico de confirmación de la solicitud.</returns>
    /// <remarks>
    /// La respuesta es siempre 202 con el mismo mensaje, exista o no la cuenta y sea cual
    /// sea su estado: no revela si el correo está registrado. Solo una cuenta pendiente de
    /// verificación recibe realmente un código nuevo, y solo si transcurrió el intervalo
    /// mínimo desde el último envío.
    ///
    /// Emitir un código invalida los anteriores. Un fallo del proveedor de correo no
    /// altera esta respuesta: la cuenta permanece pendiente y puede volver a solicitarlo.
    /// </remarks>
    /// <response code="202">Solicitud aceptada.</response>
    /// <response code="400">Datos de entrada inválidos.</response>
    /// <response code="429">Se superó el límite de solicitudes permitidas.</response>
    /// <response code="500">Error interno del servidor.</response>
    [HttpPost("resend")]
    [Consumes("application/json")]
    [EnableRateLimiting(RateLimitingExtensions.EmailVerificationResendPolicyName)]
    [ProducesResponseType<EmailVerificationResendResponse>(StatusCodes.Status202Accepted)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Resend(
        [FromBody] ResendEmailVerificationRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var command = new ResendEmailVerificationCommand(request.Email);

        Result<EmailVerificationResendResponse> result =
            await mediator.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return result.ToProblemResult();
        }

        return Accepted(result.Value);
    }

    /// <summary>
    /// Confirma el correo electrónico con el código recibido.
    /// </summary>
    /// <param name="request">Correo de la cuenta y código de seis dígitos.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <remarks>
    /// Al confirmar, la cuenta pasa de <c>Unverified</c> a <c>Active</c> y queda habilitada
    /// para iniciar sesión. El código es de un solo uso y se marca como usado en la misma
    /// transacción.
    ///
    /// Todos los rechazos devuelven exactamente la misma respuesta 400 con el código
    /// <c>EmailVerification.InvalidCode</c>: un correo inexistente, un código incorrecto,
    /// vencido, revocado o ya usado, los intentos agotados y una cuenta ya verificada son
    /// indistinguibles entre sí.
    /// </remarks>
    /// <response code="204">Correo verificado y cuenta activada.</response>
    /// <response code="400">El código de verificación no es válido o ha vencido.</response>
    /// <response code="429">Se superó el límite de solicitudes permitidas.</response>
    /// <response code="500">Error interno del servidor.</response>
    [HttpPost("confirm")]
    [Consumes("application/json")]
    [EnableRateLimiting(RateLimitingExtensions.EmailVerificationConfirmPolicyName)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Confirm(
        [FromBody] ConfirmEmailVerificationRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var command = new ConfirmEmailVerificationCommand(request.Email, request.Code);

        Result result = await mediator.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return result.ToProblemResult();
        }

        return NoContent();
    }
}

/// <summary>
/// Solicitud de reenvío de código de verificación.
/// </summary>
/// <param name="Email">Correo electrónico de la cuenta.</param>
public sealed record ResendEmailVerificationRequest(string Email);

/// <summary>
/// Solicitud de confirmación de correo electrónico.
/// </summary>
/// <param name="Email">Correo electrónico de la cuenta.</param>
/// <param name="Code">
/// Código de seis dígitos recibido por correo. Puede comenzar por cero. Nunca se
/// almacena en claro ni se registra en logs.
/// </param>
public sealed record ConfirmEmailVerificationRequest(string Email, string Code);
