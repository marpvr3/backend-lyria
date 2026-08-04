using FluentValidation.TestHelper;
using Lyria.Application.Features.MobileRegistrations.Register;
using Lyria.Domain.Users;
using Xunit;

namespace Lyria.Application.UnitTests.Features.MobileRegistrations;

public sealed class RegisterMobileUserValidatorTests
{
    private readonly RegisterMobileUserCommandValidator _validator = new();

    private static RegisterMobileUserCommand ValidCommand(
        IReadOnlyList<Guid>? restrictionIds = null) =>
        new("Andres", "Perez", "andres@email.com", "Password123",
            "3001234567", new DateOnly(1978, 12, 25), null, restrictionIds ?? []);

    [Fact]
    public void ValidCommand_HasNoErrors()
    {
        _validator.TestValidate(ValidCommand()).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void EmptyRestrictionList_IsAllowed()
    {
        _validator.TestValidate(ValidCommand([])).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void SeveralDistinctRestrictions_AreAllowed()
    {
        RegisterMobileUserCommand command =
            ValidCommand([Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()]);

        _validator.TestValidate(command).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void EmptyRestrictionId_HasError()
    {
        RegisterMobileUserCommand command = ValidCommand([Guid.NewGuid(), Guid.Empty]);

        _validator.TestValidate(command)
            .ShouldHaveValidationErrorFor(x => x.RestrictionIds);
    }

    [Fact]
    public void DuplicatedRestrictionIds_HaveError()
    {
        Guid restrictionId = Guid.NewGuid();
        RegisterMobileUserCommand command = ValidCommand([restrictionId, restrictionId]);

        _validator.TestValidate(command)
            .ShouldHaveValidationErrorFor(x => x.RestrictionIds);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void EmptyName_HasError(string name)
    {
        RegisterMobileUserCommand command = ValidCommand() with { Name = name };

        _validator.TestValidate(command).ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void NameTooLong_HasError()
    {
        RegisterMobileUserCommand command =
            ValidCommand() with { Name = new string('a', User.NameMaxLength + 1) };

        _validator.TestValidate(command).ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void EmptyLastName_HasError(string lastName)
    {
        RegisterMobileUserCommand command = ValidCommand() with { LastName = lastName };

        _validator.TestValidate(command).ShouldHaveValidationErrorFor(x => x.LastName);
    }

    [Fact]
    public void EmptyEmail_HasError()
    {
        RegisterMobileUserCommand command = ValidCommand() with { Email = "" };

        _validator.TestValidate(command).ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Theory]
    [InlineData("")]
    [InlineData("corta")]
    [InlineData("1234567")]
    public void InvalidPassword_HasError(string password)
    {
        RegisterMobileUserCommand command = ValidCommand() with { Password = password };

        _validator.TestValidate(command).ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void PasswordTooLong_HasError()
    {
        RegisterMobileUserCommand command = ValidCommand() with
        {
            Password = new string('a', RegisterMobileUserCommandValidator.PasswordMaxLength + 1)
        };

        _validator.TestValidate(command).ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void PhoneTooLong_HasError()
    {
        RegisterMobileUserCommand command =
            ValidCommand() with { Phone = new string('9', User.PhoneMaxLength + 1) };

        _validator.TestValidate(command).ShouldHaveValidationErrorFor(x => x.Phone);
    }

    [Fact]
    public void PhotoUrlTooLong_HasError()
    {
        RegisterMobileUserCommand command =
            ValidCommand() with { PhotoUrl = new string('u', User.PhotoUrlMaxLength + 1) };

        _validator.TestValidate(command).ShouldHaveValidationErrorFor(x => x.PhotoUrl);
    }

    [Fact]
    public void NullPhoneAndPhotoUrl_AreAllowed()
    {
        RegisterMobileUserCommand command =
            ValidCommand() with { Phone = null, PhotoUrl = null };

        _validator.TestValidate(command).ShouldNotHaveAnyValidationErrors();
    }
}
