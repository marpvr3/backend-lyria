using Lyria.Application.Common.Errors;
using Lyria.Application.Common.Results;
using Lyria.Application.Features.Users.ChangeEmail;
using Lyria.Application.UnitTests.Fakes;
using Lyria.Domain.Users;
using Xunit;

namespace Lyria.Application.UnitTests.Features.Users;

public sealed class ChangeUserEmailTests
{
    private readonly FakeUserRepository _repository = new();
    private readonly ChangeUserEmailCommandHandler _handler;

    public ChangeUserEmailTests()
    {
        _handler = new ChangeUserEmailCommandHandler(_repository);
    }

    [Fact]
    public async Task Handle_WhenEmailAvailable_ChangesEmail()
    {
        var userId = UserId.New();
        _repository.Seed(User.Create(
            userId, "Juan", "Pérez", "juan@example.com", "hash123", null, null, null));

        var command = new ChangeUserEmailCommand(userId.Value, "nuevo@example.com");

        Result result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("nuevo@example.com", _repository.Added[0].Email);
    }

    [Fact]
    public async Task Handle_WhenEmailAlreadyExists_ReturnsConflict()
    {
        var userId = UserId.New();
        _repository.Seed(User.Create(
            userId, "Juan", "Pérez", "juan@example.com", "hash123", null, null, null));
        _repository.Seed(User.Create(
            UserId.New(), "Ana", "García", "ana@example.com", "hash456", null, null, null));

        var command = new ChangeUserEmailCommand(userId.Value, "ana@example.com");

        Result result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
        Assert.Equal("Users.EmailAlreadyExists", result.Error.Code);
    }

    [Fact]
    public async Task Handle_ExcludesCurrentUserFromDuplicateCheck()
    {
        var userId = UserId.New();
        _repository.Seed(User.Create(
            userId, "Juan", "Pérez", "juan@example.com", "hash123", null, null, null));

        var command = new ChangeUserEmailCommand(userId.Value, "juan@example.com");

        Result result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_ReturnsNotFound()
    {
        var command = new ChangeUserEmailCommand(Guid.NewGuid(), "nuevo@example.com");

        Result result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
        Assert.Equal("Users.NotFound", result.Error.Code);
    }
}
