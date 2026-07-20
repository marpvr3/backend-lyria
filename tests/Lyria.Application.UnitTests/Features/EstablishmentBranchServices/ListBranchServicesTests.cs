using Lyria.Application.Features.EstablishmentBranchServices;
using Lyria.Application.Features.EstablishmentBranchServices.List;
using Lyria.Application.UnitTests.Fakes;
using Xunit;

namespace Lyria.Application.UnitTests.Features.EstablishmentBranchServices;

public sealed class ListBranchServicesTests
{
    private readonly FakeEstablishmentBranchServiceReadService _readService = new();
    private readonly ListBranchServicesQueryHandler _handler;
    private readonly Guid _branchId = Guid.NewGuid();

    public ListBranchServicesTests()
    {
        _handler = new ListBranchServicesQueryHandler(_readService);

        _readService.Seed(new EstablishmentBranchServiceResponse(
            _branchId, Guid.NewGuid(), "Delivery", "Entrega.", null,
            true, null, true, DateTime.UtcNow, null));

        _readService.Seed(new EstablishmentBranchServiceResponse(
            _branchId, Guid.NewGuid(), "Wi-Fi", null, null,
            false, "En mantenimiento", true, DateTime.UtcNow, null));

        _readService.Seed(new EstablishmentBranchServiceResponse(
            _branchId, Guid.NewGuid(), "Estacionamiento", null, null,
            true, null, false, DateTime.UtcNow, null));
    }

    [Fact]
    public async Task Handle_ReturnsPagedItems()
    {
        var query = new ListBranchServicesQuery(_branchId, null, null, null, 1, 20);

        var result = await _handler.Handle(query, TestContext.Current.CancellationToken);

        Assert.Equal(3, result.TotalItems);
        Assert.Equal(3, result.Items.Count);
    }

    [Fact]
    public async Task Handle_FilterByActive_ReturnsOnlyActive()
    {
        var query = new ListBranchServicesQuery(_branchId, null, true, null, 1, 20);

        var result = await _handler.Handle(query, TestContext.Current.CancellationToken);

        Assert.All(result.Items, item => Assert.True(item.IsActive));
    }

    [Fact]
    public async Task Handle_FilterByAvailable_ReturnsOnlyAvailable()
    {
        var query = new ListBranchServicesQuery(_branchId, null, null, true, 1, 20);

        var result = await _handler.Handle(query, TestContext.Current.CancellationToken);

        Assert.All(result.Items, item => Assert.True(item.IsAvailable));
    }

    [Fact]
    public async Task Handle_SearchByName_FiltersCorrectly()
    {
        var query = new ListBranchServicesQuery(_branchId, "Delivery", null, null, 1, 20);

        var result = await _handler.Handle(query, TestContext.Current.CancellationToken);

        Assert.Single(result.Items);
        Assert.Equal("Delivery", result.Items[0].ServiceName);
    }

    [Fact]
    public async Task Handle_Pagination_Works()
    {
        var query = new ListBranchServicesQuery(_branchId, null, null, null, 1, 2);

        var result = await _handler.Handle(query, TestContext.Current.CancellationToken);

        Assert.Equal(3, result.TotalItems);
        Assert.Equal(2, result.Items.Count);
    }
}
