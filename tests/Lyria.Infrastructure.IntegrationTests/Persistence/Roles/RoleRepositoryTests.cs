using System.Globalization;
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
            Assert.Equal("Test Role", persisted.Name);
            Assert.Null(persisted.Description);
            Assert.True(persisted.IsActive);
            Assert.Equal(FixedTime.UtcDateTime, persisted.CreatedAtUtc);
        }
    }

    [Fact]
    public async Task AddAsync_Should_Issue_InsertWithoutCodigoColumn()
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
            var connection = context.Database.GetDbConnection();

            using var command = connection.CreateCommand();
            command.CommandText = "SELECT COUNT(*) FROM pragma_table_info('Roles') WHERE name = 'Codigo';";

            long matches = Convert.ToInt64(
                await command.ExecuteScalarAsync(CancellationToken.None),
                CultureInfo.InvariantCulture);

            Assert.Equal(0, matches);
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
            Assert.Equal("Test Role", result.Name);
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
    public async Task SaveChangesAsync_Should_Allow_Roles_WithSameName()
    {
        await using (var context = _fixture.CreateContext())
        {
            var repository = new RoleRepository(context);
            await repository.AddAsync(CreateTestRole(), CancellationToken.None);
            await repository.AddAsync(CreateTestRole(), CancellationToken.None);
            await repository.SaveChangesAsync(CancellationToken.None);
        }

        await using (var context = _fixture.CreateContext())
        {
            int count = await context.Set<Role>().CountAsync(CancellationToken.None);

            Assert.Equal(2, count);
        }
    }

    public void Dispose()
    {
        _fixture.Dispose();
    }

    private static Role CreateTestRole(string name = "Test Role") =>
        Role.Create(RoleId.New(), name, null);
}
