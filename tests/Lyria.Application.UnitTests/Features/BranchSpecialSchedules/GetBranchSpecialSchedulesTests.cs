using Lyria.Application.Common.Errors;
using Lyria.Application.Common.Results;
using Lyria.Application.Features.BranchSpecialSchedules;
using Lyria.Application.Features.BranchSpecialSchedules.GetByRange;
using Lyria.Application.UnitTests.Fakes;
using Xunit;

namespace Lyria.Application.UnitTests.Features.BranchSpecialSchedules;

public sealed class GetBranchSpecialSchedulesTests
{
    private readonly FakeBranchSpecialScheduleReadService _readService = new();
    private readonly GetBranchSpecialSchedulesQueryHandler _handler;

    public GetBranchSpecialSchedulesTests()
    {
        _handler = new GetBranchSpecialSchedulesQueryHandler(_readService);
    }

    [Fact]
    public async Task Handle_BranchExists_ReturnsSchedules()
    {
        var branchId = Guid.NewGuid();
        _readService.SeedBranchExists(branchId);

        var response = new BranchSpecialSchedulesResponse(
            branchId, "2026-12-24", "2026-12-25",
            [
                new BranchSpecialScheduleDateResponse("2026-12-25", true, "Navidad", [])
            ]);
        _readService.SeedResponse(response);

        var query = new GetBranchSpecialSchedulesQuery(
            branchId, new DateOnly(2026, 12, 24), new DateOnly(2026, 12, 25));

        Result<BranchSpecialSchedulesResponse> result =
            await _handler.Handle(query, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(branchId, result.Value.BranchId);
        Assert.Single(result.Value.Schedules);
    }

    [Fact]
    public async Task Handle_BranchNotFound_ReturnsNotFound()
    {
        var query = new GetBranchSpecialSchedulesQuery(
            Guid.NewGuid(), new DateOnly(2026, 12, 24), new DateOnly(2026, 12, 25));

        Result<BranchSpecialSchedulesResponse> result =
            await _handler.Handle(query, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
    }

    [Fact]
    public async Task Handle_EmptyRange_ReturnsEmptySchedules()
    {
        var branchId = Guid.NewGuid();
        _readService.SeedBranchExists(branchId);
        _readService.SeedResponse(new BranchSpecialSchedulesResponse(
            branchId, "2026-01-01", "2026-01-07", []));

        var query = new GetBranchSpecialSchedulesQuery(
            branchId, new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 7));

        Result<BranchSpecialSchedulesResponse> result =
            await _handler.Handle(query, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value.Schedules);
    }
}
