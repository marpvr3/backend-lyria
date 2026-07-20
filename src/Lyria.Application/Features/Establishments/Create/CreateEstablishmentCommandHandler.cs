using Lyria.Application.Abstractions.Messaging;
using Lyria.Application.Abstractions.Persistence;
using Lyria.Application.Common.Results;
using Lyria.Domain.Establishments;
using Lyria.Domain.Establishments.Categories;

namespace Lyria.Application.Features.Establishments.Create;

public sealed class CreateEstablishmentCommandHandler(
    IEstablishmentRepository repository,
    IEstablishmentCategoryReadService categoryReadService)
    : ICommandHandler<CreateEstablishmentCommand, EstablishmentId>
{
    public async ValueTask<Result<EstablishmentId>> Handle(
        CreateEstablishmentCommand command,
        CancellationToken cancellationToken)
    {
        var categoryId = new EstablishmentCategoryId(command.CategoryId);

        var category = await categoryReadService.GetActiveByIdAsync(
            categoryId, cancellationToken);

        if (category is null)
        {
            return Result.Failure<EstablishmentId>(
                EstablishmentErrors.CategoryNotAvailable(command.CategoryId));
        }

        string normalizedSlug = Establishment.NormalizeSlug(command.Slug);

        bool slugExists = await repository.ExistsBySlugAsync(
            normalizedSlug, excludingId: null, cancellationToken);

        if (slugExists)
        {
            return Result.Failure<EstablishmentId>(
                EstablishmentErrors.SlugAlreadyExists(normalizedSlug));
        }

        var id = EstablishmentId.New();

        var establishment = Establishment.Create(
            id,
            categoryId,
            command.Name,
            command.Slug,
            command.Description,
            command.Website,
            command.Instagram,
            command.LogoUrl,
            command.ContactEmail,
            command.ContactPhone);

        await repository.AddAsync(establishment, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);

        return Result.Success(establishment.Id);
    }
}
