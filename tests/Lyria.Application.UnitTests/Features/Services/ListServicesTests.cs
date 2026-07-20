using Lyria.Application.Features.Services;
using Lyria.Application.Features.Services.List;
using Lyria.Application.UnitTests.Fakes;
using Xunit;

namespace Lyria.Application.UnitTests.Features.Services;

public sealed class ListServicesTests
{
    private readonly FakeServiceReadService _readService = new();
    private readonly ListServicesQueryHandler _handler;

    public ListServicesTests()
    {
        _readService.Seed(new ServiceResponse(Guid.NewGuid(), "Delivery", null, null, true, DateTime.UtcNow, null));
        _readService.Seed(new ServiceResponse(Guid.NewGuid(), "Wi-Fi", null, null, false, DateTime.UtcNow, null));

        _handler = new ListServicesQueryHandler(_readService);
    }

    [Fact]
    public async Task Handle_ReturnsPagedResponse()
    {
        var query = new ListServicesQuery(null, null);

        var result = await _handler.Handle(query, TestContext.Current.CancellationToken);

        Assert.Equal(2, result.TotalItems);
        Assert.Equal(2, result.Items.Count);
    }

    [Fact]
    public async Task Handle_FilterByActive_ReturnsFiltered()
    {
        var query = new ListServicesQuery(null, true);

        var result = await _handler.Handle(query, TestContext.Current.CancellationToken);

        Assert.Equal(1, result.TotalItems);
        Assert.True(result.Items[0].IsActive);
    }

    [Fact]
    public async Task Handle_SearchByName_ReturnsFiltered()
    {
        var query = new ListServicesQuery("Wi-Fi", null);

        var result = await _handler.Handle(query, TestContext.Current.CancellationToken);

        Assert.Equal(1, result.TotalItems);
        Assert.Equal("Wi-Fi", result.Items[0].Name);
    }

    [Fact]
    public async Task Handle_Pagination_Works()
    {
        var query = new ListServicesQuery(null, null, 1, 1);

        var result = await _handler.Handle(query, TestContext.Current.CancellationToken);

        Assert.Single(result.Items);
        Assert.Equal(2, result.TotalPages);
    }
}
