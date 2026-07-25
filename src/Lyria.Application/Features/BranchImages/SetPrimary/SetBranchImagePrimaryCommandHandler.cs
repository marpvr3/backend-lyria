using Lyria.Application.Abstractions.Messaging;
using Lyria.Application.Abstractions.Persistence;
using Lyria.Application.Common.Results;
using Lyria.Domain.Establishments.Branches;

namespace Lyria.Application.Features.BranchImages.SetPrimary;

public sealed class SetBranchImagePrimaryCommandHandler(
    IBranchImageRepository imageRepository,
    IEstablishmentBranchRepository branchRepository)
    : ICommandHandler<SetBranchImagePrimaryCommand>
{
    public async ValueTask<Result> Handle(
        SetBranchImagePrimaryCommand command,
        CancellationToken cancellationToken)
    {
        var branchId = new EstablishmentBranchId(command.BranchId);

        bool branchExists = await branchRepository.ExistsByIdAsync(branchId, cancellationToken);

        if (!branchExists)
        {
            return Result.Failure(
                BranchImageErrors.BranchNotFound(command.BranchId));
        }

        var imageId = new BranchImageId(command.ImageId);

        BranchImage? image = await imageRepository.GetByIdAsync(imageId, cancellationToken);

        if (image is null)
        {
            return Result.Failure(
                BranchImageErrors.NotFound(command.ImageId));
        }

        if (image.BranchId != branchId)
        {
            return Result.Failure(
                BranchImageErrors.DoesNotBelongToBranch(command.ImageId));
        }

        if (!image.IsActive)
        {
            return Result.Failure(
                BranchImageErrors.Inactive(command.ImageId));
        }

        if (image.IsPrimary)
        {
            return Result.Success();
        }

        List<BranchImage> activeImages =
            await imageRepository.GetActiveByBranchIdAsync(branchId, cancellationToken);

        foreach (BranchImage existing in activeImages)
        {
            if (existing.IsPrimary)
            {
                existing.UnsetPrimary();
            }
        }

        image.SetAsPrimary();

        await imageRepository.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
