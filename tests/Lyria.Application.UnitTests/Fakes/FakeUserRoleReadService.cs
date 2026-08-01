using Lyria.Application.Abstractions.Persistence;
using Lyria.Application.Features.UserRoles;
using Lyria.Domain.Users;
using Lyria.Domain.Users.UserRoles;

namespace Lyria.Application.UnitTests.Fakes;

internal sealed class FakeUserRoleReadService : IUserRoleReadService
{
    private readonly List<UserRoleResponse> _responses = [];

    public Task<IReadOnlyList<UserRoleResponse>> GetByUserIdAsync(
        UserId userId,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<UserRoleResponse> found = _responses
            .Where(r => r.UserId == userId.Value)
            .ToList();
        return Task.FromResult(found);
    }

    public Task<UserRoleResponse?> GetByIdAsync(
        UserRoleId id,
        CancellationToken cancellationToken)
    {
        UserRoleResponse? found = _responses.FirstOrDefault(r => r.Id == id.Value);
        return Task.FromResult(found);
    }

    public void Seed(UserRoleResponse response)
    {
        _responses.Add(response);
    }
}
