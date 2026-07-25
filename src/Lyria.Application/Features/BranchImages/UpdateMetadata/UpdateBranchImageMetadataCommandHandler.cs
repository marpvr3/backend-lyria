using Lyria.Application.Abstractions.Messaging;
using Lyria.Application.Abstractions.Persistence;
using Lyria.Application.Common.Results;
using Lyria.Domain.Establishments.Branches;

namespace Lyria.Application.Features.BranchImages.UpdateMetadata;

public sealed class UpdateBranchImageMetadataCommandHandler(
    IBranchImageRepository imageRepository,
    IEstablishmentBranchRepository branchRepository)
    : ICommandHandler<UpdateBranchImageMetadataCommand>
{
    public async ValueTask<Result> Handle(
        UpdateBranchImageMetadataCommand command,
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

        image.UpdateMetadata(
            command.Url,
            command.FileName,
            command.AlternativeText,
            command.SortOrder);

        await imageRepository.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
