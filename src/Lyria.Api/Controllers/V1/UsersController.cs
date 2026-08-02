using Lyria.Api.Extensions;
using Lyria.Application.Common;
using Lyria.Application.Common.Results;
using Lyria.Application.Features.Users;
using Lyria.Application.Features.Users.ChangeEmail;
using Lyria.Application.Features.Users.ChangePassword;
using Lyria.Application.Features.Users.ChangeStatus;
using Lyria.Application.Features.Users.Create;
using Lyria.Application.Features.Users.GetById;
using Lyria.Application.Features.Users.List;
using Lyria.Application.Features.Users.Update;
using Lyria.Domain.Users;
using Mediator;
using Microsoft.AspNetCore.Mvc;

namespace Lyria.Api.Controllers.V1;

/// <summary>
/// Gestión de usuarios.
/// </summary>
[ApiController]
[Route("api/v1/users")]
[Produces("application/json")]
[Tags("Usuarios")]
public sealed class UsersController(IMediator mediator) : ControllerBase
{
    /// <summary>
    /// Crea un nuevo usuario.
    /// </summary>
    /// <param name="request">Datos del usuario a crear.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Identificador del usuario creado.</returns>
    /// <response code="201">Usuario creado correctamente.</response>
    /// <response code="400">Datos de entrada inválidos.</response>
    /// <response code="409">Ya existe un usuario con el mismo correo electrónico.</response>
    [HttpPost]
    [Consumes("application/json")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(
        [FromBody] CreateUserRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateUserCommand(
            request.Name,
            request.LastName,
            request.Email,
            request.Password,
            request.Phone,
            request.BirthDate,
            request.PhotoUrl);

        Result<UserId> result = await mediator.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return result.ToProblemResult();
        }

        return CreatedAtAction(
            nameof(GetById),
            new { userId = result.Value.Value },
            new { id = result.Value.Value });
    }

    /// <summary>
    /// Lista usuarios con filtros y paginación.
    /// </summary>
    /// <param name="search">Texto libre para buscar por nombre o apellido.</param>
    /// <param name="email">Filtrar por correo electrónico.</param>
    /// <param name="status">Filtrar por estado del usuario.</param>
    /// <param name="emailVerified">Filtrar por estado de verificación de correo.</param>
    /// <param name="page">Número de página (por defecto 1).</param>
    /// <param name="pageSize">Cantidad de elementos por página (por defecto 20).</param>
    /// <param name="sortBy">Campo por el cual ordenar.</param>
    /// <param name="sortDirection">Dirección del ordenamiento (asc o desc).</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Lista paginada de usuarios.</returns>
    /// <response code="200">Lista paginada de usuarios.</response>
    /// <response code="400">Parámetros de consulta inválidos.</response>
    [HttpGet]
    [ProducesResponseType<PagedResponse<UserListItemResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> List(
        [FromQuery] string? search,
        [FromQuery] string? email,
        [FromQuery] string? status,
        [FromQuery] bool? emailVerified,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? sortBy = null,
        [FromQuery] string? sortDirection = null,
        CancellationToken cancellationToken = default)
    {
        var query = new GetUsersQuery(
            search, email, status, emailVerified, page, pageSize, sortBy, sortDirection);

        PagedResponse<UserListItemResponse> result =
            await mediator.Send(query, cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Obtiene un usuario por su identificador.
    /// </summary>
    /// <param name="userId">Identificador único del usuario.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Detalle completo del usuario.</returns>
    /// <response code="200">Usuario encontrado.</response>
    /// <response code="400">Identificador con formato inválido.</response>
    /// <response code="404">No se encontró el usuario.</response>
    [HttpGet("{userId:guid}")]
    [ProducesResponseType<UserResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid userId, CancellationToken cancellationToken)
    {
        var query = new GetUserByIdQuery(userId);

        Result<UserResponse> result = await mediator.Send(query, cancellationToken);

        return result.IsSuccess
            ? Ok(result.Value)
            : result.ToProblemResult();
    }

    /// <summary>
    /// Actualiza el perfil de un usuario.
    /// </summary>
    /// <param name="userId">Identificador único del usuario.</param>
    /// <param name="request">Datos actualizados del perfil.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <remarks>
    /// Reemplaza todos los campos editables del perfil del usuario.
    /// No modifica correo electrónico, contraseña ni estado.
    /// </remarks>
    /// <response code="204">Perfil actualizado correctamente.</response>
    /// <response code="400">Datos de entrada inválidos.</response>
    /// <response code="404">No se encontró el usuario.</response>
    [HttpPut("{userId:guid}")]
    [Consumes("application/json")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        Guid userId,
        [FromBody] UpdateUserRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateUserCommand(
            userId,
            request.Name,
            request.LastName,
            request.Phone,
            request.BirthDate,
            request.PhotoUrl);

        Result result = await mediator.Send(command, cancellationToken);

        return result.IsSuccess
            ? NoContent()
            : result.ToProblemResult();
    }

    /// <summary>
    /// Cambia el correo electrónico de un usuario.
    /// </summary>
    /// <param name="userId">Identificador único del usuario.</param>
    /// <param name="request">Nuevo correo electrónico.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <response code="204">Correo electrónico actualizado correctamente.</response>
    /// <response code="400">Datos de entrada inválidos.</response>
    /// <response code="404">No se encontró el usuario.</response>
    /// <response code="409">Ya existe un usuario con el mismo correo electrónico.</response>
    [HttpPatch("{userId:guid}/email")]
    [Consumes("application/json")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ChangeEmail(
        Guid userId,
        [FromBody] ChangeUserEmailRequest request,
        CancellationToken cancellationToken)
    {
        var command = new ChangeUserEmailCommand(userId, request.Email);

        Result result = await mediator.Send(command, cancellationToken);

        return result.IsSuccess
            ? NoContent()
            : result.ToProblemResult();
    }

    /// <summary>
    /// Cambia la contraseña de un usuario.
    /// </summary>
    /// <param name="userId">Identificador único del usuario.</param>
    /// <param name="request">Nueva contraseña.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <response code="204">Contraseña actualizada correctamente.</response>
    /// <response code="400">Datos de entrada inválidos.</response>
    /// <response code="404">No se encontró el usuario.</response>
    [HttpPatch("{userId:guid}/password")]
    [Consumes("application/json")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ChangePassword(
        Guid userId,
        [FromBody] ChangeUserPasswordRequest request,
        CancellationToken cancellationToken)
    {
        var command = new ChangeUserPasswordCommand(userId, request.Password);

        Result result = await mediator.Send(command, cancellationToken);

        return result.IsSuccess
            ? NoContent()
            : result.ToProblemResult();
    }

    /// <summary>
    /// Cambia el estado de un usuario.
    /// </summary>
    /// <param name="userId">Identificador único del usuario.</param>
    /// <param name="request">Nuevo estado del usuario.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <response code="204">Estado actualizado correctamente.</response>
    /// <response code="400">Datos de entrada inválidos.</response>
    /// <response code="404">No se encontró el usuario.</response>
    [HttpPatch("{userId:guid}/status")]
    [Consumes("application/json")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ChangeStatus(
        Guid userId,
        [FromBody] ChangeUserStatusRequest request,
        CancellationToken cancellationToken)
    {
        var command = new ChangeUserStatusCommand(userId, request.Status);

        Result result = await mediator.Send(command, cancellationToken);

        return result.IsSuccess
            ? NoContent()
            : result.ToProblemResult();
    }
}

/// <summary>
/// Datos para crear un usuario.
/// </summary>
/// <param name="Name">Nombre del usuario.</param>
/// <param name="LastName">Apellido del usuario.</param>
/// <param name="Email">Correo electrónico del usuario.</param>
/// <param name="Password">Contraseña del usuario.</param>
/// <param name="Phone">Teléfono de contacto del usuario.</param>
/// <param name="BirthDate">Fecha de nacimiento del usuario.</param>
/// <param name="PhotoUrl">URL de la foto de perfil del usuario.</param>
public sealed record CreateUserRequest(
    string Name,
    string LastName,
    string Email,
    string Password,
    string? Phone,
    DateOnly? BirthDate,
    string? PhotoUrl);

/// <summary>
/// Datos para actualizar el perfil de un usuario.
/// </summary>
/// <param name="Name">Nombre del usuario.</param>
/// <param name="LastName">Apellido del usuario.</param>
/// <param name="Phone">Teléfono de contacto del usuario.</param>
/// <param name="BirthDate">Fecha de nacimiento del usuario.</param>
/// <param name="PhotoUrl">URL de la foto de perfil del usuario.</param>
public sealed record UpdateUserRequest(
    string Name,
    string LastName,
    string? Phone,
    DateOnly? BirthDate,
    string? PhotoUrl);

/// <summary>
/// Datos para cambiar el correo electrónico de un usuario.
/// </summary>
/// <param name="Email">Nuevo correo electrónico.</param>
public sealed record ChangeUserEmailRequest(string Email);

/// <summary>
/// Datos para cambiar la contraseña de un usuario.
/// </summary>
/// <param name="Password">Nueva contraseña.</param>
public sealed record ChangeUserPasswordRequest(string Password);

/// <summary>
/// Datos para cambiar el estado de un usuario.
/// </summary>
/// <param name="Status">Nuevo estado del usuario.</param>
public sealed record ChangeUserStatusRequest(string Status);
