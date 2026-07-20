using Lyria.Api.Extensions;
using Lyria.Application.Common;
using Lyria.Application.Common.Results;
using Lyria.Application.Features.Establishments;
using Lyria.Application.Features.Establishments.Create;
using Lyria.Application.Features.Establishments.GetById;
using Lyria.Application.Features.Establishments.List;
using Lyria.Application.Features.Establishments.Update;
using Lyria.Application.Features.Establishments.UpdateStatus;
using Lyria.Domain.Establishments;
using Mediator;
using Microsoft.AspNetCore.Mvc;

namespace Lyria.Api.Controllers.V1;

/// <summary>
/// Gestión de establecimientos gastronómicos.
/// </summary>
[ApiController]
[Route("api/v1/establishments")]
[Produces("application/json")]
[Tags("Establecimientos")]
public sealed class EstablishmentsController(IMediator mediator) : ControllerBase
{
    /// <summary>
    /// Crea un nuevo establecimiento.
    /// </summary>
    /// <param name="request">Datos del establecimiento a crear.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Identificador del establecimiento creado.</returns>
    /// <remarks>
    /// El establecimiento se crea con estado activo por defecto.
    /// El slug debe ser único entre todos los establecimientos.
    /// </remarks>
    /// <response code="201">Establecimiento creado correctamente.</response>
    /// <response code="400">Datos de entrada inválidos.</response>
    /// <response code="409">Ya existe un establecimiento con el mismo slug.</response>
    [HttpPost]
    [Consumes("application/json")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(
        [FromBody] CreateEstablishmentRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateEstablishmentCommand(
            request.CategoryId,
            request.Name,
            request.Slug,
            request.Description,
            request.Website,
            request.Instagram,
            request.LogoUrl,
            request.ContactEmail,
            request.ContactPhone);

        Result<EstablishmentId> result = await mediator.Send(command, cancellationToken);

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
    /// Lista establecimientos con filtros y paginación.
    /// </summary>
    /// <param name="search">Texto libre para buscar por nombre o slug.</param>
    /// <param name="categoryId">Filtrar por identificador de categoría.</param>
    /// <param name="isActive">Filtrar por estado activo/inactivo.</param>
    /// <param name="isVerified">Filtrar por estado de verificación.</param>
    /// <param name="page">Número de página (por defecto 1).</param>
    /// <param name="pageSize">Cantidad de elementos por página (por defecto 20).</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Lista paginada de establecimientos.</returns>
    /// <response code="200">Lista paginada de establecimientos.</response>
    /// <response code="400">Parámetros de consulta inválidos.</response>
    [HttpGet]
    [ProducesResponseType<PagedResponse<EstablishmentListItemResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> List(
        [FromQuery] string? search,
        [FromQuery] Guid? categoryId,
        [FromQuery] bool? isActive,
        [FromQuery] bool? isVerified,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = new ListEstablishmentsQuery(
            search, categoryId, isActive, isVerified, page, pageSize);

        PagedResponse<EstablishmentListItemResponse> result =
            await mediator.Send(query, cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Obtiene un establecimiento por su identificador.
    /// </summary>
    /// <param name="id">Identificador único del establecimiento.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Detalle completo del establecimiento.</returns>
    /// <response code="200">Establecimiento encontrado.</response>
    /// <response code="400">Identificador con formato inválido.</response>
    /// <response code="404">No se encontró el establecimiento.</response>
    [HttpGet("{id}")]
    [ProducesResponseType<EstablishmentResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var query = new GetEstablishmentByIdQuery(id);

        Result<EstablishmentResponse> result = await mediator.Send(query, cancellationToken);

        return result.IsSuccess
            ? Ok(result.Value)
            : result.ToProblemResult();
    }

    /// <summary>
    /// Actualiza todos los datos de un establecimiento.
    /// </summary>
    /// <param name="id">Identificador único del establecimiento.</param>
    /// <param name="request">Datos actualizados del establecimiento.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <remarks>
    /// Reemplaza todos los campos del establecimiento.
    /// El slug debe seguir siendo único.
    /// </remarks>
    /// <response code="204">Establecimiento actualizado correctamente.</response>
    /// <response code="400">Datos de entrada inválidos.</response>
    /// <response code="404">No se encontró el establecimiento.</response>
    /// <response code="409">Ya existe otro establecimiento con el mismo slug.</response>
    [HttpPut("{id}")]
    [Consumes("application/json")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateEstablishmentRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateEstablishmentCommand(
            id,
            request.CategoryId,
            request.Name,
            request.Slug,
            request.Description,
            request.Website,
            request.Instagram,
            request.LogoUrl,
            request.ContactEmail,
            request.ContactPhone,
            request.IsVerified);

        Result result = await mediator.Send(command, cancellationToken);

        return result.IsSuccess
            ? NoContent()
            : result.ToProblemResult();
    }

    /// <summary>
    /// Activa o desactiva un establecimiento.
    /// </summary>
    /// <param name="id">Identificador único del establecimiento.</param>
    /// <param name="request">Estado deseado (activo o inactivo).</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <remarks>
    /// La operación es idempotente: activar un establecimiento ya activo no produce error.
    /// No se permite eliminar establecimientos; en su lugar se desactivan.
    /// </remarks>
    /// <response code="204">Estado actualizado correctamente.</response>
    /// <response code="400">Datos de entrada inválidos.</response>
    /// <response code="404">No se encontró el establecimiento.</response>
    [HttpPatch("{id}/status")]
    [Consumes("application/json")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateStatus(
        Guid id,
        [FromBody] UpdateEstablishmentStatusRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateEstablishmentStatusCommand(id, request.IsActive);

        Result result = await mediator.Send(command, cancellationToken);

        return result.IsSuccess
            ? NoContent()
            : result.ToProblemResult();
    }
}

/// <summary>
/// Datos para crear un establecimiento.
/// </summary>
/// <param name="CategoryId">Identificador de la categoría del establecimiento.</param>
/// <param name="Name">Nombre del establecimiento.</param>
/// <param name="Slug">Slug único para la URL del establecimiento.</param>
/// <param name="Description">Descripción opcional del establecimiento.</param>
/// <param name="Website">Sitio web opcional del establecimiento.</param>
/// <param name="Instagram">Cuenta de Instagram opcional del establecimiento.</param>
/// <param name="LogoUrl">URL opcional del logotipo oficial del establecimiento.</param>
/// <param name="ContactEmail">Correo electrónico opcional de contacto del establecimiento.</param>
/// <param name="ContactPhone">Número telefónico opcional de contacto del establecimiento.</param>
public sealed record CreateEstablishmentRequest(
    Guid CategoryId,
    string Name,
    string Slug,
    string? Description,
    string? Website,
    string? Instagram,
    string? LogoUrl,
    string? ContactEmail,
    string? ContactPhone);

/// <summary>
/// Datos para actualizar un establecimiento.
/// </summary>
/// <param name="CategoryId">Identificador de la categoría del establecimiento.</param>
/// <param name="Name">Nombre del establecimiento.</param>
/// <param name="Slug">Slug único para la URL del establecimiento.</param>
/// <param name="Description">Descripción opcional del establecimiento.</param>
/// <param name="Website">Sitio web opcional del establecimiento.</param>
/// <param name="Instagram">Cuenta de Instagram opcional del establecimiento.</param>
/// <param name="LogoUrl">URL opcional del logotipo oficial del establecimiento.</param>
/// <param name="ContactEmail">Correo electrónico opcional de contacto del establecimiento.</param>
/// <param name="ContactPhone">Número telefónico opcional de contacto del establecimiento.</param>
/// <param name="IsVerified">Indica si el establecimiento ha sido verificado.</param>
public sealed record UpdateEstablishmentRequest(
    Guid CategoryId,
    string Name,
    string Slug,
    string? Description,
    string? Website,
    string? Instagram,
    string? LogoUrl,
    string? ContactEmail,
    string? ContactPhone,
    bool IsVerified);

/// <summary>
/// Datos para activar o desactivar un establecimiento.
/// </summary>
/// <param name="IsActive">true para activar, false para desactivar.</param>
public sealed record UpdateEstablishmentStatusRequest(bool IsActive);
