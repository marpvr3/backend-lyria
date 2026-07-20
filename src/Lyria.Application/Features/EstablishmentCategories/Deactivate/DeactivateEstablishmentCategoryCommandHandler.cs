using Lyria.Application.Abstractions.Messaging;
using Lyria.Application.Abstractions.Persistence;
using Lyria.Application.Common.Results;
using Lyria.Domain.Establishments.Categories;

namespace Lyria.Application.Features.EstablishmentCategories.Deactivate;

public sealed class DeactivateEstablishmentCategoryCommandHandler(
    IEstablishmentCategoryRepository repository)
    : ICommandHandler<DeactivateEstablishmentCategoryCommand>
{
    public async ValueTask<Result> Handle(
        DeactivateEstablishmentCategoryCommand command,
        CancellationToken cancellationToken)
    {
        var id = new EstablishmentCategoryId(command.Id);

        EstablishmentCategory? category = await repository.GetByIdAsync(id, cancellationToken);

        if (category is null)
        {
            return Result.Failure(EstablishmentCategoryErrors.NotFound(command.Id));
        }

        category.Deactivate();

        await repository.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
