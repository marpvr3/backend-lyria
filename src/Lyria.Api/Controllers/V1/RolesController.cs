using Lyria.Api.Extensions;
using Lyria.Application.Common;
using Lyria.Application.Common.Results;
using Lyria.Application.Features.Roles;
using Lyria.Application.Features.Roles.Create;
using Lyria.Application.Features.Roles.GetById;
using Lyria.Application.Features.Roles.List;
using Lyria.Application.Features.Roles.SetActiveStatus;
using Lyria.Application.Features.Roles.Update;
using Lyria.Domain.Roles;
using Mediator;
using Microsoft.AspNetCore.Mvc;

namespace Lyria.Api.Controllers.V1;

/// <summary>
/// Gestión de roles.
/// </summary>
[ApiController]
[Route("api/v1/roles")]
[Produces("application/json")]
[Tags("Roles")]
public sealed class RolesController(IMediator mediator) : ControllerBase
{
    /// <summary>
    /// Crea un nuevo rol.
    /// </summary>
    /// <param name="request">Datos del rol a crear.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Identificador del rol creado.</returns>
    /// <response code="201">Rol creado correctamente.</response>
    /// <response code="400">Datos de entrada inválidos.</response>
    /// <response code="409">Ya existe un rol con el mismo código.</response>
    [HttpPost]
    [Consumes("application/json")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(
        [FromBody] CreateRoleRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateRoleCommand(
            request.Code,
            request.Name,
            request.Description);

        Result<RoleId> result = await mediator.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return result.ToProblemResult();
        }

        return CreatedAtAction(
            nameof(GetById),
            new { roleId = result.Value.Value },
            new { id = result.Value.Value });
    }

    /// <summary>
    /// Lista roles con filtros y paginación.
    /// </summary>
    /// <param name="search">Texto libre para buscar por nombre.</param>
    /// <param name="code">Filtrar por código del rol.</param>
    /// <param name="isActive">Filtrar por estado activo/inactivo.</param>
    /// <param name="page">Número de página (por defecto 1).</param>
    /// <param name="pageSize">Cantidad de elementos por página (por defecto 20).</param>
    /// <param name="sortBy">Campo por el cual ordenar.</param>
    /// <param name="sortDirection">Dirección del ordenamiento (asc o desc).</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Lista paginada de roles.</returns>
    /// <response code="200">Lista paginada de roles.</response>
    /// <response code="400">Parámetros de consulta inválidos.</response>
    [HttpGet]
    [ProducesResponseType<PagedResponse<RoleListItemResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> List(
        [FromQuery] string? search,
        [FromQuery] string? code,
        [FromQuery] bool? isActive,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? sortBy = null,
        [FromQuery] string? sortDirection = null,
        CancellationToken cancellationToken = default)
    {
        var query = new GetRolesQuery(
            search, code, isActive, page, pageSize, sortBy, sortDirection);

        PagedResponse<RoleListItemResponse> result =
            await mediator.Send(query, cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Obtiene un rol por su identificador.
    /// </summary>
    /// <param name="roleId">Identificador único del rol.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Detalle completo del rol.</returns>
    /// <response code="200">Rol encontrado.</response>
    /// <response code="400">Identificador con formato inválido.</response>
    /// <response code="404">No se encontró el rol.</response>
    [HttpGet("{roleId:guid}")]
    [ProducesResponseType<RoleResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid roleId, CancellationToken cancellationToken)
    {
        var query = new GetRoleByIdQuery(roleId);

        Result<RoleResponse> result = await mediator.Send(query, cancellationToken);

        return result.IsSuccess
            ? Ok(result.Value)
            : result.ToProblemResult();
    }

    /// <summary>
    /// Actualiza los datos de un rol.
    /// </summary>
    /// <param name="roleId">Identificador único del rol.</param>
    /// <param name="request">Datos actualizados del rol.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <remarks>
    /// Reemplaza todos los campos editables del rol.
    /// No se puede modificar el código del rol.
    /// </remarks>
    /// <response code="204">Rol actualizado correctamente.</response>
    /// <response code="400">Datos de entrada inválidos.</response>
    /// <response code="404">No se encontró el rol.</response>
    [HttpPut("{roleId:guid}")]
    [Consumes("application/json")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        Guid roleId,
        [FromBody] UpdateRoleRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateRoleCommand(
            roleId,
            request.Name,
            request.Description);

        Result result = await mediator.Send(command, cancellationToken);

        return result.IsSuccess
            ? NoContent()
            : result.ToProblemResult();
    }

    /// <summary>
    /// Activa o desactiva un rol.
    /// </summary>
    /// <param name="roleId">Identificador único del rol.</param>
    /// <param name="request">Estado deseado (activo o inactivo).</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <remarks>
    /// La operación es idempotente: activar un rol ya activo no produce error.
    /// No se permite eliminar roles; en su lugar se desactivan.
    /// </remarks>
    /// <response code="204">Estado actualizado correctamente.</response>
    /// <response code="400">Datos de entrada inválidos.</response>
    /// <response code="404">No se encontró el rol.</response>
    [HttpPatch("{roleId:guid}/active-status")]
    [Consumes("application/json")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetActiveStatus(
        Guid roleId,
        [FromBody] SetRoleActiveStatusRequest request,
        CancellationToken cancellationToken)
    {
        var command = new SetRoleActiveStatusCommand(roleId, request.IsActive);

        Result result = await mediator.Send(command, cancellationToken);

        return result.IsSuccess
            ? NoContent()
            : result.ToProblemResult();
    }
}

/// <summary>
/// Datos para crear un rol.
/// </summary>
/// <param name="Code">Código único del rol.</param>
/// <param name="Name">Nombre del rol.</param>
/// <param name="Description">Descripción opcional del rol.</param>
public sealed record CreateRoleRequest(
    string Code,
    string Name,
    string? Description);

/// <summary>
/// Datos para actualizar un rol.
/// </summary>
/// <param name="Name">Nombre del rol.</param>
/// <param name="Description">Descripción opcional del rol.</param>
public sealed record UpdateRoleRequest(
    string Name,
    string? Description);

/// <summary>
/// Datos para activar o desactivar un rol.
/// </summary>
/// <param name="IsActive">true para activar, false para desactivar.</param>
public sealed record SetRoleActiveStatusRequest(bool IsActive);
