using Lyria.Application.Features.Roles;
using Lyria.Domain.Roles;
using Lyria.Infrastructure.Persistence.ReadServices;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Lyria.Infrastructure.IntegrationTests.Persistence.Roles;

public sealed class RoleReadServiceTests : IDisposable
{
    private readonly SqliteFixture _fixture = new();

    [Fact]
    public async Task GetByIdAsync_Should_Return_RoleResponse()
    {
        var role = CreateTestRole();

        await using (var context = _fixture.CreateContext())
        {
            context.Set<Role>().Add(role);
            await context.SaveChangesAsync(CancellationToken.None);
        }

        await using (var context = _fixture.CreateContext())
        {
            var readService = new RoleReadService(context);
            var result = await readService.GetByIdAsync(role.Id, CancellationToken.None);

            Assert.NotNull(result);
            Assert.Equal(role.Id.Value, result.Id);
            Assert.Equal("Test Role", result.Name);
            Assert.Null(result.Description);
            Assert.True(result.IsActive);
        }
    }

    [Fact]
    public async Task GetByIdAsync_Should_Return_Null_WhenNotFound()
    {
        await using var context = _fixture.CreateContext();
        var readService = new RoleReadService(context);

        var result = await readService.GetByIdAsync(RoleId.New(), CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task ListAsync_Should_Return_PagedResponse()
    {
        await using (var context = _fixture.CreateContext())
        {
            context.Set<Role>().AddRange(
                CreateTestRole("Role A"),
                CreateTestRole("Role B"),
                CreateTestRole("Role C"));
            await context.SaveChangesAsync(CancellationToken.None);
        }

        await using (var context = _fixture.CreateContext())
        {
            var readService = new RoleReadService(context);
            var filter = new RoleListFilter(null, null, 1, 10, null, null);

            var result = await readService.ListAsync(filter, CancellationToken.None);

            Assert.Equal(3, result.TotalItems);
            Assert.Equal(3, result.Items.Count);
            Assert.Equal(1, result.Page);
            Assert.Equal(10, result.PageSize);
        }
    }

    [Fact]
    public async Task ListAsync_Should_Filter_By_IsActive()
    {
        var activeRole = CreateTestRole("Active Role");
        var inactiveRole = CreateTestRole("Inactive Role");
        inactiveRole.Deactivate();

        await using (var context = _fixture.CreateContext())
        {
            context.Set<Role>().AddRange(activeRole, inactiveRole);
            await context.SaveChangesAsync(CancellationToken.None);
        }

        await using (var context = _fixture.CreateContext())
        {
            var readService = new RoleReadService(context);
            var filter = new RoleListFilter(null, true, 1, 10, null, null);

            var result = await readService.ListAsync(filter, CancellationToken.None);

            Assert.Single(result.Items);
            Assert.True(result.Items[0].IsActive);
        }
    }

    [Fact]
    public async Task ListAsync_Should_Paginate()
    {
        await using (var context = _fixture.CreateContext())
        {
            for (int i = 1; i <= 5; i++)
            {
                context.Set<Role>().Add(
                    CreateTestRole($"Role {i:D2}"));
            }

            await context.SaveChangesAsync(CancellationToken.None);
        }

        await using (var context = _fixture.CreateContext())
        {
            var readService = new RoleReadService(context);
            var filter = new RoleListFilter(null, null, 2, 2, null, null);

            var result = await readService.ListAsync(filter, CancellationToken.None);

            Assert.Equal(5, result.TotalItems);
            Assert.Equal(2, result.Items.Count);
            Assert.Equal(2, result.Page);
            Assert.Equal(2, result.PageSize);
        }
    }

    public void Dispose()
    {
        _fixture.Dispose();
    }

    private static Role CreateTestRole(string name = "Test Role") =>
        Role.Create(RoleId.New(), name, null);
}
