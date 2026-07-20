using Lyria.Application.Abstractions.Messaging;
using Lyria.Application.Abstractions.Persistence;
using Lyria.Application.Common.Results;
using Lyria.Domain.Restrictions;

namespace Lyria.Application.Features.Restrictions.Update;

public sealed class UpdateRestrictionCommandHandler(
    IRestrictionRepository repository)
    : ICommandHandler<UpdateRestrictionCommand>
{
    public async ValueTask<Result> Handle(
        UpdateRestrictionCommand command,
        CancellationToken cancellationToken)
    {
        var id = new RestrictionId(command.Id);

        Restriction? restriction = await repository.GetByIdAsync(id, cancellationToken);

        if (restriction is null)
        {
            return Result.Failure(RestrictionErrors.NotFound(command.Id));
        }

        string normalizedName = Restriction.NormalizeName(command.Name);

        bool nameExists = await repository.ExistsByNameAsync(
            normalizedName, excludingId: id, cancellationToken);

        if (nameExists)
        {
            return Result.Failure(RestrictionErrors.NameAlreadyExists(normalizedName));
        }

        restriction.Update(command.Name, command.Description);

        await repository.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
