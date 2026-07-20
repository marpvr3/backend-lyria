using Lyria.Api.Extensions;
using Lyria.Application.Common;
using Lyria.Application.Common.Results;
using Lyria.Application.Features.EstablishmentBranches;
using Lyria.Application.Features.EstablishmentBranches.Create;
using Lyria.Application.Features.EstablishmentBranches.GetById;
using Lyria.Application.Features.EstablishmentBranches.List;
using Lyria.Application.Features.EstablishmentBranches.Update;
using Lyria.Application.Features.EstablishmentBranches.UpdateStatus;
using Lyria.Domain.Establishments.Branches;
using Mediator;
using Microsoft.AspNetCore.Mvc;

namespace Lyria.Api.Controllers.V1;

/// <summary>
/// Gestión de sedes de establecimientos.
/// </summary>
[ApiController]
[Produces("application/json")]
[Tags("Sedes")]
public sealed class EstablishmentBranchesController(IMediator mediator) : ControllerBase
{
    /// <summary>
    /// Crea una sede asociada a un establecimiento.
    /// </summary>
    /// <param name="establishmentId">Identificador del establecimiento al que se asociará la sede.</param>
    /// <param name="request">Datos de la sede a crear.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Identificador de la sede creada.</returns>
    /// <remarks>
    /// La sede se crea con estado activo por defecto.
    /// El nombre debe ser único dentro del mismo establecimiento.
    /// El establecimiento debe existir y estar activo.
    /// </remarks>
    /// <response code="201">Sede creada correctamente.</response>
    /// <response code="400">Datos de entrada inválidos.</response>
    /// <response code="404">No se encontró el establecimiento.</response>
    /// <response code="409">Ya existe una sede con el mismo nombre en este establecimiento.</response>
    [HttpPost("api/v1/establishments/{establishmentId}/branches")]
    [Consumes("application/json")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(
        Guid establishmentId,
        [FromBody] CreateEstablishmentBranchRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateEstablishmentBranchCommand(
            establishmentId,
            request.Name,
            request.Street,
            request.Number,
            request.AddressComplement,
            request.Neighborhood,
            request.City,
            request.Province,
            request.PostalCode,
            request.Country,
            request.Latitude,
            request.Longitude,
            request.Phone,
            request.WhatsApp,
            request.Email);

        Result<EstablishmentBranchId> result = await mediator.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return result.ToProblemResult();
        }

        return CreatedAtAction(
            nameof(GetById),
            new { branchId = result.Value.Value },
            new { id = result.Value.Value });
    }

    /// <summary>
    /// Lista las sedes pertenecientes a un establecimiento.
    /// </summary>
    /// <param name="establishmentId">Identificador del establecimiento.</param>
    /// <param name="search">Texto libre para buscar por nombre de sede.</param>
    /// <param name="isActive">Filtrar por estado activo/inactivo.</param>
    /// <param name="page">Número de página (por defecto 1).</param>
    /// <param name="pageSize">Cantidad de elementos por página (por defecto 20).</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Lista paginada de sedes.</returns>
    /// <response code="200">Lista paginada de sedes.</response>
    /// <response code="400">Parámetros de consulta inválidos.</response>
    [HttpGet("api/v1/establishments/{establishmentId}/branches")]
    [ProducesResponseType<PagedResponse<EstablishmentBranchListItemResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ListByEstablishment(
        Guid establishmentId,
        [FromQuery] string? search,
        [FromQuery] bool? isActive,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = new ListEstablishmentBranchesQuery(
            establishmentId, search, isActive, page, pageSize);

        PagedResponse<EstablishmentBranchListItemResponse> result =
            await mediator.Send(query, cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Obtiene el detalle de una sede específica.
    /// </summary>
    /// <param name="branchId">Identificador único de la sede.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Detalle completo de la sede.</returns>
    /// <response code="200">Sede encontrada.</response>
    /// <response code="400">Identificador con formato inválido.</response>
    /// <response code="404">No se encontró la sede.</response>
    [HttpGet("api/v1/branches/{branchId}")]
    [ProducesResponseType<EstablishmentBranchResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid branchId, CancellationToken cancellationToken)
    {
        var query = new GetEstablishmentBranchByIdQuery(branchId);

        Result<EstablishmentBranchResponse> result = await mediator.Send(query, cancellationToken);

        return result.IsSuccess
            ? Ok(result.Value)
            : result.ToProblemResult();
    }

    /// <summary>
    /// Actualiza los datos editables de una sede.
    /// </summary>
    /// <param name="branchId">Identificador único de la sede.</param>
    /// <param name="request">Datos actualizados de la sede.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <remarks>
    /// Reemplaza todos los campos editables de la sede.
    /// El nombre debe seguir siendo único dentro del establecimiento.
    /// No se puede cambiar el establecimiento al que pertenece la sede.
    /// </remarks>
    /// <response code="204">Sede actualizada correctamente.</response>
    /// <response code="400">Datos de entrada inválidos.</response>
    /// <response code="404">No se encontró la sede.</response>
    /// <response code="409">Ya existe otra sede con el mismo nombre en este establecimiento.</response>
    [HttpPut("api/v1/branches/{branchId}")]
    [Consumes("application/json")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(
        Guid branchId,
        [FromBody] UpdateEstablishmentBranchRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateEstablishmentBranchCommand(
            branchId,
            request.Name,
            request.Street,
            request.Number,
            request.AddressComplement,
            request.Neighborhood,
            request.City,
            request.Province,
            request.PostalCode,
            request.Country,
            request.Latitude,
            request.Longitude,
            request.Phone,
            request.WhatsApp,
            request.Email);

        Result result = await mediator.Send(command, cancellationToken);

        return result.IsSuccess
            ? NoContent()
            : result.ToProblemResult();
    }

    /// <summary>
    /// Activa o desactiva una sede sin eliminarla.
    /// </summary>
    /// <param name="branchId">Identificador único de la sede.</param>
    /// <param name="request">Estado deseado (activo o inactivo).</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <remarks>
    /// La operación es idempotente: activar una sede ya activa no produce error.
    /// No se permite eliminar sedes; en su lugar se desactivan.
    /// </remarks>
    /// <response code="204">Estado actualizado correctamente.</response>
    /// <response code="400">Datos de entrada inválidos.</response>
    /// <response code="404">No se encontró la sede.</response>
    [HttpPatch("api/v1/branches/{branchId}/status")]
    [Consumes("application/json")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateStatus(
        Guid branchId,
        [FromBody] UpdateEstablishmentBranchStatusRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateEstablishmentBranchStatusCommand(branchId, request.IsActive);

        Result result = await mediator.Send(command, cancellationToken);

        return result.IsSuccess
            ? NoContent()
            : result.ToProblemResult();
    }
}

/// <summary>
/// Datos para crear una sede.
/// </summary>
/// <param name="Name">Nombre de la sede.</param>
/// <param name="Street">Calle de la sede.</param>
/// <param name="Number">Número de la dirección.</param>
/// <param name="AddressComplement">Complemento de dirección (piso, local, etc.).</param>
/// <param name="Neighborhood">Barrio de la sede.</param>
/// <param name="City">Ciudad de la sede.</param>
/// <param name="Province">Provincia de la sede.</param>
/// <param name="PostalCode">Código postal de la sede.</param>
/// <param name="Country">País de la sede.</param>
/// <param name="Latitude">Latitud geográfica de la sede.</param>
/// <param name="Longitude">Longitud geográfica de la sede.</param>
/// <param name="Phone">Teléfono de contacto de la sede.</param>
/// <param name="WhatsApp">WhatsApp de contacto de la sede.</param>
/// <param name="Email">Correo electrónico de contacto de la sede.</param>
public sealed record CreateEstablishmentBranchRequest(
    string Name,
    string Street,
    string? Number,
    string? AddressComplement,
    string? Neighborhood,
    string? City,
    string? Province,
    string? PostalCode,
    string? Country,
    decimal? Latitude,
    decimal? Longitude,
    string? Phone,
    string? WhatsApp,
    string? Email);

/// <summary>
/// Datos para actualizar una sede.
/// </summary>
/// <param name="Name">Nombre de la sede.</param>
/// <param name="Street">Calle de la sede.</param>
/// <param name="Number">Número de la dirección.</param>
/// <param name="AddressComplement">Complemento de dirección (piso, local, etc.).</param>
/// <param name="Neighborhood">Barrio de la sede.</param>
/// <param name="City">Ciudad de la sede.</param>
/// <param name="Province">Provincia de la sede.</param>
/// <param name="PostalCode">Código postal de la sede.</param>
/// <param name="Country">País de la sede.</param>
/// <param name="Latitude">Latitud geográfica de la sede.</param>
/// <param name="Longitude">Longitud geográfica de la sede.</param>
/// <param name="Phone">Teléfono de contacto de la sede.</param>
/// <param name="WhatsApp">WhatsApp de contacto de la sede.</param>
/// <param name="Email">Correo electrónico de contacto de la sede.</param>
public sealed record UpdateEstablishmentBranchRequest(
    string Name,
    string Street,
    string? Number,
    string? AddressComplement,
    string? Neighborhood,
    string? City,
    string? Province,
    string? PostalCode,
    string? Country,
    decimal? Latitude,
    decimal? Longitude,
    string? Phone,
    string? WhatsApp,
    string? Email);

/// <summary>
/// Datos para activar o desactivar una sede.
/// </summary>
/// <param name="IsActive">true para activar, false para desactivar.</param>
public sealed record UpdateEstablishmentBranchStatusRequest(bool IsActive);
