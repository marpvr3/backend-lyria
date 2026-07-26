using Lyria.Api.Extensions;
using Lyria.Application.Common.Results;
using Lyria.Application.Features.BranchSpecialSchedules;
using Lyria.Application.Features.BranchSpecialSchedules.GetOpenStatus;
using Lyria.Application.Features.PublicCatalog;
using Lyria.Application.Features.PublicCatalog.GetBranchById;
using Mediator;
using Microsoft.AspNetCore.Mvc;

namespace Lyria.Api.Controllers.V1;

/// <summary>
/// Consulta pública de sedes de establecimientos.
/// </summary>
[ApiController]
[Route("api/v1/public/branches")]
[Produces("application/json")]
[Tags("Catálogo público de sedes")]
public sealed class PublicBranchesController(IMediator mediator) : ControllerBase
{
    /// <summary>
    /// Obtiene el detalle público de una sede por su identificador.
    /// </summary>
    /// <param name="branchId">Identificador único de la sede.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Detalle consolidado de la sede con información del establecimiento padre.</returns>
    /// <response code="200">Sede encontrada.</response>
    /// <response code="400">Identificador con formato inválido.</response>
    /// <response code="404">No se encontró la sede.</response>
    [HttpGet("{branchId}")]
    [ProducesResponseType<PublicBranchFullDetailResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(
        Guid branchId,
        CancellationToken cancellationToken)
    {
        var query = new GetPublicBranchByIdQuery(branchId);

        Result<PublicBranchFullDetailResponse> result =
            await mediator.Send(query, cancellationToken);

        return result.IsSuccess
            ? Ok(result.Value)
            : result.ToProblemResult();
    }

    /// <summary>
    /// Consulta si la sede está abierta en este instante.
    /// </summary>
    /// <param name="branchId">Identificador de la sede.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Estado de apertura actual de la sede.</returns>
    /// <remarks>
    /// Evalúa horarios especiales (prioridad) y semanales para determinar
    /// si la sede está abierta, cerrada, o si no se encontró programación.
    /// </remarks>
    /// <response code="200">Estado de apertura actual.</response>
    /// <response code="400">Identificador con formato inválido.</response>
    /// <response code="404">No se encontró la sede indicada.</response>
    [HttpGet("{branchId}/open-status")]
    [ProducesResponseType<BranchOpenStatusResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetOpenStatus(
        Guid branchId,
        CancellationToken cancellationToken)
    {
        var query = new GetBranchOpenStatusQuery(branchId);

        Result<BranchOpenStatusResponse> result =
            await mediator.Send(query, cancellationToken);

        return result.IsSuccess
            ? Ok(result.Value)
            : result.ToProblemResult();
    }
}
