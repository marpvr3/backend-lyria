using Lyria.Application.Common;
using Lyria.Application.Features.Users;
using Lyria.Application.Features.Users.List;
using Lyria.Application.UnitTests.Fakes;
using Xunit;

namespace Lyria.Application.UnitTests.Features.Users;

public sealed class GetUsersTests
{
    private readonly FakeUserReadService _readService = new();
    private readonly GetUsersQueryHandler _handler;

    public GetUsersTests()
    {
        _handler = new GetUsersQueryHandler(_readService);
    }

    [Fact]
    public async Task Handle_ReturnsPagedResponse()
    {
        _readService.Seed(new UserResponse(
            Guid.NewGuid(), "Juan", "Pérez", "juan@example.com", null, null, null,
            "Active", true, null, DateTime.UtcNow, null));
        _readService.Seed(new UserResponse(
            Guid.NewGuid(), "Ana", "García", "ana@example.com", null, null, null,
            "Active", false, null, DateTime.UtcNow, null));

        var query = new GetUsersQuery(null, null, null, null);

        PagedResponse<UserListItemResponse> result =
            await _handler.Handle(query, CancellationToken.None);

        Assert.Equal(2, result.TotalItems);
        Assert.Equal(2, result.Items.Count);
        Assert.Equal(1, result.Page);
    }
}
