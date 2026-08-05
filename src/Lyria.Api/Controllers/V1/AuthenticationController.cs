using Lyria.Api.Extensions;
using Lyria.Application.Common.Results;
using Lyria.Application.Features.Authentication;
using Lyria.Application.Features.Authentication.Login;
using Lyria.Application.Features.Authentication.Logout;
using Lyria.Application.Features.Authentication.Refresh;
using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Lyria.Api.Controllers.V1;

/// <summary>
/// Autenticación de usuarios de la aplicación móvil.
/// </summary>
/// <remarks>
/// Los tres endpoints son anónimos por definición: el cliente aún no dispone de un
/// access token cuando los invoca. El inicio de sesión y la renovación están sujetos
/// a limitación de solicitudes.
///
/// Las credenciales y los tokens viajan en el cuerpo de la solicitud, por lo que la
/// API debe exponerse siempre sobre HTTPS.
/// </remarks>
[ApiController]
[Route("api/v1/auth")]
[Produces("application/json")]
[Tags("Autenticación")]
[AllowAnonymous]
public sealed class AuthenticationController(IMediator mediator) : ControllerBase
{
    /// <summary>
    /// Inicia sesión con correo y contraseña.
    /// </summary>
    /// <param name="request">Credenciales del usuario.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Par de tokens y perfil mínimo del usuario.</returns>
    /// <remarks>
    /// El refresh token se devuelve en texto plano una única vez: el servidor solo
    /// conserva su hash y no puede volver a mostrarlo. La aplicación móvil debe
    /// guardarlo en almacenamiento seguro del dispositivo.
    ///
    /// Un correo inexistente, una contraseña incorrecta o una cuenta no habilitada
    /// producen exactamente la misma respuesta 401, sin indicar la causa.
    /// </remarks>
    /// <response code="200">Inicio de sesión correcto.</response>
    /// <response code="400">Datos de entrada inválidos.</response>
    /// <response code="401">El correo o la contraseña no son válidos.</response>
    /// <response code="429">Se superó el límite de intentos permitidos.</response>
    [HttpPost("login")]
    [Consumes("application/json")]
    [EnableRateLimiting(RateLimitingExtensions.AuthenticationPolicyName)]
    [ProducesResponseType<AuthenticationResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var command = new LoginCommand(request.Email, request.Password);

        Result<AuthenticationResponse> result =
            await mediator.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return result.ToProblemResult();
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Renueva el par de tokens rotando el refresh token.
    /// </summary>
    /// <param name="request">Refresh token vigente.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Par de tokens nuevo.</returns>
    /// <remarks>
    /// La rotación revoca el refresh token presentado y emite uno nuevo en la misma
    /// transacción. El token anterior queda inutilizable de forma inmediata: volver a
    /// enviarlo produce 401.
    /// </remarks>
    /// <response code="200">Renovación correcta.</response>
    /// <response code="400">Datos de entrada inválidos.</response>
    /// <response code="401">La sesión no es válida o ha expirado.</response>
    /// <response code="429">Se superó el límite de intentos permitidos.</response>
    [HttpPost("refresh")]
    [Consumes("application/json")]
    [EnableRateLimiting(RateLimitingExtensions.AuthenticationPolicyName)]
    [ProducesResponseType<AuthenticationResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Refresh(
        [FromBody] RefreshRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var command = new RefreshAuthenticationCommand(request.RefreshToken);

        Result<AuthenticationResponse> result =
            await mediator.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return result.ToProblemResult();
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Cierra la sesión revocando el refresh token indicado.
    /// </summary>
    /// <param name="request">Refresh token de la sesión a cerrar.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <remarks>
    /// La operación es idempotente: un token inexistente o ya revocado devuelve
    /// igualmente 204, sin revelar si la sesión existía. La revocación es lógica y
    /// conserva el historial de la sesión.
    /// </remarks>
    /// <response code="204">Sesión cerrada.</response>
    /// <response code="400">Datos de entrada inválidos.</response>
    [HttpPost("logout")]
    [Consumes("application/json")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Logout(
        [FromBody] LogoutRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var command = new LogoutCommand(request.RefreshToken);

        Result result = await mediator.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return result.ToProblemResult();
        }

        return NoContent();
    }
}

/// <summary>
/// Credenciales de inicio de sesión.
/// </summary>
/// <param name="Email">Correo electrónico de la cuenta.</param>
/// <param name="Password">
/// Contraseña en claro. Nunca se almacena ni se registra en logs.
/// </param>
public sealed record LoginRequest(string Email, string Password);

/// <summary>
/// Solicitud de renovación de tokens.
/// </summary>
/// <param name="RefreshToken">Refresh token opaco vigente.</param>
public sealed record RefreshRequest(string RefreshToken);

/// <summary>
/// Solicitud de cierre de sesión.
/// </summary>
/// <param name="RefreshToken">Refresh token opaco de la sesión a cerrar.</param>
public sealed record LogoutRequest(string RefreshToken);
