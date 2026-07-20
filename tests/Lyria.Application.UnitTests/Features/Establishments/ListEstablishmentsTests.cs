using Lyria.Application.Common;
using Lyria.Application.Features.Establishments;
using Lyria.Application.Features.Establishments.List;
using Lyria.Application.UnitTests.Fakes;
using Xunit;

namespace Lyria.Application.UnitTests.Features.Establishments;

public sealed class ListEstablishmentsTests
{
    private readonly FakeEstablishmentReadService _readService = new();
    private readonly ListEstablishmentsQueryHandler _handler;

    public ListEstablishmentsTests()
    {
        _readService.Seed(new EstablishmentResponse(
            Guid.NewGuid(), Guid.NewGuid(), "Restaurante", "Let It V", "let-it-v",
            null, null, null, null, null, null, true, null, true,
            new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), null));
        _readService.Seed(new EstablishmentResponse(
            Guid.NewGuid(), Guid.NewGuid(), "Cafetería", "Café Zen", "cafe-zen",
            null, null, null, null, null, null, false, null, false,
            new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc), null));

        _handler = new ListEstablishmentsQueryHandler(_readService);
    }

    [Fact]
    public async Task Handle_ReturnsPagedResponse()
    {
        var query = new ListEstablishmentsQuery(null, null, null, null, 1, 20);

        PagedResponse<EstablishmentListItemResponse> result =
            await _handler.Handle(query, CancellationToken.None);

        Assert.Equal(2, result.TotalItems);
        Assert.Equal(2, result.Items.Count);
    }

    [Fact]
    public async Task Handle_FiltersPassedToService()
    {
        var query = new ListEstablishmentsQuery(null, null, true, null, 1, 20);

        PagedResponse<EstablishmentListItemResponse> result =
            await _handler.Handle(query, CancellationToken.None);

        Assert.Equal(1, result.TotalItems);
    }

    [Fact]
    public async Task Handle_PaginationWorks()
    {
        var query = new ListEstablishmentsQuery(null, null, null, null, 1, 1);

        PagedResponse<EstablishmentListItemResponse> result =
            await _handler.Handle(query, CancellationToken.None);

        Assert.Equal(2, result.TotalItems);
        Assert.Single(result.Items);
        Assert.Equal(2, result.TotalPages);
    }
}
