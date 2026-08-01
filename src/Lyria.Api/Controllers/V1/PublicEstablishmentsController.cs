using Lyria.Api.Extensions;
using Lyria.Application.Common;
using Lyria.Application.Common.Results;
using Lyria.Application.Features.PublicCatalog;
using Lyria.Application.Features.PublicCatalog.GetEstablishmentBySlug;
using Lyria.Application.Features.PublicCatalog.GetEstablishments;
using Mediator;
using Microsoft.AspNetCore.Mvc;

namespace Lyria.Api.Controllers.V1;

/// <summary>
/// Catálogo público de establecimientos.
/// </summary>
[ApiController]
[Route("api/v1/public/establishments")]
[Produces("application/json")]
[Tags("Catálogo público de establecimientos")]
public sealed class PublicEstablishmentsController(IMediator mediator) : ControllerBase
{
    /// <summary>
    /// Lista establecimientos activos con filtros y paginación.
    /// </summary>
    /// <param name="search">Texto libre para buscar por nombre o descripción.</param>
    /// <param name="categoryId">Filtrar por identificador de categoría.</param>
    /// <param name="city">Filtrar por ciudad.</param>
    /// <param name="province">Filtrar por provincia.</param>
    /// <param name="country">Filtrar por país.</param>
    /// <param name="serviceId">Filtrar por servicio disponible.</param>
    /// <param name="restrictionId">Filtrar por restricción alimentaria.</param>
    /// <param name="complianceLevel">Filtrar por nivel de cumplimiento (1=Garantizado, 2=Parcial, 3=Bajo solicitud).</param>
    /// <param name="isCertified">Filtrar por certificación.</param>
    /// <param name="openNow">Filtrar por disponibilidad: true = con sede abierta, false = sin sedes abiertas.</param>
    /// <param name="page">Número de página (por defecto 1).</param>
    /// <param name="pageSize">Cantidad de elementos por página (por defecto 20, máximo 100).</param>
    /// <param name="sortBy">Criterio de ordenamiento: name, newest, branchCount (por defecto name).</param>
    /// <param name="sortDirection">Dirección de ordenamiento: asc, desc (por defecto asc).</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Lista paginada de establecimientos públicos.</returns>
    /// <response code="200">Lista paginada de establecimientos.</response>
    /// <response code="400">Parámetros de consulta inválidos.</response>
    [HttpGet]
    [ProducesResponseType<PagedResponse<PublicEstablishmentListItemResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> List(
        [FromQuery] string? search,
        [FromQuery] Guid? categoryId,
        [FromQuery] string? city,
        [FromQuery] string? province,
        [FromQuery] string? country,
        [FromQuery] Guid? serviceId,
        [FromQuery] Guid? restrictionId,
        [FromQuery] int? complianceLevel,
        [FromQuery] bool? isCertified,
        [FromQuery] bool? openNow,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string sortBy = "name",
        [FromQuery] string sortDirection = "asc",
        CancellationToken cancellationToken = default)
    {
        var query = new GetPublicEstablishmentsQuery(
            search, categoryId, city, province, country,
            serviceId, restrictionId, complianceLevel, isCertified,
            openNow, page, pageSize, sortBy, sortDirection);

        Result<PagedResponse<PublicEstablishmentListItemResponse>> result =
            await mediator.Send(query, cancellationToken);

        return result.IsSuccess
            ? Ok(result.Value)
            : result.ToProblemResult();
    }

    /// <summary>
    /// Obtiene el detalle público de un establecimiento por su slug.
    /// </summary>
    /// <param name="slug">Slug único del establecimiento.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Detalle completo del establecimiento con sus sedes activas.</returns>
    /// <response code="200">Establecimiento encontrado.</response>
    /// <response code="400">Slug con formato inválido.</response>
    /// <response code="404">No se encontró el establecimiento.</response>
    [HttpGet("{slug}")]
    [ProducesResponseType<PublicEstablishmentDetailResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBySlug(
        string slug,
        CancellationToken cancellationToken)
    {
        var query = new GetPublicEstablishmentBySlugQuery(slug);

        Result<PublicEstablishmentDetailResponse> result =
            await mediator.Send(query, cancellationToken);

        return result.IsSuccess
            ? Ok(result.Value)
            : result.ToProblemResult();
    }
}
