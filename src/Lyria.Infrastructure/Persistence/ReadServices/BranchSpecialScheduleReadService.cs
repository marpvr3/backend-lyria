using System.Globalization;
using Lyria.Application.Abstractions.Persistence;
using Lyria.Application.Features.BranchSpecialSchedules;
using Lyria.Domain.Establishments.Branches;
using Microsoft.EntityFrameworkCore;

namespace Lyria.Infrastructure.Persistence.ReadServices;

internal sealed class BranchSpecialScheduleReadService(LyriaDbContext dbContext)
    : IBranchSpecialScheduleReadService
{
    public async Task<bool> BranchExistsAsync(
        EstablishmentBranchId branchId,
        CancellationToken cancellationToken)
    {
        return await dbContext.Set<EstablishmentBranch>()
            .AsNoTracking()
            .AnyAsync(b => b.Id == branchId, cancellationToken);
    }

    public async Task<BranchSpecialSchedulesResponse?> GetByDateRangeAsync(
        EstablishmentBranchId branchId,
        DateOnly fromDate,
        DateOnly toDate,
        CancellationToken cancellationToken)
    {
        List<BranchSpecialSchedule> schedules = await dbContext.Set<BranchSpecialSchedule>()
            .AsNoTracking()
            .Where(s => s.BranchId == branchId && s.IsActive && s.Date >= fromDate && s.Date <= toDate)
            .OrderBy(s => s.Date)
            .ThenBy(s => s.OpeningTime)
            .ToListAsync(cancellationToken);

        var grouped = schedules
            .GroupBy(s => s.Date)
            .Select(g =>
            {
                bool isClosed = g.Any(s => s.IsClosed);
                string? reason = g.First().Reason;

                var timeSlots = g
                    .Where(s => !s.IsClosed)
                    .Select(s => new BranchSpecialScheduleTimeSlotResponse(
                        s.Id.Value,
                        s.OpeningTime?.ToString("HH:mm", CultureInfo.InvariantCulture),
                        s.ClosingTime?.ToString("HH:mm", CultureInfo.InvariantCulture),
                        s.CrossesMidnight))
                    .ToList();

                return new BranchSpecialScheduleDateResponse(
                    g.Key.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                    isClosed,
                    reason,
                    timeSlots);
            })
            .ToList();

        return new BranchSpecialSchedulesResponse(
            branchId.Value,
            fromDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            toDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            grouped);
    }
}
