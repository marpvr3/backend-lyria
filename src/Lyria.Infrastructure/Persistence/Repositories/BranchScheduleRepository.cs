using Lyria.Application.Abstractions.Persistence;
using Lyria.Domain.Establishments.Branches;
using Microsoft.EntityFrameworkCore;

namespace Lyria.Infrastructure.Persistence.Repositories;

internal sealed class BranchScheduleRepository(LyriaDbContext dbContext)
    : IBranchScheduleRepository
{
    public async Task<List<BranchSchedule>> GetActiveByBranchIdAsync(
        EstablishmentBranchId branchId,
        CancellationToken cancellationToken)
    {
        return await dbContext.Set<BranchSchedule>()
            .Where(s => s.BranchId == branchId && s.IsActive)
            .OrderBy(s => s.DayOfWeek)
            .ThenBy(s => s.OpeningTime)
            .ToListAsync(cancellationToken);
    }

    public async Task ReplaceSchedulesAsync(
        EstablishmentBranchId branchId,
        IReadOnlyList<BranchSchedule> newSchedules,
        CancellationToken cancellationToken)
    {
        List<BranchSchedule> existing = await dbContext.Set<BranchSchedule>()
            .Where(s => s.BranchId == branchId && s.IsActive)
            .ToListAsync(cancellationToken);

        foreach (BranchSchedule schedule in existing)
        {
            schedule.Deactivate();
        }

        dbContext.Set<BranchSchedule>().AddRange(newSchedules);

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
