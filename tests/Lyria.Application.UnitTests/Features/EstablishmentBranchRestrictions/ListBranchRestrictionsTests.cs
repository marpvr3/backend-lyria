using Lyria.Application.Features.EstablishmentBranchRestrictions;
using Lyria.Application.Features.EstablishmentBranchRestrictions.List;
using Lyria.Application.UnitTests.Fakes;
using Lyria.Domain.Establishments.Branches;
using Xunit;

namespace Lyria.Application.UnitTests.Features.EstablishmentBranchRestrictions;

public sealed class ListBranchRestrictionsTests
{
    private readonly FakeEstablishmentBranchRestrictionReadService _readService = new();
    private readonly ListBranchRestrictionsQueryHandler _handler;
    private readonly Guid _branchId = Guid.NewGuid();

    public ListBranchRestrictionsTests()
    {
        _handler = new ListBranchRestrictionsQueryHandler(_readService);

        _readService.Seed(new EstablishmentBranchRestrictionResponse(
            _branchId, Guid.NewGuid(), "Vegano", "Sin productos animales.",
            RestrictionComplianceLevel.Guaranteed, true, null, true, DateTime.UtcNow, null));

        _readService.Seed(new EstablishmentBranchRestrictionResponse(
            _branchId, Guid.NewGuid(), "Sin TACC", "Sin trigo, avena, cebada, centeno.",
            RestrictionComplianceLevel.Partial, false, "Cocina compartida", true, DateTime.UtcNow, null));

        _readService.Seed(new EstablishmentBranchRestrictionResponse(
            _branchId, Guid.NewGuid(), "Sin lactosa", null,
            RestrictionComplianceLevel.OnRequest, false, null, false, DateTime.UtcNow, null));
    }

    [Fact]
    public async Task Handle_ReturnsPagedItems()
    {
        var query = new ListBranchRestrictionsQuery(_branchId, null, null, null, null, 1, 20);

        var result = await _handler.Handle(query, TestContext.Current.CancellationToken);

        Assert.Equal(3, result.TotalItems);
        Assert.Equal(3, result.Items.Count);
    }

    [Fact]
    public async Task Handle_FilterByActive_ReturnsOnlyActive()
    {
        var query = new ListBranchRestrictionsQuery(_branchId, null, true, null, null, 1, 20);

        var result = await _handler.Handle(query, TestContext.Current.CancellationToken);

        Assert.All(result.Items, item => Assert.True(item.IsActive));
    }

    [Fact]
    public async Task Handle_FilterByComplianceLevel_ReturnsCorrect()
    {
        var query = new ListBranchRestrictionsQuery(
            _branchId, null, null, RestrictionComplianceLevel.Guaranteed, null, 1, 20);

        var result = await _handler.Handle(query, TestContext.Current.CancellationToken);

        Assert.Single(result.Items);
        Assert.Equal(RestrictionComplianceLevel.Guaranteed, result.Items[0].ComplianceLevel);
    }

    [Fact]
    public async Task Handle_FilterByCertified_ReturnsOnlyCertified()
    {
        var query = new ListBranchRestrictionsQuery(_branchId, null, null, null, true, 1, 20);

        var result = await _handler.Handle(query, TestContext.Current.CancellationToken);

        Assert.Single(result.Items);
        Assert.True(result.Items[0].IsCertified);
    }

    [Fact]
    public async Task Handle_SearchByName_FiltersCorrectly()
    {
        var query = new ListBranchRestrictionsQuery(_branchId, "Vegano", null, null, null, 1, 20);

        var result = await _handler.Handle(query, TestContext.Current.CancellationToken);

        Assert.Single(result.Items);
        Assert.Equal("Vegano", result.Items[0].RestrictionName);
    }

    [Fact]
    public async Task Handle_Pagination_Works()
    {
        var query = new ListBranchRestrictionsQuery(_branchId, null, null, null, null, 1, 2);

        var result = await _handler.Handle(query, TestContext.Current.CancellationToken);

        Assert.Equal(3, result.TotalItems);
        Assert.Equal(2, result.Items.Count);
    }
}
