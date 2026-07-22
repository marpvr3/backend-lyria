using Lyria.Api.Extensions;
using Lyria.Application.Common.Results;
using Lyria.Application.Features.EstablishmentCategories;
using Lyria.Application.Features.EstablishmentCategories.Create;
using Lyria.Application.Features.EstablishmentCategories.Deactivate;
using Lyria.Application.Features.EstablishmentCategories.GetById;
using Lyria.Application.Features.EstablishmentCategories.ListActive;
using Lyria.Application.Features.EstablishmentCategories.Reactivate;
using Lyria.Application.Features.EstablishmentCategories.Update;
using Lyria.Domain.Establishments.Categories;
using Mediator;
using Microsoft.AspNetCore.Mvc;

namespace Lyria.Api.Controllers.V1;

/// <summary>
/// Gestión de categorías de establecimientos.
/// </summary>
[ApiController]
[Route("api/v1/establishment-categories")]
[Produces("application/json")]
[Tags("Categorías de establecimientos")]
public sealed class EstablishmentCategoriesController(IMediator mediator) : ControllerBase
{
    /// <summary>
    /// Crea una nueva categoría de establecimiento.
    /// </summary>
    /// <param name="request">Datos de la categoría a crear.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Identificador de la categoría creada.</returns>
    /// <response code="201">Categoría creada correctamente.</response>
    /// <response code="400">Datos de entrada inválidos.</response>
    /// <response code="409">Ya existe una categoría con el mismo nombre.</response>
    [HttpPost]
    [Consumes("application/json")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(
        [FromBody] CreateEstablishmentCategoryRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateEstablishmentCategoryCommand(
            request.Name,
            request.Description,
            request.IconUrl,
            request.SortOrder);

        Result<EstablishmentCategoryId> result = await mediator.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return result.ToProblemResult();
        }

        return CreatedAtAction(
            nameof(GetById),
            new { id = result.Value.Value },
            new { id = result.Value.Value });
    }

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

    /// <summary>
    /// Actualiza una categoría de establecimiento.
    /// </summary>
    /// <param name="id">Identificador único de la categoría.</param>
    /// <param name="request">Datos actualizados de la categoría.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <response code="204">Categoría actualizada correctamente.</response>
    /// <response code="400">Datos de entrada inválidos.</response>
    /// <response code="404">No se encontró la categoría.</response>
    /// <response code="409">Ya existe otra categoría con el mismo nombre.</response>
    [HttpPut("{id}")]
    [Consumes("application/json")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateEstablishmentCategoryRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateEstablishmentCategoryCommand(
            id,
            request.Name,
            request.Description,
            request.IconUrl,
            request.SortOrder);

        Result result = await mediator.Send(command, cancellationToken);

        return result.IsSuccess
            ? NoContent()
            : result.ToProblemResult();
    }

    /// <summary>
    /// Activa o desactiva una categoría de establecimiento.
    /// </summary>
    /// <param name="id">Identificador único de la categoría.</param>
    /// <param name="request">Estado deseado (activo o inactivo).</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <remarks>
    /// La operación es idempotente: activar una categoría ya activa no produce error.
    /// No se permite eliminar categorías; en su lugar se desactivan.
    /// </remarks>
    /// <response code="204">Estado actualizado correctamente.</response>
    /// <response code="400">Datos de entrada inválidos.</response>
    /// <response code="404">No se encontró la categoría.</response>
    [HttpPatch("{id}/status")]
    [Consumes("application/json")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateStatus(
        Guid id,
        [FromBody] UpdateEstablishmentCategoryStatusRequest request,
        CancellationToken cancellationToken)
    {
        Result result = request.IsActive
            ? await mediator.Send(new ReactivateEstablishmentCategoryCommand(id), cancellationToken)
            : await mediator.Send(new DeactivateEstablishmentCategoryCommand(id), cancellationToken);

        return result.IsSuccess
            ? NoContent()
            : result.ToProblemResult();
    }
}

/// <summary>
/// Datos para crear una categoría de establecimiento.
/// </summary>
/// <param name="Name">Nombre de la categoría.</param>
/// <param name="Description">Descripción opcional de la categoría.</param>
/// <param name="IconUrl">URL opcional del ícono representativo.</param>
/// <param name="SortOrder">Orden de prioridad para mostrar la categoría.</param>
public sealed record CreateEstablishmentCategoryRequest(
    string Name,
    string? Description,
    string? IconUrl,
    int SortOrder);

/// <summary>
/// Datos para actualizar una categoría de establecimiento.
/// </summary>
/// <param name="Name">Nombre de la categoría.</param>
/// <param name="Description">Descripción opcional de la categoría.</param>
/// <param name="IconUrl">URL opcional del ícono representativo.</param>
/// <param name="SortOrder">Orden de prioridad para mostrar la categoría.</param>
public sealed record UpdateEstablishmentCategoryRequest(
    string Name,
    string? Description,
    string? IconUrl,
    int SortOrder);

/// <summary>
/// Datos para activar o desactivar una categoría de establecimiento.
/// </summary>
/// <param name="IsActive">true para activar, false para desactivar.</param>
public sealed record UpdateEstablishmentCategoryStatusRequest(bool IsActive);
