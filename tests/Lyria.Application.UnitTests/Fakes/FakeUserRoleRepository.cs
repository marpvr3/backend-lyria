using Lyria.Application.Abstractions.Persistence;
using Lyria.Domain.Users.UserRoles;

namespace Lyria.Application.UnitTests.Fakes;

internal sealed class FakeUserRoleRepository : IUserRoleRepository
{
    private readonly List<UserRole> _userRoles = [];
    public IReadOnlyList<UserRole> Added => _userRoles;
    public int SaveChangesCallCount { get; private set; }

    public Task<UserRole?> GetByIdAsync(
        UserRoleId id,
        CancellationToken cancellationToken)
    {
        UserRole? found = _userRoles.FirstOrDefault(ur => ur.Id == id);
        return Task.FromResult(found);
    }

    public Task AddAsync(
        UserRole userRole,
        CancellationToken cancellationToken)
    {
        _userRoles.Add(userRole);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        SaveChangesCallCount++;
        return Task.CompletedTask;
    }

    public void Seed(UserRole userRole)
    {
        _userRoles.Add(userRole);
    }
}
