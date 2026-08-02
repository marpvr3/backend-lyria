using Lyria.Domain.Users;
using Xunit;

namespace Lyria.Domain.UnitTests.Users;

public sealed class UserMarkEmailVerifiedTests
{
    [Fact]
    public void MarkEmailAsVerified_SetsIsEmailVerifiedToTrue()
    {
        var user = CreateUser();

        user.MarkEmailAsVerified();

        Assert.True(user.IsEmailVerified);
    }

    [Fact]
    public void MarkEmailAsVerified_WhenAlreadyVerified_RemainsTrue()
    {
        var user = CreateUser();
        user.MarkEmailAsVerified();

        user.MarkEmailAsVerified();

        Assert.True(user.IsEmailVerified);
    }

    private static User CreateUser() =>
        User.Create(
            UserId.New(), "Carlos", "García", "carlos@example.com",
            "hashed123", null, null, null);
}
