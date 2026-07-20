using Lyria.Application.Abstractions.Messaging;
using Lyria.Application.Abstractions.Persistence;
using Lyria.Application.Common.Results;
using Lyria.Domain.Establishments.Categories;

namespace Lyria.Application.Features.EstablishmentCategories.Create;

public sealed class CreateEstablishmentCategoryCommandHandler(
    IEstablishmentCategoryRepository repository)
    : ICommandHandler<CreateEstablishmentCategoryCommand, EstablishmentCategoryId>
{
    public async ValueTask<Result<EstablishmentCategoryId>> Handle(
        CreateEstablishmentCategoryCommand command,
        CancellationToken cancellationToken)
    {
        string normalizedName = EstablishmentCategory.NormalizeName(command.Name);

        bool nameExists = await repository.ExistsByNameAsync(
            normalizedName, excludingId: null, cancellationToken);

        if (nameExists)
        {
            return Result.Failure<EstablishmentCategoryId>(
                EstablishmentCategoryErrors.NameAlreadyExists(normalizedName));
        }

        var id = EstablishmentCategoryId.New();

        var category = EstablishmentCategory.Create(
            id,
            command.Name,
            command.Description,
            command.IconUrl,
            command.SortOrder);

        await repository.AddAsync(category, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);

        return Result.Success(category.Id);
    }
}
