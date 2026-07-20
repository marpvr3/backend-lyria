using Lyria.Application.Features.Restrictions.GetById;
using Lyria.Application.UnitTests.Fakes;
using Lyria.Application.Features.Restrictions;
using Xunit;

namespace Lyria.Application.UnitTests.Features.Restrictions;

public sealed class GetRestrictionByIdTests
{
    private readonly FakeRestrictionReadService _readService = new();
    private readonly GetRestrictionByIdQueryHandler _handler;

    public GetRestrictionByIdTests()
    {
        _handler = new GetRestrictionByIdQueryHandler(_readService);
    }

    [Fact]
    public async Task Handle_ExistingId_ReturnsSuccess()
    {
        var id = Guid.NewGuid();
        _readService.Seed(new RestrictionResponse(id, "Vegano", null, true, DateTime.UtcNow, null));

        var query = new GetRestrictionByIdQuery(id);

        var result = await _handler.Handle(query, TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal(id, result.Value.Id);
    }

    [Fact]
    public async Task Handle_NonExistentId_ReturnsNotFound()
    {
        var query = new GetRestrictionByIdQuery(Guid.NewGuid());

        var result = await _handler.Handle(query, TestContext.Current.CancellationToken);

        Assert.True(result.IsFailure);
        Assert.Equal("Restriction.NotFound", result.Error.Code);
    }
}
