using Lyria.Application.Abstractions.Persistence;
using Lyria.Application.Features.BranchSchedules;
using Lyria.Domain.Establishments.Branches;
using Microsoft.EntityFrameworkCore;

namespace Lyria.Infrastructure.Persistence.ReadServices;

internal sealed class BranchAvailabilityReadService(LyriaDbContext dbContext)
    : IBranchAvailabilityReadService
{
    public async Task<BranchAvailabilityContext?> GetAvailabilityContextAsync(
        EstablishmentBranchId branchId,
        DateOnly localDate,
        CancellationToken cancellationToken)
    {
        var branch = await dbContext.Set<EstablishmentBranch>()
            .AsNoTracking()
            .Where(b => b.Id == branchId)
            .Select(b => new { b.Id, b.TimeZoneId, b.IsActive })
            .FirstOrDefaultAsync(cancellationToken);

        if (branch is null)
        {
            return null;
        }

        var previousDate = localDate.AddDays(-1);

        // Load special schedules for both dates in a single query
        var specialSchedules = await dbContext.Set<BranchSpecialSchedule>()
            .AsNoTracking()
            .Where(s => s.BranchId == branchId && s.IsActive
                && (s.Date == localDate || s.Date == previousDate))
            .OrderBy(s => s.Date)
            .ThenBy(s => s.OpeningTime)
            .ToListAsync(cancellationToken);

        var currentDateSpecials = specialSchedules.Where(s => s.Date == localDate).ToList();
        var previousDateSpecials = specialSchedules.Where(s => s.Date == previousDate).ToList();

        // Determine day of week for previous and current date
        var previousDayOfWeek = (WeekDay)(previousDate.DayOfWeek == System.DayOfWeek.Sunday ? 7
            : (int)previousDate.DayOfWeek);
        var currentDayOfWeek = (WeekDay)(localDate.DayOfWeek == System.DayOfWeek.Sunday ? 7
            : (int)localDate.DayOfWeek);

        // Load weekly schedules for both days in a single query
        var weeklySchedules = await dbContext.Set<BranchSchedule>()
            .AsNoTracking()
            .Where(s => s.BranchId == branchId && s.IsActive
                && (s.DayOfWeek == previousDayOfWeek || s.DayOfWeek == currentDayOfWeek))
            .OrderBy(s => s.DayOfWeek)
            .ThenBy(s => s.OpeningTime)
            .ToListAsync(cancellationToken);

        var previousDayWeekly = weeklySchedules.Where(s => s.DayOfWeek == previousDayOfWeek).ToList();
        var currentDayWeekly = weeklySchedules.Where(s => s.DayOfWeek == currentDayOfWeek).ToList();

        // Build effective schedules using priority rule:
        // Special schedules replace weekly schedules completely for that date
        var previousDaySchedule = BuildEffectiveSchedule(previousDateSpecials, previousDayWeekly);
        var currentDaySchedule = BuildEffectiveSchedule(currentDateSpecials, currentDayWeekly);

        return new BranchAvailabilityContext(
            branch.Id.Value,
            branch.TimeZoneId,
            branch.IsActive,
            previousDaySchedule,
            currentDaySchedule);
    }

    private static EffectiveSchedule? BuildEffectiveSchedule(
        List<BranchSpecialSchedule> specialSchedules,
        List<BranchSchedule> weeklySchedules)
    {
        if (specialSchedules.Count > 0)
        {
            return BuildFromSpecial(specialSchedules);
        }

        if (weeklySchedules.Count > 0)
        {
            return BuildFromWeekly(weeklySchedules);
        }

        return null;
    }

    private static EffectiveSchedule BuildFromSpecial(List<BranchSpecialSchedule> schedules)
    {
        bool isClosed = schedules.Any(s => s.IsClosed);
        string? reason = schedules.First().Reason;

        var timeSlots = schedules
            .Where(s => !s.IsClosed && s.OpeningTime.HasValue && s.ClosingTime.HasValue)
            .Select(s => new AvailabilityTimeSlot(
                s.OpeningTime!.Value,
                s.ClosingTime!.Value,
                s.CrossesMidnight))
            .ToList();

        return new EffectiveSchedule(isClosed, reason, ScheduleSource.Special, timeSlots);
    }

    private static EffectiveSchedule BuildFromWeekly(List<BranchSchedule> schedules)
    {
        bool isClosed = schedules.Any(s => s.IsClosed);

        var timeSlots = schedules
            .Where(s => !s.IsClosed && s.OpeningTime.HasValue && s.ClosingTime.HasValue)
            .Select(s => new AvailabilityTimeSlot(
                s.OpeningTime!.Value,
                s.ClosingTime!.Value,
                s.CrossesMidnight))
            .ToList();

        return new EffectiveSchedule(isClosed, null, ScheduleSource.Weekly, timeSlots);
    }
}
