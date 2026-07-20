using Lyria.Application.Abstractions.Messaging;
using Lyria.Application.Abstractions.Persistence;
using Lyria.Application.Common.Results;
using Lyria.Domain.Services;

namespace Lyria.Application.Features.Services.Update;

public sealed class UpdateServiceCommandHandler(
    IServiceRepository repository)
    : ICommandHandler<UpdateServiceCommand>
{
    public async ValueTask<Result> Handle(
        UpdateServiceCommand command,
        CancellationToken cancellationToken)
    {
        var id = new ServiceId(command.Id);

        Service? service = await repository.GetByIdAsync(id, cancellationToken);

        if (service is null)
        {
            return Result.Failure(ServiceErrors.NotFound(command.Id));
        }

        string normalizedName = Service.NormalizeName(command.Name);

        bool nameExists = await repository.ExistsByNameAsync(
            normalizedName, excludingId: id, cancellationToken);

        if (nameExists)
        {
            return Result.Failure(ServiceErrors.NameAlreadyExists(normalizedName));
        }

        service.Update(command.Name, command.Description, command.IconUrl);

        await repository.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
