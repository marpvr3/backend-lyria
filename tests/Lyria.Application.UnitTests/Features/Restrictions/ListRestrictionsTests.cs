using Lyria.Application.Features.Restrictions;
using Lyria.Application.Features.Restrictions.List;
using Lyria.Application.UnitTests.Fakes;
using Xunit;

namespace Lyria.Application.UnitTests.Features.Restrictions;

public sealed class ListRestrictionsTests
{
    private readonly FakeRestrictionReadService _readService = new();
    private readonly ListRestrictionsQueryHandler _handler;

    public ListRestrictionsTests()
    {
        _readService.Seed(new RestrictionResponse(Guid.NewGuid(), "Vegano", null, true, DateTime.UtcNow, null));
        _readService.Seed(new RestrictionResponse(Guid.NewGuid(), "Sin TACC", null, false, DateTime.UtcNow, null));

        _handler = new ListRestrictionsQueryHandler(_readService);
    }

    [Fact]
    public async Task Handle_ReturnsPagedResponse()
    {
        var query = new ListRestrictionsQuery(null, null);

        var result = await _handler.Handle(query, TestContext.Current.CancellationToken);

        Assert.Equal(2, result.TotalItems);
        Assert.Equal(2, result.Items.Count);
    }

    [Fact]
    public async Task Handle_FilterByActive_ReturnsFiltered()
    {
        var query = new ListRestrictionsQuery(null, true);

        var result = await _handler.Handle(query, TestContext.Current.CancellationToken);

        Assert.Equal(1, result.TotalItems);
        Assert.True(result.Items[0].IsActive);
    }

    [Fact]
    public async Task Handle_SearchByName_ReturnsFiltered()
    {
        var query = new ListRestrictionsQuery("TACC", null);

        var result = await _handler.Handle(query, TestContext.Current.CancellationToken);

        Assert.Equal(1, result.TotalItems);
        Assert.Equal("Sin TACC", result.Items[0].Name);
    }

    [Fact]
    public async Task Handle_Pagination_Works()
    {
        var query = new ListRestrictionsQuery(null, null, 1, 1);

        var result = await _handler.Handle(query, TestContext.Current.CancellationToken);

        Assert.Single(result.Items);
        Assert.Equal(2, result.TotalPages);
    }
}
