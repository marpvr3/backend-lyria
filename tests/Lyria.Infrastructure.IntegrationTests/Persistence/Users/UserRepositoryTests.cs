using Lyria.Domain.Users;
using Lyria.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using Xunit;

namespace Lyria.Infrastructure.IntegrationTests.Persistence.Users;

public sealed class UserRepositoryTests : IDisposable
{
    private static readonly DateTimeOffset FixedTime =
        new(2026, 7, 15, 10, 30, 0, TimeSpan.Zero);

    private readonly FakeTimeProvider _timeProvider;
    private readonly SqliteFixture _fixture;

    public UserRepositoryTests()
    {
        _timeProvider = new FakeTimeProvider(FixedTime);
        _fixture = new SqliteFixture(_timeProvider);
    }

    [Fact]
    public async Task AddAsync_And_SaveChangesAsync_Should_Persist_User()
    {
        var user = CreateTestUser();

        await using (var context = _fixture.CreateContext())
        {
            var repository = new UserRepository(context);
            await repository.AddAsync(user, CancellationToken.None);
            await repository.SaveChangesAsync(CancellationToken.None);
        }

        await using (var context = _fixture.CreateContext())
        {
            var persisted = await context.Set<User>()
                .FirstOrDefaultAsync(u => u.Id == user.Id, CancellationToken.None);

            Assert.NotNull(persisted);
            Assert.Equal("Juan", persisted.Name);
            Assert.Equal("Garcia", persisted.LastName);
            Assert.Equal("test@example.com", persisted.Email);
            Assert.Equal(UserStatus.Unverified, persisted.Status);
            Assert.False(persisted.IsEmailVerified);
        }
    }

    [Fact]
    public async Task GetByIdAsync_Should_Return_Persisted_User()
    {
        var user = CreateTestUser();

        await using (var context = _fixture.CreateContext())
        {
            var repository = new UserRepository(context);
            await repository.AddAsync(user, CancellationToken.None);
            await repository.SaveChangesAsync(CancellationToken.None);
        }

        await using (var context = _fixture.CreateContext())
        {
            var repository = new UserRepository(context);
            var result = await repository.GetByIdAsync(user.Id, CancellationToken.None);

            Assert.NotNull(result);
            Assert.Equal(user.Id, result.Id);
            Assert.Equal("Juan", result.Name);
        }
    }

    [Fact]
    public async Task GetByIdAsync_Should_Return_Null_WhenNotFound()
    {
        await using var context = _fixture.CreateContext();
        var repository = new UserRepository(context);

        var result = await repository.GetByIdAsync(
            UserId.New(), CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task ExistsByEmailAsync_Should_Return_True_WhenEmailExists()
    {
        var user = CreateTestUser("exists@example.com");

        await using (var context = _fixture.CreateContext())
        {
            var repository = new UserRepository(context);
            await repository.AddAsync(user, CancellationToken.None);
            await repository.SaveChangesAsync(CancellationToken.None);
        }

        await using (var context = _fixture.CreateContext())
        {
            var repository = new UserRepository(context);
            var exists = await repository.ExistsByEmailAsync(
                "exists@example.com", null, CancellationToken.None);

            Assert.True(exists);
        }
    }

    [Fact]
    public async Task ExistsByEmailAsync_Should_Return_False_WhenEmailNotExists()
    {
        await using var context = _fixture.CreateContext();
        var repository = new UserRepository(context);

        var exists = await repository.ExistsByEmailAsync(
            "noexiste@example.com", null, CancellationToken.None);

        Assert.False(exists);
    }

    [Fact]
    public async Task ExistsByEmailAsync_Should_Exclude_Specified_Id()
    {
        var user = CreateTestUser("exclude@example.com");

        await using (var context = _fixture.CreateContext())
        {
            var repository = new UserRepository(context);
            await repository.AddAsync(user, CancellationToken.None);
            await repository.SaveChangesAsync(CancellationToken.None);
        }

        await using (var context = _fixture.CreateContext())
        {
            var repository = new UserRepository(context);
            var exists = await repository.ExistsByEmailAsync(
                "exclude@example.com", user.Id, CancellationToken.None);

            Assert.False(exists);
        }
    }

    [Fact]
    public async Task SaveChangesAsync_Should_Set_CreatedAtUtc_On_Add()
    {
        var user = CreateTestUser();

        await using var context = _fixture.CreateContext();
        var repository = new UserRepository(context);
        await repository.AddAsync(user, CancellationToken.None);
        await repository.SaveChangesAsync(CancellationToken.None);

        Assert.Equal(FixedTime.UtcDateTime, user.CreatedAtUtc);
    }

    [Fact]
    public async Task SaveChangesAsync_Should_Set_UpdatedAtUtc_On_Modify()
    {
        var user = CreateTestUser();

        await using (var context = _fixture.CreateContext())
        {
            var repository = new UserRepository(context);
            await repository.AddAsync(user, CancellationToken.None);
            await repository.SaveChangesAsync(CancellationToken.None);
        }

        var updateTime = FixedTime.AddHours(1);
        _timeProvider.SetUtcNow(updateTime);

        await using (var context = _fixture.CreateContext())
        {
            var tracked = await context.Set<User>()
                .FirstAsync(u => u.Id == user.Id, CancellationToken.None);
            tracked.UpdateProfile("Pedro", "Lopez", null, null, null);
            await context.SaveChangesAsync(CancellationToken.None);

            Assert.Equal(updateTime.UtcDateTime, tracked.UpdatedAtUtc);
        }
    }

    [Fact]
    public async Task PasswordHash_Should_Be_Persisted()
    {
        var user = CreateTestUser();

        await using (var context = _fixture.CreateContext())
        {
            var repository = new UserRepository(context);
            await repository.AddAsync(user, CancellationToken.None);
            await repository.SaveChangesAsync(CancellationToken.None);
        }

        await using (var context = _fixture.CreateContext())
        {
            var persisted = await context.Set<User>()
                .FirstOrDefaultAsync(u => u.Id == user.Id, CancellationToken.None);

            Assert.NotNull(persisted);
            Assert.Equal("hashed_password_123", persisted.PasswordHash);
        }
    }

    public void Dispose()
    {
        _fixture.Dispose();
    }

    private static User CreateTestUser(string email = "test@example.com") =>
        User.Create(UserId.New(), "Juan", "Garcia", email, "hashed_password_123", null, null, null);
}
