using Lyria.Application.Common.Errors;
using Lyria.Application.Common.Results;
using Lyria.Application.Features.Users.Update;
using Lyria.Application.UnitTests.Fakes;
using Lyria.Domain.Users;
using Xunit;

namespace Lyria.Application.UnitTests.Features.Users;

public sealed class UpdateUserTests
{
    private readonly FakeUserRepository _repository = new();
    private readonly UpdateUserCommandHandler _handler;

    public UpdateUserTests()
    {
        _handler = new UpdateUserCommandHandler(_repository);
    }

    [Fact]
    public async Task Handle_WhenUserExists_UpdatesProfile()
    {
        var userId = UserId.New();
        _repository.Seed(User.Create(
            userId, "Juan", "Pérez", "juan@example.com", "hash123", null, null, null));

        var command = new UpdateUserCommand(
            userId.Value, "Carlos", "López", "+573001234567", new DateOnly(1990, 5, 15), null);

        Result result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Carlos", _repository.Added[0].Name);
        Assert.Equal("López", _repository.Added[0].LastName);
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_ReturnsNotFound()
    {
        var command = new UpdateUserCommand(
            Guid.NewGuid(), "Carlos", "López", null, null, null);

        Result result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
        Assert.Equal("Users.NotFound", result.Error.Code);
    }

    [Fact]
    public async Task Handle_CallsSaveChanges()
    {
        var userId = UserId.New();
        _repository.Seed(User.Create(
            userId, "Juan", "Pérez", "juan@example.com", "hash123", null, null, null));

        var command = new UpdateUserCommand(
            userId.Value, "Carlos", "López", null, null, null);

        await _handler.Handle(command, CancellationToken.None);

        Assert.Equal(1, _repository.SaveChangesCallCount);
    }
}
