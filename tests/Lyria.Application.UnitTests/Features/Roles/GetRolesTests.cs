using System.Reflection;
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
            Guid.NewGuid(), "Administrador", null, true,
            DateTime.UtcNow, null));
        _readService.Seed(new RoleResponse(
            Guid.NewGuid(), "Usuario", null, true,
            DateTime.UtcNow, null));

        var query = new GetRolesQuery(null, null);

        PagedResponse<RoleListItemResponse> result =
            await _handler.Handle(query, CancellationToken.None);

        Assert.Equal(2, result.TotalItems);
        Assert.Equal(2, result.Items.Count);
        Assert.Equal(1, result.Page);
    }

    [Fact]
    public async Task Handle_ListItemsExposeOnlyIdNameAndIsActive()
    {
        _readService.Seed(new RoleResponse(
            Guid.NewGuid(), "Administrador", null, true,
            DateTime.UtcNow, null));

        var query = new GetRolesQuery(null, null);

        PagedResponse<RoleListItemResponse> result =
            await _handler.Handle(query, CancellationToken.None);

        Assert.Equal("Administrador", result.Items[0].Name);

        string[] properties = typeof(RoleListItemResponse)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Select(p => p.Name)
            .ToArray();

        Assert.Equal(["Id", "Name", "IsActive"], properties);
    }

    [Fact]
    public void Query_DoesNotExposeCodeFilter()
    {
        string[] properties = typeof(GetRolesQuery)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Select(p => p.Name)
            .ToArray();

        Assert.DoesNotContain("Code", properties);
    }

    [Fact]
    public void Filter_DoesNotExposeCode()
    {
        string[] properties = typeof(RoleListFilter)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Select(p => p.Name)
            .ToArray();

        Assert.DoesNotContain("Code", properties);
    }
}
