using Lyria.Application.Abstractions.Messaging;
using Lyria.Application.Abstractions.Persistence;
using Lyria.Application.Common.Results;
using Lyria.Domain.Restrictions;

namespace Lyria.Application.Features.Restrictions.GetById;

public sealed class GetRestrictionByIdQueryHandler(
    IRestrictionReadService readService)
    : IQueryHandler<GetRestrictionByIdQuery, Result<RestrictionResponse>>
{
    public async ValueTask<Result<RestrictionResponse>> Handle(
        GetRestrictionByIdQuery query,
        CancellationToken cancellationToken)
    {
        var id = new RestrictionId(query.Id);

        RestrictionResponse? response = await readService.GetByIdAsync(id, cancellationToken);

        if (response is null)
        {
            return Result.Failure<RestrictionResponse>(RestrictionErrors.NotFound(query.Id));
        }

        return Result.Success(response);
    }
}
