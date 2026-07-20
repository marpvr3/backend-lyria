using Lyria.Application.Features.EstablishmentBranchRestrictions;
using Lyria.Application.Features.EstablishmentBranchRestrictions.GetByIds;
using Lyria.Application.UnitTests.Fakes;
using Lyria.Domain.Establishments.Branches;
using Xunit;

namespace Lyria.Application.UnitTests.Features.EstablishmentBranchRestrictions;

public sealed class GetBranchRestrictionByIdsTests
{
    private readonly FakeEstablishmentBranchRestrictionReadService _readService = new();
    private readonly GetBranchRestrictionByIdsQueryHandler _handler;

    public GetBranchRestrictionByIdsTests()
    {
        _handler = new GetBranchRestrictionByIdsQueryHandler(_readService);
    }

    [Fact]
    public async Task Handle_ExistingIds_ReturnsResponse()
    {
        var branchId = Guid.NewGuid();
        var restrictionId = Guid.NewGuid();

        _readService.Seed(new EstablishmentBranchRestrictionResponse(
            branchId, restrictionId, "Vegano", "Sin productos animales.",
            RestrictionComplianceLevel.Guaranteed, true, null, true, DateTime.UtcNow, null));

        var query = new GetBranchRestrictionByIdsQuery(branchId, restrictionId);

        var result = await _handler.Handle(query, TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal("Vegano", result.Value.RestrictionName);
    }

    [Fact]
    public async Task Handle_NotFound_ReturnsNotFound()
    {
        var query = new GetBranchRestrictionByIdsQuery(Guid.NewGuid(), Guid.NewGuid());

        var result = await _handler.Handle(query, TestContext.Current.CancellationToken);

        Assert.True(result.IsFailure);
        Assert.Equal("EstablishmentBranchRestriction.NotFound", result.Error.Code);
    }
}
