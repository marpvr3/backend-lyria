using Lyria.Application.Abstractions.Messaging;
using Lyria.Application.Abstractions.Persistence;
using Lyria.Application.Common.Results;
using Lyria.Domain.Establishments.Branches;

namespace Lyria.Application.Features.BranchImages.Create;

public sealed class CreateBranchImageCommandHandler(
    IBranchImageRepository imageRepository,
    IEstablishmentBranchRepository branchRepository)
    : ICommandHandler<CreateBranchImageCommand, BranchImageId>
{
    public async ValueTask<Result<BranchImageId>> Handle(
        CreateBranchImageCommand command,
        CancellationToken cancellationToken)
    {
        var branchId = new EstablishmentBranchId(command.BranchId);

        bool branchExists = await branchRepository.ExistsByIdAsync(branchId, cancellationToken);

        if (!branchExists)
        {
            return Result.Failure<BranchImageId>(
                BranchImageErrors.BranchNotFound(command.BranchId));
        }

        var id = BranchImageId.New();

        var image = BranchImage.Create(
            id,
            branchId,
            command.Url,
            command.FileName,
            command.AlternativeText,
            command.IsPrimary,
            command.SortOrder);

        if (command.IsPrimary)
        {
            List<BranchImage> activeImages =
                await imageRepository.GetActiveByBranchIdAsync(branchId, cancellationToken);

            foreach (BranchImage existing in activeImages)
            {
                if (existing.IsPrimary)
                {
                    existing.UnsetPrimary();
                }
            }
        }

        imageRepository.Add(image);
        await imageRepository.SaveChangesAsync(cancellationToken);

        return Result.Success(image.Id);
    }
}
