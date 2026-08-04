using Lyria.Domain.Restrictions;
using Lyria.Domain.Roles;
using Lyria.Domain.Users;
using Lyria.Domain.Users.UserRestrictions;
using Lyria.Domain.Users.UserRoles;
using Lyria.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Lyria.Infrastructure.IntegrationTests.Persistence.MobileRegistrations;

/// <summary>
/// Verifica la atomicidad real del registro móvil sobre el proveedor relacional
/// utilizado en pruebas (SQLite). No se usa EF Core InMemory: no soporta
/// transacciones ni claves foráneas.
/// </summary>
public sealed class MobileRegistrationWriterTests : IDisposable
{
    private static readonly DateTime UtcNow =
        new(2026, 8, 3, 10, 30, 0, DateTimeKind.Utc);

    private const string PasswordHash = "AQAAAAIAAYagAAAAEHashSimuladoDePruebas==";

    private readonly SqliteFixture _fixture = new();

    private RoleId SeedRole()
    {
        var role = Role.Create(RoleId.New(), "Usuario", null);

        using var context = _fixture.CreateContext();
        context.Set<Role>().Add(role);
        context.SaveChanges();

        return role.Id;
    }

    private RestrictionId SeedRestriction()
    {
        var restriction = Restriction.Create(
            RestrictionId.New(), $"R-{Guid.NewGuid():N}", null);

        using var context = _fixture.CreateContext();
        context.Set<Restriction>().Add(restriction);
        context.SaveChanges();

        return restriction.Id;
    }

    private static User NewUser(UserId userId) =>
        User.Create(
            userId, "Andres", "Perez", $"{Guid.NewGuid():N}@email.com",
            PasswordHash, "3001234567", new DateOnly(1978, 12, 25), null);

    private static UserRole NewUserRole(UserId userId, RoleId roleId) =>
        UserRole.Assign(
            UserRoleId.New(), userId, roleId, ScopeType.Global, null, null, UtcNow);

    private static UserRestriction NewUserRestriction(
        UserId userId, RestrictionId restrictionId, string level) =>
        UserRestriction.Create(userId, restrictionId, level, UtcNow);

    // --- Confirmación conjunta ---

    [Fact]
    public async Task RegisterAsync_PersistsUserRoleAndRestrictions_Together()
    {
        RoleId roleId = SeedRole();
        RestrictionId first = SeedRestriction();
        RestrictionId second = SeedRestriction();
        var userId = UserId.New();

        await using (var context = _fixture.CreateContext())
        {
            var writer = new MobileRegistrationWriter(context);

            await writer.RegisterAsync(
                NewUser(userId),
                NewUserRole(userId, roleId),
                [
                    NewUserRestriction(userId, first, UserRestrictionImportanceLevels.High),
                    NewUserRestriction(userId, second, UserRestrictionImportanceLevels.High)
                ],
                CancellationToken.None);
        }

        await using var verification = _fixture.CreateContext();

        Assert.NotNull(await verification.Set<User>()
            .FirstOrDefaultAsync(u => u.Id == userId, CancellationToken.None));

        Assert.Single(await verification.Set<UserRole>()
            .Where(ur => ur.UserId == userId).ToListAsync(CancellationToken.None));

        Assert.Equal(2, await verification.Set<UserRestriction>()
            .CountAsync(ur => ur.UserId == userId, CancellationToken.None));
    }

    [Fact]
    public async Task RegisterAsync_WithoutRestrictions_PersistsUserAndUserRole()
    {
        RoleId roleId = SeedRole();
        var userId = UserId.New();

        await using (var context = _fixture.CreateContext())
        {
            var writer = new MobileRegistrationWriter(context);

            await writer.RegisterAsync(
                NewUser(userId), NewUserRole(userId, roleId), [], CancellationToken.None);
        }

        await using var verification = _fixture.CreateContext();

        Assert.NotNull(await verification.Set<User>()
            .FirstOrDefaultAsync(u => u.Id == userId, CancellationToken.None));
        Assert.Single(await verification.Set<UserRole>()
            .Where(ur => ur.UserId == userId).ToListAsync(CancellationToken.None));
        Assert.Empty(await verification.Set<UserRestriction>()
            .Where(ur => ur.UserId == userId).ToListAsync(CancellationToken.None));
    }

    // --- Rollback ---

    [Fact]
    public async Task RegisterAsync_WhenUserRoleFails_RollsBackUser()
    {
        // La FK de Roles se viola: el rol no existe en la base.
        var missingRoleId = RoleId.New();
        var userId = UserId.New();

        await using (var context = _fixture.CreateContext())
        {
            var writer = new MobileRegistrationWriter(context);

            await Assert.ThrowsAnyAsync<DbUpdateException>(() =>
                writer.RegisterAsync(
                    NewUser(userId),
                    NewUserRole(userId, missingRoleId),
                    [],
                    CancellationToken.None));
        }

        await using var verification = _fixture.CreateContext();

        Assert.Null(await verification.Set<User>()
            .FirstOrDefaultAsync(u => u.Id == userId, CancellationToken.None));
        Assert.Empty(await verification.Set<UserRole>()
            .Where(ur => ur.UserId == userId).ToListAsync(CancellationToken.None));
    }

    [Fact]
    public async Task RegisterAsync_WhenUserRestrictionFails_RollsBackUserAndUserRole()
    {
        RoleId roleId = SeedRole();
        RestrictionId existing = SeedRestriction();
        var missingRestrictionId = RestrictionId.New();
        var userId = UserId.New();

        await using (var context = _fixture.CreateContext())
        {
            var writer = new MobileRegistrationWriter(context);

            await Assert.ThrowsAnyAsync<DbUpdateException>(() =>
                writer.RegisterAsync(
                    NewUser(userId),
                    NewUserRole(userId, roleId),
                    [
                        NewUserRestriction(userId, existing, UserRestrictionImportanceLevels.High),
                        NewUserRestriction(
                            userId, missingRestrictionId, UserRestrictionImportanceLevels.High)
                    ],
                    CancellationToken.None));
        }

        await using var verification = _fixture.CreateContext();

        Assert.Null(await verification.Set<User>()
            .FirstOrDefaultAsync(u => u.Id == userId, CancellationToken.None));
        Assert.Empty(await verification.Set<UserRole>()
            .Where(ur => ur.UserId == userId).ToListAsync(CancellationToken.None));
        Assert.Empty(await verification.Set<UserRestriction>()
            .Where(ur => ur.UserId == userId).ToListAsync(CancellationToken.None));
    }

    [Fact]
    public async Task RegisterAsync_WhenRestrictionIsDuplicated_RollsBackEverything()
    {
        // La PK compuesta (UsuarioId, RestriccionId) impide duplicados.
        RoleId roleId = SeedRole();
        RestrictionId restrictionId = SeedRestriction();
        var userId = UserId.New();

        await using (var context = _fixture.CreateContext())
        {
            var writer = new MobileRegistrationWriter(context);

            await Assert.ThrowsAnyAsync<Exception>(() =>
                writer.RegisterAsync(
                    NewUser(userId),
                    NewUserRole(userId, roleId),
                    [
                        NewUserRestriction(
                            userId, restrictionId, UserRestrictionImportanceLevels.High),
                        NewUserRestriction(
                            userId, restrictionId, UserRestrictionImportanceLevels.Low)
                    ],
                    CancellationToken.None));
        }

        await using var verification = _fixture.CreateContext();

        Assert.Null(await verification.Set<User>()
            .FirstOrDefaultAsync(u => u.Id == userId, CancellationToken.None));
        Assert.Empty(await verification.Set<UserRole>()
            .Where(ur => ur.UserId == userId).ToListAsync(CancellationToken.None));
    }

    [Fact]
    public async Task RegisterAsync_WhenEmailIsDuplicated_RollsBackEverything()
    {
        RoleId roleId = SeedRole();
        var existingUserId = UserId.New();
        User existingUser = NewUser(existingUserId);

        await using (var seedContext = _fixture.CreateContext())
        {
            seedContext.Set<User>().Add(existingUser);
            await seedContext.SaveChangesAsync(CancellationToken.None);
        }

        var userId = UserId.New();
        var duplicate = User.Create(
            userId, "Otro", "Usuario", existingUser.Email, PasswordHash, null, null, null);

        await using (var context = _fixture.CreateContext())
        {
            var writer = new MobileRegistrationWriter(context);

            await Assert.ThrowsAnyAsync<DbUpdateException>(() =>
                writer.RegisterAsync(
                    duplicate, NewUserRole(userId, roleId), [], CancellationToken.None));
        }

        await using var verification = _fixture.CreateContext();

        Assert.Null(await verification.Set<User>()
            .FirstOrDefaultAsync(u => u.Id == userId, CancellationToken.None));
        Assert.Empty(await verification.Set<UserRole>()
            .Where(ur => ur.UserId == userId).ToListAsync(CancellationToken.None));
    }

    [Fact]
    public async Task RegisterAsync_DoesNotConfirmData_BeforeCompleting()
    {
        // Una conexión distinta no debe ver nada mientras la transacción está abierta.
        // Se comprueba de forma indirecta: tras un fallo no queda ningún rastro,
        // ni siquiera del primer INSERT que sí se ejecutó con éxito.
        var missingRoleId = RoleId.New();
        var userId = UserId.New();
        User user = NewUser(userId);

        await using (var context = _fixture.CreateContext())
        {
            var writer = new MobileRegistrationWriter(context);

            await Assert.ThrowsAnyAsync<DbUpdateException>(() =>
                writer.RegisterAsync(
                    user, NewUserRole(userId, missingRoleId), [], CancellationToken.None));
        }

        await using var verification = _fixture.CreateContext();

        Assert.False(await verification.Set<User>()
            .AnyAsync(u => u.Email == user.Email, CancellationToken.None));
    }

    // --- Datos persistidos ---

    [Fact]
    public async Task RegisterAsync_PreservesGlobalScopeAndConfiguredRole()
    {
        RoleId roleId = SeedRole();
        var userId = UserId.New();

        await using (var context = _fixture.CreateContext())
        {
            var writer = new MobileRegistrationWriter(context);
            await writer.RegisterAsync(
                NewUser(userId), NewUserRole(userId, roleId), [], CancellationToken.None);
        }

        await using var verification = _fixture.CreateContext();

        UserRole persisted = await verification.Set<UserRole>()
            .SingleAsync(ur => ur.UserId == userId, CancellationToken.None);

        Assert.Equal(roleId, persisted.RoleId);
        Assert.Equal(ScopeType.Global, persisted.ScopeType);
        Assert.Null(persisted.EstablishmentId);
        Assert.Null(persisted.BranchId);
        Assert.True(persisted.IsActive);
        Assert.Equal(UtcNow, persisted.AssignedAtUtc);
        Assert.Null(persisted.EndedAtUtc);
    }

    [Fact]
    public async Task RegisterAsync_PersistsConfiguredImportanceLevel()
    {
        RoleId roleId = SeedRole();
        RestrictionId restrictionId = SeedRestriction();
        var userId = UserId.New();

        await using (var context = _fixture.CreateContext())
        {
            var writer = new MobileRegistrationWriter(context);
            await writer.RegisterAsync(
                NewUser(userId),
                NewUserRole(userId, roleId),
                [NewUserRestriction(
                    userId, restrictionId, UserRestrictionImportanceLevels.High)],
                CancellationToken.None);
        }

        await using var verification = _fixture.CreateContext();

        UserRestriction persisted = await verification.Set<UserRestriction>()
            .SingleAsync(ur => ur.UserId == userId, CancellationToken.None);

        Assert.Equal(UserRestrictionImportanceLevels.High, persisted.ImportanceLevel);
        Assert.Equal(UtcNow, persisted.CreatedAtUtc);
    }

    [Fact]
    public async Task RegisterAsync_NeverStoresPlainTextPassword()
    {
        RoleId roleId = SeedRole();
        var userId = UserId.New();

        await using (var context = _fixture.CreateContext())
        {
            var writer = new MobileRegistrationWriter(context);
            await writer.RegisterAsync(
                NewUser(userId), NewUserRole(userId, roleId), [], CancellationToken.None);
        }

        await using var verification = _fixture.CreateContext();

        User persisted = await verification.Set<User>()
            .SingleAsync(u => u.Id == userId, CancellationToken.None);

        Assert.Equal(PasswordHash, persisted.PasswordHash);
        Assert.DoesNotContain(
            "Password123", persisted.PasswordHash, StringComparison.OrdinalIgnoreCase);
    }

    public void Dispose() => _fixture.Dispose();
}
