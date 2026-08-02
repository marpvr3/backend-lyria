using Lyria.Domain.Roles;
using Lyria.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using Xunit;

namespace Lyria.Infrastructure.IntegrationTests.Persistence.Roles;

public sealed class RoleRepositoryTests : IDisposable
{
    private static readonly DateTimeOffset FixedTime =
        new(2026, 7, 15, 10, 30, 0, TimeSpan.Zero);

    private readonly FakeTimeProvider _timeProvider;
    private readonly SqliteFixture _fixture;

    public RoleRepositoryTests()
    {
        _timeProvider = new FakeTimeProvider(FixedTime);
        _fixture = new SqliteFixture(_timeProvider);
    }

    [Fact]
    public async Task AddAsync_And_SaveChangesAsync_Should_Persist_Role()
    {
        var role = CreateTestRole();

        await using (var context = _fixture.CreateContext())
        {
            var repository = new RoleRepository(context);
            await repository.AddAsync(role, CancellationToken.None);
            await repository.SaveChangesAsync(CancellationToken.None);
        }

        await using (var context = _fixture.CreateContext())
        {
            var persisted = await context.Set<Role>()
                .FirstOrDefaultAsync(r => r.Id == role.Id, CancellationToken.None);

            Assert.NotNull(persisted);
            Assert.Equal("TEST_ROLE", persisted.Code);
            Assert.Equal("Test Role", persisted.Name);
            Assert.Null(persisted.Description);
            Assert.True(persisted.IsActive);
        }
    }

    [Fact]
    public async Task GetByIdAsync_Should_Return_Persisted_Role()
    {
        var role = CreateTestRole();

        await using (var context = _fixture.CreateContext())
        {
            var repository = new RoleRepository(context);
            await repository.AddAsync(role, CancellationToken.None);
            await repository.SaveChangesAsync(CancellationToken.None);
        }

        await using (var context = _fixture.CreateContext())
        {
            var repository = new RoleRepository(context);
            var result = await repository.GetByIdAsync(role.Id, CancellationToken.None);

            Assert.NotNull(result);
            Assert.Equal(role.Id, result.Id);
            Assert.Equal("TEST_ROLE", result.Code);
        }
    }

    [Fact]
    public async Task GetByIdAsync_Should_Return_Null_WhenNotFound()
    {
        await using var context = _fixture.CreateContext();
        var repository = new RoleRepository(context);

        var result = await repository.GetByIdAsync(
            RoleId.New(), CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task ExistsByCodeAsync_Should_Return_True_WhenCodeExists()
    {
        var role = CreateTestRole("EXISTING_ROLE");

        await using (var context = _fixture.CreateContext())
        {
            var repository = new RoleRepository(context);
            await repository.AddAsync(role, CancellationToken.None);
            await repository.SaveChangesAsync(CancellationToken.None);
        }

        await using (var context = _fixture.CreateContext())
        {
            var repository = new RoleRepository(context);
            var exists = await repository.ExistsByCodeAsync(
                "EXISTING_ROLE", null, CancellationToken.None);

            Assert.True(exists);
        }
    }

    [Fact]
    public async Task ExistsByCodeAsync_Should_Return_False_WhenCodeNotExists()
    {
        await using var context = _fixture.CreateContext();
        var repository = new RoleRepository(context);

        var exists = await repository.ExistsByCodeAsync(
            "NONEXISTENT_ROLE", null, CancellationToken.None);

        Assert.False(exists);
    }

    [Fact]
    public async Task ExistsByCodeAsync_Should_Exclude_Specified_Id()
    {
        var role = CreateTestRole("EXCLUDE_ROLE");

        await using (var context = _fixture.CreateContext())
        {
            var repository = new RoleRepository(context);
            await repository.AddAsync(role, CancellationToken.None);
            await repository.SaveChangesAsync(CancellationToken.None);
        }

        await using (var context = _fixture.CreateContext())
        {
            var repository = new RoleRepository(context);
            var exists = await repository.ExistsByCodeAsync(
                "EXCLUDE_ROLE", role.Id, CancellationToken.None);

            Assert.False(exists);
        }
    }

    public void Dispose()
    {
        _fixture.Dispose();
    }

    private static Role CreateTestRole(string code = "TEST_ROLE") =>
        Role.Create(RoleId.New(), code, "Test Role", null);
}
