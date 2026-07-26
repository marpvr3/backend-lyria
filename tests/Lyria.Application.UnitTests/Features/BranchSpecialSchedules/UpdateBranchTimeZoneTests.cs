using Lyria.Application.Common.Errors;
using Lyria.Application.Common.Results;
using Lyria.Application.Features.BranchSpecialSchedules.UpdateTimeZone;
using Lyria.Application.UnitTests.Fakes;
using Lyria.Domain.Establishments;
using Lyria.Domain.Establishments.Branches;
using Lyria.Domain.Establishments.Categories;
using Xunit;

namespace Lyria.Application.UnitTests.Features.BranchSpecialSchedules;

public sealed class UpdateBranchTimeZoneTests
{
    private readonly FakeEstablishmentBranchRepository _branchRepository = new();
    private readonly FakeTimeZoneService _timeZoneService = new();
    private readonly UpdateBranchTimeZoneCommandHandler _handler;
    private readonly EstablishmentBranch _branch;

    public UpdateBranchTimeZoneTests()
    {
        var establishment = Establishment.Create(
            EstablishmentId.New(), EstablishmentCategoryId.New(),
            "Let It V", "let-it-v", null, null, null, null, null, null);

        _branch = EstablishmentBranch.Create(
            EstablishmentBranchId.New(), establishment.Id,
            "Sede Central", "Costa Rica 5865",
            null, null, null, null, null, null, null,
            null, null, null, null, null, "America/Argentina/Buenos_Aires");
        _branchRepository.Seed(_branch);

        _handler = new UpdateBranchTimeZoneCommandHandler(_branchRepository, _timeZoneService);
    }

    [Fact]
    public async Task Handle_ValidTimeZone_UpdatesSuccessfully()
    {
        var command = new UpdateBranchTimeZoneCommand(
            _branch.Id.Value, "America/Bogota");

        Result result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("America/Bogota", _branch.TimeZoneId);
    }

    [Fact]
    public async Task Handle_BranchNotFound_ReturnsNotFound()
    {
        var command = new UpdateBranchTimeZoneCommand(
            Guid.NewGuid(), "America/Bogota");

        Result result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
    }

    [Fact]
    public async Task Handle_InvalidTimeZone_ReturnsValidation()
    {
        _timeZoneService.SetValid(false);

        var command = new UpdateBranchTimeZoneCommand(
            _branch.Id.Value, "Invalid/Zone");

        Result result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
        Assert.Contains("InvalidTimeZone", result.Error.Code);
    }

    [Fact]
    public async Task Handle_ValidTimeZone_CallsSaveChanges()
    {
        var command = new UpdateBranchTimeZoneCommand(
            _branch.Id.Value, "Europe/Madrid");

        await _handler.Handle(command, CancellationToken.None);

        Assert.Equal(1, _branchRepository.SaveChangesCallCount);
    }
}
