using Lyria.Domain.Users;
using Xunit;

namespace Lyria.Domain.UnitTests.Users;

public sealed class UserNormalizationTests
{
    [Fact]
    public void NormalizeName_TrimsWhitespace()
    {
        string result = User.NormalizeName("  Carlos  ");

        Assert.Equal("Carlos", result);
    }

    [Fact]
    public void NormalizeName_ReducesMultipleSpaces()
    {
        string result = User.NormalizeName("Carlos   Andrés   García");

        Assert.Equal("Carlos Andrés García", result);
    }

    [Fact]
    public void NormalizeName_EmptyInput_ReturnsEmpty()
    {
        string result = User.NormalizeName("");

        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void NormalizeEmail_TrimsAndLowercases()
    {
        string result = User.NormalizeEmail("  Carlos@Example.COM  ");

        Assert.Equal("carlos@example.com", result);
    }

    [Fact]
    public void NormalizeEmail_EmptyInput_ReturnsEmpty()
    {
        string result = User.NormalizeEmail("");

        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void NormalizeOptionalString_Null_ReturnsNull()
    {
        string? result = User.NormalizeOptionalString(null);

        Assert.Null(result);
    }

    [Fact]
    public void NormalizeOptionalString_Whitespace_ReturnsNull()
    {
        string? result = User.NormalizeOptionalString("   ");

        Assert.Null(result);
    }

    [Fact]
    public void NormalizeOptionalString_WithValue_Trims()
    {
        string? result = User.NormalizeOptionalString("  valor  ");

        Assert.Equal("valor", result);
    }
}
