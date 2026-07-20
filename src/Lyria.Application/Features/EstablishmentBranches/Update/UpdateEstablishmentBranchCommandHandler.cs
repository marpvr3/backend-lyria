using Lyria.Application.Abstractions.Messaging;
using Lyria.Application.Abstractions.Persistence;
using Lyria.Application.Common.Results;
using Lyria.Domain.Establishments.Branches;

namespace Lyria.Application.Features.EstablishmentBranches.Update;

public sealed class UpdateEstablishmentBranchCommandHandler(
    IEstablishmentBranchRepository branchRepository)
    : ICommandHandler<UpdateEstablishmentBranchCommand>
{
    public async ValueTask<Result> Handle(
        UpdateEstablishmentBranchCommand command,
        CancellationToken cancellationToken)
    {
        var id = new EstablishmentBranchId(command.Id);

        EstablishmentBranch? branch = await branchRepository.GetByIdAsync(id, cancellationToken);

        if (branch is null)
        {
            return Result.Failure(EstablishmentBranchErrors.NotFound(command.Id));
        }

        string normalizedName = EstablishmentBranch.NormalizeName(command.Name);

        bool nameExists = await branchRepository.ExistsByNameWithinEstablishmentAsync(
            branch.EstablishmentId, normalizedName, excludingId: id, cancellationToken);

        if (nameExists)
        {
            return Result.Failure(EstablishmentBranchErrors.NameAlreadyExists(normalizedName));
        }

        branch.UpdateDetails(
            command.Name,
            command.Street,
            command.Number,
            command.AddressComplement,
            command.Neighborhood,
            command.City,
            command.Province,
            command.PostalCode,
            command.Country,
            command.Latitude,
            command.Longitude,
            command.Phone,
            command.WhatsApp,
            command.Email);

        await branchRepository.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
