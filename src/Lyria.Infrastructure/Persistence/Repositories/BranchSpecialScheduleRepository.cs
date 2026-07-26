using Lyria.Application.Abstractions.Persistence;
using Lyria.Domain.Establishments.Branches;
using Microsoft.EntityFrameworkCore;

namespace Lyria.Infrastructure.Persistence.Repositories;

internal sealed class BranchSpecialScheduleRepository(LyriaDbContext dbContext)
    : IBranchSpecialScheduleRepository
{
    public async Task<List<BranchSpecialSchedule>> GetActiveByBranchAndDateAsync(
        EstablishmentBranchId branchId,
        DateOnly scheduleDate,
        CancellationToken cancellationToken)
    {
        return await dbContext.Set<BranchSpecialSchedule>()
            .Where(s => s.BranchId == branchId && s.Date == scheduleDate && s.IsActive)
            .OrderBy(s => s.OpeningTime)
            .ToListAsync(cancellationToken);
    }

    public async Task ReplaceForDateAsync(
        EstablishmentBranchId branchId,
        DateOnly scheduleDate,
        IReadOnlyList<BranchSpecialSchedule> newSchedules,
        CancellationToken cancellationToken)
    {
        List<BranchSpecialSchedule> existing = await dbContext.Set<BranchSpecialSchedule>()
            .Where(s => s.BranchId == branchId && s.Date == scheduleDate && s.IsActive)
            .ToListAsync(cancellationToken);

        foreach (BranchSpecialSchedule schedule in existing)
        {
            schedule.Deactivate();
        }

        dbContext.Set<BranchSpecialSchedule>().AddRange(newSchedules);

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
