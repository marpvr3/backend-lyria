using Lyria.Application.Abstractions.Messaging;
using Lyria.Application.Abstractions.Persistence;
using Lyria.Application.Common.Results;
using Lyria.Domain.Restrictions;

namespace Lyria.Application.Features.Restrictions.UpdateStatus;

public sealed class UpdateRestrictionStatusCommandHandler(
    IRestrictionRepository repository)
    : ICommandHandler<UpdateRestrictionStatusCommand>
{
    public async ValueTask<Result> Handle(
        UpdateRestrictionStatusCommand command,
        CancellationToken cancellationToken)
    {
        var id = new RestrictionId(command.Id);

        Restriction? restriction = await repository.GetByIdAsync(id, cancellationToken);

        if (restriction is null)
        {
            return Result.Failure(RestrictionErrors.NotFound(command.Id));
        }

        if (command.IsActive)
        {
            restriction.Activate();
        }
        else
        {
            restriction.Deactivate();
        }

        await repository.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
