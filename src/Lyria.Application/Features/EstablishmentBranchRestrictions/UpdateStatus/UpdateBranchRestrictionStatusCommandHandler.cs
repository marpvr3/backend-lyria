using Lyria.Application.Abstractions.Messaging;
using Lyria.Application.Abstractions.Persistence;
using Lyria.Application.Common.Results;
using Lyria.Domain.Establishments.Branches;
using Lyria.Domain.Restrictions;

namespace Lyria.Application.Features.EstablishmentBranchRestrictions.UpdateStatus;

public sealed class UpdateBranchRestrictionStatusCommandHandler(
    IEstablishmentBranchRestrictionRepository repository)
    : ICommandHandler<UpdateBranchRestrictionStatusCommand>
{
    public async ValueTask<Result> Handle(
        UpdateBranchRestrictionStatusCommand command,
        CancellationToken cancellationToken)
    {
        var branchId = new EstablishmentBranchId(command.BranchId);
        var restrictionId = new RestrictionId(command.RestrictionId);

        EstablishmentBranchRestriction? branchRestriction =
            await repository.GetByIdsAsync(branchId, restrictionId, cancellationToken);

        if (branchRestriction is null)
        {
            return Result.Failure(
                EstablishmentBranchRestrictionErrors.NotFound(command.BranchId, command.RestrictionId));
        }

        if (command.IsActive)
        {
            branchRestriction.Activate();
        }
        else
        {
            branchRestriction.Deactivate();
        }

        await repository.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
