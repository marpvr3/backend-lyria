using Lyria.Domain.Restrictions;
using Lyria.Domain.Users;
using Lyria.Domain.Users.UserRestrictions;
using Xunit;

namespace Lyria.Domain.UnitTests.Users.UserRestrictions;

public sealed class UserRestrictionTests
{
    private static readonly UserId UserId = UserId.New();
    private static readonly RestrictionId RestrictionId = RestrictionId.New();
    private static readonly DateTime CreatedAtUtc = new(2026, 8, 2, 5, 0, 0, DateTimeKind.Utc);

    private static UserRestriction CreateUserRestriction(
        string importanceLevel = UserRestrictionImportanceLevels.High)
    {
        return UserRestriction.Create(UserId, RestrictionId, importanceLevel, CreatedAtUtc);
    }

    [Fact]
    public void Create_ValidData_SetsAllProperties()
    {
        var userRestriction = CreateUserRestriction();

        Assert.Equal(UserId, userRestriction.UserId);
        Assert.Equal(RestrictionId, userRestriction.RestrictionId);
        Assert.Equal(UserRestrictionImportanceLevels.High, userRestriction.ImportanceLevel);
        Assert.Equal(CreatedAtUtc, userRestriction.CreatedAtUtc);
    }

    [Fact]
    public void Create_EmptyUserId_Throws()
    {
        var exception = Assert.Throws<UserRestrictionException>(() =>
            UserRestriction.Create(
                new UserId(Guid.Empty),
                RestrictionId,
                UserRestrictionImportanceLevels.Low,
                CreatedAtUtc));

        Assert.Equal("El usuario es obligatorio.", exception.Message);
    }

    [Fact]
    public void Create_EmptyRestrictionId_Throws()
    {
        var exception = Assert.Throws<UserRestrictionException>(() =>
            UserRestriction.Create(
                UserId,
                new RestrictionId(Guid.Empty),
                UserRestrictionImportanceLevels.Low,
                CreatedAtUtc));

        Assert.Equal("La restricción es obligatoria.", exception.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_EmptyImportanceLevel_Throws(string importanceLevel)
    {
        var exception = Assert.Throws<UserRestrictionException>(() =>
            CreateUserRestriction(importanceLevel));

        Assert.Equal("El nivel de importancia es obligatorio.", exception.Message);
    }

    [Theory]
    [InlineData("Critical")]
    [InlineData("low")]
    [InlineData("HIGH")]
    [InlineData("Medio")]
    public void Create_InvalidImportanceLevel_Throws(string importanceLevel)
    {
        var exception = Assert.Throws<UserRestrictionException>(() =>
            CreateUserRestriction(importanceLevel));

        Assert.Contains("no es válido", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Create_LowImportanceLevel_IsAccepted()
    {
        var userRestriction = CreateUserRestriction(UserRestrictionImportanceLevels.Low);

        Assert.Equal(UserRestrictionImportanceLevels.Low, userRestriction.ImportanceLevel);
    }

    [Fact]
    public void Create_MediumImportanceLevel_IsAccepted()
    {
        var userRestriction = CreateUserRestriction(UserRestrictionImportanceLevels.Medium);

        Assert.Equal(UserRestrictionImportanceLevels.Medium, userRestriction.ImportanceLevel);
    }

    [Fact]
    public void Create_HighImportanceLevel_IsAccepted()
    {
        var userRestriction = CreateUserRestriction(UserRestrictionImportanceLevels.High);

        Assert.Equal(UserRestrictionImportanceLevels.High, userRestriction.ImportanceLevel);
    }

    [Fact]
    public void Create_ImportanceLevelWithSpaces_IsTrimmed()
    {
        var userRestriction = CreateUserRestriction("  Medium  ");

        Assert.Equal(UserRestrictionImportanceLevels.Medium, userRestriction.ImportanceLevel);
    }

    [Fact]
    public void Create_DoesNotChangeCaseOfImportanceLevel()
    {
        var userRestriction = CreateUserRestriction(UserRestrictionImportanceLevels.Medium);

        Assert.Equal("Medium", userRestriction.ImportanceLevel);
    }

    [Fact]
    public void UpdateImportanceLevel_ValidLevel_UpdatesValue()
    {
        var userRestriction = CreateUserRestriction(UserRestrictionImportanceLevels.High);

        userRestriction.UpdateImportanceLevel(UserRestrictionImportanceLevels.Low);

        Assert.Equal(UserRestrictionImportanceLevels.Low, userRestriction.ImportanceLevel);
    }

    [Fact]
    public void UpdateImportanceLevel_WithSpaces_IsTrimmed()
    {
        var userRestriction = CreateUserRestriction();

        userRestriction.UpdateImportanceLevel("  Low  ");

        Assert.Equal(UserRestrictionImportanceLevels.Low, userRestriction.ImportanceLevel);
    }

    [Fact]
    public void UpdateImportanceLevel_PreservesCreatedAtUtc()
    {
        var userRestriction = CreateUserRestriction();

        userRestriction.UpdateImportanceLevel(UserRestrictionImportanceLevels.Medium);

        Assert.Equal(CreatedAtUtc, userRestriction.CreatedAtUtc);
    }

    [Fact]
    public void UpdateImportanceLevel_PreservesIdentity()
    {
        var userRestriction = CreateUserRestriction();

        userRestriction.UpdateImportanceLevel(UserRestrictionImportanceLevels.Medium);

        Assert.Equal(UserId, userRestriction.UserId);
        Assert.Equal(RestrictionId, userRestriction.RestrictionId);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void UpdateImportanceLevel_EmptyValue_Throws(string importanceLevel)
    {
        var userRestriction = CreateUserRestriction();

        var exception = Assert.Throws<UserRestrictionException>(() =>
            userRestriction.UpdateImportanceLevel(importanceLevel));

        Assert.Equal("El nivel de importancia es obligatorio.", exception.Message);
    }

    [Fact]
    public void UpdateImportanceLevel_InvalidValue_Throws()
    {
        var userRestriction = CreateUserRestriction();

        var exception = Assert.Throws<UserRestrictionException>(() =>
            userRestriction.UpdateImportanceLevel("Urgent"));

        Assert.Contains("no es válido", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void UpdateImportanceLevel_InvalidValue_KeepsPreviousLevel()
    {
        var userRestriction = CreateUserRestriction(UserRestrictionImportanceLevels.High);

        Assert.Throws<UserRestrictionException>(() =>
            userRestriction.UpdateImportanceLevel("Urgent"));

        Assert.Equal(UserRestrictionImportanceLevels.High, userRestriction.ImportanceLevel);
    }
}
