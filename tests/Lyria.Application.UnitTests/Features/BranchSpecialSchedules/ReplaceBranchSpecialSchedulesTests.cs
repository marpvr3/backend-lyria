using Lyria.Application.Common.Errors;
using Lyria.Application.Common.Results;
using Lyria.Application.Features.BranchSpecialSchedules.Replace;
using Lyria.Application.UnitTests.Fakes;
using Lyria.Domain.Establishments;
using Lyria.Domain.Establishments.Branches;
using Lyria.Domain.Establishments.Categories;
using Xunit;

namespace Lyria.Application.UnitTests.Features.BranchSpecialSchedules;

public sealed class ReplaceBranchSpecialSchedulesTests
{
    private readonly FakeBranchSpecialScheduleRepository _specialScheduleRepository = new();
    private readonly FakeEstablishmentBranchRepository _branchRepository = new();
    private readonly ReplaceBranchSpecialSchedulesCommandHandler _handler;
    private readonly Guid _branchId;

    public ReplaceBranchSpecialSchedulesTests()
    {
        var establishment = Establishment.Create(
            EstablishmentId.New(), EstablishmentCategoryId.New(),
            "Let It V", "let-it-v", null, null, null, null, null, null);

        var branchId = EstablishmentBranchId.New();
        var branch = EstablishmentBranch.Create(
            branchId, establishment.Id,
            "Sede Central", "Costa Rica 5865",
            null, null, null, null, null, null, null,
            null, null, null, null, null, "America/Argentina/Buenos_Aires");
        _branchRepository.Seed(branch);
        _branchId = branchId.Value;

        _handler = new ReplaceBranchSpecialSchedulesCommandHandler(
            _specialScheduleRepository, _branchRepository);
    }

    [Fact]
    public async Task Handle_ClosedDay_CreatesClosedSchedule()
    {
        var command = new ReplaceBranchSpecialSchedulesCommand(
            _branchId,
            new DateOnly(2026, 12, 25),
            IsClosed: true,
            Reason: "Navidad",
            TimeSlots: []);

        Result result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, _specialScheduleRepository.ReplaceCallCount);
    }

    [Fact]
    public async Task Handle_OpenDay_CreatesTimeSlots()
    {
        var command = new ReplaceBranchSpecialSchedulesCommand(
            _branchId,
            new DateOnly(2026, 12, 24),
            IsClosed: false,
            Reason: "Víspera de Navidad",
            TimeSlots:
            [
                new SpecialScheduleTimeSlotItem("09:00", "14:00", false),
                new SpecialScheduleTimeSlotItem("18:00", "22:00", false)
            ]);

        Result result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, _specialScheduleRepository.ReplaceCallCount);
    }

    [Fact]
    public async Task Handle_MidnightCrossing_CreatesSchedule()
    {
        var command = new ReplaceBranchSpecialSchedulesCommand(
            _branchId,
            new DateOnly(2026, 12, 31),
            IsClosed: false,
            Reason: "Año Nuevo",
            TimeSlots:
            [
                new SpecialScheduleTimeSlotItem("22:00", "04:00", true)
            ]);

        Result result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_BranchNotFound_ReturnsNotFound()
    {
        var command = new ReplaceBranchSpecialSchedulesCommand(
            Guid.NewGuid(),
            new DateOnly(2026, 12, 25),
            IsClosed: true,
            Reason: null,
            TimeSlots: []);

        Result result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
    }
}
