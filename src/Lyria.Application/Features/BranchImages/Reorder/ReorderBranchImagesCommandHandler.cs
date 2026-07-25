using Lyria.Application.Abstractions.Messaging;
using Lyria.Application.Abstractions.Persistence;
using Lyria.Application.Common.Results;
using Lyria.Domain.Establishments.Branches;

namespace Lyria.Application.Features.BranchImages.Reorder;

public sealed class ReorderBranchImagesCommandHandler(
    IBranchImageRepository imageRepository,
    IEstablishmentBranchRepository branchRepository)
    : ICommandHandler<ReorderBranchImagesCommand>
{
    public async ValueTask<Result> Handle(
        ReorderBranchImagesCommand command,
        CancellationToken cancellationToken)
    {
        var branchId = new EstablishmentBranchId(command.BranchId);

        bool branchExists = await branchRepository.ExistsByIdAsync(branchId, cancellationToken);

        if (!branchExists)
        {
            return Result.Failure(
                BranchImageErrors.BranchNotFound(command.BranchId));
        }

        var uniqueIds = new HashSet<Guid>(command.Images.Count);
        foreach (ImageOrderItem item in command.Images)
        {
            if (!uniqueIds.Add(item.ImageId))
            {
                return Result.Failure(
                    BranchImageErrors.DuplicateImageOrderEntry());
            }
        }

        List<BranchImage> activeImages =
            await imageRepository.GetActiveByBranchIdAsync(branchId, cancellationToken);

        var imageMap = activeImages.ToDictionary(i => i.Id.Value);

        foreach (ImageOrderItem item in command.Images)
        {
            if (!imageMap.TryGetValue(item.ImageId, out BranchImage? image))
            {
                return Result.Failure(
                    BranchImageErrors.ImageNotFromBranch(item.ImageId));
            }

            if (item.SortOrder < 0)
            {
                return Result.Failure(
                    BranchImageErrors.InvalidSortOrder());
            }
        }

        foreach (ImageOrderItem item in command.Images)
        {
            BranchImage image = imageMap[item.ImageId];
            image.UpdateMetadata(
                image.Url,
                image.FileName,
                image.AlternativeText,
                item.SortOrder);
        }

        await imageRepository.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
