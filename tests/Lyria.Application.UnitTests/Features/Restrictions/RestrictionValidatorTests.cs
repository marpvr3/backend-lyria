using FluentValidation.TestHelper;
using Lyria.Application.Features.Restrictions.Create;
using Lyria.Application.Features.Restrictions.Update;
using Lyria.Domain.Restrictions;
using Xunit;

namespace Lyria.Application.UnitTests.Features.Restrictions;

public sealed class RestrictionValidatorTests
{
    private readonly CreateRestrictionCommandValidator _createValidator = new();
    private readonly UpdateRestrictionCommandValidator _updateValidator = new();

    [Fact]
    public void CreateValidator_EmptyName_HasError()
    {
        var command = new CreateRestrictionCommand("", null);
        var result = _createValidator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void CreateValidator_ValidName_NoError()
    {
        var command = new CreateRestrictionCommand("Vegano", null);
        var result = _createValidator.TestValidate(command);
        result.ShouldNotHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void CreateValidator_NameTooLong_HasError()
    {
        string longName = new('A', Restriction.NameMaxLength + 1);
        var command = new CreateRestrictionCommand(longName, null);
        var result = _createValidator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void CreateValidator_DescriptionTooLong_HasError()
    {
        string longDesc = new('A', Restriction.DescriptionMaxLength + 1);
        var command = new CreateRestrictionCommand("Vegano", longDesc);
        var result = _createValidator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Description);
    }

    [Fact]
    public void CreateValidator_NullDescription_NoError()
    {
        var command = new CreateRestrictionCommand("Vegano", null);
        var result = _createValidator.TestValidate(command);
        result.ShouldNotHaveValidationErrorFor(x => x.Description);
    }

    [Fact]
    public void UpdateValidator_EmptyId_HasError()
    {
        var command = new UpdateRestrictionCommand(Guid.Empty, "Vegano", null);
        var result = _updateValidator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Id);
    }

    [Fact]
    public void UpdateValidator_EmptyName_HasError()
    {
        var command = new UpdateRestrictionCommand(Guid.NewGuid(), "", null);
        var result = _updateValidator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void UpdateValidator_ValidInput_NoError()
    {
        var command = new UpdateRestrictionCommand(Guid.NewGuid(), "Vegano", null);
        var result = _updateValidator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void UpdateValidator_DescriptionTooLong_HasError()
    {
        string longDesc = new('A', Restriction.DescriptionMaxLength + 1);
        var command = new UpdateRestrictionCommand(Guid.NewGuid(), "Vegano", longDesc);
        var result = _updateValidator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Description);
    }
}
