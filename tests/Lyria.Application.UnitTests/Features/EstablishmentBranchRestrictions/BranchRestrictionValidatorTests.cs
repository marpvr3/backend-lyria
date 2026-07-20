using FluentValidation.TestHelper;
using Lyria.Application.Features.EstablishmentBranchRestrictions.Assign;
using Lyria.Application.Features.EstablishmentBranchRestrictions.GetByIds;
using Lyria.Application.Features.EstablishmentBranchRestrictions.List;
using Lyria.Application.Features.EstablishmentBranchRestrictions.Update;
using Lyria.Application.Features.EstablishmentBranchRestrictions.UpdateStatus;
using Lyria.Domain.Establishments.Branches;
using Xunit;

namespace Lyria.Application.UnitTests.Features.EstablishmentBranchRestrictions;

public sealed class BranchRestrictionValidatorTests
{
    [Fact]
    public void AssignValidator_EmptyBranchId_HasError()
    {
        var validator = new AssignRestrictionToBranchCommandValidator();
        var command = new AssignRestrictionToBranchCommand(
            Guid.Empty, Guid.NewGuid(), RestrictionComplianceLevel.Guaranteed, false, null);
        var result = validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.BranchId);
    }

    [Fact]
    public void AssignValidator_EmptyRestrictionId_HasError()
    {
        var validator = new AssignRestrictionToBranchCommandValidator();
        var command = new AssignRestrictionToBranchCommand(
            Guid.NewGuid(), Guid.Empty, RestrictionComplianceLevel.Guaranteed, false, null);
        var result = validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.RestrictionId);
    }

    [Fact]
    public void AssignValidator_ComplianceLevelZero_HasError()
    {
        var validator = new AssignRestrictionToBranchCommandValidator();
        var command = new AssignRestrictionToBranchCommand(
            Guid.NewGuid(), Guid.NewGuid(), (RestrictionComplianceLevel)0, false, null);
        var result = validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.ComplianceLevel);
    }

    [Fact]
    public void AssignValidator_ComplianceLevelOutOfEnum_HasError()
    {
        var validator = new AssignRestrictionToBranchCommandValidator();
        var command = new AssignRestrictionToBranchCommand(
            Guid.NewGuid(), Guid.NewGuid(), (RestrictionComplianceLevel)99, false, null);
        var result = validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.ComplianceLevel);
    }

    [Fact]
    public void AssignValidator_ObservationTooLong_HasError()
    {
        var validator = new AssignRestrictionToBranchCommandValidator();
        string longObs = new('A', EstablishmentBranchRestriction.ObservationMaxLength + 1);
        var command = new AssignRestrictionToBranchCommand(
            Guid.NewGuid(), Guid.NewGuid(), RestrictionComplianceLevel.Guaranteed, false, longObs);
        var result = validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Observation);
    }

    [Fact]
    public void AssignValidator_ValidCommand_NoErrors()
    {
        var validator = new AssignRestrictionToBranchCommandValidator();
        var command = new AssignRestrictionToBranchCommand(
            Guid.NewGuid(), Guid.NewGuid(), RestrictionComplianceLevel.Guaranteed, true, "Obs");
        var result = validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void UpdateValidator_EmptyBranchId_HasError()
    {
        var validator = new UpdateBranchRestrictionCommandValidator();
        var command = new UpdateBranchRestrictionCommand(
            Guid.Empty, Guid.NewGuid(), RestrictionComplianceLevel.Guaranteed, false, null);
        var result = validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.BranchId);
    }

    [Fact]
    public void UpdateValidator_ComplianceLevelInvalid_HasError()
    {
        var validator = new UpdateBranchRestrictionCommandValidator();
        var command = new UpdateBranchRestrictionCommand(
            Guid.NewGuid(), Guid.NewGuid(), (RestrictionComplianceLevel)0, false, null);
        var result = validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.ComplianceLevel);
    }

    [Fact]
    public void UpdateValidator_ObservationTooLong_HasError()
    {
        var validator = new UpdateBranchRestrictionCommandValidator();
        string longObs = new('A', EstablishmentBranchRestriction.ObservationMaxLength + 1);
        var command = new UpdateBranchRestrictionCommand(
            Guid.NewGuid(), Guid.NewGuid(), RestrictionComplianceLevel.Guaranteed, false, longObs);
        var result = validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Observation);
    }

    [Fact]
    public void StatusValidator_EmptyBranchId_HasError()
    {
        var validator = new UpdateBranchRestrictionStatusCommandValidator();
        var command = new UpdateBranchRestrictionStatusCommand(Guid.Empty, Guid.NewGuid(), true);
        var result = validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.BranchId);
    }

    [Fact]
    public void StatusValidator_EmptyRestrictionId_HasError()
    {
        var validator = new UpdateBranchRestrictionStatusCommandValidator();
        var command = new UpdateBranchRestrictionStatusCommand(Guid.NewGuid(), Guid.Empty, true);
        var result = validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.RestrictionId);
    }

    [Fact]
    public void GetByIdsValidator_EmptyIds_HasErrors()
    {
        var validator = new GetBranchRestrictionByIdsQueryValidator();
        var query = new GetBranchRestrictionByIdsQuery(Guid.Empty, Guid.Empty);
        var result = validator.TestValidate(query);
        result.ShouldHaveValidationErrorFor(x => x.BranchId);
        result.ShouldHaveValidationErrorFor(x => x.RestrictionId);
    }

    [Fact]
    public void ListValidator_PageLessThan1_HasError()
    {
        var validator = new ListBranchRestrictionsQueryValidator();
        var query = new ListBranchRestrictionsQuery(Guid.NewGuid(), null, null, null, null, 0, 20);
        var result = validator.TestValidate(query);
        result.ShouldHaveValidationErrorFor(x => x.Page);
    }

    [Fact]
    public void ListValidator_PageSizeTooLarge_HasError()
    {
        var validator = new ListBranchRestrictionsQueryValidator();
        var query = new ListBranchRestrictionsQuery(Guid.NewGuid(), null, null, null, null, 1, 101);
        var result = validator.TestValidate(query);
        result.ShouldHaveValidationErrorFor(x => x.PageSize);
    }

    [Fact]
    public void ListValidator_InvalidComplianceLevel_HasError()
    {
        var validator = new ListBranchRestrictionsQueryValidator();
        var query = new ListBranchRestrictionsQuery(
            Guid.NewGuid(), null, null, (RestrictionComplianceLevel)99, null, 1, 20);
        var result = validator.TestValidate(query);
        result.ShouldHaveValidationErrorFor(x => x.ComplianceLevel);
    }

    [Fact]
    public void ListValidator_ValidComplianceLevel_NoError()
    {
        var validator = new ListBranchRestrictionsQueryValidator();
        var query = new ListBranchRestrictionsQuery(
            Guid.NewGuid(), null, null, RestrictionComplianceLevel.Partial, null, 1, 20);
        var result = validator.TestValidate(query);
        result.ShouldNotHaveValidationErrorFor(x => x.ComplianceLevel);
    }
}
