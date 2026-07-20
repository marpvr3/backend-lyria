using FluentValidation.TestHelper;
using Lyria.Application.Features.Services.Create;
using Lyria.Application.Features.Services.Update;
using Lyria.Domain.Services;
using Xunit;

namespace Lyria.Application.UnitTests.Features.Services;

public sealed class ServiceValidatorTests
{
    private readonly CreateServiceCommandValidator _createValidator = new();
    private readonly UpdateServiceCommandValidator _updateValidator = new();

    [Fact]
    public void CreateValidator_EmptyName_HasError()
    {
        var command = new CreateServiceCommand("", null, null);
        var result = _createValidator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void CreateValidator_ValidName_NoError()
    {
        var command = new CreateServiceCommand("Delivery", null, null);
        var result = _createValidator.TestValidate(command);
        result.ShouldNotHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void CreateValidator_NameTooLong_HasError()
    {
        string longName = new('A', Service.NameMaxLength + 1);
        var command = new CreateServiceCommand(longName, null, null);
        var result = _createValidator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void CreateValidator_DescriptionTooLong_HasError()
    {
        string longDesc = new('A', Service.DescriptionMaxLength + 1);
        var command = new CreateServiceCommand("Delivery", longDesc, null);
        var result = _createValidator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Description);
    }

    [Fact]
    public void CreateValidator_NullDescription_NoError()
    {
        var command = new CreateServiceCommand("Delivery", null, null);
        var result = _createValidator.TestValidate(command);
        result.ShouldNotHaveValidationErrorFor(x => x.Description);
    }

    [Fact]
    public void CreateValidator_IconUrlTooLong_HasError()
    {
        string longUrl = new('A', Service.IconUrlMaxLength + 1);
        var command = new CreateServiceCommand("Delivery", null, longUrl);
        var result = _createValidator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.IconUrl);
    }

    [Fact]
    public void CreateValidator_NullIconUrl_NoError()
    {
        var command = new CreateServiceCommand("Delivery", null, null);
        var result = _createValidator.TestValidate(command);
        result.ShouldNotHaveValidationErrorFor(x => x.IconUrl);
    }

    [Fact]
    public void UpdateValidator_EmptyId_HasError()
    {
        var command = new UpdateServiceCommand(Guid.Empty, "Delivery", null, null);
        var result = _updateValidator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Id);
    }

    [Fact]
    public void UpdateValidator_EmptyName_HasError()
    {
        var command = new UpdateServiceCommand(Guid.NewGuid(), "", null, null);
        var result = _updateValidator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void UpdateValidator_ValidInput_NoError()
    {
        var command = new UpdateServiceCommand(Guid.NewGuid(), "Delivery", null, null);
        var result = _updateValidator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void UpdateValidator_DescriptionTooLong_HasError()
    {
        string longDesc = new('A', Service.DescriptionMaxLength + 1);
        var command = new UpdateServiceCommand(Guid.NewGuid(), "Delivery", longDesc, null);
        var result = _updateValidator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Description);
    }

    [Fact]
    public void UpdateValidator_IconUrlTooLong_HasError()
    {
        string longUrl = new('A', Service.IconUrlMaxLength + 1);
        var command = new UpdateServiceCommand(Guid.NewGuid(), "Delivery", null, longUrl);
        var result = _updateValidator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.IconUrl);
    }
}
