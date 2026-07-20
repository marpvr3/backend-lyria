using FluentValidation.TestHelper;
using Lyria.Application.Features.EstablishmentBranchServices.Assign;
using Lyria.Application.Features.EstablishmentBranchServices.GetByIds;
using Lyria.Application.Features.EstablishmentBranchServices.List;
using Lyria.Application.Features.EstablishmentBranchServices.Update;
using Lyria.Application.Features.EstablishmentBranchServices.UpdateStatus;
using Lyria.Domain.Establishments.Branches;
using Xunit;

namespace Lyria.Application.UnitTests.Features.EstablishmentBranchServices;

public sealed class BranchServiceValidatorTests
{
    [Fact]
    public void AssignValidator_EmptyBranchId_HasError()
    {
        var validator = new AssignServiceToBranchCommandValidator();
        var command = new AssignServiceToBranchCommand(Guid.Empty, Guid.NewGuid(), true, null);
        var result = validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.BranchId);
    }

    [Fact]
    public void AssignValidator_EmptyServiceId_HasError()
    {
        var validator = new AssignServiceToBranchCommandValidator();
        var command = new AssignServiceToBranchCommand(Guid.NewGuid(), Guid.Empty, true, null);
        var result = validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.ServiceId);
    }

    [Fact]
    public void AssignValidator_ObservationTooLong_HasError()
    {
        var validator = new AssignServiceToBranchCommandValidator();
        string longObs = new('A', EstablishmentBranchService.ObservationMaxLength + 1);
        var command = new AssignServiceToBranchCommand(Guid.NewGuid(), Guid.NewGuid(), true, longObs);
        var result = validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Observation);
    }

    [Fact]
    public void AssignValidator_ValidCommand_NoErrors()
    {
        var validator = new AssignServiceToBranchCommandValidator();
        var command = new AssignServiceToBranchCommand(Guid.NewGuid(), Guid.NewGuid(), true, "Obs");
        var result = validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void UpdateValidator_EmptyBranchId_HasError()
    {
        var validator = new UpdateBranchServiceCommandValidator();
        var command = new UpdateBranchServiceCommand(Guid.Empty, Guid.NewGuid(), true, null);
        var result = validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.BranchId);
    }

    [Fact]
    public void UpdateValidator_ObservationTooLong_HasError()
    {
        var validator = new UpdateBranchServiceCommandValidator();
        string longObs = new('A', EstablishmentBranchService.ObservationMaxLength + 1);
        var command = new UpdateBranchServiceCommand(Guid.NewGuid(), Guid.NewGuid(), true, longObs);
        var result = validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Observation);
    }

    [Fact]
    public void StatusValidator_EmptyBranchId_HasError()
    {
        var validator = new UpdateBranchServiceStatusCommandValidator();
        var command = new UpdateBranchServiceStatusCommand(Guid.Empty, Guid.NewGuid(), true);
        var result = validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.BranchId);
    }

    [Fact]
    public void StatusValidator_EmptyServiceId_HasError()
    {
        var validator = new UpdateBranchServiceStatusCommandValidator();
        var command = new UpdateBranchServiceStatusCommand(Guid.NewGuid(), Guid.Empty, true);
        var result = validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.ServiceId);
    }

    [Fact]
    public void GetByIdsValidator_EmptyIds_HasErrors()
    {
        var validator = new GetBranchServiceByIdsQueryValidator();
        var query = new GetBranchServiceByIdsQuery(Guid.Empty, Guid.Empty);
        var result = validator.TestValidate(query);
        result.ShouldHaveValidationErrorFor(x => x.BranchId);
        result.ShouldHaveValidationErrorFor(x => x.ServiceId);
    }

    [Fact]
    public void ListValidator_PageLessThan1_HasError()
    {
        var validator = new ListBranchServicesQueryValidator();
        var query = new ListBranchServicesQuery(Guid.NewGuid(), null, null, null, 0, 20);
        var result = validator.TestValidate(query);
        result.ShouldHaveValidationErrorFor(x => x.Page);
    }

    [Fact]
    public void ListValidator_PageSizeTooLarge_HasError()
    {
        var validator = new ListBranchServicesQueryValidator();
        var query = new ListBranchServicesQuery(Guid.NewGuid(), null, null, null, 1, 101);
        var result = validator.TestValidate(query);
        result.ShouldHaveValidationErrorFor(x => x.PageSize);
    }
}
