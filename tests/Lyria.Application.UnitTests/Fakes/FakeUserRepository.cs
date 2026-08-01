using Lyria.Application.Abstractions.Persistence;
using Lyria.Domain.Users;

namespace Lyria.Application.UnitTests.Fakes;

internal sealed class FakeUserRepository : IUserRepository
{
    private readonly List<User> _users = [];
    public IReadOnlyList<User> Added => _users;
    public int SaveChangesCallCount { get; private set; }

    public Task<User?> GetByIdAsync(
        UserId id,
        CancellationToken cancellationToken)
    {
        User? found = _users.FirstOrDefault(u => u.Id == id);
        return Task.FromResult(found);
    }

    public Task<bool> ExistsByEmailAsync(
        string normalizedEmail,
        UserId? excludingId,
        CancellationToken cancellationToken)
    {
        bool exists = _users.Any(u =>
            string.Equals(u.Email, normalizedEmail, StringComparison.OrdinalIgnoreCase) &&
            (excludingId is null || u.Id != excludingId.Value));
        return Task.FromResult(exists);
    }

    public Task AddAsync(
        User user,
        CancellationToken cancellationToken)
    {
        _users.Add(user);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        SaveChangesCallCount++;
        return Task.CompletedTask;
    }

    public void Seed(User user)
    {
        _users.Add(user);
    }
}
