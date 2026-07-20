using Lyria.Application.Abstractions.Messaging;
using Lyria.Application.Abstractions.Persistence;
using Lyria.Application.Common.Results;
using Lyria.Domain.Establishments.Categories;

namespace Lyria.Application.Features.EstablishmentCategories.Update;

public sealed class UpdateEstablishmentCategoryCommandHandler(
    IEstablishmentCategoryRepository repository)
    : ICommandHandler<UpdateEstablishmentCategoryCommand>
{
    public async ValueTask<Result> Handle(
        UpdateEstablishmentCategoryCommand command,
        CancellationToken cancellationToken)
    {
        var id = new EstablishmentCategoryId(command.Id);

        EstablishmentCategory? category = await repository.GetByIdAsync(id, cancellationToken);

        if (category is null)
        {
            return Result.Failure(EstablishmentCategoryErrors.NotFound(command.Id));
        }

        string normalizedName = EstablishmentCategory.NormalizeName(command.Name);

        bool nameExists = await repository.ExistsByNameAsync(
            normalizedName, excludingId: id, cancellationToken);

        if (nameExists)
        {
            return Result.Failure(EstablishmentCategoryErrors.NameAlreadyExists(normalizedName));
        }

        category.UpdateDetails(command.Name, command.Description, command.IconUrl, command.SortOrder);

        await repository.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
