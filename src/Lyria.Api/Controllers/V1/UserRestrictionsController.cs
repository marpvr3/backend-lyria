using Lyria.Api.Extensions;
using Lyria.Application.Common.Results;
using Lyria.Application.Features.UserRestrictions;
using Lyria.Application.Features.UserRestrictions.Assign;
using Lyria.Application.Features.UserRestrictions.GetById;
using Lyria.Application.Features.UserRestrictions.GetByUserId;
using Lyria.Application.Features.UserRestrictions.Remove;
using Lyria.Application.Features.UserRestrictions.Update;
using Mediator;
using Microsoft.AspNetCore.Mvc;

namespace Lyria.Api.Controllers.V1;

/// <summary>
/// Gestión de las restricciones alimentarias asociadas a un usuario.
/// </summary>
[ApiController]
[Route("api/v1/users/{userId:guid}/restrictions")]
[Produces("application/json")]
[Tags("Restricciones de Usuario")]
public sealed class UserRestrictionsController(IMediator mediator) : ControllerBase
{
    /// <summary>
    /// Obtiene las restricciones asociadas a un usuario.
    /// </summary>
    /// <param name="userId">Identificador único del usuario.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Lista de restricciones del usuario.</returns>
    /// <response code="200">Lista de restricciones del usuario. Puede estar vacía.</response>
    /// <response code="400">Identificador con formato inválido.</response>
    /// <response code="404">No se encontró el usuario.</response>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<UserRestrictionResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByUserId(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var query = new GetUserRestrictionsQuery(userId);

        Result<IReadOnlyList<UserRestrictionResponse>> result =
            await mediator.Send(query, cancellationToken);

        return result.IsSuccess
            ? Ok(result.Value)
            : result.ToProblemResult();
    }

    /// <summary>
    /// Obtiene una restricción específica asociada a un usuario.
    /// </summary>
    /// <param name="userId">Identificador único del usuario.</param>
    /// <param name="restrictionId">Identificador único de la restricción.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Detalle de la asociación entre el usuario y la restricción.</returns>
    /// <response code="200">Detalle de la restricción del usuario.</response>
    /// <response code="400">Datos de entrada inválidos.</response>
    /// <response code="404">No se encontró la asociación entre el usuario y la restricción.</response>
    [HttpGet("{restrictionId:guid}")]
    [ProducesResponseType<UserRestrictionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(
        Guid userId,
        Guid restrictionId,
        CancellationToken cancellationToken)
    {
        var query = new GetUserRestrictionByIdQuery(userId, restrictionId);

        Result<UserRestrictionResponse> result = await mediator.Send(query, cancellationToken);

        return result.IsSuccess
            ? Ok(result.Value)
            : result.ToProblemResult();
    }

    /// <summary>
    /// Asigna una restricción a un usuario.
    /// </summary>
    /// <param name="userId">Identificador único del usuario.</param>
    /// <param name="request">Datos de la restricción a asignar.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Ubicación de la asociación creada.</returns>
    /// <response code="201">Restricción asignada correctamente.</response>
    /// <response code="400">Datos de entrada inválidos.</response>
    /// <response code="404">No se encontró el usuario o la restricción.</response>
    /// <response code="409">El usuario ya tiene asignada esta restricción.</response>
    [HttpPost]
    [Consumes("application/json")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Assign(
        Guid userId,
        [FromBody] AssignRestrictionToUserRequest request,
        CancellationToken cancellationToken)
    {
        var command = new AssignRestrictionToUserCommand(
            userId,
            request.RestrictionId,
            request.ImportanceLevel);

        Result result = await mediator.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return result.ToProblemResult();
        }

        return CreatedAtAction(
            nameof(GetById),
            new { userId, restrictionId = request.RestrictionId },
            null);
    }

    /// <summary>
    /// Actualiza el nivel de importancia de una restricción asociada a un usuario.
    /// </summary>
    /// <param name="userId">Identificador único del usuario.</param>
    /// <param name="restrictionId">Identificador único de la restricción.</param>
    /// <param name="request">Nuevo nivel de importancia.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <remarks>
    /// Solo se modifica el nivel de importancia. La fecha de creación se conserva.
    /// </remarks>
    /// <response code="204">Nivel de importancia actualizado correctamente.</response>
    /// <response code="400">Datos de entrada inválidos.</response>
    /// <response code="404">No se encontró la asociación entre el usuario y la restricción.</response>
    [HttpPut("{restrictionId:guid}")]
    [Consumes("application/json")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        Guid userId,
        Guid restrictionId,
        [FromBody] UpdateUserRestrictionRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateUserRestrictionCommand(
            userId,
            restrictionId,
            request.ImportanceLevel);

        Result result = await mediator.Send(command, cancellationToken);

        return result.IsSuccess
            ? NoContent()
            : result.ToProblemResult();
    }

    /// <summary>
    /// Elimina la asociación entre un usuario y una restricción.
    /// </summary>
    /// <param name="userId">Identificador único del usuario.</param>
    /// <param name="restrictionId">Identificador único de la restricción.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <remarks>
    /// Solo se elimina la asociación. El usuario y la restricción maestra se conservan.
    /// </remarks>
    /// <response code="204">Asociación eliminada correctamente.</response>
    /// <response code="400">Datos de entrada inválidos.</response>
    /// <response code="404">No se encontró la asociación entre el usuario y la restricción.</response>
    [HttpDelete("{restrictionId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Remove(
        Guid userId,
        Guid restrictionId,
        CancellationToken cancellationToken)
    {
        var command = new RemoveRestrictionFromUserCommand(userId, restrictionId);

        Result result = await mediator.Send(command, cancellationToken);

        return result.IsSuccess
            ? NoContent()
            : result.ToProblemResult();
    }
}

/// <summary>
/// Datos para asignar una restricción a un usuario.
/// </summary>
/// <param name="RestrictionId">Identificador de la restricción a asignar.</param>
/// <param name="ImportanceLevel">Nivel de importancia: Low, Medium o High.</param>
public sealed record AssignRestrictionToUserRequest(
    Guid RestrictionId,
    string ImportanceLevel);

/// <summary>
/// Datos para actualizar el nivel de importancia de una restricción de un usuario.
/// </summary>
/// <param name="ImportanceLevel">Nivel de importancia: Low, Medium o High.</param>
public sealed record UpdateUserRestrictionRequest(string ImportanceLevel);
