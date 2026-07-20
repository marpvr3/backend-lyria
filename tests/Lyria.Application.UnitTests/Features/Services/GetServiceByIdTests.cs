using Lyria.Application.Features.Services;
using Lyria.Application.Features.Services.GetById;
using Lyria.Application.UnitTests.Fakes;
using Xunit;

namespace Lyria.Application.UnitTests.Features.Services;

public sealed class GetServiceByIdTests
{
    private readonly FakeServiceReadService _readService = new();
    private readonly GetServiceByIdQueryHandler _handler;

    public GetServiceByIdTests()
    {
        _handler = new GetServiceByIdQueryHandler(_readService);
    }

    [Fact]
    public async Task Handle_ExistingId_ReturnsSuccess()
    {
        var id = Guid.NewGuid();
        _readService.Seed(new ServiceResponse(id, "Delivery", null, null, true, DateTime.UtcNow, null));

        var query = new GetServiceByIdQuery(id);

        var result = await _handler.Handle(query, TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal(id, result.Value.Id);
    }

    [Fact]
    public async Task Handle_NonExistentId_ReturnsNotFound()
    {
        var query = new GetServiceByIdQuery(Guid.NewGuid());

        var result = await _handler.Handle(query, TestContext.Current.CancellationToken);

        Assert.True(result.IsFailure);
        Assert.Equal("Service.NotFound", result.Error.Code);
    }
}
