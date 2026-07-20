using Lyria.Application.Abstractions.Messaging;
using Lyria.Application.Abstractions.Persistence;
using Lyria.Application.Common.Results;
using Lyria.Domain.Establishments.Branches;
using Lyria.Domain.Services;

namespace Lyria.Application.Features.EstablishmentBranchServices.Assign;

public sealed class AssignServiceToBranchCommandHandler(
    IEstablishmentBranchServiceRepository branchServiceRepository,
    IEstablishmentBranchRepository branchRepository,
    IServiceRepository serviceRepository)
    : ICommandHandler<AssignServiceToBranchCommand>
{
    public async ValueTask<Result> Handle(
        AssignServiceToBranchCommand command,
        CancellationToken cancellationToken)
    {
        var branchId = new EstablishmentBranchId(command.BranchId);
        var serviceId = new ServiceId(command.ServiceId);

        EstablishmentBranch? branch = await branchRepository.GetByIdAsync(branchId, cancellationToken);

        if (branch is null)
        {
            return Result.Failure(
                EstablishmentBranchServiceErrors.BranchNotFound(command.BranchId));
        }

        if (!branch.IsActive)
        {
            return Result.Failure(
                EstablishmentBranchServiceErrors.BranchInactive(command.BranchId));
        }

        Service? service = await serviceRepository.GetByIdAsync(serviceId, cancellationToken);

        if (service is null)
        {
            return Result.Failure(
                EstablishmentBranchServiceErrors.ServiceNotFound(command.ServiceId));
        }

        if (!service.IsActive)
        {
            return Result.Failure(
                EstablishmentBranchServiceErrors.ServiceInactive(command.ServiceId));
        }

        bool exists = await branchServiceRepository.ExistsAsync(branchId, serviceId, cancellationToken);

        if (exists)
        {
            return Result.Failure(
                EstablishmentBranchServiceErrors.AlreadyExists(command.BranchId, command.ServiceId));
        }

        var branchService = EstablishmentBranchService.Create(
            branchId, serviceId, command.IsAvailable, command.Observation);

        branchServiceRepository.Add(branchService);
        await branchServiceRepository.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
