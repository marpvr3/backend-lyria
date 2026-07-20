using Lyria.Application.Features.EstablishmentBranches;
using Lyria.Application.Features.EstablishmentBranches.GetById;
using Lyria.Application.Common.Errors;
using Lyria.Application.Common.Results;
using Lyria.Application.UnitTests.Fakes;
using Xunit;

namespace Lyria.Application.UnitTests.Features.EstablishmentBranches;

public sealed class GetEstablishmentBranchByIdTests
{
    private readonly FakeEstablishmentBranchReadService _readService = new();
    private readonly GetEstablishmentBranchByIdQueryHandler _handler;

    public GetEstablishmentBranchByIdTests()
    {
        _handler = new GetEstablishmentBranchByIdQueryHandler(_readService);
    }

    [Fact]
    public async Task Handle_ExistingId_ReturnsSuccess()
    {
        var id = Guid.NewGuid();
        _readService.Seed(new EstablishmentBranchResponse(
            id, Guid.NewGuid(), "Sede Central", "Costa Rica 5865", "Piso 2",
            null, "Palermo", "Buenos Aires", "Buenos Aires", "C1414", "Argentina",
            "Costa Rica 5865, Piso 2, Palermo, Buenos Aires, Buenos Aires, C1414, Argentina",
            -34.5795m, -58.4321m, "+54 11 5555-0001", "+54 11 5555-0002", "sede@letitv.com",
            4.5m, 10, true,
            new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), null));

        var query = new GetEstablishmentBranchByIdQuery(id);

        Result<EstablishmentBranchResponse> result = await _handler.Handle(query, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(id, result.Value.Id);
    }

    [Fact]
    public async Task Handle_NonExistentId_ReturnsNotFound()
    {
        var query = new GetEstablishmentBranchByIdQuery(Guid.NewGuid());

        Result<EstablishmentBranchResponse> result = await _handler.Handle(query, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
        Assert.Contains("EstablishmentBranch.NotFound", result.Error.Code);
    }
}
