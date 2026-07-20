using Lyria.Application.Abstractions.Messaging;
using Lyria.Application.Abstractions.Persistence;
using Lyria.Application.Common.Results;
using Lyria.Domain.Establishments;

namespace Lyria.Application.Features.Establishments.UpdateStatus;

public sealed class UpdateEstablishmentStatusCommandHandler(
    IEstablishmentRepository repository)
    : ICommandHandler<UpdateEstablishmentStatusCommand>
{
    public async ValueTask<Result> Handle(
        UpdateEstablishmentStatusCommand command,
        CancellationToken cancellationToken)
    {
        var id = new EstablishmentId(command.Id);

        Establishment? establishment = await repository.GetByIdAsync(id, cancellationToken);

        if (establishment is null)
        {
            return Result.Failure(EstablishmentErrors.NotFound(command.Id));
        }

        if (command.IsActive)
        {
            establishment.Activate();
        }
        else
        {
            establishment.Deactivate();
        }

        await repository.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
