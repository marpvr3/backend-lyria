using Lyria.Application.Abstractions.Messaging;
using Lyria.Application.Abstractions.Persistence;
using Lyria.Application.Common.Results;
using Lyria.Domain.Restrictions;

namespace Lyria.Application.Features.Restrictions.Create;

public sealed class CreateRestrictionCommandHandler(
    IRestrictionRepository repository)
    : ICommandHandler<CreateRestrictionCommand, RestrictionId>
{
    public async ValueTask<Result<RestrictionId>> Handle(
        CreateRestrictionCommand command,
        CancellationToken cancellationToken)
    {
        string normalizedName = Restriction.NormalizeName(command.Name);

        bool nameExists = await repository.ExistsByNameAsync(
            normalizedName, excludingId: null, cancellationToken);

        if (nameExists)
        {
            return Result.Failure<RestrictionId>(RestrictionErrors.NameAlreadyExists(normalizedName));
        }

        var id = RestrictionId.New();

        var restriction = Restriction.Create(id, command.Name, command.Description);

        await repository.AddAsync(restriction, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);

        return Result.Success(restriction.Id);
    }
}
