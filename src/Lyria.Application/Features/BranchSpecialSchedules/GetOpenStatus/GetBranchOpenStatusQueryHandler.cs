using System.Globalization;
using Lyria.Application.Abstractions.Messaging;
using Lyria.Application.Abstractions.Persistence;
using Lyria.Application.Abstractions.Services;
using Lyria.Application.Common.Results;
using Lyria.Domain.Establishments.Branches;

namespace Lyria.Application.Features.BranchSpecialSchedules.GetOpenStatus;

public sealed class GetBranchOpenStatusQueryHandler(
    IBranchAvailabilityReadService availabilityReadService,
    ITimeZoneService timeZoneService,
    TimeProvider timeProvider)
    : IQueryHandler<GetBranchOpenStatusQuery, Result<BranchOpenStatusResponse>>
{
    public async ValueTask<Result<BranchOpenStatusResponse>> Handle(
        GetBranchOpenStatusQuery query,
        CancellationToken cancellationToken)
    {
        var branchId = new EstablishmentBranchId(query.BranchId);

        DateTimeOffset utcNow = timeProvider.GetUtcNow();

        // We need to first get the context to know the timezone, then convert
        // Use a preliminary local date based on UTC; the service will use the correct one
        var preliminaryLocalDate = DateOnly.FromDateTime(utcNow.UtcDateTime);

        BranchAvailabilityContext? context =
            await availabilityReadService.GetAvailabilityContextAsync(
                branchId, preliminaryLocalDate, cancellationToken);

        if (context is null)
        {
            return Result.Failure<BranchOpenStatusResponse>(
                BranchSpecialScheduleErrors.BranchNotFound(query.BranchId));
        }

        if (!context.IsActive)
        {
            return Result.Failure<BranchOpenStatusResponse>(
                BranchSpecialScheduleErrors.BranchInactive(query.BranchId));
        }

        DateTime localDateTime = timeZoneService.ConvertUtcToLocal(utcNow, context.TimeZoneId);
        var localDate = DateOnly.FromDateTime(localDateTime);

        // If the local date differs from the preliminary one, re-fetch
        if (localDate != preliminaryLocalDate)
        {
            context = await availabilityReadService.GetAvailabilityContextAsync(
                branchId, localDate, cancellationToken);

            if (context is null)
            {
                return Result.Failure<BranchOpenStatusResponse>(
                    BranchSpecialScheduleErrors.BranchNotFound(query.BranchId));
            }
        }

        BranchAvailabilityResult availability = BranchAvailabilityCalculator.Calculate(
            localDateTime,
            context.PreviousDaySchedule,
            context.CurrentDaySchedule);

        string statusName = availability.Status switch
        {
            BranchOpenStatus.Open => "Abierto",
            BranchOpenStatus.OpensLaterToday => "Abre más tarde",
            BranchOpenStatus.Closed => "Cerrado",
            BranchOpenStatus.NoSchedule => "Sin programación",
            _ => "Desconocido"
        };

        string? opensAtLocal = availability.OpensAtLocal?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) is not null
            ? localDate.ToDateTime(availability.OpensAtLocal.Value).ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)
            : null;

        string? closesAtLocal = availability.ClosesAtLocal is not null
            ? localDate.ToDateTime(availability.ClosesAtLocal.Value).ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)
            : null;

        var response = new BranchOpenStatusResponse(
            BranchId: context.BranchId,
            IsOpen: availability.Status == BranchOpenStatus.Open,
            Status: availability.Status.ToString(),
            StatusName: statusName,
            EvaluatedAtUtc: utcNow.UtcDateTime.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
            LocalDateTime: localDateTime.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
            TimeZoneId: context.TimeZoneId,
            ScheduleSource: availability.Source.ToString(),
            ScheduleDate: availability.ScheduleDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            OpensAtLocal: opensAtLocal,
            ClosesAtLocal: closesAtLocal,
            Reason: availability.Reason);

        return Result.Success(response);
    }
}
