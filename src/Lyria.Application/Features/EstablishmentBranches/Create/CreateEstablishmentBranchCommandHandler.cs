using Lyria.Application.Abstractions.Messaging;
using Lyria.Application.Abstractions.Persistence;
using Lyria.Application.Common.Results;
using Lyria.Domain.Establishments;
using Lyria.Domain.Establishments.Branches;

namespace Lyria.Application.Features.EstablishmentBranches.Create;

public sealed class CreateEstablishmentBranchCommandHandler(
    IEstablishmentBranchRepository branchRepository,
    IEstablishmentRepository establishmentRepository)
    : ICommandHandler<CreateEstablishmentBranchCommand, EstablishmentBranchId>
{
    public async ValueTask<Result<EstablishmentBranchId>> Handle(
        CreateEstablishmentBranchCommand command,
        CancellationToken cancellationToken)
    {
        var establishmentId = new EstablishmentId(command.EstablishmentId);

        Establishment? establishment = await establishmentRepository.GetByIdAsync(
            establishmentId, cancellationToken);

        if (establishment is null)
        {
            return Result.Failure<EstablishmentBranchId>(
                EstablishmentBranchErrors.EstablishmentNotFound(command.EstablishmentId));
        }

        if (!establishment.IsActive)
        {
            return Result.Failure<EstablishmentBranchId>(
                EstablishmentBranchErrors.EstablishmentInactive(command.EstablishmentId));
        }

        string normalizedName = EstablishmentBranch.NormalizeName(command.Name);

        bool nameExists = await branchRepository.ExistsByNameWithinEstablishmentAsync(
            establishmentId, normalizedName, excludingId: null, cancellationToken);

        if (nameExists)
        {
            return Result.Failure<EstablishmentBranchId>(
                EstablishmentBranchErrors.NameAlreadyExists(normalizedName));
        }

        var id = EstablishmentBranchId.New();

        var branch = EstablishmentBranch.Create(
            id,
            establishmentId,
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

        branchRepository.Add(branch);
        await branchRepository.SaveChangesAsync(cancellationToken);

        return Result.Success(branch.Id);
    }
}
