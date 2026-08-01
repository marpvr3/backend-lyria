using Lyria.Application.Common.Errors;
using Lyria.Application.Common.Results;
using Lyria.Application.Features.UserRoles.SetActiveStatus;
using Lyria.Application.UnitTests.Fakes;
using Lyria.Domain.Roles;
using Lyria.Domain.Users;
using Lyria.Domain.Users.UserRoles;
using Xunit;

namespace Lyria.Application.UnitTests.Features.UserRoles;

public sealed class SetUserRoleActiveStatusTests
{
    private readonly FakeUserRoleRepository _repository = new();
    private readonly SetUserRoleActiveStatusCommandHandler _handler;

    public SetUserRoleActiveStatusTests()
    {
        _handler = new SetUserRoleActiveStatusCommandHandler(_repository);
    }

    [Fact]
    public async Task Handle_Activate_Succeeds()
    {
        var userId = UserId.New();
        var roleId = RoleId.New();
        var userRoleId = UserRoleId.New();

        var userRole = UserRole.Assign(
            userRoleId, userId, roleId, ScopeType.Global, null, null, DateTime.UtcNow);
        userRole.Deactivate();
        _repository.Seed(userRole);

        var command = new SetUserRoleActiveStatusCommand(
            userId.Value, userRoleId.Value, true);

        Result result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(_repository.Added[0].IsActive);
        Assert.Equal(1, _repository.SaveChangesCallCount);
    }

    [Fact]
    public async Task Handle_WhenNotFound_ReturnsNotFound()
    {
        var command = new SetUserRoleActiveStatusCommand(
            Guid.NewGuid(), Guid.NewGuid(), true);

        Result result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
        Assert.Equal("UserRoles.NotFound", result.Error.Code);
    }
}
