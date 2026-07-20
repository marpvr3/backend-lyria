using Lyria.Application.Abstractions.Messaging;
using Lyria.Application.Abstractions.Persistence;
using Lyria.Application.Common.Results;
using Lyria.Domain.Services;

namespace Lyria.Application.Features.Services.Create;

public sealed class CreateServiceCommandHandler(
    IServiceRepository repository)
    : ICommandHandler<CreateServiceCommand, ServiceId>
{
    public async ValueTask<Result<ServiceId>> Handle(
        CreateServiceCommand command,
        CancellationToken cancellationToken)
    {
        string normalizedName = Service.NormalizeName(command.Name);

        bool nameExists = await repository.ExistsByNameAsync(
            normalizedName, excludingId: null, cancellationToken);

        if (nameExists)
        {
            return Result.Failure<ServiceId>(ServiceErrors.NameAlreadyExists(normalizedName));
        }

        var id = ServiceId.New();

        var service = Service.Create(id, command.Name, command.Description, command.IconUrl);

        await repository.AddAsync(service, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);

        return Result.Success(service.Id);
    }
}
