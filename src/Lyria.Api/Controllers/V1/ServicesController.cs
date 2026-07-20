using Lyria.Api.Extensions;
using Lyria.Application.Common;
using Lyria.Application.Common.Results;
using Lyria.Application.Features.Services;
using Lyria.Application.Features.Services.Create;
using Lyria.Application.Features.Services.GetById;
using Lyria.Application.Features.Services.List;
using Lyria.Application.Features.Services.Update;
using Lyria.Application.Features.Services.UpdateStatus;
using Lyria.Domain.Services;
using Mediator;
using Microsoft.AspNetCore.Mvc;

namespace Lyria.Api.Controllers.V1;

/// <summary>
/// Administra el catálogo de servicios que pueden ofrecer las sedes.
/// </summary>
[ApiController]
[Route("api/v1/services")]
[Produces("application/json")]
[Tags("Servicios")]
public sealed class ServicesController(IMediator mediator) : ControllerBase
{
    /// <summary>
    /// Crea un nuevo servicio en el catálogo.
    /// </summary>
    /// <param name="request">Datos del servicio a crear.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Identificador del servicio creado.</returns>
    /// <remarks>
    /// El servicio se crea con estado activo por defecto.
    /// El nombre debe ser único entre todos los servicios.
    /// El campo iconUrl es opcional y debe ser una URL absoluta HTTP o HTTPS.
    ///
    /// Ejemplos de servicios: Delivery, Comer en el lugar, Takeaway, Reservas, Wi-Fi.
    /// </remarks>
    /// <response code="201">Servicio creado correctamente.</response>
    /// <response code="400">Datos de entrada inválidos.</response>
    /// <response code="409">Ya existe un servicio con el mismo nombre.</response>
    [HttpPost]
    [Consumes("application/json")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(
        [FromBody] CreateServiceRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateServiceCommand(
            request.Name,
            request.Description,
            request.IconUrl);

        Result<ServiceId> result = await mediator.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return result.ToProblemResult();
        }

        return CreatedAtAction(
            nameof(GetById),
            new { serviceId = result.Value.Value },
            new { id = result.Value.Value });
    }

    /// <summary>
    /// Lista servicios del catálogo con filtros y paginación.
    /// </summary>
    /// <param name="search">Texto libre para buscar por nombre.</param>
    /// <param name="isActive">Filtrar por estado activo/inactivo.</param>
    /// <param name="page">Número de página (por defecto 1).</param>
    /// <param name="pageSize">Cantidad de elementos por página (por defecto 20).</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Lista paginada de servicios.</returns>
    /// <response code="200">Lista paginada de servicios.</response>
    /// <response code="400">Parámetros de consulta inválidos.</response>
    [HttpGet]
    [ProducesResponseType<PagedResponse<ServiceListItemResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> List(
        [FromQuery] string? search,
        [FromQuery] bool? isActive,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = new ListServicesQuery(search, isActive, page, pageSize);

        PagedResponse<ServiceListItemResponse> result =
            await mediator.Send(query, cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Obtiene un servicio por su identificador.
    /// </summary>
    /// <param name="serviceId">Identificador único del servicio.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Detalle completo del servicio.</returns>
    /// <remarks>
    /// Devuelve tanto servicios activos como inactivos.
    /// </remarks>
    /// <response code="200">Servicio encontrado.</response>
    /// <response code="400">Identificador con formato inválido.</response>
    /// <response code="404">No se encontró el servicio.</response>
    [HttpGet("{serviceId}")]
    [ProducesResponseType<ServiceResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid serviceId, CancellationToken cancellationToken)
    {
        var query = new GetServiceByIdQuery(serviceId);

        Result<ServiceResponse> result = await mediator.Send(query, cancellationToken);

        return result.IsSuccess
            ? Ok(result.Value)
            : result.ToProblemResult();
    }

    /// <summary>
    /// Actualiza el nombre, descripción e icono de un servicio.
    /// </summary>
    /// <param name="serviceId">Identificador único del servicio.</param>
    /// <param name="request">Datos actualizados del servicio.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <remarks>
    /// Permite modificar nombre, descripción e iconUrl.
    /// El nombre debe seguir siendo único entre todos los servicios.
    /// El campo iconUrl es opcional y debe ser una URL absoluta HTTP o HTTPS.
    /// </remarks>
    /// <response code="204">Servicio actualizado correctamente.</response>
    /// <response code="400">Datos de entrada inválidos.</response>
    /// <response code="404">No se encontró el servicio.</response>
    /// <response code="409">Ya existe otro servicio con el mismo nombre.</response>
    [HttpPut("{serviceId}")]
    [Consumes("application/json")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(
        Guid serviceId,
        [FromBody] UpdateServiceRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateServiceCommand(
            serviceId,
            request.Name,
            request.Description,
            request.IconUrl);

        Result result = await mediator.Send(command, cancellationToken);

        return result.IsSuccess
            ? NoContent()
            : result.ToProblemResult();
    }

    /// <summary>
    /// Activa o desactiva un servicio del catálogo.
    /// </summary>
    /// <param name="serviceId">Identificador único del servicio.</param>
    /// <param name="request">Estado deseado (activo o inactivo).</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <remarks>
    /// La operación es idempotente: activar un servicio ya activo no produce error.
    /// No se permite eliminar servicios; en su lugar se desactivan.
    /// </remarks>
    /// <response code="204">Estado actualizado correctamente.</response>
    /// <response code="400">Datos de entrada inválidos.</response>
    /// <response code="404">No se encontró el servicio.</response>
    [HttpPatch("{serviceId}/status")]
    [Consumes("application/json")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateStatus(
        Guid serviceId,
        [FromBody] UpdateServiceStatusRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateServiceStatusCommand(serviceId, request.IsActive);

        Result result = await mediator.Send(command, cancellationToken);

        return result.IsSuccess
            ? NoContent()
            : result.ToProblemResult();
    }
}

/// <summary>
/// Datos para crear un servicio.
/// </summary>
/// <param name="Name">Nombre del servicio (ejemplo: Delivery, Wi-Fi, Reservas).</param>
/// <param name="Description">Descripción opcional del servicio.</param>
/// <param name="IconUrl">URL opcional del icono del servicio (HTTP o HTTPS).</param>
public sealed record CreateServiceRequest(
    string Name,
    string? Description,
    string? IconUrl);

/// <summary>
/// Datos para actualizar un servicio.
/// </summary>
/// <param name="Name">Nombre del servicio.</param>
/// <param name="Description">Descripción opcional del servicio.</param>
/// <param name="IconUrl">URL opcional del icono del servicio (HTTP o HTTPS).</param>
public sealed record UpdateServiceRequest(
    string Name,
    string? Description,
    string? IconUrl);

/// <summary>
/// Datos para activar o desactivar un servicio.
/// </summary>
/// <param name="IsActive">true para activar, false para desactivar.</param>
public sealed record UpdateServiceStatusRequest(bool IsActive);
