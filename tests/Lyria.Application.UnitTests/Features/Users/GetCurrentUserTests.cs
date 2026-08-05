using Lyria.Application.Common.Errors;
using Lyria.Application.Common.Results;
using Lyria.Application.Features.Users;
using Lyria.Application.Features.Users.GetCurrent;
using Lyria.Application.UnitTests.Fakes;
using Lyria.Domain.Users;
using Xunit;

namespace Lyria.Application.UnitTests.Features.Users;

public sealed class GetCurrentUserTests
{
    private readonly FakeCurrentUser _currentUser = new();
    private readonly FakeUserReadService _readService = new();

    private GetCurrentUserQueryHandler CreateHandler() =>
        new(_currentUser, _readService);

    private async Task<Result<CurrentUserResponse>> HandleAsync() =>
        await CreateHandler().Handle(
            new GetCurrentUserQuery(), TestContext.Current.CancellationToken);

    private UserResponse SeedUser(UserId userId)
    {
        var response = new UserResponse(
            userId.Value,
            "Andres",
            "Perez",
            "andres@email.com",
            "3001234567",
            new DateOnly(1978, 12, 25),
            null,
            nameof(UserStatus.Unverified),
            false,
            null,
            new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc),
            null);

        _readService.Seed(response);

        return response;
    }

    [Fact]
    public async Task GetCurrentUser_WithValidIdentity_ReturnsTheMinimumProfile()
    {
        var userId = UserId.New();
        SeedUser(userId);
        _currentUser.UserId = userId;

        Result<CurrentUserResponse> result = await HandleAsync();

        Assert.True(result.IsSuccess);
        Assert.Equal(userId.Value, result.Value.UserId);
        Assert.Equal("Andres", result.Value.Name);
        Assert.Equal("Perez", result.Value.LastName);
        Assert.Equal("andres@email.com", result.Value.Email);
        Assert.Equal("3001234567", result.Value.Phone);
        Assert.Equal(new DateOnly(1978, 12, 25), result.Value.BirthDate);
        Assert.Null(result.Value.PhotoUrl);
        Assert.Equal("Unverified", result.Value.Status);
        Assert.False(result.Value.IsEmailVerified);
    }

    [Fact]
    public async Task GetCurrentUser_WithoutIdentity_ReturnsNotAuthenticated()
    {
        _currentUser.UserId = null;

        Result<CurrentUserResponse> result = await HandleAsync();

        Assert.True(result.IsFailure);
        Assert.Equal("Authentication.NotAuthenticated", result.Error.Code);
        Assert.Equal(ErrorType.Unauthorized, result.Error.Type);
    }

    /// <summary>
    /// Un token válido cuyo usuario ya no existe no debe producir un 404 que permita
    /// sondear qué identificadores existen.
    /// </summary>
    [Fact]
    public async Task GetCurrentUser_WhenTheUserNoLongerExists_ReturnsNotAuthenticated()
    {
        _currentUser.UserId = UserId.New();

        Result<CurrentUserResponse> result = await HandleAsync();

        Assert.True(result.IsFailure);
        Assert.Equal("Authentication.NotAuthenticated", result.Error.Code);
        Assert.Equal(ErrorType.Unauthorized, result.Error.Type);
    }

    [Fact]
    public async Task GetCurrentUser_NeverReadsAnIdentityProvidedByTheClient()
    {
        var authenticated = UserId.New();
        var other = UserId.New();

        SeedUser(authenticated);
        SeedUser(other);

        _currentUser.UserId = authenticated;

        Result<CurrentUserResponse> result = await HandleAsync();

        // La consulta no admite parámetros: solo puede resolver la identidad autenticada.
        Assert.True(result.IsSuccess);
        Assert.Equal(authenticated.Value, result.Value.UserId);
        Assert.NotEqual(other.Value, result.Value.UserId);
    }
}
