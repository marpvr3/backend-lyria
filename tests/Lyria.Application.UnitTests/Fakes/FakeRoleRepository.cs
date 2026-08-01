using Lyria.Application.Abstractions.Persistence;
using Lyria.Domain.Roles;

namespace Lyria.Application.UnitTests.Fakes;

internal sealed class FakeRoleRepository : IRoleRepository
{
    private readonly List<Role> _roles = [];
    public IReadOnlyList<Role> Added => _roles;
    public int SaveChangesCallCount { get; private set; }

    public Task<Role?> GetByIdAsync(
        RoleId id,
        CancellationToken cancellationToken)
    {
        Role? found = _roles.FirstOrDefault(r => r.Id == id);
        return Task.FromResult(found);
    }

    public Task<bool> ExistsByCodeAsync(
        string normalizedCode,
        RoleId? excludingId,
        CancellationToken cancellationToken)
    {
        bool exists = _roles.Any(r =>
            string.Equals(r.Code, normalizedCode, StringComparison.OrdinalIgnoreCase) &&
            (excludingId is null || r.Id != excludingId.Value));
        return Task.FromResult(exists);
    }

    public Task AddAsync(
        Role role,
        CancellationToken cancellationToken)
    {
        _roles.Add(role);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        SaveChangesCallCount++;
        return Task.CompletedTask;
    }

    public void Seed(Role role)
    {
        _roles.Add(role);
    }
}
