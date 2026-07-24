using FluentValidation.TestHelper;
using Lyria.Application.Features.BranchSchedules.GetToday;
using Lyria.Application.Features.BranchSchedules.GetWeekly;
using Lyria.Application.Features.BranchSchedules.Replace;
using Xunit;

namespace Lyria.Application.UnitTests.Features.BranchSchedules;

public sealed class BranchScheduleValidatorTests
{
    private readonly ReplaceBranchSchedulesCommandValidator _replaceValidator = new();
    private readonly GetBranchWeeklyScheduleQueryValidator _weeklyValidator = new();
    private readonly GetBranchTodayScheduleQueryValidator _todayValidator = new();

    [Fact]
    public void Replace_EmptyBranchId_HasError()
    {
        var command = new ReplaceBranchSchedulesCommand(Guid.Empty,
        [
            new BranchScheduleItem(1, "09:00", "17:00", false, false)
        ]);

        var result = _replaceValidator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.BranchId);
    }

    [Fact]
    public void Replace_ValidCommand_NoErrors()
    {
        var command = new ReplaceBranchSchedulesCommand(Guid.NewGuid(),
        [
            new BranchScheduleItem(1, "09:00", "17:00", false, false),
            new BranchScheduleItem(7, null, null, false, true)
        ]);

        var result = _replaceValidator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void GetWeekly_EmptyBranchId_HasError()
    {
        var query = new GetBranchWeeklyScheduleQuery(Guid.Empty);
        var result = _weeklyValidator.TestValidate(query);
        result.ShouldHaveValidationErrorFor(x => x.BranchId);
    }

    [Fact]
    public void GetWeekly_ValidBranchId_NoErrors()
    {
        var query = new GetBranchWeeklyScheduleQuery(Guid.NewGuid());
        var result = _weeklyValidator.TestValidate(query);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void GetToday_EmptyBranchId_HasError()
    {
        var query = new GetBranchTodayScheduleQuery(Guid.Empty);
        var result = _todayValidator.TestValidate(query);
        result.ShouldHaveValidationErrorFor(x => x.BranchId);
    }

    [Fact]
    public void GetToday_ValidBranchId_NoErrors()
    {
        var query = new GetBranchTodayScheduleQuery(Guid.NewGuid());
        var result = _todayValidator.TestValidate(query);
        result.ShouldNotHaveAnyValidationErrors();
    }
}
