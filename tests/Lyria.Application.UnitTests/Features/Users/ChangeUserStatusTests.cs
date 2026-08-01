using Lyria.Application.Common.Errors;
using Lyria.Application.Common.Results;
using Lyria.Application.Features.Users.ChangeStatus;
using Lyria.Application.UnitTests.Fakes;
using Lyria.Domain.Users;
using Xunit;

namespace Lyria.Application.UnitTests.Features.Users;

public sealed class ChangeUserStatusTests
{
    private readonly FakeUserRepository _repository = new();
    private readonly ChangeUserStatusCommandHandler _handler;

    public ChangeUserStatusTests()
    {
        _handler = new ChangeUserStatusCommandHandler(_repository);
    }

    [Fact]
    public async Task Handle_WithValidTransition_ChangesStatus()
    {
        var userId = UserId.New();
        _repository.Seed(User.Create(
            userId, "Juan", "Pérez", "juan@example.com", "hash123", null, null, null));

        var command = new ChangeUserStatusCommand(userId.Value, "Active");

        Result result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(UserStatus.Active, _repository.Added[0].Status);
        Assert.Equal(1, _repository.SaveChangesCallCount);
    }

    [Fact]
    public async Task Handle_WithInvalidTransition_ReturnsError()
    {
        var userId = UserId.New();
        _repository.Seed(User.Create(
            userId, "Juan", "Pérez", "juan@example.com", "hash123", null, null, null));

        // Unverified -> Suspended is not a valid transition
        var command = new ChangeUserStatusCommand(userId.Value, "Suspended");

        Result result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
        Assert.Equal("Users.InvalidStatusTransition", result.Error.Code);
    }

    [Fact]
    public async Task Handle_WithInvalidStatusValue_ReturnsError()
    {
        var userId = UserId.New();
        _repository.Seed(User.Create(
            userId, "Juan", "Pérez", "juan@example.com", "hash123", null, null, null));

        var command = new ChangeUserStatusCommand(userId.Value, "InvalidStatus");

        Result result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
        Assert.Equal("Users.InvalidStatus", result.Error.Code);
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_ReturnsNotFound()
    {
        var command = new ChangeUserStatusCommand(Guid.NewGuid(), "Active");

        Result result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
        Assert.Equal("Users.NotFound", result.Error.Code);
    }
}
