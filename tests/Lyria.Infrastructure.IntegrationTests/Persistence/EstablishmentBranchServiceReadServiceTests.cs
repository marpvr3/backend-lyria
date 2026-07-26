using Lyria.Application.Features.EstablishmentBranchServices;
using Lyria.Domain.Establishments;
using Lyria.Domain.Establishments.Branches;
using Lyria.Domain.Establishments.Categories;
using Lyria.Domain.Services;
using Lyria.Infrastructure.Persistence;
using Lyria.Infrastructure.Persistence.ReadServices;
using Xunit;

namespace Lyria.Infrastructure.IntegrationTests.Persistence;

public sealed class EstablishmentBranchServiceReadServiceTests : IDisposable
{
    private readonly SqliteFixture _fixture = new();

    private static async Task<(EstablishmentBranch Branch, Service Service1, Service Service2)> SeedData(
        LyriaDbContext context)
    {
        var category = EstablishmentCategory.Create(
            EstablishmentCategoryId.New(), "Restaurante", null, null, 1);
        context.Set<EstablishmentCategory>().Add(category);

        var establishment = Establishment.Create(
            EstablishmentId.New(), category.Id, "Let It V", "let-it-v",
            null, null, null, null, null, null);
        context.Set<Establishment>().Add(establishment);

        var branch = EstablishmentBranch.Create(
            EstablishmentBranchId.New(), establishment.Id,
            "Sede Central", "Costa Rica 5865",
            null, null, null, null, null, null, null,
            null, null, null, null, null, "America/Argentina/Buenos_Aires");
        context.Set<EstablishmentBranch>().Add(branch);

        var service1 = Service.Create(ServiceId.New(), "Delivery", "Entrega a domicilio.", "https://cdn.lyria.com/icons/delivery.svg");
        var service2 = Service.Create(ServiceId.New(), "Wi-Fi", "Conexión inalámbrica.", null);
        context.Set<Service>().Add(service1);
        context.Set<Service>().Add(service2);

        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        return (branch, service1, service2);
    }

    [Fact]
    public async Task GetByIdsAsync_Existing_ReturnsWithServiceData()
    {
        using var context = _fixture.CreateContext();
        var (branch, service1, _) = await SeedData(context);

        var branchService = EstablishmentBranchService.Create(
            branch.Id, service1.Id, true, "Observación");
        context.Set<EstablishmentBranchService>().Add(branchService);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var readService = new EstablishmentBranchServiceReadService(context);
        var response = await readService.GetByIdsAsync(
            branch.Id, service1.Id, TestContext.Current.CancellationToken);

        Assert.NotNull(response);
        Assert.Equal(branch.Id.Value, response.BranchId);
        Assert.Equal(service1.Id.Value, response.ServiceId);
        Assert.Equal("Delivery", response.ServiceName);
        Assert.Equal("Entrega a domicilio.", response.ServiceDescription);
        Assert.Equal("https://cdn.lyria.com/icons/delivery.svg", response.ServiceIconUrl);
        Assert.True(response.IsAvailable);
        Assert.Equal("Observación", response.Observation);
        Assert.True(response.IsActive);
    }

    [Fact]
    public async Task GetByIdsAsync_NonExistent_ReturnsNull()
    {
        using var context = _fixture.CreateContext();
        var readService = new EstablishmentBranchServiceReadService(context);

        var response = await readService.GetByIdsAsync(
            EstablishmentBranchId.New(), ServiceId.New(),
            TestContext.Current.CancellationToken);

        Assert.Null(response);
    }

    [Fact]
    public async Task ListByBranchAsync_ReturnsPaginatedItems()
    {
        using var context = _fixture.CreateContext();
        var (branch, service1, service2) = await SeedData(context);

        context.Set<EstablishmentBranchService>().Add(
            EstablishmentBranchService.Create(branch.Id, service1.Id, true, null));
        context.Set<EstablishmentBranchService>().Add(
            EstablishmentBranchService.Create(branch.Id, service2.Id, false, "En mantenimiento"));
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var readService = new EstablishmentBranchServiceReadService(context);
        var filter = new EstablishmentBranchServiceListFilter(
            branch.Id, null, null, null, 1, 20);

        var result = await readService.ListByBranchAsync(filter, TestContext.Current.CancellationToken);

        Assert.Equal(2, result.TotalItems);
        Assert.Equal(2, result.Items.Count);
    }

    [Fact]
    public async Task ListByBranchAsync_FilterByActive_Works()
    {
        using var context = _fixture.CreateContext();
        var (branch, service1, service2) = await SeedData(context);

        var bs1 = EstablishmentBranchService.Create(branch.Id, service1.Id, true, null);
        var bs2 = EstablishmentBranchService.Create(branch.Id, service2.Id, true, null);
        bs2.Deactivate();
        context.Set<EstablishmentBranchService>().Add(bs1);
        context.Set<EstablishmentBranchService>().Add(bs2);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var readService = new EstablishmentBranchServiceReadService(context);
        var filter = new EstablishmentBranchServiceListFilter(
            branch.Id, null, true, null, 1, 20);

        var result = await readService.ListByBranchAsync(filter, TestContext.Current.CancellationToken);

        Assert.All(result.Items, item => Assert.True(item.IsActive));
    }

    [Fact]
    public async Task ListByBranchAsync_FilterByAvailable_Works()
    {
        using var context = _fixture.CreateContext();
        var (branch, service1, service2) = await SeedData(context);

        context.Set<EstablishmentBranchService>().Add(
            EstablishmentBranchService.Create(branch.Id, service1.Id, true, null));
        context.Set<EstablishmentBranchService>().Add(
            EstablishmentBranchService.Create(branch.Id, service2.Id, false, null));
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var readService = new EstablishmentBranchServiceReadService(context);
        var filter = new EstablishmentBranchServiceListFilter(
            branch.Id, null, null, true, 1, 20);

        var result = await readService.ListByBranchAsync(filter, TestContext.Current.CancellationToken);

        Assert.All(result.Items, item => Assert.True(item.IsAvailable));
    }

    [Fact]
    public async Task ListByBranchAsync_SearchByServiceName_Works()
    {
        using var context = _fixture.CreateContext();
        var (branch, service1, service2) = await SeedData(context);

        context.Set<EstablishmentBranchService>().Add(
            EstablishmentBranchService.Create(branch.Id, service1.Id, true, null));
        context.Set<EstablishmentBranchService>().Add(
            EstablishmentBranchService.Create(branch.Id, service2.Id, true, null));
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var readService = new EstablishmentBranchServiceReadService(context);
        var filter = new EstablishmentBranchServiceListFilter(
            branch.Id, "Delivery", null, null, 1, 20);

        var result = await readService.ListByBranchAsync(filter, TestContext.Current.CancellationToken);

        Assert.Single(result.Items);
        Assert.Equal("Delivery", result.Items[0].ServiceName);
    }

    [Fact]
    public async Task ListByBranchAsync_Pagination_Works()
    {
        using var context = _fixture.CreateContext();
        var (branch, service1, service2) = await SeedData(context);

        context.Set<EstablishmentBranchService>().Add(
            EstablishmentBranchService.Create(branch.Id, service1.Id, true, null));
        context.Set<EstablishmentBranchService>().Add(
            EstablishmentBranchService.Create(branch.Id, service2.Id, true, null));
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var readService = new EstablishmentBranchServiceReadService(context);
        var filter = new EstablishmentBranchServiceListFilter(
            branch.Id, null, null, null, 1, 1);

        var result = await readService.ListByBranchAsync(filter, TestContext.Current.CancellationToken);

        Assert.Equal(2, result.TotalItems);
        Assert.Single(result.Items);
        Assert.Equal(2, result.TotalPages);
    }

    [Fact]
    public async Task ListByBranchAsync_IncludesServiceIconUrl()
    {
        using var context = _fixture.CreateContext();
        var (branch, service1, _) = await SeedData(context);

        context.Set<EstablishmentBranchService>().Add(
            EstablishmentBranchService.Create(branch.Id, service1.Id, true, null));
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var readService = new EstablishmentBranchServiceReadService(context);
        var filter = new EstablishmentBranchServiceListFilter(
            branch.Id, null, null, null, 1, 20);

        var result = await readService.ListByBranchAsync(filter, TestContext.Current.CancellationToken);

        Assert.Equal("https://cdn.lyria.com/icons/delivery.svg", result.Items[0].ServiceIconUrl);
    }

    public void Dispose() => _fixture.Dispose();
}
