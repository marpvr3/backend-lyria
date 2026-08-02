using FluentValidation.TestHelper;
using Lyria.Application.Features.UserRestrictions.Assign;
using Lyria.Application.Features.UserRestrictions.GetById;
using Lyria.Application.Features.UserRestrictions.GetByUserId;
using Lyria.Application.Features.UserRestrictions.Remove;
using Lyria.Application.Features.UserRestrictions.Update;
using Lyria.Domain.Users.UserRestrictions;
using Xunit;

namespace Lyria.Application.UnitTests.Features.UserRestrictions;

public sealed class UserRestrictionValidatorTests
{
    // --- AssignRestrictionToUserCommandValidator ---

    [Fact]
    public void AssignValidator_ValidCommand_HasNoErrors()
    {
        var validator = new AssignRestrictionToUserCommandValidator();
        var command = new AssignRestrictionToUserCommand(
            Guid.NewGuid(), Guid.NewGuid(), UserRestrictionImportanceLevels.High);

        validator.TestValidate(command).ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("Low")]
    [InlineData("Medium")]
    [InlineData("High")]
    [InlineData("  High  ")]
    public void AssignValidator_AuthorizedImportanceLevel_HasNoErrors(string importanceLevel)
    {
        var validator = new AssignRestrictionToUserCommandValidator();
        var command = new AssignRestrictionToUserCommand(
            Guid.NewGuid(), Guid.NewGuid(), importanceLevel);

        validator.TestValidate(command).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void AssignValidator_EmptyUserId_HasError()
    {
        var validator = new AssignRestrictionToUserCommandValidator();
        var command = new AssignRestrictionToUserCommand(
            Guid.Empty, Guid.NewGuid(), UserRestrictionImportanceLevels.High);

        validator.TestValidate(command).ShouldHaveValidationErrorFor(x => x.UserId);
    }

    [Fact]
    public void AssignValidator_EmptyRestrictionId_HasError()
    {
        var validator = new AssignRestrictionToUserCommandValidator();
        var command = new AssignRestrictionToUserCommand(
            Guid.NewGuid(), Guid.Empty, UserRestrictionImportanceLevels.High);

        validator.TestValidate(command).ShouldHaveValidationErrorFor(x => x.RestrictionId);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void AssignValidator_EmptyImportanceLevel_HasError(string importanceLevel)
    {
        var validator = new AssignRestrictionToUserCommandValidator();
        var command = new AssignRestrictionToUserCommand(
            Guid.NewGuid(), Guid.NewGuid(), importanceLevel);

        validator.TestValidate(command).ShouldHaveValidationErrorFor(x => x.ImportanceLevel);
    }

    [Theory]
    [InlineData("low")]
    [InlineData("MEDIUM")]
    [InlineData("Critical")]
    public void AssignValidator_UnauthorizedImportanceLevel_HasError(string importanceLevel)
    {
        var validator = new AssignRestrictionToUserCommandValidator();
        var command = new AssignRestrictionToUserCommand(
            Guid.NewGuid(), Guid.NewGuid(), importanceLevel);

        validator.TestValidate(command).ShouldHaveValidationErrorFor(x => x.ImportanceLevel);
    }

    // --- UpdateUserRestrictionCommandValidator ---

    [Fact]
    public void UpdateValidator_ValidCommand_HasNoErrors()
    {
        var validator = new UpdateUserRestrictionCommandValidator();
        var command = new UpdateUserRestrictionCommand(
            Guid.NewGuid(), Guid.NewGuid(), UserRestrictionImportanceLevels.Medium);

        validator.TestValidate(command).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void UpdateValidator_EmptyUserId_HasError()
    {
        var validator = new UpdateUserRestrictionCommandValidator();
        var command = new UpdateUserRestrictionCommand(
            Guid.Empty, Guid.NewGuid(), UserRestrictionImportanceLevels.Medium);

        validator.TestValidate(command).ShouldHaveValidationErrorFor(x => x.UserId);
    }

    [Fact]
    public void UpdateValidator_EmptyRestrictionId_HasError()
    {
        var validator = new UpdateUserRestrictionCommandValidator();
        var command = new UpdateUserRestrictionCommand(
            Guid.NewGuid(), Guid.Empty, UserRestrictionImportanceLevels.Medium);

        validator.TestValidate(command).ShouldHaveValidationErrorFor(x => x.RestrictionId);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("Critical")]
    public void UpdateValidator_InvalidImportanceLevel_HasError(string importanceLevel)
    {
        var validator = new UpdateUserRestrictionCommandValidator();
        var command = new UpdateUserRestrictionCommand(
            Guid.NewGuid(), Guid.NewGuid(), importanceLevel);

        validator.TestValidate(command).ShouldHaveValidationErrorFor(x => x.ImportanceLevel);
    }

    // --- RemoveRestrictionFromUserCommandValidator ---

    [Fact]
    public void RemoveValidator_ValidCommand_HasNoErrors()
    {
        var validator = new RemoveRestrictionFromUserCommandValidator();
        var command = new RemoveRestrictionFromUserCommand(Guid.NewGuid(), Guid.NewGuid());

        validator.TestValidate(command).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void RemoveValidator_EmptyUserId_HasError()
    {
        var validator = new RemoveRestrictionFromUserCommandValidator();
        var command = new RemoveRestrictionFromUserCommand(Guid.Empty, Guid.NewGuid());

        validator.TestValidate(command).ShouldHaveValidationErrorFor(x => x.UserId);
    }

    [Fact]
    public void RemoveValidator_EmptyRestrictionId_HasError()
    {
        var validator = new RemoveRestrictionFromUserCommandValidator();
        var command = new RemoveRestrictionFromUserCommand(Guid.NewGuid(), Guid.Empty);

        validator.TestValidate(command).ShouldHaveValidationErrorFor(x => x.RestrictionId);
    }

    // --- GetUserRestrictionsQueryValidator ---

    [Fact]
    public void GetByUserIdValidator_ValidQuery_HasNoErrors()
    {
        var validator = new GetUserRestrictionsQueryValidator();
        var query = new GetUserRestrictionsQuery(Guid.NewGuid());

        validator.TestValidate(query).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void GetByUserIdValidator_EmptyUserId_HasError()
    {
        var validator = new GetUserRestrictionsQueryValidator();
        var query = new GetUserRestrictionsQuery(Guid.Empty);

        validator.TestValidate(query).ShouldHaveValidationErrorFor(x => x.UserId);
    }

    // --- GetUserRestrictionByIdQueryValidator ---

    [Fact]
    public void GetByIdValidator_ValidQuery_HasNoErrors()
    {
        var validator = new GetUserRestrictionByIdQueryValidator();
        var query = new GetUserRestrictionByIdQuery(Guid.NewGuid(), Guid.NewGuid());

        validator.TestValidate(query).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void GetByIdValidator_EmptyUserId_HasError()
    {
        var validator = new GetUserRestrictionByIdQueryValidator();
        var query = new GetUserRestrictionByIdQuery(Guid.Empty, Guid.NewGuid());

        validator.TestValidate(query).ShouldHaveValidationErrorFor(x => x.UserId);
    }

    [Fact]
    public void GetByIdValidator_EmptyRestrictionId_HasError()
    {
        var validator = new GetUserRestrictionByIdQueryValidator();
        var query = new GetUserRestrictionByIdQuery(Guid.NewGuid(), Guid.Empty);

        validator.TestValidate(query).ShouldHaveValidationErrorFor(x => x.RestrictionId);
    }
}
