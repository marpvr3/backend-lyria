using System.Globalization;
using Lyria.Application.Abstractions.Persistence;
using Lyria.Application.Features.BranchSchedules;
using Lyria.Domain.Establishments.Branches;
using Microsoft.EntityFrameworkCore;

namespace Lyria.Infrastructure.Persistence.ReadServices;

internal sealed class BranchScheduleReadService(LyriaDbContext dbContext)
    : IBranchScheduleReadService
{
    public async Task<bool> BranchExistsAsync(
        EstablishmentBranchId branchId,
        CancellationToken cancellationToken)
    {
        return await dbContext.Set<EstablishmentBranch>()
            .AsNoTracking()
            .AnyAsync(b => b.Id == branchId, cancellationToken);
    }

    public async Task<BranchWeeklyScheduleResponse?> GetWeeklyScheduleAsync(
        EstablishmentBranchId branchId,
        CancellationToken cancellationToken)
    {
        List<BranchSchedule> schedules = await dbContext.Set<BranchSchedule>()
            .AsNoTracking()
            .Where(s => s.BranchId == branchId && s.IsActive)
            .OrderBy(s => s.DayOfWeek)
            .ThenBy(s => s.OpeningTime)
            .ToListAsync(cancellationToken);

        var days = new List<BranchDayScheduleResponse>();

        for (int day = 1; day <= 7; day++)
        {
            var weekDay = (WeekDay)day;
            var daySlots = schedules.Where(s => s.DayOfWeek == weekDay).ToList();

            bool isClosed = daySlots.Any(s => s.IsClosed);

            var timeSlots = daySlots
                .Where(s => !s.IsClosed)
                .Select(s => new BranchTimeSlotResponse(
                    s.Id.Value,
                    s.OpeningTime?.ToString("HH:mm", CultureInfo.InvariantCulture),
                    s.ClosingTime?.ToString("HH:mm", CultureInfo.InvariantCulture),
                    s.CrossesMidnight))
                .ToList();

            days.Add(new BranchDayScheduleResponse(
                day,
                WeekDayNames.GetSpanishName(weekDay),
                isClosed,
                timeSlots));
        }

        return new BranchWeeklyScheduleResponse(branchId.Value, days);
    }

    public async Task<BranchDayScheduleResponse?> GetDayScheduleAsync(
        EstablishmentBranchId branchId,
        WeekDay dayOfWeek,
        CancellationToken cancellationToken)
    {
        List<BranchSchedule> schedules = await dbContext.Set<BranchSchedule>()
            .AsNoTracking()
            .Where(s => s.BranchId == branchId && s.IsActive && s.DayOfWeek == dayOfWeek)
            .OrderBy(s => s.OpeningTime)
            .ToListAsync(cancellationToken);

        if (schedules.Count == 0)
        {
            return null;
        }

        bool isClosed = schedules.Any(s => s.IsClosed);

        var timeSlots = schedules
            .Where(s => !s.IsClosed)
            .Select(s => new BranchTimeSlotResponse(
                s.Id.Value,
                s.OpeningTime?.ToString("HH:mm", CultureInfo.InvariantCulture),
                s.ClosingTime?.ToString("HH:mm", CultureInfo.InvariantCulture),
                s.CrossesMidnight))
            .ToList();

        return new BranchDayScheduleResponse(
            (int)dayOfWeek,
            WeekDayNames.GetSpanishName(dayOfWeek),
            isClosed,
            timeSlots);
    }
}
