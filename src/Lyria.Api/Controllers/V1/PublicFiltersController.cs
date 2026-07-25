using Lyria.Application.Features.PublicCatalog;
using Lyria.Application.Features.PublicCatalog.GetCatalogs;
using Mediator;
using Microsoft.AspNetCore.Mvc;

namespace Lyria.Api.Controllers.V1;

/// <summary>
/// Catálogos activos para filtros del frontend público.
/// </summary>
[ApiController]
[Route("api/v1/public/catalogs")]
[Produces("application/json")]
[Tags("Catálogos públicos")]
public sealed class PublicFiltersController(IMediator mediator) : ControllerBase
{
    /// <summary>
    /// Obtiene los catálogos activos necesarios para construir filtros en el frontend.
    /// </summary>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Categorías, servicios, restricciones y ubicaciones activas.</returns>
    /// <response code="200">Catálogos obtenidos correctamente.</response>
    [HttpGet]
    [ProducesResponseType<PublicCatalogsResponse>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCatalogs(
        CancellationToken cancellationToken)
    {
        var query = new GetPublicCatalogsQuery();

        PublicCatalogsResponse result = await mediator.Send(query, cancellationToken);

        return Ok(result);
    }
}
