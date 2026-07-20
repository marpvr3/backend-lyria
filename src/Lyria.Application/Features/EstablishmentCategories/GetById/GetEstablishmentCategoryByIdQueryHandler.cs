using Lyria.Application.Abstractions.Messaging;
using Lyria.Application.Abstractions.Persistence;
using Lyria.Application.Common.Results;
using Lyria.Domain.Establishments.Categories;

namespace Lyria.Application.Features.EstablishmentCategories.GetById;

public sealed class GetEstablishmentCategoryByIdQueryHandler(
    IEstablishmentCategoryReadService readService)
    : IQueryHandler<GetEstablishmentCategoryByIdQuery, Result<EstablishmentCategoryResponse>>
{
    public async ValueTask<Result<EstablishmentCategoryResponse>> Handle(
        GetEstablishmentCategoryByIdQuery query,
        CancellationToken cancellationToken)
    {
        var categoryId = new EstablishmentCategoryId(query.Id);

        EstablishmentCategoryResponse? response = await readService.GetActiveByIdAsync(
            categoryId, cancellationToken);

        if (response is null)
        {
            return Result.Failure<EstablishmentCategoryResponse>(
                EstablishmentCategoryErrors.NotFound(query.Id));
        }

        return Result.Success(response);
    }
}
