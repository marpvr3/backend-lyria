using Lyria.Domain.Roles;
using Lyria.Domain.Users;
using Lyria.Domain.Users.UserRoles;
using Lyria.Infrastructure.Persistence.ReadServices;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Lyria.Infrastructure.IntegrationTests.Persistence.UserRoles;

public sealed class UserRoleReadServiceTests : IDisposable
{
    private readonly SqliteFixture _fixture = new();

    [Fact]
    public async Task GetByUserIdAsync_Should_Return_UserRoles()
    {
        var user = CreateTestUser();
        var role = CreateTestRole();

        await using (var context = _fixture.CreateContext())
        {
            context.Set<User>().Add(user);
            context.Set<Role>().Add(role);
            await context.SaveChangesAsync(CancellationToken.None);
        }

        var userRole = UserRole.Assign(
            UserRoleId.New(), user.Id, role.Id,
            ScopeType.Global, null, null, DateTime.UtcNow);

        await using (var context = _fixture.CreateContext())
        {
            context.Set<UserRole>().Add(userRole);
            await context.SaveChangesAsync(CancellationToken.None);
        }

        await using (var context = _fixture.CreateContext())
        {
            var readService = new UserRoleReadService(context);
            var result = await readService.GetByUserIdAsync(user.Id, CancellationToken.None);

            Assert.Single(result);
            Assert.Equal(userRole.Id.Value, result[0].Id);
            Assert.Equal(user.Id.Value, result[0].UserId);
            Assert.Equal(role.Id.Value, result[0].RoleId);
        }
    }

    [Fact]
    public async Task GetByUserIdAsync_Should_Return_Empty_WhenNoRoles()
    {
        await using var context = _fixture.CreateContext();
        var readService = new UserRoleReadService(context);

        var result = await readService.GetByUserIdAsync(UserId.New(), CancellationToken.None);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetByUserIdAsync_Should_Include_RoleIdAndName()
    {
        var user = CreateTestUser();
        var role = Role.Create(RoleId.New(), "Administrador", null);

        await using (var context = _fixture.CreateContext())
        {
            context.Set<User>().Add(user);
            context.Set<Role>().Add(role);
            await context.SaveChangesAsync(CancellationToken.None);
        }

        var userRole = UserRole.Assign(
            UserRoleId.New(), user.Id, role.Id,
            ScopeType.Global, null, null, DateTime.UtcNow);

        await using (var context = _fixture.CreateContext())
        {
            context.Set<UserRole>().Add(userRole);
            await context.SaveChangesAsync(CancellationToken.None);
        }

        await using (var context = _fixture.CreateContext())
        {
            var readService = new UserRoleReadService(context);
            var result = await readService.GetByUserIdAsync(user.Id, CancellationToken.None);

            Assert.Single(result);
            Assert.Equal(role.Id.Value, result[0].RoleId);
            Assert.Equal("Administrador", result[0].RoleName);
        }
    }

    [Fact]
    public async Task GetByIdAsync_Should_Return_UserRole()
    {
        var user = CreateTestUser();
        var role = CreateTestRole();

        await using (var context = _fixture.CreateContext())
        {
            context.Set<User>().Add(user);
            context.Set<Role>().Add(role);
            await context.SaveChangesAsync(CancellationToken.None);
        }

        var userRole = UserRole.Assign(
            UserRoleId.New(), user.Id, role.Id,
            ScopeType.Global, null, null, DateTime.UtcNow);

        await using (var context = _fixture.CreateContext())
        {
            context.Set<UserRole>().Add(userRole);
            await context.SaveChangesAsync(CancellationToken.None);
        }

        await using (var context = _fixture.CreateContext())
        {
            var readService = new UserRoleReadService(context);
            var result = await readService.GetByIdAsync(userRole.Id, CancellationToken.None);

            Assert.NotNull(result);
            Assert.Equal(userRole.Id.Value, result.Id);
            Assert.Equal(user.Id.Value, result.UserId);
            Assert.Equal(role.Id.Value, result.RoleId);
            Assert.Equal("Global", result.ScopeType);
            Assert.True(result.IsActive);
        }
    }

    [Fact]
    public async Task GetByIdAsync_Should_Return_Null_WhenNotFound()
    {
        await using var context = _fixture.CreateContext();
        var readService = new UserRoleReadService(context);

        var result = await readService.GetByIdAsync(UserRoleId.New(), CancellationToken.None);

        Assert.Null(result);
    }

    public void Dispose()
    {
        _fixture.Dispose();
    }

    private static User CreateTestUser(string email = "test@example.com") =>
        User.Create(UserId.New(), "Juan", "Garcia", email, "hashed_pw", null, null, null);

    private static Role CreateTestRole(string name = "Test Role") =>
        Role.Create(RoleId.New(), name, null);
}
