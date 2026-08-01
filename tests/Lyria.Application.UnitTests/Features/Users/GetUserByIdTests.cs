using Lyria.Application.Common.Errors;
using Lyria.Application.Common.Results;
using Lyria.Application.Features.Users;
using Lyria.Application.Features.Users.GetById;
using Lyria.Application.UnitTests.Fakes;
using Xunit;

namespace Lyria.Application.UnitTests.Features.Users;

public sealed class GetUserByIdTests
{
    private readonly FakeUserReadService _readService = new();
    private readonly GetUserByIdQueryHandler _handler;

    public GetUserByIdTests()
    {
        _handler = new GetUserByIdQueryHandler(_readService);
    }

    [Fact]
    public async Task Handle_WhenUserExists_ReturnsUser()
    {
        var id = Guid.NewGuid();
        _readService.Seed(new UserResponse(
            id, "Juan", "Pérez", "juan@example.com", null, null, null,
            "Active", true, null, DateTime.UtcNow, null));

        var query = new GetUserByIdQuery(id);

        Result<UserResponse> result = await _handler.Handle(query, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(id, result.Value.Id);
        Assert.Equal("Juan", result.Value.Name);
        Assert.Equal("Pérez", result.Value.LastName);
        Assert.Equal("juan@example.com", result.Value.Email);
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_ReturnsNotFound()
    {
        var query = new GetUserByIdQuery(Guid.NewGuid());

        Result<UserResponse> result = await _handler.Handle(query, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
        Assert.Equal("Users.NotFound", result.Error.Code);
    }
}
