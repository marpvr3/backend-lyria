using System.Globalization;
using Lyria.Application.Abstractions.Messaging;
using Lyria.Application.Abstractions.Persistence;
using Lyria.Application.Common.Results;
using Lyria.Domain.Establishments.Branches;

namespace Lyria.Application.Features.BranchSchedules.Replace;

public sealed class ReplaceBranchSchedulesCommandHandler(
    IBranchScheduleRepository scheduleRepository,
    IEstablishmentBranchRepository branchRepository)
    : ICommandHandler<ReplaceBranchSchedulesCommand>
{
    public async ValueTask<Result> Handle(
        ReplaceBranchSchedulesCommand command,
        CancellationToken cancellationToken)
    {
        var branchId = new EstablishmentBranchId(command.BranchId);

        bool branchExists = await branchRepository.ExistsByIdAsync(branchId, cancellationToken);

        if (!branchExists)
        {
            return Result.Failure(
                BranchScheduleErrors.BranchNotFound(command.BranchId));
        }

        var schedules = new List<BranchSchedule>(command.Schedules.Count);

        foreach (BranchScheduleItem item in command.Schedules)
        {
            TimeOnly? opening = item.OpeningTime is not null
                ? TimeOnly.Parse(item.OpeningTime, CultureInfo.InvariantCulture)
                : null;

            TimeOnly? closing = item.ClosingTime is not null
                ? TimeOnly.Parse(item.ClosingTime, CultureInfo.InvariantCulture)
                : null;

            var schedule = BranchSchedule.Create(
                BranchScheduleId.New(),
                branchId,
                (WeekDay)item.DayOfWeek,
                opening,
                closing,
                item.CrossesMidnight,
                item.IsClosed);

            schedules.Add(schedule);
        }

        BranchSchedule.ValidateNoClosedDayConflicts(schedules);
        BranchSchedule.ValidateNoOverlaps(schedules);

        await scheduleRepository.ReplaceSchedulesAsync(branchId, schedules, cancellationToken);

        return Result.Success();
    }
}
