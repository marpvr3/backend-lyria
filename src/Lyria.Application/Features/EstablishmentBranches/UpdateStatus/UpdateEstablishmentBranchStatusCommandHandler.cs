using Lyria.Application.Abstractions.Messaging;
using Lyria.Application.Abstractions.Persistence;
using Lyria.Application.Common.Results;
using Lyria.Domain.Establishments.Branches;

namespace Lyria.Application.Features.EstablishmentBranches.UpdateStatus;

public sealed class UpdateEstablishmentBranchStatusCommandHandler(
    IEstablishmentBranchRepository branchRepository)
    : ICommandHandler<UpdateEstablishmentBranchStatusCommand>
{
    public async ValueTask<Result> Handle(
        UpdateEstablishmentBranchStatusCommand command,
        CancellationToken cancellationToken)
    {
        var id = new EstablishmentBranchId(command.Id);

        EstablishmentBranch? branch = await branchRepository.GetByIdAsync(id, cancellationToken);

        if (branch is null)
        {
            return Result.Failure(EstablishmentBranchErrors.NotFound(command.Id));
        }

        if (command.IsActive)
        {
            branch.Activate();
        }
        else
        {
            branch.Deactivate();
        }

        await branchRepository.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
