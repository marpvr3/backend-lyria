using Lyria.Domain.Users;
using Lyria.Domain.Users.RefreshTokens;
using Lyria.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Lyria.Infrastructure.IntegrationTests.Persistence.RefreshTokens;

/// <summary>
/// Verifica la atomicidad real de la autenticación sobre el proveedor relacional
/// utilizado en pruebas (SQLite). No se usa EF Core InMemory: no soporta transacciones
/// ni claves foráneas.
/// </summary>
public sealed class AuthenticationSessionWriterTests : IDisposable
{
    private const string PasswordHash = "AQAAAAIAAYagAAAAEHashSimuladoDePruebas==";

    private static readonly DateTime UtcNow =
        new(2026, 8, 4, 23, 0, 0, DateTimeKind.Utc);

    private static readonly DateTime ExpiresAtUtc =
        new(2026, 9, 3, 23, 0, 0, DateTimeKind.Utc);

    private readonly SqliteFixture _fixture = new();

    /// <summary>
    /// Hash SHA-256 simulado, único por invocación y en formato hexadecimal válido.
    /// </summary>
    private static string NewTokenHash() => Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");

    private async Task<UserId> SeedUserAsync()
    {
        var user = User.Create(
            UserId.New(), "Andres", "Perez", $"{Guid.NewGuid():N}@email.com",
            PasswordHash, "3001234567", new DateOnly(1978, 12, 25), null);

        await using var context = _fixture.CreateContext();
        context.Set<User>().Add(user);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        return user.Id;
    }

    private static UserRefreshToken NewSession(UserId userId, string? tokenHash = null) =>
        UserRefreshToken.Create(
            UserRefreshTokenId.New(), userId, tokenHash ?? NewTokenHash(), UtcNow, ExpiresAtUtc);

    // --- Login ---

    [Fact]
    public async Task CompleteLoginAsync_PersistsLastLoginAndSessionTogether()
    {
        UserId userId = await SeedUserAsync();
        string tokenHash = NewTokenHash();

        await using (var context = _fixture.CreateContext())
        {
            User user = await context.Set<User>()
                .SingleAsync(u => u.Id == userId, TestContext.Current.CancellationToken);

            user.RegisterLastLogin(UtcNow);

            var writer = new AuthenticationSessionWriter(context);

            await writer.CompleteLoginAsync(
                user, NewSession(userId, tokenHash), TestContext.Current.CancellationToken);
        }

        await using var verification = _fixture.CreateContext();

        User persisted = await verification.Set<User>()
            .SingleAsync(u => u.Id == userId, TestContext.Current.CancellationToken);

        Assert.Equal(UtcNow, persisted.LastLoginAtUtc);

        Assert.Single(await verification.Set<UserRefreshToken>()
            .Where(rt => rt.TokenHash == tokenHash)
            .ToListAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task CompleteLoginAsync_AlsoPersistsARehashedPassword()
    {
        UserId userId = await SeedUserAsync();
        const string rehashed = "AQAAAAIAAYagAAAAEHashRegeneradoConElAlgoritmoActual==";

        await using (var context = _fixture.CreateContext())
        {
            User user = await context.Set<User>()
                .SingleAsync(u => u.Id == userId, TestContext.Current.CancellationToken);

            user.ChangePasswordHash(rehashed);
            user.RegisterLastLogin(UtcNow);

            var writer = new AuthenticationSessionWriter(context);

            await writer.CompleteLoginAsync(
                user, NewSession(userId), TestContext.Current.CancellationToken);
        }

        await using var verification = _fixture.CreateContext();

        User persisted = await verification.Set<User>()
            .SingleAsync(u => u.Id == userId, TestContext.Current.CancellationToken);

        Assert.Equal(rehashed, persisted.PasswordHash);
        Assert.Equal(UtcNow, persisted.LastLoginAtUtc);
    }

    /// <summary>
    /// Si la sesión no puede insertarse, la última conexión tampoco queda confirmada.
    /// </summary>
    [Fact]
    public async Task CompleteLoginAsync_WhenTheSessionFails_RollsBackLastLogin()
    {
        UserId userId = await SeedUserAsync();

        await using (var context = _fixture.CreateContext())
        {
            User user = await context.Set<User>()
                .SingleAsync(u => u.Id == userId, TestContext.Current.CancellationToken);

            user.RegisterLastLogin(UtcNow);

            var writer = new AuthenticationSessionWriter(context);

            // La sesión apunta a un usuario inexistente: la FK la rechaza.
            await Assert.ThrowsAnyAsync<DbUpdateException>(() =>
                writer.CompleteLoginAsync(
                    user, NewSession(UserId.New()), TestContext.Current.CancellationToken));
        }

        await using var verification = _fixture.CreateContext();

        User persisted = await verification.Set<User>()
            .SingleAsync(u => u.Id == userId, TestContext.Current.CancellationToken);

        Assert.Null(persisted.LastLoginAtUtc);
        Assert.Empty(await verification.Set<UserRefreshToken>()
            .ToListAsync(TestContext.Current.CancellationToken));
    }

    // --- Rotación ---

    [Fact]
    public async Task RotateAsync_RevokesThePreviousSessionAndCreatesTheNewOne()
    {
        UserId userId = await SeedUserAsync();
        string previousHash = NewTokenHash();
        string newHash = NewTokenHash();

        await using (var context = _fixture.CreateContext())
        {
            context.Set<UserRefreshToken>().Add(NewSession(userId, previousHash));
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using (var context = _fixture.CreateContext())
        {
            UserRefreshToken previous = await context.Set<UserRefreshToken>()
                .SingleAsync(
                    rt => rt.TokenHash == previousHash, TestContext.Current.CancellationToken);

            previous.Revoke(UtcNow);

            var writer = new AuthenticationSessionWriter(context);

            await writer.RotateAsync(
                previous, NewSession(userId, newHash), TestContext.Current.CancellationToken);
        }

        await using var verification = _fixture.CreateContext();

        UserRefreshToken revoked = await verification.Set<UserRefreshToken>()
            .SingleAsync(rt => rt.TokenHash == previousHash, TestContext.Current.CancellationToken);

        UserRefreshToken created = await verification.Set<UserRefreshToken>()
            .SingleAsync(rt => rt.TokenHash == newHash, TestContext.Current.CancellationToken);

        Assert.True(revoked.IsRevoked);
        Assert.Equal(UtcNow, revoked.RevokedAtUtc);
        Assert.False(created.IsRevoked);
    }

    /// <summary>
    /// Si la sesión nueva no puede crearse, la revocación de la anterior se revierte.
    /// </summary>
    [Fact]
    public async Task RotateAsync_WhenTheNewSessionFails_RollsBackTheRevocation()
    {
        UserId userId = await SeedUserAsync();
        string previousHash = NewTokenHash();
        string collidingHash = NewTokenHash();

        await using (var context = _fixture.CreateContext())
        {
            context.Set<UserRefreshToken>().Add(NewSession(userId, previousHash));
            context.Set<UserRefreshToken>().Add(NewSession(userId, collidingHash));
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using (var context = _fixture.CreateContext())
        {
            UserRefreshToken previous = await context.Set<UserRefreshToken>()
                .SingleAsync(
                    rt => rt.TokenHash == previousHash, TestContext.Current.CancellationToken);

            previous.Revoke(UtcNow);

            var writer = new AuthenticationSessionWriter(context);

            // El hash ya existe: el índice único rechaza la inserción.
            await Assert.ThrowsAnyAsync<DbUpdateException>(() =>
                writer.RotateAsync(
                    previous,
                    NewSession(userId, collidingHash),
                    TestContext.Current.CancellationToken));
        }

        await using var verification = _fixture.CreateContext();

        UserRefreshToken previousSession = await verification.Set<UserRefreshToken>()
            .SingleAsync(rt => rt.TokenHash == previousHash, TestContext.Current.CancellationToken);

        Assert.False(previousSession.IsRevoked);
        Assert.Null(previousSession.RevokedAtUtc);

        Assert.Equal(2, await verification.Set<UserRefreshToken>()
            .CountAsync(TestContext.Current.CancellationToken));
    }

    // --- Logout ---

    [Fact]
    public async Task RevokeAsync_KeepsTheRowAndSetsFechaRevocacion()
    {
        UserId userId = await SeedUserAsync();
        string tokenHash = NewTokenHash();

        await using (var context = _fixture.CreateContext())
        {
            context.Set<UserRefreshToken>().Add(NewSession(userId, tokenHash));
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using (var context = _fixture.CreateContext())
        {
            UserRefreshToken session = await context.Set<UserRefreshToken>()
                .SingleAsync(rt => rt.TokenHash == tokenHash, TestContext.Current.CancellationToken);

            session.Revoke(UtcNow);

            var writer = new AuthenticationSessionWriter(context);

            await writer.RevokeAsync(session, TestContext.Current.CancellationToken);
        }

        await using var verification = _fixture.CreateContext();

        UserRefreshToken persisted = await verification.Set<UserRefreshToken>()
            .SingleAsync(rt => rt.TokenHash == tokenHash, TestContext.Current.CancellationToken);

        // Borrado lógico: la fila sigue ahí con su historial completo.
        Assert.Equal(UtcNow, persisted.RevokedAtUtc);
        Assert.Equal(tokenHash, persisted.TokenHash);
        Assert.Equal(ExpiresAtUtc, persisted.ExpiresAtUtc);

        Assert.Single(await verification.Set<UserRefreshToken>()
            .ToListAsync(TestContext.Current.CancellationToken));
    }

    public void Dispose() => _fixture.Dispose();
}
