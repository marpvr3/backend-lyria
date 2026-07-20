using Lyria.Application.Features.EstablishmentBranchServices;
using Lyria.Application.Features.EstablishmentBranchServices.GetByIds;
using Lyria.Application.UnitTests.Fakes;
using Xunit;

namespace Lyria.Application.UnitTests.Features.EstablishmentBranchServices;

public sealed class GetBranchServiceByIdsTests
{
    private readonly FakeEstablishmentBranchServiceReadService _readService = new();
    private readonly GetBranchServiceByIdsQueryHandler _handler;

    public GetBranchServiceByIdsTests()
    {
        _handler = new GetBranchServiceByIdsQueryHandler(_readService);
    }

    [Fact]
    public async Task Handle_ExistingIds_ReturnsResponse()
    {
        var branchId = Guid.NewGuid();
        var serviceId = Guid.NewGuid();

        _readService.Seed(new EstablishmentBranchServiceResponse(
            branchId, serviceId, "Delivery", "Entrega a domicilio.", null,
            true, null, true, DateTime.UtcNow, null));

        var query = new GetBranchServiceByIdsQuery(branchId, serviceId);

        var result = await _handler.Handle(query, TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal("Delivery", result.Value.ServiceName);
    }

    [Fact]
    public async Task Handle_NotFound_ReturnsNotFound()
    {
        var query = new GetBranchServiceByIdsQuery(Guid.NewGuid(), Guid.NewGuid());

        var result = await _handler.Handle(query, TestContext.Current.CancellationToken);

        Assert.True(result.IsFailure);
        Assert.Equal("EstablishmentBranchService.NotFound", result.Error.Code);
    }
}
