using Lyria.Api.Extensions;
using Lyria.Application.Common.Results;
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
}
