using Lyria.Api.Extensions;
using Lyria.Application.Common;
using Lyria.Application.Common.Results;
using Lyria.Application.Features.EstablishmentBranchServices;
using Lyria.Application.Features.EstablishmentBranchServices.Assign;
using Lyria.Application.Features.EstablishmentBranchServices.GetByIds;
using Lyria.Application.Features.EstablishmentBranchServices.List;
using Lyria.Application.Features.EstablishmentBranchServices.Update;
using Lyria.Application.Features.EstablishmentBranchServices.UpdateStatus;
using Mediator;
using Microsoft.AspNetCore.Mvc;

namespace Lyria.Api.Controllers.V1;

/// <summary>
/// Administra los servicios asignados y disponibles en cada sede.
/// </summary>
[ApiController]
[Produces("application/json")]
[Tags("Servicios de sedes")]
public sealed class BranchServicesController(IMediator mediator) : ControllerBase
{
    /// <summary>
    /// Asigna un servicio a una sede.
    /// </summary>
    /// <param name="branchId">Identificador de la sede.</param>
    /// <param name="request">Datos de la asignación.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Ubicación del recurso creado.</returns>
    /// <remarks>
    /// La sede y el servicio deben existir y estar activos.
    /// Si el servicio ya está asignado a la sede (incluso si está inactivo), se retorna 409 Conflict.
    /// Para reactivar una asociación existente, use PATCH /status.
    /// </remarks>
    /// <response code="201">Servicio asignado correctamente a la sede.</response>
    /// <response code="400">Datos de entrada inválidos.</response>
    /// <response code="404">No se encontró la sede o el servicio.</response>
    /// <response code="409">El servicio ya está asignado, la sede está inactiva o el servicio está inactivo.</response>
    [HttpPost("api/v1/branches/{branchId}/services")]
    [Consumes("application/json")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Assign(
        Guid branchId,
        [FromBody] AssignServiceToBranchRequest request,
        CancellationToken cancellationToken)
    {
        var command = new AssignServiceToBranchCommand(
            branchId,
            request.ServiceId,
            request.IsAvailable,
            request.Observation);

        Result result = await mediator.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return result.ToProblemResult();
        }

        return CreatedAtAction(
            nameof(GetByIds),
            new { branchId, serviceId = request.ServiceId },
            new { branchId, serviceId = request.ServiceId });
    }

    /// <summary>
    /// Lista los servicios asignados a una sede con filtros y paginación.
    /// </summary>
    /// <param name="branchId">Identificador de la sede.</param>
    /// <param name="search">Texto libre para buscar por nombre del servicio.</param>
    /// <param name="isActive">Filtrar por estado activo/inactivo de la asociación. Activo: indica si la asociación entre la sede y el servicio está vigente.</param>
    /// <param name="isAvailable">Filtrar por disponibilidad del servicio en la sede. Disponible: indica si el servicio se ofrece actualmente en la sede.</param>
    /// <param name="page">Número de página (por defecto 1).</param>
    /// <param name="pageSize">Cantidad de elementos por página (por defecto 20).</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Lista paginada de servicios asignados.</returns>
    /// <response code="200">Lista paginada de servicios de la sede.</response>
    /// <response code="400">Parámetros de consulta inválidos.</response>
    [HttpGet("api/v1/branches/{branchId}/services")]
    [ProducesResponseType<PagedResponse<EstablishmentBranchServiceListItemResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> List(
        Guid branchId,
        [FromQuery] string? search,
        [FromQuery] bool? isActive,
        [FromQuery] bool? isAvailable,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = new ListBranchServicesQuery(
            branchId, search, isActive, isAvailable, page, pageSize);

        PagedResponse<EstablishmentBranchServiceListItemResponse> result =
            await mediator.Send(query, cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Obtiene el detalle de una asociación específica entre sede y servicio.
    /// </summary>
    /// <param name="branchId">Identificador de la sede.</param>
    /// <param name="serviceId">Identificador del servicio.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Detalle de la asociación.</returns>
    /// <response code="200">Asociación encontrada.</response>
    /// <response code="400">Identificadores con formato inválido.</response>
    /// <response code="404">No se encontró la asociación entre la sede y el servicio.</response>
    [HttpGet("api/v1/branches/{branchId}/services/{serviceId}")]
    [ProducesResponseType<EstablishmentBranchServiceResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByIds(
        Guid branchId,
        Guid serviceId,
        CancellationToken cancellationToken)
    {
        var query = new GetBranchServiceByIdsQuery(branchId, serviceId);

        Result<EstablishmentBranchServiceResponse> result =
            await mediator.Send(query, cancellationToken);

        return result.IsSuccess
            ? Ok(result.Value)
            : result.ToProblemResult();
    }

    /// <summary>
    /// Actualiza la disponibilidad y observación de un servicio en una sede.
    /// </summary>
    /// <param name="branchId">Identificador de la sede.</param>
    /// <param name="serviceId">Identificador del servicio.</param>
    /// <param name="request">Datos actualizados.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <remarks>
    /// Solo modifica IsAvailable y Observation.
    /// No modifica el estado activo (usar PATCH /status).
    /// Disponible: indica si el servicio se ofrece actualmente en la sede.
    /// </remarks>
    /// <response code="204">Asociación actualizada correctamente.</response>
    /// <response code="400">Datos de entrada inválidos.</response>
    /// <response code="404">No se encontró la asociación entre la sede y el servicio.</response>
    [HttpPut("api/v1/branches/{branchId}/services/{serviceId}")]
    [Consumes("application/json")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        Guid branchId,
        Guid serviceId,
        [FromBody] UpdateBranchServiceRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateBranchServiceCommand(
            branchId, serviceId, request.IsAvailable, request.Observation);

        Result result = await mediator.Send(command, cancellationToken);

        return result.IsSuccess
            ? NoContent()
            : result.ToProblemResult();
    }

    /// <summary>
    /// Activa o desactiva la asociación entre una sede y un servicio.
    /// </summary>
    /// <param name="branchId">Identificador de la sede.</param>
    /// <param name="serviceId">Identificador del servicio.</param>
    /// <param name="request">Estado deseado. Activo: indica si la asociación entre la sede y el servicio está vigente.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <remarks>
    /// La operación es idempotente: activar una asociación ya activa no produce error.
    /// No modifica la disponibilidad ni la observación.
    /// </remarks>
    /// <response code="204">Estado actualizado correctamente.</response>
    /// <response code="400">Datos de entrada inválidos.</response>
    /// <response code="404">No se encontró la asociación entre la sede y el servicio.</response>
    [HttpPatch("api/v1/branches/{branchId}/services/{serviceId}/status")]
    [Consumes("application/json")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateStatus(
        Guid branchId,
        Guid serviceId,
        [FromBody] UpdateBranchServiceStatusRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateBranchServiceStatusCommand(branchId, serviceId, request.IsActive);

        Result result = await mediator.Send(command, cancellationToken);

        return result.IsSuccess
            ? NoContent()
            : result.ToProblemResult();
    }
}

/// <summary>
/// Datos para asignar un servicio a una sede.
/// </summary>
/// <param name="ServiceId">Identificador del servicio a asignar.</param>
/// <param name="IsAvailable">Indica si el servicio se ofrece actualmente en la sede.</param>
/// <param name="Observation">Observación opcional sobre la asignación.</param>
public sealed record AssignServiceToBranchRequest(
    Guid ServiceId,
    bool IsAvailable,
    string? Observation);

/// <summary>
/// Datos para actualizar la disponibilidad y observación de un servicio en una sede.
/// </summary>
/// <param name="IsAvailable">Indica si el servicio se ofrece actualmente en la sede.</param>
/// <param name="Observation">Observación opcional.</param>
public sealed record UpdateBranchServiceRequest(
    bool IsAvailable,
    string? Observation);

/// <summary>
/// Datos para activar o desactivar la asociación entre una sede y un servicio.
/// </summary>
/// <param name="IsActive">true para activar, false para desactivar la asociación.</param>
public sealed record UpdateBranchServiceStatusRequest(bool IsActive);
