using Lyria.Application.Common.Errors;
using Lyria.Application.Common.Results;
using Lyria.Application.Features.Users.Create;
using Lyria.Application.UnitTests.Fakes;
using Lyria.Domain.Users;
using Xunit;

namespace Lyria.Application.UnitTests.Features.Users;

public sealed class CreateUserTests
{
    private readonly FakeUserRepository _repository = new();
    private readonly FakePasswordHasher _passwordHasher = new();
    private readonly CreateUserCommandHandler _handler;

    public CreateUserTests()
    {
        _handler = new CreateUserCommandHandler(_repository, _passwordHasher);
    }

    [Fact]
    public async Task Handle_WhenEmailDoesNotExist_CreatesUser()
    {
        var command = new CreateUserCommand(
            "Juan", "Pérez", "juan@example.com", "Secret123!", null, null, null);

        Result<UserId> result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(_repository.Added);
        Assert.Equal(1, _repository.SaveChangesCallCount);
    }

    [Fact]
    public async Task Handle_WhenEmailExists_ReturnsConflict()
    {
        _repository.Seed(User.Create(
            UserId.New(), "Ana", "García", "ana@example.com", "hash123", null, null, null));

        var command = new CreateUserCommand(
            "Otro", "Usuario", "ana@example.com", "Secret123!", null, null, null);

        Result<UserId> result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
        Assert.Equal("Users.EmailAlreadyExists", result.Error.Code);
    }

    [Fact]
    public async Task Handle_NormalizesEmailBeforeCheckingDuplicate()
    {
        _repository.Seed(User.Create(
            UserId.New(), "Ana", "García", "ana@example.com", "hash123", null, null, null));

        var command = new CreateUserCommand(
            "Otro", "Usuario", "  ANA@EXAMPLE.COM  ", "Secret123!", null, null, null);

        Result<UserId> result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
    }

    [Fact]
    public async Task Handle_HashesPassword()
    {
        var command = new CreateUserCommand(
            "Juan", "Pérez", "juan@example.com", "MyPassword", null, null, null);

        Result<UserId> result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("hashed_MyPassword", _repository.Added[0].PasswordHash);
    }

    [Fact]
    public async Task Handle_PassesCancellationToken()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var command = new CreateUserCommand(
            "Juan", "Pérez", "juan@example.com", "Secret123!", null, null, null);

        Result<UserId> result = await _handler.Handle(command, cts.Token);

        Assert.True(result.IsSuccess);
    }
}
