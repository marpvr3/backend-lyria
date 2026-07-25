using Lyria.Application.Abstractions.Persistence;
using Lyria.Application.Features.BranchImages;
using Lyria.Domain.Establishments.Branches;
using Microsoft.EntityFrameworkCore;

namespace Lyria.Infrastructure.Persistence.ReadServices;

internal sealed class BranchImageReadService(LyriaDbContext dbContext)
    : IBranchImageReadService
{
    public async Task<bool> BranchExistsAsync(
        EstablishmentBranchId branchId,
        CancellationToken cancellationToken)
    {
        return await dbContext.Set<EstablishmentBranch>()
            .AsNoTracking()
            .AnyAsync(b => b.Id == branchId, cancellationToken);
    }

    public async Task<BranchImagesResponse?> GetByBranchIdAsync(
        EstablishmentBranchId branchId,
        CancellationToken cancellationToken)
    {
        List<BranchImage> images = await dbContext.Set<BranchImage>()
            .AsNoTracking()
            .Where(i => i.BranchId == branchId && i.IsActive)
            .OrderBy(i => i.SortOrder)
            .ThenBy(i => i.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        var items = images
            .Select(i => new BranchImageResponse(
                i.Id.Value,
                i.Url,
                i.FileName,
                i.AlternativeText,
                i.IsPrimary,
                i.SortOrder,
                i.IsActive))
            .ToList();

        return new BranchImagesResponse(branchId.Value, items);
    }
}
