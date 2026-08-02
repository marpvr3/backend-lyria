using Lyria.Application.Common.Errors;
using Lyria.Application.Common.Results;
using Lyria.Application.Features.UserRoles;
using Lyria.Application.Features.UserRoles.GetById;
using Lyria.Application.UnitTests.Fakes;
using Xunit;

namespace Lyria.Application.UnitTests.Features.UserRoles;

public sealed class GetUserRoleByIdTests
{
    private readonly FakeUserRoleReadService _readService = new();
    private readonly GetUserRoleByIdQueryHandler _handler;

    public GetUserRoleByIdTests()
    {
        _handler = new GetUserRoleByIdQueryHandler(_readService);
    }

    [Fact]
    public async Task Handle_WhenExists_ReturnsUserRole()
    {
        var id = Guid.NewGuid();
        _readService.Seed(new UserRoleResponse(
            id, Guid.NewGuid(), Guid.NewGuid(), "ADMIN", "Administrador",
            "Global", null, null, true, DateTime.UtcNow, null));

        var query = new GetUserRoleByIdQuery(id);

        Result<UserRoleResponse> result = await _handler.Handle(query, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(id, result.Value.Id);
    }

    [Fact]
    public async Task Handle_WhenNotFound_ReturnsNotFound()
    {
        var query = new GetUserRoleByIdQuery(Guid.NewGuid());

        Result<UserRoleResponse> result = await _handler.Handle(query, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
        Assert.Equal("UserRoles.NotFound", result.Error.Code);
    }
}
