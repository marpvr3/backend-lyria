using Lyria.Application.Abstractions.Messaging;
using Lyria.Application.Abstractions.Persistence;
using Lyria.Application.Common.Results;
using Lyria.Domain.Establishments.Branches;
using Lyria.Domain.Services;

namespace Lyria.Application.Features.EstablishmentBranchServices.UpdateStatus;

public sealed class UpdateBranchServiceStatusCommandHandler(
    IEstablishmentBranchServiceRepository repository)
    : ICommandHandler<UpdateBranchServiceStatusCommand>
{
    public async ValueTask<Result> Handle(
        UpdateBranchServiceStatusCommand command,
        CancellationToken cancellationToken)
    {
        var branchId = new EstablishmentBranchId(command.BranchId);
        var serviceId = new ServiceId(command.ServiceId);

        EstablishmentBranchService? branchService =
            await repository.GetByIdsAsync(branchId, serviceId, cancellationToken);

        if (branchService is null)
        {
            return Result.Failure(
                EstablishmentBranchServiceErrors.NotFound(command.BranchId, command.ServiceId));
        }

        if (command.IsActive)
        {
            branchService.Activate();
        }
        else
        {
            branchService.Deactivate();
        }

        await repository.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
