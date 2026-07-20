using Lyria.Application.Features.Restrictions;
using Lyria.Domain.Restrictions;
using Lyria.Infrastructure.Persistence;
using Lyria.Infrastructure.Persistence.ReadServices;
using Xunit;

namespace Lyria.Infrastructure.IntegrationTests.Persistence;

public sealed class RestrictionReadServiceTests : IDisposable
{
    private readonly SqliteFixture _fixture = new();

    private static async Task<Restriction> SeedRestriction(LyriaDbContext context, string name, string? description = null, bool isActive = true)
    {
        var restriction = Restriction.Create(RestrictionId.New(), name, description);
        if (!isActive)
        {
            restriction.Deactivate();
        }

        context.Set<Restriction>().Add(restriction);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        return restriction;
    }

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsAllFields()
    {
        using var context = _fixture.CreateContext();
        var restriction = await SeedRestriction(context, "Vegano", "Sin productos de origen animal.");

        var readService = new RestrictionReadService(context);

        var response = await readService.GetByIdAsync(restriction.Id, TestContext.Current.CancellationToken);

        Assert.NotNull(response);
        Assert.Equal(restriction.Id.Value, response.Id);
        Assert.Equal("Vegano", response.Name);
        Assert.Equal("Sin productos de origen animal.", response.Description);
        Assert.True(response.IsActive);
    }

    [Fact]
    public async Task GetByIdAsync_InactiveRestriction_StillReturns()
    {
        using var context = _fixture.CreateContext();
        var restriction = await SeedRestriction(context, "Inactiva", isActive: false);

        var readService = new RestrictionReadService(context);

        var response = await readService.GetByIdAsync(restriction.Id, TestContext.Current.CancellationToken);

        Assert.NotNull(response);
        Assert.False(response.IsActive);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistentId_ReturnsNull()
    {
        using var context = _fixture.CreateContext();
        var readService = new RestrictionReadService(context);

        var response = await readService.GetByIdAsync(RestrictionId.New(), TestContext.Current.CancellationToken);

        Assert.Null(response);
    }

    [Fact]
    public async Task ListAsync_ReturnsAll_OrderedByName()
    {
        using var context = _fixture.CreateContext();
        await SeedRestriction(context, "Vegetariano");
        await SeedRestriction(context, "Sin TACC");
        await SeedRestriction(context, "Vegano");

        var readService = new RestrictionReadService(context);
        var filter = new RestrictionListFilter(null, null, 1, 20);

        var result = await readService.ListAsync(filter, TestContext.Current.CancellationToken);

        Assert.True(result.TotalItems >= 3);
        var names = result.Items.Select(r => r.Name).ToList();
        var sorted = names.OrderBy(n => n).ToList();
        Assert.Equal(sorted, names);
    }

    [Fact]
    public async Task ListAsync_FilterByActive_ReturnsOnlyActive()
    {
        using var context = _fixture.CreateContext();
        await SeedRestriction(context, "Activa1" + Guid.NewGuid(), isActive: true);
        await SeedRestriction(context, "Inactiva1" + Guid.NewGuid(), isActive: false);

        var readService = new RestrictionReadService(context);
        var filter = new RestrictionListFilter(null, true, 1, 100);

        var result = await readService.ListAsync(filter, TestContext.Current.CancellationToken);

        Assert.All(result.Items, item => Assert.True(item.IsActive));
    }

    [Fact]
    public async Task ListAsync_SearchByName_FiltersCorrectly()
    {
        using var context = _fixture.CreateContext();
        string unique = Guid.NewGuid().ToString("N")[..8];
        await SeedRestriction(context, $"UniqueSearch{unique}");
        await SeedRestriction(context, "OtherRestriction" + Guid.NewGuid());

        var readService = new RestrictionReadService(context);
        var filter = new RestrictionListFilter($"UniqueSearch{unique}", null, 1, 20);

        var result = await readService.ListAsync(filter, TestContext.Current.CancellationToken);

        Assert.Equal(1, result.TotalItems);
        Assert.Contains(unique, result.Items[0].Name);
    }

    [Fact]
    public async Task ListAsync_Pagination_Works()
    {
        using var context = _fixture.CreateContext();
        string prefix = Guid.NewGuid().ToString("N")[..6];
        await SeedRestriction(context, $"{prefix}A");
        await SeedRestriction(context, $"{prefix}B");
        await SeedRestriction(context, $"{prefix}C");

        var readService = new RestrictionReadService(context);
        var filter = new RestrictionListFilter(prefix, null, 1, 2);

        var result = await readService.ListAsync(filter, TestContext.Current.CancellationToken);

        Assert.Equal(3, result.TotalItems);
        Assert.Equal(2, result.Items.Count);
        Assert.Equal(2, result.TotalPages);
    }

    [Fact]
    public async Task ListAsync_Empty_ReturnsEmptyPage()
    {
        using var context = _fixture.CreateContext();
        var readService = new RestrictionReadService(context);
        string nonExistentSearch = "ZZZNONEXISTENT" + Guid.NewGuid();
        var filter = new RestrictionListFilter(nonExistentSearch, null, 1, 20);

        var result = await readService.ListAsync(filter, TestContext.Current.CancellationToken);

        Assert.Equal(0, result.TotalItems);
        Assert.Empty(result.Items);
    }

    public void Dispose() => _fixture.Dispose();
}
