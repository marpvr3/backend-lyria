using Lyria.Domain.Users;
using Xunit;

namespace Lyria.Domain.UnitTests.Users;

public sealed class UserUpdateProfileTests
{
    [Fact]
    public void UpdateProfile_WithValidData_UpdatesProperties()
    {
        var user = CreateUser();

        user.UpdateProfile("Nuevo", "Apellido", "+573009999999", new DateOnly(1995, 1, 1), "https://cdn.example.com/new.jpg");

        Assert.Equal("Nuevo", user.Name);
        Assert.Equal("Apellido", user.LastName);
        Assert.Equal("+573009999999", user.Phone);
        Assert.Equal(new DateOnly(1995, 1, 1), user.BirthDate);
        Assert.Equal("https://cdn.example.com/new.jpg", user.PhotoUrl);
    }

    [Fact]
    public void UpdateProfile_DoesNotChangeEmail()
    {
        var user = CreateUser();
        string originalEmail = user.Email;

        user.UpdateProfile("Nuevo", "Apellido", null, null, null);

        Assert.Equal(originalEmail, user.Email);
    }

    [Fact]
    public void UpdateProfile_DoesNotChangePasswordHash()
    {
        var user = CreateUser();
        string originalHash = user.PasswordHash;

        user.UpdateProfile("Nuevo", "Apellido", null, null, null);

        Assert.Equal(originalHash, user.PasswordHash);
    }

    [Fact]
    public void UpdateProfile_DoesNotChangeStatus()
    {
        var user = CreateUser();
        var originalStatus = user.Status;

        user.UpdateProfile("Nuevo", "Apellido", null, null, null);

        Assert.Equal(originalStatus, user.Status);
    }

    [Fact]
    public void UpdateProfile_DoesNotChangeIsEmailVerified()
    {
        var user = CreateUser();
        bool originalVerified = user.IsEmailVerified;

        user.UpdateProfile("Nuevo", "Apellido", null, null, null);

        Assert.Equal(originalVerified, user.IsEmailVerified);
    }

    [Fact]
    public void UpdateProfile_DoesNotChangeLastLoginAtUtc()
    {
        var user = CreateUser();
        var loginTime = new DateTime(2026, 7, 20, 10, 0, 0, DateTimeKind.Utc);
        user.RegisterLastLogin(loginTime);

        user.UpdateProfile("Nuevo", "Apellido", null, null, null);

        Assert.Equal(loginTime, user.LastLoginAtUtc);
    }

    [Fact]
    public void UpdateProfile_WithEmptyName_ThrowsUserException()
    {
        var user = CreateUser();

        Assert.Throws<UserException>(() =>
            user.UpdateProfile("", "Apellido", null, null, null));
    }

    [Fact]
    public void UpdateProfile_NormalizesName()
    {
        var user = CreateUser();

        user.UpdateProfile("  Carlos   Andrés  ", "García", null, null, null);

        Assert.Equal("Carlos Andrés", user.Name);
    }

    [Fact]
    public void UpdateProfile_WithNullPhone_SetsNull()
    {
        var user = CreateUser();

        user.UpdateProfile("Carlos", "García", null, null, null);

        Assert.Null(user.Phone);
    }

    [Fact]
    public void UpdateProfile_WithNullPhotoUrl_SetsNull()
    {
        var user = CreateUser();

        user.UpdateProfile("Carlos", "García", null, null, null);

        Assert.Null(user.PhotoUrl);
    }

    private static User CreateUser() =>
        User.Create(
            UserId.New(), "Carlos", "García", "carlos@example.com",
            "hashed123", "+573001234567", new DateOnly(1990, 5, 15), null);
}
