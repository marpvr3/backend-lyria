using Lyria.Application.Abstractions.Messaging;
using Lyria.Application.Abstractions.Persistence;
using Lyria.Application.Common.Results;
using Lyria.Domain.Establishments.Branches;
using Lyria.Domain.Restrictions;

namespace Lyria.Application.Features.EstablishmentBranchRestrictions.Assign;

public sealed class AssignRestrictionToBranchCommandHandler(
    IEstablishmentBranchRestrictionRepository branchRestrictionRepository,
    IEstablishmentBranchRepository branchRepository,
    IRestrictionRepository restrictionRepository)
    : ICommandHandler<AssignRestrictionToBranchCommand>
{
    public async ValueTask<Result> Handle(
        AssignRestrictionToBranchCommand command,
        CancellationToken cancellationToken)
    {
        var branchId = new EstablishmentBranchId(command.BranchId);
        var restrictionId = new RestrictionId(command.RestrictionId);

        EstablishmentBranch? branch = await branchRepository.GetByIdAsync(branchId, cancellationToken);

        if (branch is null)
        {
            return Result.Failure(
                EstablishmentBranchRestrictionErrors.BranchNotFound(command.BranchId));
        }

        if (!branch.IsActive)
        {
            return Result.Failure(
                EstablishmentBranchRestrictionErrors.BranchInactive(command.BranchId));
        }

        Restriction? restriction = await restrictionRepository.GetByIdAsync(restrictionId, cancellationToken);

        if (restriction is null)
        {
            return Result.Failure(
                EstablishmentBranchRestrictionErrors.RestrictionNotFound(command.RestrictionId));
        }

        if (!restriction.IsActive)
        {
            return Result.Failure(
                EstablishmentBranchRestrictionErrors.RestrictionInactive(command.RestrictionId));
        }

        bool exists = await branchRestrictionRepository.ExistsAsync(branchId, restrictionId, cancellationToken);

        if (exists)
        {
            return Result.Failure(
                EstablishmentBranchRestrictionErrors.AlreadyExists(command.BranchId, command.RestrictionId));
        }

        var branchRestriction = EstablishmentBranchRestriction.Create(
            branchId, restrictionId, command.ComplianceLevel, command.IsCertified, command.Observation);

        branchRestrictionRepository.Add(branchRestriction);
        await branchRestrictionRepository.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
