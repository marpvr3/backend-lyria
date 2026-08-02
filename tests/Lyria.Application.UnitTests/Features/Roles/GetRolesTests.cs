using Lyria.Application.Common;
using Lyria.Application.Features.Roles;
using Lyria.Application.Features.Roles.List;
using Lyria.Application.UnitTests.Fakes;
using Xunit;

namespace Lyria.Application.UnitTests.Features.Roles;

public sealed class GetRolesTests
{
    private readonly FakeRoleReadService _readService = new();
    private readonly GetRolesQueryHandler _handler;

    public GetRolesTests()
    {
        _handler = new GetRolesQueryHandler(_readService);
    }

    [Fact]
    public async Task Handle_ReturnsPagedResponse()
    {
        _readService.Seed(new RoleResponse(
            Guid.NewGuid(), "ADMIN", "Administrador", null, true,
            DateTime.UtcNow, null));
        _readService.Seed(new RoleResponse(
            Guid.NewGuid(), "USER", "Usuario", null, true,
            DateTime.UtcNow, null));

        var query = new GetRolesQuery(null, null, null);

        PagedResponse<RoleListItemResponse> result =
            await _handler.Handle(query, CancellationToken.None);

        Assert.Equal(2, result.TotalItems);
        Assert.Equal(2, result.Items.Count);
        Assert.Equal(1, result.Page);
    }
}
