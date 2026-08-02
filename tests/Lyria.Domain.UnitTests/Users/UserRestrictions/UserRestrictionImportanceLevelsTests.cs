using Lyria.Domain.Users.UserRestrictions;
using Xunit;

namespace Lyria.Domain.UnitTests.Users.UserRestrictions;

public sealed class UserRestrictionImportanceLevelsTests
{
    [Fact]
    public void All_ContainsOnlyAuthorizedLevels()
    {
        Assert.Equal(
            ["Low", "Medium", "High"],
            UserRestrictionImportanceLevels.All);
    }

    [Theory]
    [InlineData("Low")]
    [InlineData("Medium")]
    [InlineData("High")]
    public void IsValid_AuthorizedLevel_ReturnsTrue(string importanceLevel)
    {
        Assert.True(UserRestrictionImportanceLevels.IsValid(importanceLevel));
    }

    [Theory]
    [InlineData("low")]
    [InlineData("MEDIUM")]
    [InlineData("Critical")]
    [InlineData("")]
    [InlineData(null)]
    public void IsValid_UnauthorizedLevel_ReturnsFalse(string? importanceLevel)
    {
        Assert.False(UserRestrictionImportanceLevels.IsValid(importanceLevel));
    }
}
