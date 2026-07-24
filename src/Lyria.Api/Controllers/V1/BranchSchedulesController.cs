using Lyria.Api.Extensions;
using Lyria.Application.Common.Results;
using Lyria.Application.Features.BranchSchedules;
using Lyria.Application.Features.BranchSchedules.GetToday;
using Lyria.Application.Features.BranchSchedules.GetWeekly;
using Lyria.Application.Features.BranchSchedules.Replace;
using Mediator;
using Microsoft.AspNetCore.Mvc;

namespace Lyria.Api.Controllers.V1;

/// <summary>
/// Administra la programación semanal de horarios de cada sede.
/// </summary>
[ApiController]
[Produces("application/json")]
[Tags("Horarios de sedes")]
public sealed class BranchSchedulesController(IMediator mediator) : ControllerBase
{
    /// <summary>
    /// Obtiene la programación semanal completa de una sede.
    /// </summary>
    /// <param name="branchId">Identificador de la sede.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Programación agrupada de lunes a domingo.</returns>
    /// <response code="200">Programación semanal de la sede.</response>
    /// <response code="400">Identificador con formato inválido.</response>
    /// <response code="404">No se encontró la sede indicada.</response>
    [HttpGet("api/v1/branches/{branchId}/schedules")]
    [ProducesResponseType<BranchWeeklyScheduleResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetWeekly(
        Guid branchId,
        CancellationToken cancellationToken)
    {
        var query = new GetBranchWeeklyScheduleQuery(branchId);

        Result<BranchWeeklyScheduleResponse> result =
            await mediator.Send(query, cancellationToken);

        return result.IsSuccess
            ? Ok(result.Value)
            : result.ToProblemResult();
    }

    /// <summary>
    /// Obtiene las franjas horarias del día actual para una sede.
    /// </summary>
    /// <param name="branchId">Identificador de la sede.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Franjas horarias del día actual.</returns>
    /// <remarks>
    /// Utiliza TimeProvider para determinar el día actual de forma testeable.
    /// No afirma si la sede está abierta o cerrada en este instante.
    /// </remarks>
    /// <response code="200">Franjas horarias del día actual.</response>
    /// <response code="400">Identificador con formato inválido.</response>
    /// <response code="404">No se encontró la sede indicada.</response>
    [HttpGet("api/v1/branches/{branchId}/schedules/today")]
    [ProducesResponseType<BranchDayScheduleResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetToday(
        Guid branchId,
        CancellationToken cancellationToken)
    {
        var query = new GetBranchTodayScheduleQuery(branchId);

        Result<BranchDayScheduleResponse> result =
            await mediator.Send(query, cancellationToken);

        return result.IsSuccess
            ? Ok(result.Value)
            : result.ToProblemResult();
    }

    /// <summary>
    /// Reemplaza toda la programación semanal de una sede.
    /// </summary>
    /// <param name="branchId">Identificador de la sede.</param>
    /// <param name="request">Nueva programación semanal.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Sin contenido si la operación fue exitosa.</returns>
    /// <remarks>
    /// La operación desactiva los horarios anteriores y los reemplaza con los nuevos
    /// en una única transacción. Si falla, no quedan datos parciales.
    ///
    /// Cada día puede tener varias franjas horarias abiertas (por ejemplo, almuerzo y cena),
    /// pero no pueden superponerse. Un día puede marcarse como cerrado con una sola fila.
    /// </remarks>
    /// <response code="204">Programación reemplazada correctamente.</response>
    /// <response code="400">Datos de entrada inválidos.</response>
    /// <response code="404">No se encontró la sede indicada.</response>
    /// <response code="409">Franjas superpuestas o conflicto con día cerrado.</response>
    [HttpPut("api/v1/branches/{branchId}/schedules")]
    [Consumes("application/json")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Replace(
        Guid branchId,
        [FromBody] ReplaceBranchSchedulesRequest request,
        CancellationToken cancellationToken)
    {
        var items = request.Schedules
            .Select(s => new BranchScheduleItem(
                s.DayOfWeek, s.OpeningTime, s.ClosingTime, s.CrossesMidnight, s.IsClosed))
            .ToList();

        var command = new ReplaceBranchSchedulesCommand(branchId, items);

        Result result = await mediator.Send(command, cancellationToken);

        return result.IsSuccess
            ? NoContent()
            : result.ToProblemResult();
    }
}

/// <summary>
/// Request para reemplazar la programación semanal de una sede.
/// </summary>
/// <param name="Schedules">Lista de franjas horarias.</param>
public sealed record ReplaceBranchSchedulesRequest(
    IReadOnlyList<ScheduleItemRequest> Schedules);

/// <summary>
/// Franja horaria individual dentro de la programación.
/// </summary>
/// <param name="DayOfWeek">Día de la semana (1=Lunes, 7=Domingo).</param>
/// <param name="OpeningTime">Hora de apertura (HH:mm). Null si está cerrado.</param>
/// <param name="ClosingTime">Hora de cierre (HH:mm). Null si está cerrado.</param>
/// <param name="CrossesMidnight">Indica si la franja cruza medianoche.</param>
/// <param name="IsClosed">Indica si el día está cerrado.</param>
public sealed record ScheduleItemRequest(
    int DayOfWeek,
    string? OpeningTime,
    string? ClosingTime,
    bool CrossesMidnight,
    bool IsClosed);
