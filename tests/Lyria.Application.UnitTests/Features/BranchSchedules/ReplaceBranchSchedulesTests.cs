using Lyria.Application.Features.BranchSchedules;
using Lyria.Application.Features.BranchSchedules.Replace;
using Lyria.Application.UnitTests.Fakes;
using Lyria.Domain.Establishments;
using Lyria.Domain.Establishments.Branches;
using Lyria.Domain.Establishments.Categories;
using Xunit;

namespace Lyria.Application.UnitTests.Features.BranchSchedules;

public sealed class ReplaceBranchSchedulesTests
{
    private readonly FakeBranchScheduleRepository _scheduleRepository = new();
    private readonly FakeEstablishmentBranchRepository _branchRepository = new();
    private readonly ReplaceBranchSchedulesCommandHandler _handler;

    public ReplaceBranchSchedulesTests()
    {
        _handler = new ReplaceBranchSchedulesCommandHandler(
            _scheduleRepository, _branchRepository);
    }

    private void SeedActiveBranch(Guid branchId)
    {
        var branch = EstablishmentBranch.Create(
            new EstablishmentBranchId(branchId),
            EstablishmentId.New(),
            "Sede Test", "Calle Test 123",
            null, null, null, null, null, null, null,
            null, null, null, null, null, "America/Argentina/Buenos_Aires");
        _branchRepository.Seed(branch);
    }

    [Fact]
    public async Task Handle_ValidSchedule_ReturnsSuccess()
    {
        var branchId = Guid.NewGuid();
        SeedActiveBranch(branchId);

        var command = new ReplaceBranchSchedulesCommand(branchId,
        [
            new BranchScheduleItem(1, "09:00", "17:00", false, false),
            new BranchScheduleItem(7, null, null, false, true)
        ]);

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, _scheduleRepository.ReplaceCallCount);
    }

    [Fact]
    public async Task Handle_MultipleSlotsPerDay_ReturnsSuccess()
    {
        var branchId = Guid.NewGuid();
        SeedActiveBranch(branchId);

        var command = new ReplaceBranchSchedulesCommand(branchId,
        [
            new BranchScheduleItem(1, "12:00", "15:00", false, false),
            new BranchScheduleItem(1, "19:00", "23:30", false, false)
        ]);

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_BranchNotFound_ReturnsNotFound()
    {
        var command = new ReplaceBranchSchedulesCommand(Guid.NewGuid(),
        [
            new BranchScheduleItem(1, "09:00", "17:00", false, false)
        ]);

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("EstablishmentBranch.NotFound", result.Error.Code);
    }

    [Fact]
    public async Task Handle_OverlappingSlots_ReturnsFailure()
    {
        var branchId = Guid.NewGuid();
        SeedActiveBranch(branchId);

        var command = new ReplaceBranchSchedulesCommand(branchId,
        [
            new BranchScheduleItem(1, "12:00", "16:00", false, false),
            new BranchScheduleItem(1, "15:00", "20:00", false, false)
        ]);

        Assert.Throws<EstablishmentBranchException>(
            () => _handler.Handle(command, CancellationToken.None).AsTask().GetAwaiter().GetResult());
    }

    [Fact]
    public async Task Handle_ClosedDayWithOpenSlot_Throws()
    {
        var branchId = Guid.NewGuid();
        SeedActiveBranch(branchId);

        var command = new ReplaceBranchSchedulesCommand(branchId,
        [
            new BranchScheduleItem(7, null, null, false, true),
            new BranchScheduleItem(7, "10:00", "14:00", false, false)
        ]);

        Assert.Throws<EstablishmentBranchException>(
            () => _handler.Handle(command, CancellationToken.None).AsTask().GetAwaiter().GetResult());
    }

    [Fact]
    public async Task Handle_MidnightCrossing_ReturnsSuccess()
    {
        var branchId = Guid.NewGuid();
        SeedActiveBranch(branchId);

        var command = new ReplaceBranchSchedulesCommand(branchId,
        [
            new BranchScheduleItem(5, "22:00", "02:00", true, false)
        ]);

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_ReplacesExistingSchedules()
    {
        var branchId = Guid.NewGuid();
        SeedActiveBranch(branchId);

        // First replacement
        var command1 = new ReplaceBranchSchedulesCommand(branchId,
        [
            new BranchScheduleItem(1, "09:00", "17:00", false, false)
        ]);
        await _handler.Handle(command1, CancellationToken.None);

        // Second replacement
        var command2 = new ReplaceBranchSchedulesCommand(branchId,
        [
            new BranchScheduleItem(1, "10:00", "18:00", false, false)
        ]);
        await _handler.Handle(command2, CancellationToken.None);

        Assert.Equal(2, _scheduleRepository.ReplaceCallCount);
    }

    [Fact]
    public async Task Handle_FailureDoesNotPartiallyUpdate()
    {
        var branchId = Guid.NewGuid();
        SeedActiveBranch(branchId);

        var command = new ReplaceBranchSchedulesCommand(branchId,
        [
            new BranchScheduleItem(1, "12:00", "16:00", false, false),
            new BranchScheduleItem(1, "15:00", "20:00", false, false)
        ]);

        try
        {
            await _handler.Handle(command, CancellationToken.None);
        }
        catch (EstablishmentBranchException)
        {
            // Expected
        }

        Assert.Equal(0, _scheduleRepository.ReplaceCallCount);
    }
}
