using Lyria.Api.Extensions;
using Lyria.Application.Common.Results;
using Lyria.Application.Features.BranchSpecialSchedules;
using Lyria.Application.Features.BranchSpecialSchedules.GetByRange;
using Lyria.Application.Features.BranchSpecialSchedules.Replace;
using Lyria.Application.Features.BranchSpecialSchedules.UpdateTimeZone;
using Mediator;
using Microsoft.AspNetCore.Mvc;

namespace Lyria.Api.Controllers.V1;

/// <summary>
/// Administra horarios especiales y zona horaria de cada sede.
/// </summary>
[ApiController]
[Produces("application/json")]
[Tags("Horarios especiales de sedes")]
public sealed class BranchSpecialSchedulesController(IMediator mediator) : ControllerBase
{
    /// <summary>
    /// Obtiene los horarios especiales de una sede en un rango de fechas.
    /// </summary>
    /// <param name="branchId">Identificador de la sede.</param>
    /// <param name="from">Fecha de inicio (yyyy-MM-dd).</param>
    /// <param name="to">Fecha de fin (yyyy-MM-dd).</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Horarios especiales agrupados por fecha.</returns>
    /// <response code="200">Horarios especiales de la sede.</response>
    /// <response code="400">Parámetros de consulta inválidos.</response>
    /// <response code="404">No se encontró la sede indicada.</response>
    [HttpGet("api/v1/branches/{branchId}/special-schedules")]
    [ProducesResponseType<BranchSpecialSchedulesResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByRange(
        Guid branchId,
        [FromQuery] DateOnly from,
        [FromQuery] DateOnly to,
        CancellationToken cancellationToken)
    {
        var query = new GetBranchSpecialSchedulesQuery(branchId, from, to);

        Result<BranchSpecialSchedulesResponse> result =
            await mediator.Send(query, cancellationToken);

        return result.IsSuccess
            ? Ok(result.Value)
            : result.ToProblemResult();
    }

    /// <summary>
    /// Reemplaza los horarios especiales de una sede para una fecha específica.
    /// </summary>
    /// <param name="branchId">Identificador de la sede.</param>
    /// <param name="date">Fecha del horario especial (yyyy-MM-dd).</param>
    /// <param name="request">Horarios especiales para la fecha indicada.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Sin contenido si la operación fue exitosa.</returns>
    /// <remarks>
    /// Los horarios especiales de la fecha indicada son reemplazados por completo.
    /// Si la sede se marca como cerrada, no se deben incluir franjas horarias.
    /// Los horarios especiales tienen prioridad sobre la programación semanal.
    /// </remarks>
    /// <response code="204">Horarios especiales reemplazados correctamente.</response>
    /// <response code="400">Datos de entrada inválidos.</response>
    /// <response code="404">No se encontró la sede indicada.</response>
    /// <response code="409">Franjas superpuestas o conflicto con día cerrado.</response>
    [HttpPut("api/v1/branches/{branchId}/special-schedules/{date}")]
    [Consumes("application/json")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Replace(
        Guid branchId,
        DateOnly date,
        [FromBody] ReplaceBranchSpecialSchedulesRequest request,
        CancellationToken cancellationToken)
    {
        var timeSlots = request.TimeSlots
            .Select(s => new SpecialScheduleTimeSlotItem(
                s.OpeningTime, s.ClosingTime, s.CrossesMidnight))
            .ToList();

        var command = new ReplaceBranchSpecialSchedulesCommand(
            branchId,
            date,
            request.IsClosed,
            request.Reason,
            timeSlots);

        Result result = await mediator.Send(command, cancellationToken);

        return result.IsSuccess
            ? NoContent()
            : result.ToProblemResult();
    }

    /// <summary>
    /// Actualiza la zona horaria de una sede.
    /// </summary>
    /// <param name="branchId">Identificador de la sede.</param>
    /// <param name="request">Nueva zona horaria.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Sin contenido si la operación fue exitosa.</returns>
    /// <remarks>
    /// La zona horaria debe ser un identificador IANA válido (por ejemplo, "America/Argentina/Buenos_Aires").
    /// </remarks>
    /// <response code="204">Zona horaria actualizada correctamente.</response>
    /// <response code="400">Zona horaria inválida.</response>
    /// <response code="404">No se encontró la sede indicada.</response>
    [HttpPatch("api/v1/branches/{branchId}/time-zone")]
    [Consumes("application/json")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateTimeZone(
        Guid branchId,
        [FromBody] UpdateBranchTimeZoneRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateBranchTimeZoneCommand(branchId, request.TimeZoneId);

        Result result = await mediator.Send(command, cancellationToken);

        return result.IsSuccess
            ? NoContent()
            : result.ToProblemResult();
    }
}

/// <summary>
/// Datos para reemplazar los horarios especiales de una fecha.
/// </summary>
/// <param name="IsClosed">Indica si la sede está cerrada en esa fecha.</param>
/// <param name="Reason">Motivo del horario especial.</param>
/// <param name="TimeSlots">Franjas horarias. Vacío si la sede está cerrada.</param>
public sealed record ReplaceBranchSpecialSchedulesRequest(
    bool IsClosed,
    string? Reason,
    IReadOnlyList<SpecialScheduleTimeSlotRequest> TimeSlots);

/// <summary>
/// Franja horaria de un horario especial.
/// </summary>
/// <param name="OpeningTime">Hora de apertura (HH:mm).</param>
/// <param name="ClosingTime">Hora de cierre (HH:mm).</param>
/// <param name="CrossesMidnight">Indica si la franja cruza medianoche.</param>
public sealed record SpecialScheduleTimeSlotRequest(
    string? OpeningTime,
    string? ClosingTime,
    bool CrossesMidnight);

/// <summary>
/// Datos para actualizar la zona horaria de una sede.
/// </summary>
/// <param name="TimeZoneId">Identificador IANA de zona horaria.</param>
public sealed record UpdateBranchTimeZoneRequest(string TimeZoneId);
