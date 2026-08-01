using Lyria.Api.Extensions;
using Lyria.Application.Common.Results;
using Lyria.Application.Features.UserRoles;
using Lyria.Application.Features.UserRoles.Assign;
using Lyria.Application.Features.UserRoles.Finalize;
using Lyria.Application.Features.UserRoles.GetByUserId;
using Lyria.Application.Features.UserRoles.SetActiveStatus;
using Lyria.Domain.Users.UserRoles;
using Mediator;
using Microsoft.AspNetCore.Mvc;

namespace Lyria.Api.Controllers.V1;

/// <summary>
/// Gestión de roles asignados a usuarios.
/// </summary>
[ApiController]
[Route("api/v1/users/{userId:guid}/roles")]
[Produces("application/json")]
[Tags("Roles de Usuario")]
public sealed class UserRolesController(IMediator mediator) : ControllerBase
{
    /// <summary>
    /// Obtiene los roles asignados a un usuario.
    /// </summary>
    /// <param name="userId">Identificador único del usuario.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Lista de roles asignados al usuario.</returns>
    /// <response code="200">Lista de roles del usuario.</response>
    /// <response code="400">Identificador con formato inválido.</response>
    /// <response code="404">No se encontró el usuario.</response>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<UserRoleResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByUserId(Guid userId, CancellationToken cancellationToken)
    {
        var query = new GetUserRolesQuery(userId);

        Result<IReadOnlyList<UserRoleResponse>> result =
            await mediator.Send(query, cancellationToken);

        return result.IsSuccess
            ? Ok(result.Value)
            : result.ToProblemResult();
    }

    /// <summary>
    /// Asigna un rol a un usuario.
    /// </summary>
    /// <param name="userId">Identificador único del usuario.</param>
    /// <param name="request">Datos de la asignación de rol.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Identificador de la asignación creada.</returns>
    /// <response code="201">Rol asignado correctamente.</response>
    /// <response code="400">Datos de entrada inválidos.</response>
    /// <response code="404">No se encontró el usuario o el rol.</response>
    /// <response code="409">El usuario ya tiene asignado este rol con el mismo alcance.</response>
    [HttpPost]
    [Consumes("application/json")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Assign(
        Guid userId,
        [FromBody] AssignRoleToUserRequest request,
        CancellationToken cancellationToken)
    {
        var command = new AssignRoleToUserCommand(
            userId,
            request.RoleId,
            request.ScopeType,
            request.EstablishmentId,
            request.BranchId);

        Result<UserRoleId> result = await mediator.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return result.ToProblemResult();
        }

        return CreatedAtAction(
            nameof(GetByUserId),
            new { userId },
            new { id = result.Value.Value });
    }

    /// <summary>
    /// Activa o desactiva la asignación de un rol a un usuario.
    /// </summary>
    /// <param name="userId">Identificador único del usuario.</param>
    /// <param name="userRoleId">Identificador único de la asignación de rol.</param>
    /// <param name="request">Estado deseado (activo o inactivo).</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <remarks>
    /// La operación es idempotente: activar una asignación ya activa no produce error.
    /// </remarks>
    /// <response code="204">Estado actualizado correctamente.</response>
    /// <response code="400">Datos de entrada inválidos.</response>
    /// <response code="404">No se encontró la asignación de rol.</response>
    [HttpPatch("{userRoleId:guid}/active-status")]
    [Consumes("application/json")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetActiveStatus(
        Guid userId,
        Guid userRoleId,
        [FromBody] SetUserRoleActiveStatusRequest request,
        CancellationToken cancellationToken)
    {
        var command = new SetUserRoleActiveStatusCommand(userId, userRoleId, request.IsActive);

        Result result = await mediator.Send(command, cancellationToken);

        return result.IsSuccess
            ? NoContent()
            : result.ToProblemResult();
    }

    /// <summary>
    /// Finaliza la asignación de un rol a un usuario.
    /// </summary>
    /// <param name="userId">Identificador único del usuario.</param>
    /// <param name="userRoleId">Identificador único de la asignación de rol.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <remarks>
    /// Marca la asignación como finalizada registrando la fecha de finalización.
    /// Una vez finalizada, la asignación no puede reactivarse.
    /// </remarks>
    /// <response code="204">Asignación finalizada correctamente.</response>
    /// <response code="400">Datos de entrada inválidos.</response>
    /// <response code="404">No se encontró la asignación de rol.</response>
    [HttpPatch("{userRoleId:guid}/finalize")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Finalize(
        Guid userId,
        Guid userRoleId,
        CancellationToken cancellationToken)
    {
        var command = new FinalizeUserRoleCommand(userId, userRoleId);

        Result result = await mediator.Send(command, cancellationToken);

        return result.IsSuccess
            ? NoContent()
            : result.ToProblemResult();
    }
}

/// <summary>
/// Datos para asignar un rol a un usuario.
/// </summary>
/// <param name="RoleId">Identificador del rol a asignar.</param>
/// <param name="ScopeType">Tipo de alcance de la asignación.</param>
/// <param name="EstablishmentId">Identificador del establecimiento asociado al alcance.</param>
/// <param name="BranchId">Identificador de la sede asociada al alcance.</param>
public sealed record AssignRoleToUserRequest(
    Guid RoleId,
    string ScopeType,
    Guid? EstablishmentId,
    Guid? BranchId);

/// <summary>
/// Datos para activar o desactivar la asignación de un rol.
/// </summary>
/// <param name="IsActive">true para activar, false para desactivar.</param>
public sealed record SetUserRoleActiveStatusRequest(bool IsActive);
