using Lyria.Application.Common;
using Lyria.Application.Features.Restrictions;
using Lyria.Domain.Restrictions;

namespace Lyria.Application.Abstractions.Persistence;

public interface IRestrictionReadService
{
    Task<RestrictionResponse?> GetByIdAsync(
        RestrictionId id,
        CancellationToken cancellationToken);

    Task<PagedResponse<RestrictionListItemResponse>> ListAsync(
        RestrictionListFilter filter,
        CancellationToken cancellationToken);
}
