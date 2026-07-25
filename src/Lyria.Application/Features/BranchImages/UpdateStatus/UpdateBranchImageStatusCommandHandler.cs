using Lyria.Application.Abstractions.Messaging;
using Lyria.Application.Abstractions.Persistence;
using Lyria.Application.Common.Results;
using Lyria.Domain.Establishments.Branches;

namespace Lyria.Application.Features.BranchImages.UpdateStatus;

public sealed class UpdateBranchImageStatusCommandHandler(
    IBranchImageRepository imageRepository,
    IEstablishmentBranchRepository branchRepository)
    : ICommandHandler<UpdateBranchImageStatusCommand>
{
    public async ValueTask<Result> Handle(
        UpdateBranchImageStatusCommand command,
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

        if (command.IsActive)
        {
            image.Activate();
        }
        else
        {
            image.Deactivate();
        }

        await imageRepository.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
