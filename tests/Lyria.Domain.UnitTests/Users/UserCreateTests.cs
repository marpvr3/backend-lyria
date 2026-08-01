using Lyria.Domain.Users;
using Xunit;

namespace Lyria.Domain.UnitTests.Users;

public sealed class UserCreateTests
{
    private static readonly UserId DefaultId = UserId.New();

    [Fact]
    public void Create_WithValidData_CreatesUser()
    {
        var user = User.Create(
            DefaultId, "Carlos", "García", "carlos@example.com",
            "hashed123", "+573001234567", new DateOnly(1990, 5, 15), "https://cdn.example.com/photo.jpg");

        Assert.Equal(DefaultId, user.Id);
        Assert.Equal("Carlos", user.Name);
        Assert.Equal("García", user.LastName);
        Assert.Equal("carlos@example.com", user.Email);
        Assert.Equal("hashed123", user.PasswordHash);
        Assert.Equal("+573001234567", user.Phone);
        Assert.Equal(new DateOnly(1990, 5, 15), user.BirthDate);
        Assert.Equal("https://cdn.example.com/photo.jpg", user.PhotoUrl);
        Assert.Equal(UserStatus.Unverified, user.Status);
        Assert.False(user.IsEmailVerified);
        Assert.Null(user.LastLoginAtUtc);
    }

    [Fact]
    public void Create_WithEmptyName_ThrowsUserException()
    {
        Assert.Throws<UserException>(() =>
            User.Create(DefaultId, "", "García", "carlos@example.com",
                "hashed123", null, null, null));
    }

    [Fact]
    public void Create_WithNameTooShort_ThrowsUserException()
    {
        Assert.Throws<UserException>(() =>
            User.Create(DefaultId, "A", "García", "carlos@example.com",
                "hashed123", null, null, null));
    }

    [Fact]
    public void Create_WithNameTooLong_ThrowsUserException()
    {
        string longName = new('A', User.NameMaxLength + 1);

        Assert.Throws<UserException>(() =>
            User.Create(DefaultId, longName, "García", "carlos@example.com",
                "hashed123", null, null, null));
    }

    [Fact]
    public void Create_NormalizesName()
    {
        var user = User.Create(
            DefaultId, "  Carlos   Andrés  ", "García", "carlos@example.com",
            "hashed123", null, null, null);

        Assert.Equal("Carlos Andrés", user.Name);
    }

    [Fact]
    public void Create_WithEmptyLastName_ThrowsUserException()
    {
        Assert.Throws<UserException>(() =>
            User.Create(DefaultId, "Carlos", "", "carlos@example.com",
                "hashed123", null, null, null));
    }

    [Fact]
    public void Create_WithLastNameTooShort_ThrowsUserException()
    {
        Assert.Throws<UserException>(() =>
            User.Create(DefaultId, "Carlos", "G", "carlos@example.com",
                "hashed123", null, null, null));
    }

    [Fact]
    public void Create_WithLastNameTooLong_ThrowsUserException()
    {
        string longLastName = new('A', User.LastNameMaxLength + 1);

        Assert.Throws<UserException>(() =>
            User.Create(DefaultId, "Carlos", longLastName, "carlos@example.com",
                "hashed123", null, null, null));
    }

    [Fact]
    public void Create_NormalizesLastName()
    {
        var user = User.Create(
            DefaultId, "Carlos", "  García   López  ", "carlos@example.com",
            "hashed123", null, null, null);

        Assert.Equal("García López", user.LastName);
    }

    [Fact]
    public void Create_WithEmptyEmail_ThrowsUserException()
    {
        Assert.Throws<UserException>(() =>
            User.Create(DefaultId, "Carlos", "García", "",
                "hashed123", null, null, null));
    }

    [Fact]
    public void Create_WithEmailTooLong_ThrowsUserException()
    {
        string longEmail = new string('a', User.EmailMaxLength) + "@example.com";

        Assert.Throws<UserException>(() =>
            User.Create(DefaultId, "Carlos", "García", longEmail,
                "hashed123", null, null, null));
    }

    [Fact]
    public void Create_WithInvalidEmailFormat_ThrowsUserException()
    {
        Assert.Throws<UserException>(() =>
            User.Create(DefaultId, "Carlos", "García", "carlosnoarroba",
                "hashed123", null, null, null));
    }

    [Fact]
    public void Create_WithInvalidEmailDomain_ThrowsUserException()
    {
        Assert.Throws<UserException>(() =>
            User.Create(DefaultId, "Carlos", "García", "carlos@domainwithoutdot",
                "hashed123", null, null, null));
    }

    [Fact]
    public void Create_NormalizesEmail()
    {
        var user = User.Create(
            DefaultId, "Carlos", "García", "  Carlos@Example.COM  ",
            "hashed123", null, null, null);

        Assert.Equal("carlos@example.com", user.Email);
    }

    [Fact]
    public void Create_WithEmptyPasswordHash_ThrowsUserException()
    {
        Assert.Throws<UserException>(() =>
            User.Create(DefaultId, "Carlos", "García", "carlos@example.com",
                "", null, null, null));
    }

    [Fact]
    public void Create_WithPasswordHashTooLong_ThrowsUserException()
    {
        string longHash = new('A', User.PasswordHashMaxLength + 1);

        Assert.Throws<UserException>(() =>
            User.Create(DefaultId, "Carlos", "García", "carlos@example.com",
                longHash, null, null, null));
    }

    [Fact]
    public void Create_WithNullPhone_SetsPhoneToNull()
    {
        var user = User.Create(
            DefaultId, "Carlos", "García", "carlos@example.com",
            "hashed123", null, null, null);

        Assert.Null(user.Phone);
    }

    [Fact]
    public void Create_WithValidPhone_SetsPhone()
    {
        var user = User.Create(
            DefaultId, "Carlos", "García", "carlos@example.com",
            "hashed123", "+573001234567", null, null);

        Assert.Equal("+573001234567", user.Phone);
    }

    [Fact]
    public void Create_WithPhoneTooLong_ThrowsUserException()
    {
        string longPhone = new('1', User.PhoneMaxLength + 1);

        Assert.Throws<UserException>(() =>
            User.Create(DefaultId, "Carlos", "García", "carlos@example.com",
                "hashed123", longPhone, null, null));
    }

    [Fact]
    public void Create_WithNullBirthDate_SetsBirthDateToNull()
    {
        var user = User.Create(
            DefaultId, "Carlos", "García", "carlos@example.com",
            "hashed123", null, null, null);

        Assert.Null(user.BirthDate);
    }

    [Fact]
    public void Create_WithValidBirthDate_SetsBirthDate()
    {
        var birthDate = new DateOnly(1990, 5, 15);

        var user = User.Create(
            DefaultId, "Carlos", "García", "carlos@example.com",
            "hashed123", null, birthDate, null);

        Assert.Equal(birthDate, user.BirthDate);
    }

    [Fact]
    public void Create_WithNullPhotoUrl_SetsPhotoUrlToNull()
    {
        var user = User.Create(
            DefaultId, "Carlos", "García", "carlos@example.com",
            "hashed123", null, null, null);

        Assert.Null(user.PhotoUrl);
    }

    [Fact]
    public void Create_WithPhotoUrlTooLong_ThrowsUserException()
    {
        string longUrl = "https://example.com/" + new string('a', User.PhotoUrlMaxLength);

        Assert.Throws<UserException>(() =>
            User.Create(DefaultId, "Carlos", "García", "carlos@example.com",
                "hashed123", null, null, longUrl));
    }

    [Fact]
    public void Create_WithWhitespacePhone_SetsPhoneToNull()
    {
        var user = User.Create(
            DefaultId, "Carlos", "García", "carlos@example.com",
            "hashed123", "   ", null, null);

        Assert.Null(user.Phone);
    }

    [Fact]
    public void Create_WithWhitespacePhotoUrl_SetsPhotoUrlToNull()
    {
        var user = User.Create(
            DefaultId, "Carlos", "García", "carlos@example.com",
            "hashed123", null, null, "   ");

        Assert.Null(user.PhotoUrl);
    }
}
