using Lyria.Application.Abstractions.Persistence;
using Lyria.Application.Features.UserRoles;
using Lyria.Domain.Roles;
using Lyria.Domain.Users;
using Lyria.Domain.Users.UserRoles;
using Microsoft.EntityFrameworkCore;

namespace Lyria.Infrastructure.Persistence.ReadServices;

internal sealed class UserRoleReadService(LyriaDbContext dbContext)
    : IUserRoleReadService
{
    public async Task<IReadOnlyList<UserRoleResponse>> GetByUserIdAsync(
        UserId userId,
        CancellationToken cancellationToken)
    {
        return await dbContext.Set<UserRole>()
            .AsNoTracking()
            .Where(ur => ur.UserId == userId)
            .Join(
                dbContext.Set<Role>().AsNoTracking(),
                ur => ur.RoleId,
                r => r.Id,
                (ur, r) => new UserRoleResponse(
                    ur.Id.Value,
                    ur.UserId.Value,
                    ur.RoleId.Value,
                    r.Name,
                    ur.ScopeType.ToString(),
                    ur.EstablishmentId == null ? null : ur.EstablishmentId.Value.Value,
                    ur.BranchId == null ? null : ur.BranchId.Value.Value,
                    ur.IsActive,
                    ur.AssignedAtUtc,
                    ur.EndedAtUtc))
            .ToListAsync(cancellationToken);
    }

    public async Task<UserRoleResponse?> GetByIdAsync(
        UserRoleId id,
        CancellationToken cancellationToken)
    {
        return await dbContext.Set<UserRole>()
            .AsNoTracking()
            .Where(ur => ur.Id == id)
            .Join(
                dbContext.Set<Role>().AsNoTracking(),
                ur => ur.RoleId,
                r => r.Id,
                (ur, r) => new UserRoleResponse(
                    ur.Id.Value,
                    ur.UserId.Value,
                    ur.RoleId.Value,
                    r.Name,
                    ur.ScopeType.ToString(),
                    ur.EstablishmentId == null ? null : ur.EstablishmentId.Value.Value,
                    ur.BranchId == null ? null : ur.BranchId.Value.Value,
                    ur.IsActive,
                    ur.AssignedAtUtc,
                    ur.EndedAtUtc))
            .FirstOrDefaultAsync(cancellationToken);
    }
}
