using Lyria.Application.Features.Establishments;
using Lyria.Application.Features.Establishments.GetById;
using Lyria.Application.Common.Errors;
using Lyria.Application.Common.Results;
using Lyria.Application.UnitTests.Fakes;
using Xunit;

namespace Lyria.Application.UnitTests.Features.Establishments;

public sealed class GetEstablishmentByIdTests
{
    private readonly FakeEstablishmentReadService _readService = new();
    private readonly GetEstablishmentByIdQueryHandler _handler;

    public GetEstablishmentByIdTests()
    {
        _handler = new GetEstablishmentByIdQueryHandler(_readService);
    }

    [Fact]
    public async Task Handle_ExistingId_ReturnsSuccess()
    {
        var id = Guid.NewGuid();
        _readService.Seed(new EstablishmentResponse(
            id, Guid.NewGuid(), "Restaurante", "Let It V", "let-it-v",
            "Desc", "https://x.com", "@x", null, null, null, false, null, true,
            new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), null));

        var query = new GetEstablishmentByIdQuery(id);

        Result<EstablishmentResponse> result = await _handler.Handle(query, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(id, result.Value.Id);
    }

    [Fact]
    public async Task Handle_NonExistentId_ReturnsNotFound()
    {
        var query = new GetEstablishmentByIdQuery(Guid.NewGuid());

        Result<EstablishmentResponse> result = await _handler.Handle(query, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
        Assert.Contains("Establishments.NotFound", result.Error.Code);
    }
}
