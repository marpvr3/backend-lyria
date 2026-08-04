using Lyria.Domain.Roles;
using Lyria.Domain.Users;
using Lyria.Domain.Users.UserRoles;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using Xunit;

namespace Lyria.Infrastructure.IntegrationTests.Persistence.Roles;

/// <summary>
/// Verifica que, con el modelo sin Code, un rol conserva el resto de sus datos
/// y sus asociaciones UserRole siguen siendo válidas mediante RoleId.
/// </summary>
public sealed class RoleCodeRemovalDataPreservationTests : IDisposable
{
    private static readonly DateTimeOffset FixedTime =
        new(2026, 8, 3, 12, 0, 0, TimeSpan.Zero);

    private readonly SqliteFixture _fixture;

    public RoleCodeRemovalDataPreservationTests()
    {
        _fixture = new SqliteFixture(new FakeTimeProvider(FixedTime));
    }

    [Fact]
    public async Task ExistingRole_PreservesAllRemainingFields()
    {
        var role = Role.Create(RoleId.New(), "Administrador", "Rol de administración");
        RoleId roleId = role.Id;

        await using (var context = _fixture.CreateContext())
        {
            context.Set<Role>().Add(role);
            await context.SaveChangesAsync(CancellationToken.None);
        }

        await using (var context = _fixture.CreateContext())
        {
            Role? persisted = await context.Set<Role>()
                .FirstOrDefaultAsync(r => r.Id == roleId, CancellationToken.None);

            Assert.NotNull(persisted);
            Assert.Equal(roleId, persisted.Id);
            Assert.Equal("Administrador", persisted.Name);
            Assert.Equal("Rol de administración", persisted.Description);
            Assert.True(persisted.IsActive);
            Assert.Equal(FixedTime.UtcDateTime, persisted.CreatedAtUtc);
            Assert.Null(persisted.UpdatedAtUtc);
        }
    }

    [Fact]
    public async Task ExistingRole_PreservesUpdatedAtUtc_AfterUpdate()
    {
        var role = Role.Create(RoleId.New(), "Administrador", null);
        RoleId roleId = role.Id;

        await using (var context = _fixture.CreateContext())
        {
            context.Set<Role>().Add(role);
            await context.SaveChangesAsync(CancellationToken.None);
        }

        await using (var context = _fixture.CreateContext())
        {
            Role persisted = await context.Set<Role>()
                .FirstAsync(r => r.Id == roleId, CancellationToken.None);

            persisted.Update("Super Administrador", "Actualizado");
            await context.SaveChangesAsync(CancellationToken.None);
        }

        await using (var context = _fixture.CreateContext())
        {
            Role persisted = await context.Set<Role>()
                .FirstAsync(r => r.Id == roleId, CancellationToken.None);

            Assert.Equal("Super Administrador", persisted.Name);
            Assert.Equal("Actualizado", persisted.Description);
            Assert.NotNull(persisted.UpdatedAtUtc);
        }
    }

    [Fact]
    public async Task UserRoleAssociation_RemainsValidThroughRoleId()
    {
        var role = Role.Create(RoleId.New(), "Administrador", null);
        var user = User.Create(
            UserId.New(), "Juan", "Pérez", "juan@example.com",
            "hashed_password123", null, null, null);

        await using (var context = _fixture.CreateContext())
        {
            context.Set<Role>().Add(role);
            context.Set<User>().Add(user);
            await context.SaveChangesAsync(CancellationToken.None);
        }

        var userRole = UserRole.Assign(
            UserRoleId.New(), user.Id, role.Id,
            ScopeType.Global, null, null, FixedTime.UtcDateTime);

        await using (var context = _fixture.CreateContext())
        {
            context.Set<UserRole>().Add(userRole);
            await context.SaveChangesAsync(CancellationToken.None);
        }

        await using (var context = _fixture.CreateContext())
        {
            UserRole? persisted = await context.Set<UserRole>()
                .FirstOrDefaultAsync(ur => ur.Id == userRole.Id, CancellationToken.None);

            Assert.NotNull(persisted);
            Assert.Equal(role.Id, persisted.RoleId);
            Assert.Equal(user.Id, persisted.UserId);
            Assert.True(persisted.IsActive);

            bool roleStillResolvable = await context.Set<Role>()
                .AnyAsync(r => r.Id == persisted.RoleId, CancellationToken.None);

            Assert.True(roleStillResolvable);
        }
    }

    [Fact]
    public async Task UserRoles_ForeignKeyToRoles_IsStillDeclared()
    {
        await using var context = _fixture.CreateContext();

        var entityType = context.Model.FindEntityType(typeof(UserRole))!;

        Assert.Contains(
            entityType.GetForeignKeys(),
            fk => fk.PrincipalEntityType.ClrType == typeof(Role) &&
                  fk.GetConstraintName() == "FK_UsuarioRoles_Roles_RolId");
    }

    public void Dispose()
    {
        _fixture.Dispose();
    }
}
