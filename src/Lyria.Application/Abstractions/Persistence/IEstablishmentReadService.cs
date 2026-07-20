using Lyria.Application.Common;
using Lyria.Application.Features.Establishments;
using Lyria.Domain.Establishments;

namespace Lyria.Application.Abstractions.Persistence;

public interface IEstablishmentReadService
{
    Task<EstablishmentResponse?> GetByIdAsync(
        EstablishmentId id,
        CancellationToken cancellationToken);

    Task<PagedResponse<EstablishmentListItemResponse>> ListAsync(
        EstablishmentListFilter filter,
        CancellationToken cancellationToken);
}
