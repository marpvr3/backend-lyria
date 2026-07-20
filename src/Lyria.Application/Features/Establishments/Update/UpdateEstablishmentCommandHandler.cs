using Lyria.Application.Abstractions.Messaging;
using Lyria.Application.Abstractions.Persistence;
using Lyria.Application.Common.Results;
using Lyria.Domain.Establishments;
using Lyria.Domain.Establishments.Categories;

namespace Lyria.Application.Features.Establishments.Update;

public sealed class UpdateEstablishmentCommandHandler(
    IEstablishmentRepository repository,
    IEstablishmentCategoryReadService categoryReadService,
    TimeProvider timeProvider)
    : ICommandHandler<UpdateEstablishmentCommand>
{
    public async ValueTask<Result> Handle(
        UpdateEstablishmentCommand command,
        CancellationToken cancellationToken)
    {
        var id = new EstablishmentId(command.Id);

        Establishment? establishment = await repository.GetByIdAsync(id, cancellationToken);

        if (establishment is null)
        {
            return Result.Failure(EstablishmentErrors.NotFound(command.Id));
        }

        var categoryId = new EstablishmentCategoryId(command.CategoryId);

        var category = await categoryReadService.GetActiveByIdAsync(
            categoryId, cancellationToken);

        if (category is null)
        {
            return Result.Failure(EstablishmentErrors.CategoryNotAvailable(command.CategoryId));
        }

        string normalizedSlug = Establishment.NormalizeSlug(command.Slug);

        bool slugExists = await repository.ExistsBySlugAsync(
            normalizedSlug, excludingId: id, cancellationToken);

        if (slugExists)
        {
            return Result.Failure(EstablishmentErrors.SlugAlreadyExists(normalizedSlug));
        }

        establishment.UpdateDetails(
            command.Name,
            command.Slug,
            command.Description,
            command.Website,
            command.Instagram,
            command.LogoUrl,
            command.ContactEmail,
            command.ContactPhone);

        establishment.ChangeCategory(categoryId);

        if (command.IsVerified)
        {
            establishment.Verify(timeProvider.GetUtcNow().UtcDateTime);
        }
        else
        {
            establishment.RevokeVerification();
        }

        await repository.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
