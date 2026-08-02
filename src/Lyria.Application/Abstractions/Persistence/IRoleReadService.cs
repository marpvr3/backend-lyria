using Lyria.Application.Common;
using Lyria.Application.Features.Roles;
using Lyria.Domain.Roles;

namespace Lyria.Application.Abstractions.Persistence;

public interface IRoleReadService
{
    Task<RoleResponse?> GetByIdAsync(
        RoleId id,
        CancellationToken cancellationToken);

    Task<PagedResponse<RoleListItemResponse>> ListAsync(
        RoleListFilter filter,
        CancellationToken cancellationToken);
}
