using Lyria.Application.Common;
using Lyria.Application.Features.Services;
using Lyria.Domain.Services;

namespace Lyria.Application.Abstractions.Persistence;

public interface IServiceReadService
{
    Task<ServiceResponse?> GetByIdAsync(
        ServiceId id,
        CancellationToken cancellationToken);

    Task<PagedResponse<ServiceListItemResponse>> ListAsync(
        ServiceListFilter filter,
        CancellationToken cancellationToken);
}
