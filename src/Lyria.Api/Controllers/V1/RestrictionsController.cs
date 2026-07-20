using Lyria.Api.Extensions;
using Lyria.Application.Common;
using Lyria.Application.Common.Results;
using Lyria.Application.Features.Restrictions;
using Lyria.Application.Features.Restrictions.Create;
using Lyria.Application.Features.Restrictions.GetById;
using Lyria.Application.Features.Restrictions.List;
using Lyria.Application.Features.Restrictions.Update;
using Lyria.Application.Features.Restrictions.UpdateStatus;
using Lyria.Domain.Restrictions;
using Mediator;
using Microsoft.AspNetCore.Mvc;

namespace Lyria.Api.Controllers.V1;

/// <summary>
/// Administra el catálogo de restricciones alimentarias utilizadas por usuarios y sedes.
/// </summary>
[ApiController]
[Route("api/v1/restrictions")]
[Produces("application/json")]
[Tags("Restricciones")]
public sealed class RestrictionsController(IMediator mediator) : ControllerBase
{
    /// <summary>
    /// Crea una nueva restricción alimentaria.
    /// </summary>
    /// <param name="request">Datos de la restricción a crear.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Identificador de la restricción creada.</returns>
    /// <remarks>
    /// La restricción se crea con estado activo por defecto.
    /// El nombre debe ser único entre todas las restricciones.
    ///
    /// Ejemplos de restricciones: Vegano, Vegetariano, Sin TACC, Sin lactosa.
    /// </remarks>
    /// <response code="201">Restricción creada correctamente.</response>
    /// <response code="400">Datos de entrada inválidos.</response>
    /// <response code="409">Ya existe una restricción con el mismo nombre.</response>
    [HttpPost]
    [Consumes("application/json")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(
        [FromBody] CreateRestrictionRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateRestrictionCommand(
            request.Name,
            request.Description);

        Result<RestrictionId> result = await mediator.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return result.ToProblemResult();
        }

        return CreatedAtAction(
            nameof(GetById),
            new { restrictionId = result.Value.Value },
            new { id = result.Value.Value });
    }

    /// <summary>
    /// Lista restricciones alimentarias con filtros y paginación.
    /// </summary>
    /// <param name="search">Texto libre para buscar por nombre.</param>
    /// <param name="isActive">Filtrar por estado activo/inactivo.</param>
    /// <param name="page">Número de página (por defecto 1).</param>
    /// <param name="pageSize">Cantidad de elementos por página (por defecto 20).</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Lista paginada de restricciones.</returns>
    /// <response code="200">Lista paginada de restricciones.</response>
    /// <response code="400">Parámetros de consulta inválidos.</response>
    [HttpGet]
    [ProducesResponseType<PagedResponse<RestrictionListItemResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> List(
        [FromQuery] string? search,
        [FromQuery] bool? isActive,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = new ListRestrictionsQuery(search, isActive, page, pageSize);

        PagedResponse<RestrictionListItemResponse> result =
            await mediator.Send(query, cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Obtiene una restricción alimentaria por su identificador.
    /// </summary>
    /// <param name="restrictionId">Identificador único de la restricción.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Detalle completo de la restricción.</returns>
    /// <remarks>
    /// Devuelve tanto restricciones activas como inactivas.
    /// </remarks>
    /// <response code="200">Restricción encontrada.</response>
    /// <response code="400">Identificador con formato inválido.</response>
    /// <response code="404">No se encontró la restricción.</response>
    [HttpGet("{restrictionId}")]
    [ProducesResponseType<RestrictionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid restrictionId, CancellationToken cancellationToken)
    {
        var query = new GetRestrictionByIdQuery(restrictionId);

        Result<RestrictionResponse> result = await mediator.Send(query, cancellationToken);

        return result.IsSuccess
            ? Ok(result.Value)
            : result.ToProblemResult();
    }

    /// <summary>
    /// Actualiza el nombre y la descripción de una restricción alimentaria.
    /// </summary>
    /// <param name="restrictionId">Identificador único de la restricción.</param>
    /// <param name="request">Datos actualizados de la restricción.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <remarks>
    /// Permite modificar únicamente el nombre y la descripción.
    /// El nombre debe seguir siendo único entre todas las restricciones.
    /// </remarks>
    /// <response code="204">Restricción actualizada correctamente.</response>
    /// <response code="400">Datos de entrada inválidos.</response>
    /// <response code="404">No se encontró la restricción.</response>
    /// <response code="409">Ya existe otra restricción con el mismo nombre.</response>
    [HttpPut("{restrictionId}")]
    [Consumes("application/json")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(
        Guid restrictionId,
        [FromBody] UpdateRestrictionRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateRestrictionCommand(
            restrictionId,
            request.Name,
            request.Description);

        Result result = await mediator.Send(command, cancellationToken);

        return result.IsSuccess
            ? NoContent()
            : result.ToProblemResult();
    }

    /// <summary>
    /// Activa o desactiva una restricción alimentaria.
    /// </summary>
    /// <param name="restrictionId">Identificador único de la restricción.</param>
    /// <param name="request">Estado deseado (activo o inactivo).</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <remarks>
    /// La operación es idempotente: activar una restricción ya activa no produce error.
    /// No se permite eliminar restricciones; en su lugar se desactivan.
    /// </remarks>
    /// <response code="204">Estado actualizado correctamente.</response>
    /// <response code="400">Datos de entrada inválidos.</response>
    /// <response code="404">No se encontró la restricción.</response>
    [HttpPatch("{restrictionId}/status")]
    [Consumes("application/json")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateStatus(
        Guid restrictionId,
        [FromBody] UpdateRestrictionStatusRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateRestrictionStatusCommand(restrictionId, request.IsActive);

        Result result = await mediator.Send(command, cancellationToken);

        return result.IsSuccess
            ? NoContent()
            : result.ToProblemResult();
    }
}

/// <summary>
/// Datos para crear una restricción alimentaria.
/// </summary>
/// <param name="Name">Nombre de la restricción (ejemplo: Sin TACC, Vegano).</param>
/// <param name="Description">Descripción opcional de la restricción.</param>
public sealed record CreateRestrictionRequest(
    string Name,
    string? Description);

/// <summary>
/// Datos para actualizar una restricción alimentaria.
/// </summary>
/// <param name="Name">Nombre de la restricción.</param>
/// <param name="Description">Descripción opcional de la restricción.</param>
public sealed record UpdateRestrictionRequest(
    string Name,
    string? Description);

/// <summary>
/// Datos para activar o desactivar una restricción alimentaria.
/// </summary>
/// <param name="IsActive">true para activar, false para desactivar.</param>
public sealed record UpdateRestrictionStatusRequest(bool IsActive);
