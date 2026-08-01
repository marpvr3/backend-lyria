using Lyria.Domain.Users;
using Xunit;

namespace Lyria.Domain.UnitTests.Users;

public sealed class UserChangeEmailTests
{
    [Fact]
    public void ChangeEmail_WithValidEmail_ChangesEmail()
    {
        var user = CreateUser();

        user.ChangeEmail("nuevo@example.com");

        Assert.Equal("nuevo@example.com", user.Email);
    }

    [Fact]
    public void ChangeEmail_NormalizesEmail()
    {
        var user = CreateUser();

        user.ChangeEmail("  Nuevo@Example.COM  ");

        Assert.Equal("nuevo@example.com", user.Email);
    }

    [Fact]
    public void ChangeEmail_WithEmptyEmail_ThrowsUserException()
    {
        var user = CreateUser();

        Assert.Throws<UserException>(() =>
            user.ChangeEmail(""));
    }

    [Fact]
    public void ChangeEmail_WithInvalidFormat_ThrowsUserException()
    {
        var user = CreateUser();

        Assert.Throws<UserException>(() =>
            user.ChangeEmail("sinformatovalido"));
    }

    private static User CreateUser() =>
        User.Create(
            UserId.New(), "Carlos", "García", "carlos@example.com",
            "hashed123", null, null, null);
}
