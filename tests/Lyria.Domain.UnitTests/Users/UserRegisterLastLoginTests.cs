using Lyria.Domain.Users;
using Xunit;

namespace Lyria.Domain.UnitTests.Users;

public sealed class UserRegisterLastLoginTests
{
    [Fact]
    public void RegisterLastLogin_SetsLastLoginAtUtc()
    {
        var user = CreateUser();
        var loginTime = new DateTime(2026, 7, 20, 10, 0, 0, DateTimeKind.Utc);

        user.RegisterLastLogin(loginTime);

        Assert.Equal(loginTime, user.LastLoginAtUtc);
    }

    [Fact]
    public void RegisterLastLogin_OverwritesPreviousValue()
    {
        var user = CreateUser();
        var firstLogin = new DateTime(2026, 7, 10, 10, 0, 0, DateTimeKind.Utc);
        var secondLogin = new DateTime(2026, 7, 20, 15, 0, 0, DateTimeKind.Utc);
        user.RegisterLastLogin(firstLogin);

        user.RegisterLastLogin(secondLogin);

        Assert.Equal(secondLogin, user.LastLoginAtUtc);
    }

    private static User CreateUser() =>
        User.Create(
            UserId.New(), "Carlos", "García", "carlos@example.com",
            "hashed123", null, null, null);
}
