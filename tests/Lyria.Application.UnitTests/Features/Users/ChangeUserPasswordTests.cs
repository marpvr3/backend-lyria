using Lyria.Application.Common.Errors;
using Lyria.Application.Common.Results;
using Lyria.Application.Features.Users.ChangePassword;
using Lyria.Application.UnitTests.Fakes;
using Lyria.Domain.Users;
using Xunit;

namespace Lyria.Application.UnitTests.Features.Users;

public sealed class ChangeUserPasswordTests
{
    private readonly FakeUserRepository _repository = new();
    private readonly FakePasswordHasher _passwordHasher = new();
    private readonly ChangeUserPasswordCommandHandler _handler;

    public ChangeUserPasswordTests()
    {
        _handler = new ChangeUserPasswordCommandHandler(_repository, _passwordHasher);
    }

    [Fact]
    public async Task Handle_WhenUserExists_ChangesPasswordHash()
    {
        var userId = UserId.New();
        _repository.Seed(User.Create(
            userId, "Juan", "Pérez", "juan@example.com", "old_hash", null, null, null));

        var command = new ChangeUserPasswordCommand(userId.Value, "NewPassword123");

        Result result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, _repository.SaveChangesCallCount);
    }

    [Fact]
    public async Task Handle_HashesNewPassword()
    {
        var userId = UserId.New();
        _repository.Seed(User.Create(
            userId, "Juan", "Pérez", "juan@example.com", "old_hash", null, null, null));

        var command = new ChangeUserPasswordCommand(userId.Value, "NewPassword123");

        await _handler.Handle(command, CancellationToken.None);

        Assert.Equal("hashed_NewPassword123", _repository.Added[0].PasswordHash);
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_ReturnsNotFound()
    {
        var command = new ChangeUserPasswordCommand(Guid.NewGuid(), "NewPassword123");

        Result result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
        Assert.Equal("Users.NotFound", result.Error.Code);
    }
}
