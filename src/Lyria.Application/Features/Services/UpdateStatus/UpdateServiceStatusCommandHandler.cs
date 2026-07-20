using Lyria.Application.Abstractions.Messaging;
using Lyria.Application.Abstractions.Persistence;
using Lyria.Application.Common.Results;
using Lyria.Domain.Services;

namespace Lyria.Application.Features.Services.UpdateStatus;

public sealed class UpdateServiceStatusCommandHandler(
    IServiceRepository repository)
    : ICommandHandler<UpdateServiceStatusCommand>
{
    public async ValueTask<Result> Handle(
        UpdateServiceStatusCommand command,
        CancellationToken cancellationToken)
    {
        var id = new ServiceId(command.Id);

        Service? service = await repository.GetByIdAsync(id, cancellationToken);

        if (service is null)
        {
            return Result.Failure(ServiceErrors.NotFound(command.Id));
        }

        if (command.IsActive)
        {
            service.Activate();
        }
        else
        {
            service.Deactivate();
        }

        await repository.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
