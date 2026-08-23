using Lyria.Domain.Users;
using Lyria.Domain.Users.EmailVerifications;
using Lyria.Infrastructure.Persistence;
using Lyria.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Lyria.Infrastructure.IntegrationTests.Persistence.EmailVerifications;

/// <summary>
/// Verifica la atomicidad real de la verificación de correo sobre el proveedor
/// relacional utilizado en pruebas (SQLite). No se usa EF Core InMemory: no soporta
/// transacciones ni claves foráneas.
/// </summary>
public sealed class EmailVerificationWriterTests : IDisposable
{
    private static readonly DateTime UtcNow =
        new(2026, 8, 5, 10, 0, 0, DateTimeKind.Utc);

    private readonly SqliteFixture _fixture = new();

    private static string Hash(char seed) =>
        new(seed, UserEmailVerification.CodeHashLength);

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

    private async Task<UserEmailVerificationId> SeedVerificationAsync(
        UserId userId,
        char hashSeed = 'a',
        DateTime? createdAtUtc = null)
    {
        DateTime created = createdAtUtc ?? UtcNow.AddMinutes(-5);

        var verification = UserEmailVerification.Create(
            UserEmailVerificationId.New(), userId, Hash(hashSeed), created,
            created.AddMinutes(15));

        await using var context = _fixture.CreateContext();

        context.Set<UserEmailVerification>().Add(verification);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        return verification.Id;
    }

    // --- Reenvío ---

    [Fact]
    public async Task ResendAsync_RevokesThePreviousCodesAndCreatesTheNewOne()
    {
        UserId userId = await SeedUserAsync();
        await SeedVerificationAsync(userId, 'a', UtcNow.AddMinutes(-10));
        await SeedVerificationAsync(userId, 'b', UtcNow.AddMinutes(-5));

        await using (var context = _fixture.CreateContext())
        {
            var repository = new UserEmailVerificationRepository(context);
            var writer = new EmailVerificationWriter(context);

            IReadOnlyList<UserEmailVerification> pending =
                await repository.GetPendingAsync(userId, TestContext.Current.CancellationToken);

            foreach (UserEmailVerification verification in pending)
            {
                verification.Revoke(UtcNow);
            }

            var created = UserEmailVerification.Create(
                UserEmailVerificationId.New(), userId, Hash('c'), UtcNow,
                UtcNow.AddMinutes(15));

            await writer.ResendAsync(pending, created, TestContext.Current.CancellationToken);
        }

        await using var verificationContext = _fixture.CreateContext();

        List<UserEmailVerification> stored = await verificationContext
            .Set<UserEmailVerification>()
            .Where(v => v.UserId == userId)
            .ToListAsync(TestContext.Current.CancellationToken);

        Assert.Equal(3, stored.Count);
        Assert.Equal(2, stored.Count(v => v.IsRevoked));
        Assert.Single(stored, v => !v.IsRevoked && v.CodeHash == Hash('c'));
    }

    /// <summary>
    /// Si la inserción falla, los códigos anteriores quedan sin revocar: nada a medias.
    /// </summary>
    [Fact]
    public async Task ResendAsync_WhenTheInsertFails_RollsBackTheRevocations()
    {
        UserId userId = await SeedUserAsync();
        await SeedVerificationAsync(userId);

        await using (var context = _fixture.CreateContext())
        {
            var repository = new UserEmailVerificationRepository(context);
            var writer = new EmailVerificationWriter(context);

            IReadOnlyList<UserEmailVerification> pending =
                await repository.GetPendingAsync(userId, TestContext.Current.CancellationToken);

            foreach (UserEmailVerification verification in pending)
            {
                verification.Revoke(UtcNow);
            }

            // La FK se viola: el usuario de la nueva verificación no existe.
            var orphan = UserEmailVerification.Create(
                UserEmailVerificationId.New(), UserId.New(), Hash('c'), UtcNow,
                UtcNow.AddMinutes(15));

            await Assert.ThrowsAnyAsync<DbUpdateException>(() =>
                writer.ResendAsync(pending, orphan, TestContext.Current.CancellationToken));
        }

        await using var verificationContext = _fixture.CreateContext();

        List<UserEmailVerification> stored = await verificationContext
            .Set<UserEmailVerification>()
            .Where(v => v.UserId == userId)
            .ToListAsync(TestContext.Current.CancellationToken);

        UserEmailVerification single = Assert.Single(stored);
        Assert.False(single.IsRevoked);
    }

    // --- Confirmación ---

    [Fact]
    public async Task TryConfirmAsync_UpdatesTheUserAndTheVerificationTogether()
    {
        UserId userId = await SeedUserAsync();
        UserEmailVerificationId verificationId = await SeedVerificationAsync(userId);

        await using (var context = _fixture.CreateContext())
        {
            var writer = new EmailVerificationWriter(context);

            User user = await context.Set<User>()
                .SingleAsync(u => u.Id == userId, TestContext.Current.CancellationToken);

            UserEmailVerification verification = await context.Set<UserEmailVerification>()
                .SingleAsync(v => v.Id == verificationId, TestContext.Current.CancellationToken);

            verification.MarkAsUsed(UtcNow);
            user.MarkEmailAsVerified();
            user.ChangeStatus(UserStatus.Active);

            Assert.True(await writer.TryConfirmAsync(
                user, verification, TestContext.Current.CancellationToken));
        }

        await using var verificationContext = _fixture.CreateContext();

        User storedUser = await verificationContext.Set<User>()
            .SingleAsync(u => u.Id == userId, TestContext.Current.CancellationToken);

        UserEmailVerification storedVerification = await verificationContext
            .Set<UserEmailVerification>()
            .SingleAsync(v => v.Id == verificationId, TestContext.Current.CancellationToken);

        Assert.True(storedUser.IsEmailVerified);
        Assert.Equal(UserStatus.Active, storedUser.Status);
        Assert.True(storedVerification.IsUsed);
        Assert.Equal(UtcNow, storedVerification.UsedAtUtc);
    }

    /// <summary>
    /// Dos confirmaciones simultáneas no pueden canjear el mismo código: la segunda
    /// encuentra <c>FechaUso</c> ya establecida y no confirma nada.
    /// </summary>
    [Fact]
    public async Task TryConfirmAsync_WhenTheCodeWasAlreadyUsedConcurrently_ReturnsFalse()
    {
        UserId userId = await SeedUserAsync();
        UserEmailVerificationId verificationId = await SeedVerificationAsync(userId);

        // Primera solicitud: carga la fila y aún no confirma.
        await using var firstContext = _fixture.CreateContext();

        User firstUser = await firstContext.Set<User>()
            .SingleAsync(u => u.Id == userId, TestContext.Current.CancellationToken);

        UserEmailVerification firstVerification = await firstContext
            .Set<UserEmailVerification>()
            .SingleAsync(v => v.Id == verificationId, TestContext.Current.CancellationToken);

        // Segunda solicitud: confirma primero, en su propia conexión.
        await using (var secondContext = _fixture.CreateContext())
        {
            var secondWriter = new EmailVerificationWriter(secondContext);

            User secondUser = await secondContext.Set<User>()
                .SingleAsync(u => u.Id == userId, TestContext.Current.CancellationToken);

            UserEmailVerification secondVerification = await secondContext
                .Set<UserEmailVerification>()
                .SingleAsync(v => v.Id == verificationId, TestContext.Current.CancellationToken);

            secondVerification.MarkAsUsed(UtcNow);
            secondUser.MarkEmailAsVerified();
            secondUser.ChangeStatus(UserStatus.Active);

            Assert.True(await secondWriter.TryConfirmAsync(
                secondUser, secondVerification, TestContext.Current.CancellationToken));
        }

        // La primera solicitud llega tarde.
        var firstWriter = new EmailVerificationWriter(firstContext);

        firstVerification.MarkAsUsed(UtcNow.AddSeconds(1));
        firstUser.MarkEmailAsVerified();
        firstUser.ChangeStatus(UserStatus.Active);

        Assert.False(await firstWriter.TryConfirmAsync(
            firstUser, firstVerification, TestContext.Current.CancellationToken));

        await using var verificationContext = _fixture.CreateContext();

        UserEmailVerification stored = await verificationContext
            .Set<UserEmailVerification>()
            .SingleAsync(v => v.Id == verificationId, TestContext.Current.CancellationToken);

        // Prevalece el canje de la solicitud que llegó primero.
        Assert.Equal(UtcNow, stored.UsedAtUtc);
    }

    /// <summary>
    /// Si el UPDATE del usuario falla, el canje del código tampoco queda confirmado.
    /// </summary>
    [Fact]
    public async Task TryConfirmAsync_WhenTheUserUpdateFails_RollsBackTheVerification()
    {
        UserId userId = await SeedUserAsync();
        UserEmailVerificationId verificationId = await SeedVerificationAsync(userId);

        // Un segundo usuario con el que provocar la violación del índice único de correo.
        var otherUser = User.Create(
            UserId.New(), "Otro", "Usuario", $"{Guid.NewGuid():N}@email.com",
            "AQAAAAIAAYagAAAAEHashSimuladoDePruebas==", null, null, null);

        await using (var seedContext = _fixture.CreateContext())
        {
            seedContext.Set<User>().Add(otherUser);
            await seedContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using (var context = _fixture.CreateContext())
        {
            var writer = new EmailVerificationWriter(context);

            User user = await context.Set<User>()
                .SingleAsync(u => u.Id == userId, TestContext.Current.CancellationToken);

            UserEmailVerification verification = await context.Set<UserEmailVerification>()
                .SingleAsync(v => v.Id == verificationId, TestContext.Current.CancellationToken);

            verification.MarkAsUsed(UtcNow);
            user.MarkEmailAsVerified();
            user.ChangeStatus(UserStatus.Active);

            // El correo duplicado hace fallar el UPDATE de Usuarios.
            user.ChangeEmail(otherUser.Email);

            await Assert.ThrowsAnyAsync<DbUpdateException>(() => writer.TryConfirmAsync(
                user, verification, TestContext.Current.CancellationToken));
        }

        await using var verificationContext = _fixture.CreateContext();

        User storedUser = await verificationContext.Set<User>()
            .SingleAsync(u => u.Id == userId, TestContext.Current.CancellationToken);

        UserEmailVerification storedVerification = await verificationContext
            .Set<UserEmailVerification>()
            .SingleAsync(v => v.Id == verificationId, TestContext.Current.CancellationToken);

        Assert.Equal(UserStatus.Unverified, storedUser.Status);
        Assert.False(storedUser.IsEmailVerified);
        Assert.False(storedVerification.IsUsed);
    }

    // --- Intentos fallidos ---

    [Fact]
    public async Task RegisterFailedAttemptAsync_PersistsTheCounter()
    {
        UserId userId = await SeedUserAsync();
        UserEmailVerificationId verificationId = await SeedVerificationAsync(userId);

        await using (var context = _fixture.CreateContext())
        {
            var writer = new EmailVerificationWriter(context);

            UserEmailVerification verification = await context.Set<UserEmailVerification>()
                .SingleAsync(v => v.Id == verificationId, TestContext.Current.CancellationToken);

            verification.RegisterFailedAttempt();

            await writer.RegisterFailedAttemptAsync(
                verification, TestContext.Current.CancellationToken);
        }

        await using var verificationContext = _fixture.CreateContext();

        UserEmailVerification stored = await verificationContext
            .Set<UserEmailVerification>()
            .SingleAsync(v => v.Id == verificationId, TestContext.Current.CancellationToken);

        Assert.Equal(1, stored.FailedAttempts);
    }

    [Fact]
    public async Task RegisterFailedAttemptAsync_PersistsTheRevocationOfAnExhaustedCode()
    {
        UserId userId = await SeedUserAsync();
        UserEmailVerificationId verificationId = await SeedVerificationAsync(userId);

        await using (var context = _fixture.CreateContext())
        {
            var writer = new EmailVerificationWriter(context);

            UserEmailVerification verification = await context.Set<UserEmailVerification>()
                .SingleAsync(v => v.Id == verificationId, TestContext.Current.CancellationToken);

            for (int attempt = 0;
                 attempt < UserEmailVerification.DefaultMaximumFailedAttempts;
                 attempt++)
            {
                verification.RegisterFailedAttempt();
            }

            verification.Revoke(UtcNow);

            await writer.RegisterFailedAttemptAsync(
                verification, TestContext.Current.CancellationToken);
        }

        await using var verificationContext = _fixture.CreateContext();

        UserEmailVerification stored = await verificationContext
            .Set<UserEmailVerification>()
            .SingleAsync(v => v.Id == verificationId, TestContext.Current.CancellationToken);

        Assert.Equal(
            UserEmailVerification.DefaultMaximumFailedAttempts, stored.FailedAttempts);
        Assert.True(stored.IsRevoked);
    }

    /// <summary>
    /// Un intento fallido sobre un código que otra solicitud acaba de canjear no falla:
    /// simplemente no contabiliza nada.
    /// </summary>
    [Fact]
    public async Task RegisterFailedAttemptAsync_WhenTheCodeWasUsedConcurrently_DoesNotThrow()
    {
        UserId userId = await SeedUserAsync();
        UserEmailVerificationId verificationId = await SeedVerificationAsync(userId);

        await using var firstContext = _fixture.CreateContext();

        UserEmailVerification firstVerification = await firstContext
            .Set<UserEmailVerification>()
            .SingleAsync(v => v.Id == verificationId, TestContext.Current.CancellationToken);

        await using (var secondContext = _fixture.CreateContext())
        {
            UserEmailVerification secondVerification = await secondContext
                .Set<UserEmailVerification>()
                .SingleAsync(v => v.Id == verificationId, TestContext.Current.CancellationToken);

            secondVerification.MarkAsUsed(UtcNow);

            await secondContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        var writer = new EmailVerificationWriter(firstContext);

        firstVerification.RegisterFailedAttempt();

        await writer.RegisterFailedAttemptAsync(
            firstVerification, TestContext.Current.CancellationToken);

        await using var verificationContext = _fixture.CreateContext();

        UserEmailVerification stored = await verificationContext
            .Set<UserEmailVerification>()
            .SingleAsync(v => v.Id == verificationId, TestContext.Current.CancellationToken);

        Assert.True(stored.IsUsed);
        Assert.Equal(0, stored.FailedAttempts);
    }

    // --- Consultas del repositorio ---

    [Fact]
    public async Task GetLatestPendingAsync_ReturnsTheMostRecentUnusedAndUnrevoked()
    {
        UserId userId = await SeedUserAsync();
        await SeedVerificationAsync(userId, 'a', UtcNow.AddMinutes(-30));
        await SeedVerificationAsync(userId, 'b', UtcNow.AddMinutes(-5));

        await using var context = _fixture.CreateContext();
        var repository = new UserEmailVerificationRepository(context);

        UserEmailVerification? latest = await repository.GetLatestPendingAsync(
            userId, TestContext.Current.CancellationToken);

        Assert.NotNull(latest);
        Assert.Equal(Hash('b'), latest.CodeHash);
    }

    [Fact]
    public async Task GetLatestPendingAsync_IgnoresUsedAndRevokedVerifications()
    {
        UserId userId = await SeedUserAsync();
        UserEmailVerificationId usedId = await SeedVerificationAsync(userId, 'a');
        UserEmailVerificationId revokedId = await SeedVerificationAsync(userId, 'b');

        await using (var context = _fixture.CreateContext())
        {
            UserEmailVerification used = await context.Set<UserEmailVerification>()
                .SingleAsync(v => v.Id == usedId, TestContext.Current.CancellationToken);
            used.MarkAsUsed(UtcNow);

            UserEmailVerification revoked = await context.Set<UserEmailVerification>()
                .SingleAsync(v => v.Id == revokedId, TestContext.Current.CancellationToken);
            revoked.Revoke(UtcNow);

            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using var queryContext = _fixture.CreateContext();
        var repository = new UserEmailVerificationRepository(queryContext);

        Assert.Null(await repository.GetLatestPendingAsync(
            userId, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task GetLastCreatedAtUtcAsync_ConsidersUsedAndRevokedVerificationsToo()
    {
        UserId userId = await SeedUserAsync();
        UserEmailVerificationId latestId = await SeedVerificationAsync(
            userId, 'a', UtcNow.AddMinutes(-1));

        await using (var context = _fixture.CreateContext())
        {
            UserEmailVerification used = await context.Set<UserEmailVerification>()
                .SingleAsync(v => v.Id == latestId, TestContext.Current.CancellationToken);
            used.MarkAsUsed(UtcNow);

            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using var queryContext = _fixture.CreateContext();
        var repository = new UserEmailVerificationRepository(queryContext);

        Assert.Equal(
            UtcNow.AddMinutes(-1),
            await repository.GetLastCreatedAtUtcAsync(
                userId, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task GetLastCreatedAtUtcAsync_WithoutAnyVerification_ReturnsNull()
    {
        UserId userId = await SeedUserAsync();

        await using var context = _fixture.CreateContext();
        var repository = new UserEmailVerificationRepository(context);

        Assert.Null(await repository.GetLastCreatedAtUtcAsync(
            userId, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Queries_DoNotLeakVerificationsOfOtherUsers()
    {
        UserId userId = await SeedUserAsync();
        UserId otherUserId = await SeedUserAsync();

        await SeedVerificationAsync(otherUserId, 'b');

        await using var context = _fixture.CreateContext();
        var repository = new UserEmailVerificationRepository(context);

        Assert.Null(await repository.GetLatestPendingAsync(
            userId, TestContext.Current.CancellationToken));
        Assert.Empty(await repository.GetPendingAsync(
            userId, TestContext.Current.CancellationToken));
    }

    public void Dispose() => _fixture.Dispose();
}
