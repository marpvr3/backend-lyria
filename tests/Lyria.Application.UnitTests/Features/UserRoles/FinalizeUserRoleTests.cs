using Lyria.Application.Common.Errors;
using Lyria.Application.Common.Results;
using Lyria.Application.Features.UserRoles.Finalize;
using Lyria.Application.UnitTests.Fakes;
using Lyria.Domain.Roles;
using Lyria.Domain.Users;
using Lyria.Domain.Users.UserRoles;
using Xunit;

namespace Lyria.Application.UnitTests.Features.UserRoles;

public sealed class FinalizeUserRoleTests
{
    private readonly FakeUserRoleRepository _repository = new();
    private readonly TimeProvider _timeProvider = TimeProvider.System;
    private readonly FinalizeUserRoleCommandHandler _handler;

    public FinalizeUserRoleTests()
    {
        _handler = new FinalizeUserRoleCommandHandler(_repository, _timeProvider);
    }

    [Fact]
    public async Task Handle_WhenValid_FinalizesUserRole()
    {
        var userId = UserId.New();
        var roleId = RoleId.New();
        var userRoleId = UserRoleId.New();

        var userRole = UserRole.Assign(
            userRoleId, userId, roleId, ScopeType.Global, null, null, DateTime.UtcNow);
        _repository.Seed(userRole);

        var command = new FinalizeUserRoleCommand(userId.Value, userRoleId.Value);

        Result result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(_repository.Added[0].IsActive);
        Assert.NotNull(_repository.Added[0].EndedAtUtc);
        Assert.Equal(1, _repository.SaveChangesCallCount);
    }

    [Fact]
    public async Task Handle_WhenNotFound_ReturnsNotFound()
    {
        var command = new FinalizeUserRoleCommand(Guid.NewGuid(), Guid.NewGuid());

        Result result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
        Assert.Equal("UserRoles.NotFound", result.Error.Code);
    }
}
