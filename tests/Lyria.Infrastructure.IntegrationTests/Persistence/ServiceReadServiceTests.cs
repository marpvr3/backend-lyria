using Lyria.Application.Features.Services;
using Lyria.Domain.Services;
using Lyria.Infrastructure.Persistence;
using Lyria.Infrastructure.Persistence.ReadServices;
using Xunit;

namespace Lyria.Infrastructure.IntegrationTests.Persistence;

public sealed class ServiceReadServiceTests : IDisposable
{
    private readonly SqliteFixture _fixture = new();

    private static async Task<Service> SeedService(LyriaDbContext context, string name, string? description = null, string? iconUrl = null, bool isActive = true)
    {
        var service = Service.Create(ServiceId.New(), name, description, iconUrl);
        if (!isActive)
        {
            service.Deactivate();
        }

        context.Set<Service>().Add(service);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        return service;
    }

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsAllFields()
    {
        using var context = _fixture.CreateContext();
        var service = await SeedService(context, "Delivery", "Entrega a domicilio.", "https://cdn.lyria.com/icons/delivery.svg");

        var readService = new ServiceReadService(context);

        var response = await readService.GetByIdAsync(service.Id, TestContext.Current.CancellationToken);

        Assert.NotNull(response);
        Assert.Equal(service.Id.Value, response.Id);
        Assert.Equal("Delivery", response.Name);
        Assert.Equal("Entrega a domicilio.", response.Description);
        Assert.Equal("https://cdn.lyria.com/icons/delivery.svg", response.IconUrl);
        Assert.True(response.IsActive);
    }

    [Fact]
    public async Task GetByIdAsync_InactiveService_StillReturns()
    {
        using var context = _fixture.CreateContext();
        var service = await SeedService(context, "Inactivo", isActive: false);

        var readService = new ServiceReadService(context);

        var response = await readService.GetByIdAsync(service.Id, TestContext.Current.CancellationToken);

        Assert.NotNull(response);
        Assert.False(response.IsActive);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistentId_ReturnsNull()
    {
        using var context = _fixture.CreateContext();
        var readService = new ServiceReadService(context);

        var response = await readService.GetByIdAsync(ServiceId.New(), TestContext.Current.CancellationToken);

        Assert.Null(response);
    }

    [Fact]
    public async Task ListAsync_ReturnsAll_OrderedByName()
    {
        using var context = _fixture.CreateContext();
        await SeedService(context, "Wi-Fi");
        await SeedService(context, "Delivery");
        await SeedService(context, "Takeaway");

        var readService = new ServiceReadService(context);
        var filter = new ServiceListFilter(null, null, 1, 20);

        var result = await readService.ListAsync(filter, TestContext.Current.CancellationToken);

        Assert.True(result.TotalItems >= 3);
        var names = result.Items.Select(s => s.Name).ToList();
        var sorted = names.OrderBy(n => n).ToList();
        Assert.Equal(sorted, names);
    }

    [Fact]
    public async Task ListAsync_FilterByActive_ReturnsOnlyActive()
    {
        using var context = _fixture.CreateContext();
        await SeedService(context, "Activo1" + Guid.NewGuid(), isActive: true);
        await SeedService(context, "Inactivo1" + Guid.NewGuid(), isActive: false);

        var readService = new ServiceReadService(context);
        var filter = new ServiceListFilter(null, true, 1, 100);

        var result = await readService.ListAsync(filter, TestContext.Current.CancellationToken);

        Assert.All(result.Items, item => Assert.True(item.IsActive));
    }

    [Fact]
    public async Task ListAsync_SearchByName_FiltersCorrectly()
    {
        using var context = _fixture.CreateContext();
        string unique = Guid.NewGuid().ToString("N")[..8];
        await SeedService(context, $"UniqueSearch{unique}");
        await SeedService(context, "OtherService" + Guid.NewGuid());

        var readService = new ServiceReadService(context);
        var filter = new ServiceListFilter($"UniqueSearch{unique}", null, 1, 20);

        var result = await readService.ListAsync(filter, TestContext.Current.CancellationToken);

        Assert.Equal(1, result.TotalItems);
        Assert.Contains(unique, result.Items[0].Name);
    }

    [Fact]
    public async Task ListAsync_Pagination_Works()
    {
        using var context = _fixture.CreateContext();
        string prefix = Guid.NewGuid().ToString("N")[..6];
        await SeedService(context, $"{prefix}A");
        await SeedService(context, $"{prefix}B");
        await SeedService(context, $"{prefix}C");

        var readService = new ServiceReadService(context);
        var filter = new ServiceListFilter(prefix, null, 1, 2);

        var result = await readService.ListAsync(filter, TestContext.Current.CancellationToken);

        Assert.Equal(3, result.TotalItems);
        Assert.Equal(2, result.Items.Count);
        Assert.Equal(2, result.TotalPages);
    }

    [Fact]
    public async Task ListAsync_Empty_ReturnsEmptyPage()
    {
        using var context = _fixture.CreateContext();
        var readService = new ServiceReadService(context);
        string nonExistentSearch = "ZZZNONEXISTENT" + Guid.NewGuid();
        var filter = new ServiceListFilter(nonExistentSearch, null, 1, 20);

        var result = await readService.ListAsync(filter, TestContext.Current.CancellationToken);

        Assert.Equal(0, result.TotalItems);
        Assert.Empty(result.Items);
    }

    public void Dispose() => _fixture.Dispose();
}
