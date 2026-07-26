using System.Globalization;
using Lyria.Application.Abstractions.Messaging;
using Lyria.Application.Abstractions.Persistence;
using Lyria.Application.Common.Results;
using Lyria.Domain.Establishments.Branches;

namespace Lyria.Application.Features.BranchSpecialSchedules.Replace;

public sealed class ReplaceBranchSpecialSchedulesCommandHandler(
    IBranchSpecialScheduleRepository specialScheduleRepository,
    IEstablishmentBranchRepository branchRepository)
    : ICommandHandler<ReplaceBranchSpecialSchedulesCommand>
{
    public async ValueTask<Result> Handle(
        ReplaceBranchSpecialSchedulesCommand command,
        CancellationToken cancellationToken)
    {
        var branchId = new EstablishmentBranchId(command.BranchId);

        bool branchExists = await branchRepository.ExistsByIdAsync(branchId, cancellationToken);

        if (!branchExists)
        {
            return Result.Failure(
                BranchSpecialScheduleErrors.BranchNotFound(command.BranchId));
        }

        var schedules = new List<BranchSpecialSchedule>();

        if (command.IsClosed)
        {
            var schedule = BranchSpecialSchedule.Create(
                BranchSpecialScheduleId.New(),
                branchId,
                command.Date,
                openingTime: null,
                closingTime: null,
                crossesMidnight: false,
                isClosed: true,
                command.Reason);

            schedules.Add(schedule);
        }
        else
        {
            foreach (SpecialScheduleTimeSlotItem item in command.TimeSlots)
            {
                TimeOnly? opening = item.OpeningTime is not null
                    ? TimeOnly.Parse(item.OpeningTime, CultureInfo.InvariantCulture)
                    : null;

                TimeOnly? closing = item.ClosingTime is not null
                    ? TimeOnly.Parse(item.ClosingTime, CultureInfo.InvariantCulture)
                    : null;

                var schedule = BranchSpecialSchedule.Create(
                    BranchSpecialScheduleId.New(),
                    branchId,
                    command.Date,
                    opening,
                    closing,
                    item.CrossesMidnight,
                    isClosed: false,
                    command.Reason);

                schedules.Add(schedule);
            }
        }

        BranchSpecialSchedule.ValidateNoClosedDayConflicts(schedules);
        BranchSpecialSchedule.ValidateNoOverlaps(schedules);

        await specialScheduleRepository.ReplaceForDateAsync(
            branchId, command.Date, schedules, cancellationToken);

        return Result.Success();
    }
}
