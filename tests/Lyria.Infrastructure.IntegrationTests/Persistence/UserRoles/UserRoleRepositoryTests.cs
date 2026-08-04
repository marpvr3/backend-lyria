using Lyria.Domain.Roles;
using Lyria.Domain.Users;
using Lyria.Domain.Users.UserRoles;
using Lyria.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using Xunit;

namespace Lyria.Infrastructure.IntegrationTests.Persistence.UserRoles;

public sealed class UserRoleRepositoryTests : IDisposable
{
    private static readonly DateTimeOffset FixedTime =
        new(2026, 7, 15, 10, 30, 0, TimeSpan.Zero);

    private readonly FakeTimeProvider _timeProvider;
    private readonly SqliteFixture _fixture;

    public UserRoleRepositoryTests()
    {
        _timeProvider = new FakeTimeProvider(FixedTime);
        _fixture = new SqliteFixture(_timeProvider);
    }

    [Fact]
    public async Task AddAsync_And_SaveChangesAsync_Should_Persist_UserRole()
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
            ScopeType.Global, null, null, FixedTime.UtcDateTime);

        await using (var context = _fixture.CreateContext())
        {
            var repository = new UserRoleRepository(context);
            await repository.AddAsync(userRole, CancellationToken.None);
            await repository.SaveChangesAsync(CancellationToken.None);
        }

        await using (var context = _fixture.CreateContext())
        {
            var persisted = await context.Set<UserRole>()
                .FirstOrDefaultAsync(ur => ur.Id == userRole.Id, CancellationToken.None);

            Assert.NotNull(persisted);
            Assert.Equal(user.Id, persisted.UserId);
            Assert.Equal(role.Id, persisted.RoleId);
            Assert.Equal(ScopeType.Global, persisted.ScopeType);
            Assert.Null(persisted.EstablishmentId);
            Assert.Null(persisted.BranchId);
            Assert.True(persisted.IsActive);
        }
    }

    [Fact]
    public async Task GetByIdAsync_Should_Return_Persisted_UserRole()
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
            ScopeType.Global, null, null, FixedTime.UtcDateTime);

        await using (var context = _fixture.CreateContext())
        {
            var repository = new UserRoleRepository(context);
            await repository.AddAsync(userRole, CancellationToken.None);
            await repository.SaveChangesAsync(CancellationToken.None);
        }

        await using (var context = _fixture.CreateContext())
        {
            var repository = new UserRoleRepository(context);
            var result = await repository.GetByIdAsync(userRole.Id, CancellationToken.None);

            Assert.NotNull(result);
            Assert.Equal(userRole.Id, result.Id);
            Assert.Equal(user.Id, result.UserId);
            Assert.Equal(role.Id, result.RoleId);
        }
    }

    [Fact]
    public async Task GetByIdAsync_Should_Return_Null_WhenNotFound()
    {
        await using var context = _fixture.CreateContext();
        var repository = new UserRoleRepository(context);

        var result = await repository.GetByIdAsync(
            UserRoleId.New(), CancellationToken.None);

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
