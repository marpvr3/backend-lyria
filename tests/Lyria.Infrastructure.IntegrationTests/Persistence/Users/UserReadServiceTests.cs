using Lyria.Application.Features.Users;
using Lyria.Domain.Users;
using Lyria.Infrastructure.Persistence;
using Lyria.Infrastructure.Persistence.ReadServices;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Lyria.Infrastructure.IntegrationTests.Persistence.Users;

public sealed class UserReadServiceTests : IDisposable
{
    private readonly SqliteFixture _fixture = new();

    [Fact]
    public async Task GetByIdAsync_Should_Return_UserResponse()
    {
        var user = CreateTestUser();

        await using (var context = _fixture.CreateContext())
        {
            context.Set<User>().Add(user);
            await context.SaveChangesAsync(CancellationToken.None);
        }

        await using (var context = _fixture.CreateContext())
        {
            var readService = new UserReadService(context);
            var result = await readService.GetByIdAsync(user.Id, CancellationToken.None);

            Assert.NotNull(result);
            Assert.Equal(user.Id.Value, result.Id);
            Assert.Equal("Juan", result.Name);
            Assert.Equal("Garcia", result.LastName);
            Assert.Equal("test@example.com", result.Email);
            Assert.Equal("Unverified", result.Status);
            Assert.False(result.IsEmailVerified);
        }
    }

    [Fact]
    public async Task GetByIdAsync_Should_Return_Null_WhenNotFound()
    {
        await using var context = _fixture.CreateContext();
        var readService = new UserReadService(context);

        var result = await readService.GetByIdAsync(UserId.New(), CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByIdAsync_Should_Not_Expose_PasswordHash()
    {
        var user = CreateTestUser();

        await using (var context = _fixture.CreateContext())
        {
            context.Set<User>().Add(user);
            await context.SaveChangesAsync(CancellationToken.None);
        }

        await using (var context = _fixture.CreateContext())
        {
            var readService = new UserReadService(context);
            var result = await readService.GetByIdAsync(user.Id, CancellationToken.None);

            Assert.NotNull(result);

            // UserResponse record does not have a PasswordHash property
            var properties = typeof(UserResponse).GetProperties();
            Assert.DoesNotContain(properties, p =>
                p.Name.Equals("PasswordHash", StringComparison.OrdinalIgnoreCase));
        }
    }

    [Fact]
    public async Task ListAsync_Should_Return_PagedResponse()
    {
        await using (var context = _fixture.CreateContext())
        {
            context.Set<User>().AddRange(
                CreateTestUser("a@example.com"),
                CreateTestUser("b@example.com"),
                CreateTestUser("c@example.com"));
            await context.SaveChangesAsync(CancellationToken.None);
        }

        await using (var context = _fixture.CreateContext())
        {
            var readService = new UserReadService(context);
            var filter = new UserListFilter(null, null, null, null, 1, 10, null, null);

            var result = await readService.ListAsync(filter, CancellationToken.None);

            Assert.Equal(3, result.TotalItems);
            Assert.Equal(3, result.Items.Count);
            Assert.Equal(1, result.Page);
            Assert.Equal(10, result.PageSize);
        }
    }

    [Fact]
    public async Task ListAsync_Should_Filter_By_Search()
    {
        await using (var context = _fixture.CreateContext())
        {
            context.Set<User>().AddRange(
                CreateTestUser("maria@example.com", name: "Maria", lastName: "Lopez"),
                CreateTestUser("pedro@example.com", name: "Pedro", lastName: "Garcia"),
                CreateTestUser("ana@example.com", name: "Ana", lastName: "Martinez"));
            await context.SaveChangesAsync(CancellationToken.None);
        }

        await using (var context = _fixture.CreateContext())
        {
            var readService = new UserReadService(context);

            // Search by name
            var filterByName = new UserListFilter("Maria", null, null, null, 1, 10, null, null);
            var resultByName = await readService.ListAsync(filterByName, CancellationToken.None);
            Assert.Single(resultByName.Items);
            Assert.Equal("Maria", resultByName.Items[0].Name);

            // Search by lastName
            var filterByLastName = new UserListFilter("Garcia", null, null, null, 1, 10, null, null);
            var resultByLastName = await readService.ListAsync(filterByLastName, CancellationToken.None);
            Assert.Single(resultByLastName.Items);
            Assert.Equal("Pedro", resultByLastName.Items[0].Name);

            // Search by email
            var filterByEmail = new UserListFilter("ana@", null, null, null, 1, 10, null, null);
            var resultByEmail = await readService.ListAsync(filterByEmail, CancellationToken.None);
            Assert.Single(resultByEmail.Items);
            Assert.Equal("Ana", resultByEmail.Items[0].Name);
        }
    }

    [Fact]
    public async Task ListAsync_Should_Filter_By_Email()
    {
        await using (var context = _fixture.CreateContext())
        {
            context.Set<User>().AddRange(
                CreateTestUser("target@example.com"),
                CreateTestUser("other@example.com"));
            await context.SaveChangesAsync(CancellationToken.None);
        }

        await using (var context = _fixture.CreateContext())
        {
            var readService = new UserReadService(context);
            var filter = new UserListFilter(null, "target@example.com", null, null, 1, 10, null, null);

            var result = await readService.ListAsync(filter, CancellationToken.None);

            Assert.Single(result.Items);
            Assert.Equal("target@example.com", result.Items[0].Email);
        }
    }

    [Fact]
    public async Task ListAsync_Should_Filter_By_Status()
    {
        var activeUser = User.Create(UserId.New(), "Active", "User", "active@example.com", "hash123", null, null, null);
        activeUser.MarkEmailAsVerified();
        activeUser.ChangeStatus(UserStatus.Active);

        var unverifiedUser = CreateTestUser("unverified@example.com");

        await using (var context = _fixture.CreateContext())
        {
            context.Set<User>().AddRange(activeUser, unverifiedUser);
            await context.SaveChangesAsync(CancellationToken.None);
        }

        await using (var context = _fixture.CreateContext())
        {
            var readService = new UserReadService(context);
            var filter = new UserListFilter(null, null, "Active", null, 1, 10, null, null);

            var result = await readService.ListAsync(filter, CancellationToken.None);

            Assert.Single(result.Items);
            Assert.Equal("Active", result.Items[0].Status);
        }
    }

    [Fact]
    public async Task ListAsync_Should_Filter_By_EmailVerified()
    {
        var verifiedUser = User.Create(UserId.New(), "Verified", "User", "verified@example.com", "hash123", null, null, null);
        verifiedUser.MarkEmailAsVerified();

        var unverifiedUser = CreateTestUser("unverified@example.com");

        await using (var context = _fixture.CreateContext())
        {
            context.Set<User>().AddRange(verifiedUser, unverifiedUser);
            await context.SaveChangesAsync(CancellationToken.None);
        }

        await using (var context = _fixture.CreateContext())
        {
            var readService = new UserReadService(context);
            var filter = new UserListFilter(null, null, null, true, 1, 10, null, null);

            var result = await readService.ListAsync(filter, CancellationToken.None);

            Assert.Single(result.Items);
            Assert.True(result.Items[0].IsEmailVerified);
        }
    }

    [Fact]
    public async Task ListAsync_Should_Paginate()
    {
        await using (var context = _fixture.CreateContext())
        {
            for (int i = 1; i <= 5; i++)
            {
                context.Set<User>().Add(
                    CreateTestUser($"user{i}@example.com", name: $"User{i:D2}"));
            }

            await context.SaveChangesAsync(CancellationToken.None);
        }

        await using (var context = _fixture.CreateContext())
        {
            var readService = new UserReadService(context);
            var filter = new UserListFilter(null, null, null, null, 2, 2, null, null);

            var result = await readService.ListAsync(filter, CancellationToken.None);

            Assert.Equal(5, result.TotalItems);
            Assert.Equal(2, result.Items.Count);
            Assert.Equal(2, result.Page);
            Assert.Equal(2, result.PageSize);
        }
    }

    [Fact]
    public async Task ListAsync_Should_Sort_By_Name()
    {
        await using (var context = _fixture.CreateContext())
        {
            context.Set<User>().AddRange(
                CreateTestUser("c@example.com", name: "Carlos"),
                CreateTestUser("a@example.com", name: "Ana"),
                CreateTestUser("b@example.com", name: "Beatriz"));
            await context.SaveChangesAsync(CancellationToken.None);
        }

        await using (var context = _fixture.CreateContext())
        {
            var readService = new UserReadService(context);
            var filter = new UserListFilter(null, null, null, null, 1, 10, "name", "asc");

            var result = await readService.ListAsync(filter, CancellationToken.None);

            Assert.Equal(3, result.Items.Count);
            Assert.Equal("Ana", result.Items[0].Name);
            Assert.Equal("Beatriz", result.Items[1].Name);
            Assert.Equal("Carlos", result.Items[2].Name);
        }
    }

    [Fact]
    public async Task Queries_Should_Use_AsNoTracking()
    {
        var user = CreateTestUser();

        await using (var context = _fixture.CreateContext())
        {
            context.Set<User>().Add(user);
            await context.SaveChangesAsync(CancellationToken.None);
        }

        await using (var context = _fixture.CreateContext())
        {
            var readService = new UserReadService(context);

            await readService.GetByIdAsync(user.Id, CancellationToken.None);
            await readService.ListAsync(
                new UserListFilter(null, null, null, null, 1, 10, null, null),
                CancellationToken.None);

            var trackedEntries = context.ChangeTracker.Entries<User>().ToList();
            Assert.Empty(trackedEntries);
        }
    }

    public void Dispose()
    {
        _fixture.Dispose();
    }

    private static User CreateTestUser(
        string email = "test@example.com",
        string name = "Juan",
        string lastName = "Garcia") =>
        User.Create(UserId.New(), name, lastName, email, "hashed_password_123", null, null, null);
}
