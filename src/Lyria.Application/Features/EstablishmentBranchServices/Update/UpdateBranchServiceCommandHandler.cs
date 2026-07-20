using Lyria.Application.Abstractions.Messaging;
using Lyria.Application.Abstractions.Persistence;
using Lyria.Application.Common.Results;
using Lyria.Domain.Establishments.Branches;
using Lyria.Domain.Services;

namespace Lyria.Application.Features.EstablishmentBranchServices.Update;

public sealed class UpdateBranchServiceCommandHandler(
    IEstablishmentBranchServiceRepository repository)
    : ICommandHandler<UpdateBranchServiceCommand>
{
    public async ValueTask<Result> Handle(
        UpdateBranchServiceCommand command,
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

        branchService.Update(command.IsAvailable, command.Observation);

        await repository.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
