using Lyria.Application.Abstractions.Messaging;
using Lyria.Application.Abstractions.Persistence;
using Lyria.Application.Common.Results;
using Lyria.Domain.Establishments.Branches;
using Lyria.Domain.Restrictions;

namespace Lyria.Application.Features.EstablishmentBranchRestrictions.Update;

public sealed class UpdateBranchRestrictionCommandHandler(
    IEstablishmentBranchRestrictionRepository repository)
    : ICommandHandler<UpdateBranchRestrictionCommand>
{
    public async ValueTask<Result> Handle(
        UpdateBranchRestrictionCommand command,
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

        branchRestriction.Update(command.ComplianceLevel, command.IsCertified, command.Observation);

        await repository.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
