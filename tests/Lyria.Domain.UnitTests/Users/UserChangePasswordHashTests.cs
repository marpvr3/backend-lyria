using Lyria.Domain.Users;
using Xunit;

namespace Lyria.Domain.UnitTests.Users;

public sealed class UserChangePasswordHashTests
{
    [Fact]
    public void ChangePasswordHash_WithValidHash_ChangesHash()
    {
        var user = CreateUser();

        user.ChangePasswordHash("newhashed456");

        Assert.Equal("newhashed456", user.PasswordHash);
    }

    [Fact]
    public void ChangePasswordHash_WithEmptyHash_ThrowsUserException()
    {
        var user = CreateUser();

        Assert.Throws<UserException>(() =>
            user.ChangePasswordHash(""));
    }

    private static User CreateUser() =>
        User.Create(
            UserId.New(), "Carlos", "García", "carlos@example.com",
            "hashed123", null, null, null);
}
