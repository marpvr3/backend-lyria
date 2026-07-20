using Lyria.Api.Extensions;
using Lyria.Application.Common;
using Lyria.Application.Common.Results;
using Lyria.Application.Features.EstablishmentBranchRestrictions;
using Lyria.Application.Features.EstablishmentBranchRestrictions.Assign;
using Lyria.Application.Features.EstablishmentBranchRestrictions.GetByIds;
using Lyria.Application.Features.EstablishmentBranchRestrictions.List;
using Lyria.Application.Features.EstablishmentBranchRestrictions.Update;
using Lyria.Application.Features.EstablishmentBranchRestrictions.UpdateStatus;
using Lyria.Domain.Establishments.Branches;
using Mediator;
using Microsoft.AspNetCore.Mvc;

namespace Lyria.Api.Controllers.V1;

/// <summary>
/// Administra las restricciones alimentarias o condiciones especiales que puede atender cada sede.
/// </summary>
[ApiController]
[Produces("application/json")]
[Tags("Restricciones de sedes")]
public sealed class BranchRestrictionsController(IMediator mediator) : ControllerBase
{
    /// <summary>
    /// Asigna una restricción a una sede.
    /// </summary>
    /// <param name="branchId">Identificador de la sede.</param>
    /// <param name="request">Datos de la asignación.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Ubicación del recurso creado.</returns>
    /// <remarks>
    /// La sede y la restricción deben existir y estar activas.
    /// Si la restricción ya está asignada a la sede (incluso si está inactiva), se retorna 409 Conflict.
    /// Para reactivar una asociación existente, use PATCH /status.
    ///
    /// Nivel de cumplimiento (complianceLevel):
    /// - 1 — Garantizado: la sede aplica procedimientos específicos para cumplir la restricción.
    /// - 2 — Parcial: la sede ofrece opciones compatibles, pero no garantiza completamente el cumplimiento.
    /// - 3 — Bajo solicitud: la sede puede atender la restricción únicamente cuando el cliente lo solicita.
    ///
    /// Certificado (isCertified):
    /// Indica si la sede declara contar con una certificación o respaldo formal relacionado con la restricción.
    /// </remarks>
    /// <response code="201">Restricción asignada correctamente a la sede.</response>
    /// <response code="400">Datos de entrada inválidos.</response>
    /// <response code="404">No se encontró la sede o la restricción.</response>
    /// <response code="409">La restricción ya está asignada, la sede está inactiva o la restricción está inactiva.</response>
    [HttpPost("api/v1/branches/{branchId}/restrictions")]
    [Consumes("application/json")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Assign(
        Guid branchId,
        [FromBody] AssignRestrictionToBranchRequest request,
        CancellationToken cancellationToken)
    {
        var command = new AssignRestrictionToBranchCommand(
            branchId,
            request.RestrictionId,
            request.ComplianceLevel,
            request.IsCertified,
            request.Observation);

        Result result = await mediator.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return result.ToProblemResult();
        }

        return CreatedAtAction(
            nameof(GetByIds),
            new { branchId, restrictionId = request.RestrictionId },
            new { branchId, restrictionId = request.RestrictionId });
    }

    /// <summary>
    /// Lista las restricciones asignadas a una sede con filtros y paginación.
    /// </summary>
    /// <param name="branchId">Identificador de la sede.</param>
    /// <param name="search">Texto libre para buscar por nombre de la restricción.</param>
    /// <param name="isActive">Filtrar por estado activo/inactivo de la asociación. Activo: indica si la asociación entre la sede y la restricción continúa vigente.</param>
    /// <param name="complianceLevel">Filtrar por nivel de cumplimiento: 1 (Garantizado), 2 (Parcial), 3 (Bajo solicitud).</param>
    /// <param name="isCertified">Filtrar por certificación declarada.</param>
    /// <param name="page">Número de página (por defecto 1).</param>
    /// <param name="pageSize">Cantidad de elementos por página (por defecto 20).</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Lista paginada de restricciones asignadas.</returns>
    /// <response code="200">Lista paginada de restricciones de la sede.</response>
    /// <response code="400">Parámetros de consulta inválidos.</response>
    [HttpGet("api/v1/branches/{branchId}/restrictions")]
    [ProducesResponseType<PagedResponse<EstablishmentBranchRestrictionListItemResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> List(
        Guid branchId,
        [FromQuery] string? search,
        [FromQuery] bool? isActive,
        [FromQuery] RestrictionComplianceLevel? complianceLevel,
        [FromQuery] bool? isCertified,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = new ListBranchRestrictionsQuery(
            branchId, search, isActive, complianceLevel, isCertified, page, pageSize);

        PagedResponse<EstablishmentBranchRestrictionListItemResponse> result =
            await mediator.Send(query, cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Obtiene el detalle de una asociación específica entre sede y restricción.
    /// </summary>
    /// <param name="branchId">Identificador de la sede.</param>
    /// <param name="restrictionId">Identificador de la restricción.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Detalle de la asociación.</returns>
    /// <response code="200">Asociación encontrada.</response>
    /// <response code="400">Identificadores con formato inválido.</response>
    /// <response code="404">No se encontró la asociación entre la sede y la restricción.</response>
    [HttpGet("api/v1/branches/{branchId}/restrictions/{restrictionId}")]
    [ProducesResponseType<EstablishmentBranchRestrictionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByIds(
        Guid branchId,
        Guid restrictionId,
        CancellationToken cancellationToken)
    {
        var query = new GetBranchRestrictionByIdsQuery(branchId, restrictionId);

        Result<EstablishmentBranchRestrictionResponse> result =
            await mediator.Send(query, cancellationToken);

        return result.IsSuccess
            ? Ok(result.Value)
            : result.ToProblemResult();
    }

    /// <summary>
    /// Actualiza el nivel de cumplimiento, certificación y observación de una restricción en una sede.
    /// </summary>
    /// <param name="branchId">Identificador de la sede.</param>
    /// <param name="restrictionId">Identificador de la restricción.</param>
    /// <param name="request">Datos actualizados.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <remarks>
    /// Solo modifica ComplianceLevel, IsCertified y Observation.
    /// No modifica el estado activo (usar PATCH /status).
    ///
    /// Nivel de cumplimiento (complianceLevel):
    /// Indica cómo atiende la sede la restricción informada.
    /// - 1 — Garantizado.
    /// - 2 — Parcial.
    /// - 3 — Bajo solicitud.
    ///
    /// Certificado (isCertified):
    /// Indica si la sede declara contar con una certificación o respaldo formal.
    /// </remarks>
    /// <response code="204">Asociación actualizada correctamente.</response>
    /// <response code="400">Datos de entrada inválidos.</response>
    /// <response code="404">No se encontró la asociación entre la sede y la restricción.</response>
    [HttpPut("api/v1/branches/{branchId}/restrictions/{restrictionId}")]
    [Consumes("application/json")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        Guid branchId,
        Guid restrictionId,
        [FromBody] UpdateBranchRestrictionRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateBranchRestrictionCommand(
            branchId, restrictionId, request.ComplianceLevel, request.IsCertified, request.Observation);

        Result result = await mediator.Send(command, cancellationToken);

        return result.IsSuccess
            ? NoContent()
            : result.ToProblemResult();
    }

    /// <summary>
    /// Activa o desactiva la asociación entre una sede y una restricción.
    /// </summary>
    /// <param name="branchId">Identificador de la sede.</param>
    /// <param name="restrictionId">Identificador de la restricción.</param>
    /// <param name="request">Estado deseado. Activo: indica si la asociación entre la sede y la restricción continúa vigente.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <remarks>
    /// La operación es idempotente: activar una asociación ya activa no produce error.
    /// No modifica el nivel de cumplimiento, la certificación ni la observación.
    /// </remarks>
    /// <response code="204">Estado actualizado correctamente.</response>
    /// <response code="400">Datos de entrada inválidos.</response>
    /// <response code="404">No se encontró la asociación entre la sede y la restricción.</response>
    [HttpPatch("api/v1/branches/{branchId}/restrictions/{restrictionId}/status")]
    [Consumes("application/json")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateStatus(
        Guid branchId,
        Guid restrictionId,
        [FromBody] UpdateBranchRestrictionStatusRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateBranchRestrictionStatusCommand(branchId, restrictionId, request.IsActive);

        Result result = await mediator.Send(command, cancellationToken);

        return result.IsSuccess
            ? NoContent()
            : result.ToProblemResult();
    }
}

/// <summary>
/// Datos para asignar una restricción a una sede.
/// </summary>
/// <param name="RestrictionId">Identificador de la restricción a asignar.</param>
/// <param name="ComplianceLevel">Nivel de cumplimiento: 1 (Garantizado), 2 (Parcial), 3 (Bajo solicitud).</param>
/// <param name="IsCertified">Indica si la sede declara contar con una certificación o respaldo formal.</param>
/// <param name="Observation">Observación opcional sobre la asignación.</param>
public sealed record AssignRestrictionToBranchRequest(
    Guid RestrictionId,
    RestrictionComplianceLevel ComplianceLevel,
    bool IsCertified,
    string? Observation);

/// <summary>
/// Datos para actualizar el cumplimiento, certificación y observación de una restricción en una sede.
/// </summary>
/// <param name="ComplianceLevel">Nivel de cumplimiento: 1 (Garantizado), 2 (Parcial), 3 (Bajo solicitud).</param>
/// <param name="IsCertified">Indica si la sede declara contar con una certificación o respaldo formal.</param>
/// <param name="Observation">Observación opcional.</param>
public sealed record UpdateBranchRestrictionRequest(
    RestrictionComplianceLevel ComplianceLevel,
    bool IsCertified,
    string? Observation);

/// <summary>
/// Datos para activar o desactivar la asociación entre una sede y una restricción.
/// </summary>
/// <param name="IsActive">true para activar, false para desactivar la asociación.</param>
public sealed record UpdateBranchRestrictionStatusRequest(bool IsActive);
