using Lyria.Application.Common;
using Lyria.Application.Features.EstablishmentBranches;
using Lyria.Application.Features.EstablishmentBranches.List;
using Lyria.Application.Features.Establishments;
using Lyria.Application.UnitTests.Fakes;
using Xunit;

namespace Lyria.Application.UnitTests.Features.EstablishmentBranches;

public sealed class ListEstablishmentBranchesTests
{
    private static readonly Guid EstablishmentGuid = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid CategoryGuid = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    private readonly FakeEstablishmentBranchReadService _readService = new();
    private readonly FakeEstablishmentReadService _establishmentReadService = new();
    private readonly ListEstablishmentBranchesQueryHandler _handler;

    public ListEstablishmentBranchesTests()
    {
        _establishmentReadService.Seed(new EstablishmentResponse(
            EstablishmentGuid, CategoryGuid, "Restaurante", "Let It V", "let-it-v",
            null, null, null, null, null, null, false, null, true,
            new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), null));

        _readService.Seed(new EstablishmentBranchResponse(
            Guid.NewGuid(), EstablishmentGuid, "Sede Palermo", "Costa Rica 5865",
            null, null, "Palermo", "Buenos Aires", "Buenos Aires", null, "Argentina",
            "Costa Rica 5865, Palermo, Buenos Aires, Buenos Aires, Argentina",
            null, null, null, null, null, 0m, 0, true,
            new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), null));
        _readService.Seed(new EstablishmentBranchResponse(
            Guid.NewGuid(), EstablishmentGuid, "Sede Recoleta", "Av. Santa Fe 1234",
            null, null, "Recoleta", "Buenos Aires", "Buenos Aires", null, "Argentina",
            "Av. Santa Fe 1234, Recoleta, Buenos Aires, Buenos Aires, Argentina",
            null, null, null, null, null, 0m, 0, false,
            new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc), null));

        _handler = new ListEstablishmentBranchesQueryHandler(_readService, _establishmentReadService);
    }

    [Fact]
    public async Task Handle_EstablishmentNotFound_ReturnsEmpty()
    {
        var query = new ListEstablishmentBranchesQuery(Guid.NewGuid(), null, null);

        PagedResponse<EstablishmentBranchListItemResponse> result =
            await _handler.Handle(query, CancellationToken.None);

        Assert.Equal(0, result.TotalItems);
        Assert.Empty(result.Items);
    }

    [Fact]
    public async Task Handle_ReturnsPagedResponse()
    {
        var query = new ListEstablishmentBranchesQuery(EstablishmentGuid, null, null);

        PagedResponse<EstablishmentBranchListItemResponse> result =
            await _handler.Handle(query, CancellationToken.None);

        Assert.Equal(2, result.TotalItems);
        Assert.Equal(2, result.Items.Count);
    }

    [Fact]
    public async Task Handle_FiltersPassedToService()
    {
        var query = new ListEstablishmentBranchesQuery(EstablishmentGuid, null, true);

        PagedResponse<EstablishmentBranchListItemResponse> result =
            await _handler.Handle(query, CancellationToken.None);

        Assert.Equal(1, result.TotalItems);
    }
}
