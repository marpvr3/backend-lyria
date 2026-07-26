using Lyria.Application.Abstractions.Persistence;
using Lyria.Application.Abstractions.Services;
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

    public async Task<IReadOnlyDictionary<EstablishmentBranchId, BranchAvailabilityContext>>
        GetAvailabilityContextsAsync(
            IReadOnlyCollection<EstablishmentBranchId> branchIds,
            DateTimeOffset evaluatedAtUtc,
            ITimeZoneService timeZoneService,
            CancellationToken cancellationToken)
    {
        if (branchIds.Count == 0)
        {
            return new Dictionary<EstablishmentBranchId, BranchAvailabilityContext>();
        }

        // Step 1: Get branches with timezones in a single query
        var branches = await dbContext.Set<EstablishmentBranch>()
            .AsNoTracking()
            .Where(b => branchIds.Contains(b.Id) && b.IsActive)
            .Select(b => new { b.Id, b.TimeZoneId, b.IsActive })
            .ToListAsync(cancellationToken);

        if (branches.Count == 0)
        {
            return new Dictionary<EstablishmentBranchId, BranchAvailabilityContext>();
        }

        // Step 2: Compute local dates per branch
        var branchLocalDates = new Dictionary<EstablishmentBranchId, (DateOnly LocalDate, DateOnly PreviousDate, WeekDay CurrentDow, WeekDay PreviousDow)>();
        var allDates = new HashSet<DateOnly>();
        var allDaysOfWeek = new HashSet<WeekDay>();
        var activeBranchIds = new List<EstablishmentBranchId>();

        foreach (var branch in branches)
        {
            DateTime localDateTime = timeZoneService.ConvertUtcToLocal(evaluatedAtUtc, branch.TimeZoneId);
            var localDate = DateOnly.FromDateTime(localDateTime);
            var previousDate = localDate.AddDays(-1);

            var currentDow = (WeekDay)(localDate.DayOfWeek == System.DayOfWeek.Sunday ? 7
                : (int)localDate.DayOfWeek);
            var previousDow = (WeekDay)(previousDate.DayOfWeek == System.DayOfWeek.Sunday ? 7
                : (int)previousDate.DayOfWeek);

            branchLocalDates[branch.Id] = (localDate, previousDate, currentDow, previousDow);
            allDates.Add(localDate);
            allDates.Add(previousDate);
            allDaysOfWeek.Add(currentDow);
            allDaysOfWeek.Add(previousDow);
            activeBranchIds.Add(branch.Id);
        }

        // Step 3: Batch load special schedules for all branches and relevant dates
        var specialSchedules = await dbContext.Set<BranchSpecialSchedule>()
            .AsNoTracking()
            .Where(s => activeBranchIds.Contains(s.BranchId) && s.IsActive
                && allDates.Contains(s.Date))
            .OrderBy(s => s.BranchId)
            .ThenBy(s => s.Date)
            .ThenBy(s => s.OpeningTime)
            .ToListAsync(cancellationToken);

        var specialByBranchDate = specialSchedules
            .GroupBy(s => (s.BranchId, s.Date))
            .ToDictionary(g => g.Key, g => g.ToList());

        // Step 4: Batch load weekly schedules for all branches and relevant days
        var weeklySchedules = await dbContext.Set<BranchSchedule>()
            .AsNoTracking()
            .Where(s => activeBranchIds.Contains(s.BranchId) && s.IsActive
                && allDaysOfWeek.Contains(s.DayOfWeek))
            .OrderBy(s => s.BranchId)
            .ThenBy(s => s.DayOfWeek)
            .ThenBy(s => s.OpeningTime)
            .ToListAsync(cancellationToken);

        var weeklyByBranchDay = weeklySchedules
            .GroupBy(s => (s.BranchId, s.DayOfWeek))
            .ToDictionary(g => g.Key, g => g.ToList());

        // Step 5: Assemble contexts
        var result = new Dictionary<EstablishmentBranchId, BranchAvailabilityContext>();

        foreach (var branch in branches)
        {
            var (localDate, previousDate, currentDow, previousDow) = branchLocalDates[branch.Id];

            specialByBranchDate.TryGetValue((branch.Id, localDate), out var currentSpecials);
            specialByBranchDate.TryGetValue((branch.Id, previousDate), out var previousSpecials);
            weeklyByBranchDay.TryGetValue((branch.Id, currentDow), out var currentWeekly);
            weeklyByBranchDay.TryGetValue((branch.Id, previousDow), out var previousWeekly);

            var previousDaySchedule = BuildEffectiveSchedule(
                previousSpecials ?? [], previousWeekly ?? []);
            var currentDaySchedule = BuildEffectiveSchedule(
                currentSpecials ?? [], currentWeekly ?? []);

            result[branch.Id] = new BranchAvailabilityContext(
                branch.Id.Value,
                branch.TimeZoneId,
                branch.IsActive,
                previousDaySchedule,
                currentDaySchedule);
        }

        return result;
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
