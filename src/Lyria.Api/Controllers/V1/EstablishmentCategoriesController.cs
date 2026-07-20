using Lyria.Api.Extensions;
using Lyria.Application.Common.Results;
using Lyria.Application.Features.EstablishmentCategories;
using Lyria.Application.Features.EstablishmentCategories.GetById;
using Lyria.Application.Features.EstablishmentCategories.ListActive;
using Mediator;
using Microsoft.AspNetCore.Mvc;

namespace Lyria.Api.Controllers.V1;

/// <summary>
/// Consulta de categorías de establecimientos.
/// </summary>
[ApiController]
[Route("api/v1/establishment-categories")]
[Produces("application/json")]
[Tags("Categorías de establecimientos")]
public sealed class EstablishmentCategoriesController(IMediator mediator) : ControllerBase
{
    /// <summary>
    /// Lista todas las categorías activas.
    /// </summary>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Lista de categorías activas ordenadas por prioridad y nombre.</returns>
    /// <response code="200">Lista de categorías activas.</response>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<EstablishmentCategoryResponse>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> ListActive(CancellationToken cancellationToken)
    {
        IReadOnlyList<EstablishmentCategoryResponse> result = await mediator.Send(
            new ListActiveEstablishmentCategoriesQuery(), cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Obtiene una categoría activa por su identificador.
    /// </summary>
    /// <param name="id">Identificador único de la categoría.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Detalle de la categoría solicitada.</returns>
    /// <response code="200">Categoría encontrada.</response>
    /// <response code="400">Identificador con formato inválido.</response>
    /// <response code="404">No se encontró la categoría o no está activa.</response>
    [HttpGet("{id}")]
    [ProducesResponseType<EstablishmentCategoryResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        Result<EstablishmentCategoryResponse> result = await mediator.Send(
            new GetEstablishmentCategoryByIdQuery(id), cancellationToken);

        return result.IsSuccess
            ? Ok(result.Value)
            : result.ToProblemResult();
    }
}
