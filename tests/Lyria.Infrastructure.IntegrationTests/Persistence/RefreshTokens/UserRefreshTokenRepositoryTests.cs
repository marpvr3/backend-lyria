using Lyria.Domain.Users;
using Lyria.Domain.Users.RefreshTokens;
using Lyria.Infrastructure.Persistence.Repositories;
using Xunit;

namespace Lyria.Infrastructure.IntegrationTests.Persistence.RefreshTokens;

public sealed class UserRefreshTokenRepositoryTests : IDisposable
{
    private static readonly DateTime UtcNow =
        new(2026, 8, 4, 23, 0, 0, DateTimeKind.Utc);

    private static readonly DateTime ExpiresAtUtc =
        new(2026, 9, 3, 23, 0, 0, DateTimeKind.Utc);

    private readonly SqliteFixture _fixture = new();

    private static string NewTokenHash() =>
        Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");

    private async Task<UserId> SeedUserAsync()
    {
        var user = User.Create(
            UserId.New(), "Andres", "Perez", $"{Guid.NewGuid():N}@email.com",
            "AQAAAAIAAYagAAAAEHashSimuladoDePruebas==", null, null, null);

        await using var context = _fixture.CreateContext();
        context.Set<User>().Add(user);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        return user.Id;
    }

    private async Task<string> SeedSessionAsync(UserId userId)
    {
        string tokenHash = NewTokenHash();

        await using var context = _fixture.CreateContext();

        context.Set<UserRefreshToken>().Add(UserRefreshToken.Create(
            UserRefreshTokenId.New(), userId, tokenHash, UtcNow, ExpiresAtUtc));

        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        return tokenHash;
    }

    [Fact]
    public async Task GetByTokenHashAsync_WithExistingHash_ReturnsTheSession()
    {
        UserId userId = await SeedUserAsync();
        string tokenHash = await SeedSessionAsync(userId);

        await using var context = _fixture.CreateContext();
        var repository = new UserRefreshTokenRepository(context);

        UserRefreshToken? session = await repository.GetByTokenHashAsync(
            tokenHash, TestContext.Current.CancellationToken);

        Assert.NotNull(session);
        Assert.Equal(userId, session.UserId);
        Assert.Equal(tokenHash, session.TokenHash);
    }

    [Fact]
    public async Task GetByTokenHashAsync_WithUnknownHash_ReturnsNull()
    {
        await using var context = _fixture.CreateContext();
        var repository = new UserRefreshTokenRepository(context);

        UserRefreshToken? session = await repository.GetByTokenHashAsync(
            NewTokenHash(), TestContext.Current.CancellationToken);

        Assert.Null(session);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetByTokenHashAsync_WithEmptyHash_ReturnsNull(string tokenHash)
    {
        await using var context = _fixture.CreateContext();
        var repository = new UserRefreshTokenRepository(context);

        UserRefreshToken? session = await repository.GetByTokenHashAsync(
            tokenHash, TestContext.Current.CancellationToken);

        Assert.Null(session);
    }

    [Fact]
    public async Task GetByTokenHashAsync_NormalizesTheHashCasing()
    {
        UserId userId = await SeedUserAsync();
        string tokenHash = await SeedSessionAsync(userId);

        await using var context = _fixture.CreateContext();
        var repository = new UserRefreshTokenRepository(context);

        UserRefreshToken? session = await repository.GetByTokenHashAsync(
            tokenHash.ToUpperInvariant(), TestContext.Current.CancellationToken);

        Assert.NotNull(session);
    }

    [Fact]
    public async Task GetByTokenHashAsync_ReturnsTheSessionEvenIfRevoked()
    {
        UserId userId = await SeedUserAsync();
        string tokenHash = await SeedSessionAsync(userId);

        await using (var context = _fixture.CreateContext())
        {
            var repository = new UserRefreshTokenRepository(context);

            UserRefreshToken revoked = (await repository.GetByTokenHashAsync(
                tokenHash, TestContext.Current.CancellationToken))!;

            revoked.Revoke(UtcNow.AddHours(1));

            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using (var context = _fixture.CreateContext())
        {
            var repository = new UserRefreshTokenRepository(context);

            // El caso de uso necesita recuperarla para distinguir "revocada" de
            // "inexistente", aunque responda lo mismo al cliente.
            UserRefreshToken? session = await repository.GetByTokenHashAsync(
                tokenHash, TestContext.Current.CancellationToken);

            Assert.NotNull(session);
            Assert.True(session.IsRevoked);
        }
    }

    public void Dispose() => _fixture.Dispose();
}
