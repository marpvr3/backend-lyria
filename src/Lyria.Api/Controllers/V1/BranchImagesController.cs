using Lyria.Api.Extensions;
using Lyria.Application.Common.Results;
using Lyria.Application.Features.BranchImages;
using Lyria.Application.Features.BranchImages.Create;
using Lyria.Application.Features.BranchImages.GetByBranch;
using Lyria.Application.Features.BranchImages.Reorder;
using Lyria.Application.Features.BranchImages.SetPrimary;
using Lyria.Application.Features.BranchImages.UpdateMetadata;
using Lyria.Application.Features.BranchImages.UpdateStatus;
using Lyria.Domain.Establishments.Branches;
using Mediator;
using Microsoft.AspNetCore.Mvc;

namespace Lyria.Api.Controllers.V1;

/// <summary>
/// Administra las imágenes de cada sede.
/// </summary>
[ApiController]
[Produces("application/json")]
[Tags("Imágenes de sedes")]
public sealed class BranchImagesController(IMediator mediator) : ControllerBase
{
    /// <summary>
    /// Obtiene las imágenes activas de una sede.
    /// </summary>
    /// <param name="branchId">Identificador de la sede.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Lista de imágenes de la sede ordenadas.</returns>
    /// <response code="200">Imágenes de la sede.</response>
    /// <response code="400">Identificador con formato inválido.</response>
    /// <response code="404">No se encontró la sede indicada.</response>
    [HttpGet("api/v1/branches/{branchId}/images")]
    [ProducesResponseType<BranchImagesResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByBranch(
        Guid branchId,
        CancellationToken cancellationToken)
    {
        var query = new GetBranchImagesQuery(branchId);

        Result<BranchImagesResponse> result =
            await mediator.Send(query, cancellationToken);

        return result.IsSuccess
            ? Ok(result.Value)
            : result.ToProblemResult();
    }

    /// <summary>
    /// Registra una imagen para una sede.
    /// </summary>
    /// <param name="branchId">Identificador de la sede.</param>
    /// <param name="request">Datos de la imagen a registrar.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Identificador de la imagen creada.</returns>
    /// <remarks>
    /// La imagen se crea con estado activo por defecto.
    /// Si se marca como principal, cualquier imagen principal activa anterior
    /// de la misma sede dejará de serlo en la misma transacción.
    /// </remarks>
    /// <response code="201">Imagen registrada correctamente.</response>
    /// <response code="400">Datos de entrada inválidos.</response>
    /// <response code="404">No se encontró la sede indicada.</response>
    [HttpPost("api/v1/branches/{branchId}/images")]
    [Consumes("application/json")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Create(
        Guid branchId,
        [FromBody] CreateBranchImageRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateBranchImageCommand(
            branchId,
            request.Url,
            request.FileName,
            request.AlternativeText,
            request.IsPrimary,
            request.SortOrder);

        Result<BranchImageId> result = await mediator.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return result.ToProblemResult();
        }

        return CreatedAtAction(
            nameof(GetByBranch),
            new { branchId },
            new { id = result.Value.Value });
    }

    /// <summary>
    /// Actualiza los metadatos de una imagen de sede.
    /// </summary>
    /// <param name="branchId">Identificador de la sede.</param>
    /// <param name="imageId">Identificador de la imagen.</param>
    /// <param name="request">Nuevos metadatos de la imagen.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <remarks>
    /// No permite cambiar si la imagen es principal ni su estado activo/inactivo.
    /// </remarks>
    /// <response code="204">Metadatos actualizados correctamente.</response>
    /// <response code="400">Datos de entrada inválidos.</response>
    /// <response code="404">No se encontró la sede o la imagen.</response>
    [HttpPut("api/v1/branches/{branchId}/images/{imageId}")]
    [Consumes("application/json")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateMetadata(
        Guid branchId,
        Guid imageId,
        [FromBody] UpdateBranchImageMetadataRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateBranchImageMetadataCommand(
            branchId,
            imageId,
            request.Url,
            request.FileName,
            request.AlternativeText,
            request.SortOrder);

        Result result = await mediator.Send(command, cancellationToken);

        return result.IsSuccess
            ? NoContent()
            : result.ToProblemResult();
    }

    /// <summary>
    /// Establece una imagen como la principal de la sede.
    /// </summary>
    /// <param name="branchId">Identificador de la sede.</param>
    /// <param name="imageId">Identificador de la imagen.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <remarks>
    /// La imagen debe estar activa. Cualquier imagen principal anterior de la misma
    /// sede dejará de serlo en la misma transacción. La operación es idempotente.
    /// </remarks>
    /// <response code="204">Imagen marcada como principal correctamente.</response>
    /// <response code="400">Identificador con formato inválido.</response>
    /// <response code="404">No se encontró la sede o la imagen.</response>
    /// <response code="409">La imagen está inactiva y no puede ser principal.</response>
    [HttpPatch("api/v1/branches/{branchId}/images/{imageId}/primary")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> SetPrimary(
        Guid branchId,
        Guid imageId,
        CancellationToken cancellationToken)
    {
        var command = new SetBranchImagePrimaryCommand(branchId, imageId);

        Result result = await mediator.Send(command, cancellationToken);

        return result.IsSuccess
            ? NoContent()
            : result.ToProblemResult();
    }

    /// <summary>
    /// Activa o desactiva una imagen de sede.
    /// </summary>
    /// <param name="branchId">Identificador de la sede.</param>
    /// <param name="imageId">Identificador de la imagen.</param>
    /// <param name="request">Estado deseado.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <remarks>
    /// La operación es idempotente. Si se desactiva una imagen principal,
    /// también dejará de ser principal. No selecciona automáticamente otra imagen.
    /// </remarks>
    /// <response code="204">Estado actualizado correctamente.</response>
    /// <response code="400">Datos de entrada inválidos.</response>
    /// <response code="404">No se encontró la sede o la imagen.</response>
    [HttpPatch("api/v1/branches/{branchId}/images/{imageId}/status")]
    [Consumes("application/json")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateStatus(
        Guid branchId,
        Guid imageId,
        [FromBody] UpdateBranchImageStatusRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateBranchImageStatusCommand(branchId, imageId, request.IsActive);

        Result result = await mediator.Send(command, cancellationToken);

        return result.IsSuccess
            ? NoContent()
            : result.ToProblemResult();
    }

    /// <summary>
    /// Reordena las imágenes de una sede.
    /// </summary>
    /// <param name="branchId">Identificador de la sede.</param>
    /// <param name="request">Nuevo orden de las imágenes.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <remarks>
    /// Reemplaza el orden de las imágenes indicadas en una sola transacción.
    /// No modifica el estado principal ni activo de ninguna imagen.
    /// </remarks>
    /// <response code="204">Orden actualizado correctamente.</response>
    /// <response code="400">Datos de entrada inválidos.</response>
    /// <response code="404">No se encontró la sede o alguna imagen.</response>
    [HttpPut("api/v1/branches/{branchId}/images/order")]
    [Consumes("application/json")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Reorder(
        Guid branchId,
        [FromBody] ReorderBranchImagesRequest request,
        CancellationToken cancellationToken)
    {
        var items = request.Images
            .Select(i => new ImageOrderItem(i.ImageId, i.SortOrder))
            .ToList();

        var command = new ReorderBranchImagesCommand(branchId, items);

        Result result = await mediator.Send(command, cancellationToken);

        return result.IsSuccess
            ? NoContent()
            : result.ToProblemResult();
    }
}

/// <summary>
/// Datos para registrar una imagen de sede.
/// </summary>
/// <param name="Url">URL de la imagen.</param>
/// <param name="FileName">Nombre del archivo.</param>
/// <param name="AlternativeText">Texto alternativo.</param>
/// <param name="IsPrimary">Indica si es la imagen principal.</param>
/// <param name="SortOrder">Orden de presentación.</param>
public sealed record CreateBranchImageRequest(
    string Url,
    string FileName,
    string? AlternativeText,
    bool IsPrimary,
    int SortOrder);

/// <summary>
/// Datos para actualizar los metadatos de una imagen de sede.
/// </summary>
/// <param name="Url">URL de la imagen.</param>
/// <param name="FileName">Nombre del archivo.</param>
/// <param name="AlternativeText">Texto alternativo.</param>
/// <param name="SortOrder">Orden de presentación.</param>
public sealed record UpdateBranchImageMetadataRequest(
    string Url,
    string FileName,
    string? AlternativeText,
    int SortOrder);

/// <summary>
/// Datos para activar o desactivar una imagen de sede.
/// </summary>
/// <param name="IsActive">true para activar, false para desactivar.</param>
public sealed record UpdateBranchImageStatusRequest(bool IsActive);

/// <summary>
/// Datos para reordenar las imágenes de una sede.
/// </summary>
/// <param name="Images">Lista de imágenes con su nuevo orden.</param>
public sealed record ReorderBranchImagesRequest(
    IReadOnlyList<ImageOrderItemRequest> Images);

/// <summary>
/// Imagen con su nuevo orden de presentación.
/// </summary>
/// <param name="ImageId">Identificador de la imagen.</param>
/// <param name="SortOrder">Nuevo orden.</param>
public sealed record ImageOrderItemRequest(
    Guid ImageId,
    int SortOrder);
