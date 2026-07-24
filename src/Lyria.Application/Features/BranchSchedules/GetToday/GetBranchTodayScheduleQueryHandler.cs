using Lyria.Application.Abstractions.Messaging;
using Lyria.Application.Abstractions.Persistence;
using Lyria.Application.Common.Results;
using Lyria.Domain.Establishments.Branches;

namespace Lyria.Application.Features.BranchSchedules.GetToday;

public sealed class GetBranchTodayScheduleQueryHandler(
    IBranchScheduleReadService readService,
    TimeProvider timeProvider)
    : IQueryHandler<GetBranchTodayScheduleQuery, Result<BranchDayScheduleResponse>>
{
    public async ValueTask<Result<BranchDayScheduleResponse>> Handle(
        GetBranchTodayScheduleQuery query,
        CancellationToken cancellationToken)
    {
        var branchId = new EstablishmentBranchId(query.BranchId);

        bool branchExists = await readService.BranchExistsAsync(branchId, cancellationToken);

        if (!branchExists)
        {
            return Result.Failure<BranchDayScheduleResponse>(
                BranchScheduleErrors.BranchNotFound(query.BranchId));
        }

        DateTimeOffset now = timeProvider.GetUtcNow();
        WeekDay today = ConvertToWeekDay(now.DayOfWeek);

        BranchDayScheduleResponse? response =
            await readService.GetDayScheduleAsync(branchId, today, cancellationToken);

        if (response is null)
        {
            return Result.Success(new BranchDayScheduleResponse(
                (int)today,
                WeekDayNames.GetSpanishName(today),
                false,
                []));
        }

        return Result.Success(response);
    }

    private static WeekDay ConvertToWeekDay(DayOfWeek systemDay) => systemDay switch
    {
        DayOfWeek.Monday => WeekDay.Monday,
        DayOfWeek.Tuesday => WeekDay.Tuesday,
        DayOfWeek.Wednesday => WeekDay.Wednesday,
        DayOfWeek.Thursday => WeekDay.Thursday,
        DayOfWeek.Friday => WeekDay.Friday,
        DayOfWeek.Saturday => WeekDay.Saturday,
        DayOfWeek.Sunday => WeekDay.Sunday,
        _ => WeekDay.Monday
    };
}
