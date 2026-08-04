using Lyria.Application.Common.Errors;
using Lyria.Application.Common.Results;
using Lyria.Application.Features.UserRoles;
using Lyria.Application.Features.UserRoles.GetByUserId;
using Lyria.Application.Features.Users;
using Lyria.Application.UnitTests.Fakes;
using Xunit;

namespace Lyria.Application.UnitTests.Features.UserRoles;

public sealed class GetUserRolesTests
{
    private readonly FakeUserRoleReadService _userRoleReadService = new();
    private readonly FakeUserReadService _userReadService = new();
    private readonly GetUserRolesQueryHandler _handler;

    public GetUserRolesTests()
    {
        _handler = new GetUserRolesQueryHandler(_userRoleReadService, _userReadService);
    }

    [Fact]
    public async Task Handle_WhenUserExists_ReturnsRoles()
    {
        var userId = Guid.NewGuid();

        _userReadService.Seed(new UserResponse(
            userId, "Juan", "Pérez", "juan@example.com", null, null, null,
            "Active", true, null, DateTime.UtcNow, null));

        _userRoleReadService.Seed(new UserRoleResponse(
            Guid.NewGuid(), userId, Guid.NewGuid(), "Administrador",
            "Global", null, null, true, DateTime.UtcNow, null));
        _userRoleReadService.Seed(new UserRoleResponse(
            Guid.NewGuid(), userId, Guid.NewGuid(), "Usuario",
            "Global", null, null, true, DateTime.UtcNow, null));

        var query = new GetUserRolesQuery(userId);

        Result<IReadOnlyList<UserRoleResponse>> result =
            await _handler.Handle(query, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Count);
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_ReturnsNotFound()
    {
        var query = new GetUserRolesQuery(Guid.NewGuid());

        Result<IReadOnlyList<UserRoleResponse>> result =
            await _handler.Handle(query, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
        Assert.Equal("UserRoles.UserNotFound", result.Error.Code);
    }
}
