using System.Reflection;
using Lyria.Application.Common.Errors;
using Lyria.Application.Common.Results;
using Lyria.Application.Features.Roles;
using Lyria.Application.Features.Roles.GetById;
using Lyria.Application.UnitTests.Fakes;
using Xunit;

namespace Lyria.Application.UnitTests.Features.Roles;

public sealed class GetRoleByIdTests
{
    private readonly FakeRoleReadService _readService = new();
    private readonly GetRoleByIdQueryHandler _handler;

    public GetRoleByIdTests()
    {
        _handler = new GetRoleByIdQueryHandler(_readService);
    }

    [Fact]
    public async Task Handle_WhenRoleExists_ReturnsRole()
    {
        var id = Guid.NewGuid();
        _readService.Seed(new RoleResponse(
            id, "Administrador", "Rol de administrador", true,
            DateTime.UtcNow, null));

        var query = new GetRoleByIdQuery(id);

        Result<RoleResponse> result = await _handler.Handle(query, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(id, result.Value.Id);
        Assert.Equal("Administrador", result.Value.Name);
        Assert.Equal("Rol de administrador", result.Value.Description);
    }

    [Fact]
    public void Response_DoesNotExposeCode()
    {
        string[] properties = typeof(RoleResponse)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Select(p => p.Name)
            .ToArray();

        Assert.Equal(
            ["Id", "Name", "Description", "IsActive", "CreatedAtUtc", "UpdatedAtUtc"],
            properties);
    }

    [Fact]
    public async Task Handle_WhenRoleNotFound_ReturnsNotFound()
    {
        var query = new GetRoleByIdQuery(Guid.NewGuid());

        Result<RoleResponse> result = await _handler.Handle(query, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
        Assert.Equal("Roles.NotFound", result.Error.Code);
    }
}
