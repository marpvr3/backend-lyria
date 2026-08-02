using Lyria.Domain.Users;
using Xunit;

namespace Lyria.Domain.UnitTests.Users;

public sealed class UserChangeStatusTests
{
    [Fact]
    public void ChangeStatus_UnverifiedToActive_Succeeds()
    {
        var user = CreateUser();

        user.ChangeStatus(UserStatus.Active);

        Assert.Equal(UserStatus.Active, user.Status);
    }

    [Fact]
    public void ChangeStatus_ActiveToSuspended_Succeeds()
    {
        var user = CreateUser();
        user.ChangeStatus(UserStatus.Active);

        user.ChangeStatus(UserStatus.Suspended);

        Assert.Equal(UserStatus.Suspended, user.Status);
    }

    [Fact]
    public void ChangeStatus_SuspendedToDeleted_Succeeds()
    {
        var user = CreateUser();
        user.ChangeStatus(UserStatus.Active);
        user.ChangeStatus(UserStatus.Suspended);

        user.ChangeStatus(UserStatus.Deleted);

        Assert.Equal(UserStatus.Deleted, user.Status);
    }

    [Fact]
    public void ChangeStatus_UnverifiedToSuspended_ThrowsUserException()
    {
        var user = CreateUser();

        Assert.Throws<UserException>(() =>
            user.ChangeStatus(UserStatus.Suspended));
    }

    [Fact]
    public void ChangeStatus_UnverifiedToDeleted_ThrowsUserException()
    {
        var user = CreateUser();

        Assert.Throws<UserException>(() =>
            user.ChangeStatus(UserStatus.Deleted));
    }

    [Fact]
    public void ChangeStatus_ActiveToUnverified_ThrowsUserException()
    {
        var user = CreateUser();
        user.ChangeStatus(UserStatus.Active);

        Assert.Throws<UserException>(() =>
            user.ChangeStatus(UserStatus.Unverified));
    }

    [Fact]
    public void ChangeStatus_ActiveToDeleted_ThrowsUserException()
    {
        var user = CreateUser();
        user.ChangeStatus(UserStatus.Active);

        Assert.Throws<UserException>(() =>
            user.ChangeStatus(UserStatus.Deleted));
    }

    [Fact]
    public void ChangeStatus_SuspendedToActive_ThrowsUserException()
    {
        var user = CreateUser();
        user.ChangeStatus(UserStatus.Active);
        user.ChangeStatus(UserStatus.Suspended);

        Assert.Throws<UserException>(() =>
            user.ChangeStatus(UserStatus.Active));
    }

    [Fact]
    public void ChangeStatus_DeletedToUnverified_ThrowsUserException()
    {
        var user = CreateUser();
        user.ChangeStatus(UserStatus.Active);
        user.ChangeStatus(UserStatus.Suspended);
        user.ChangeStatus(UserStatus.Deleted);

        Assert.Throws<UserException>(() =>
            user.ChangeStatus(UserStatus.Unverified));
    }

    [Fact]
    public void ChangeStatus_DeletedToActive_ThrowsUserException()
    {
        var user = CreateUser();
        user.ChangeStatus(UserStatus.Active);
        user.ChangeStatus(UserStatus.Suspended);
        user.ChangeStatus(UserStatus.Deleted);

        Assert.Throws<UserException>(() =>
            user.ChangeStatus(UserStatus.Active));
    }

    [Fact]
    public void ChangeStatus_DeletedToSuspended_ThrowsUserException()
    {
        var user = CreateUser();
        user.ChangeStatus(UserStatus.Active);
        user.ChangeStatus(UserStatus.Suspended);
        user.ChangeStatus(UserStatus.Deleted);

        Assert.Throws<UserException>(() =>
            user.ChangeStatus(UserStatus.Suspended));
    }

    [Fact]
    public void ChangeStatus_SameStatus_ThrowsUserException()
    {
        var user = CreateUser();

        Assert.Throws<UserException>(() =>
            user.ChangeStatus(UserStatus.Unverified));
    }

    private static User CreateUser() =>
        User.Create(
            UserId.New(), "Carlos", "García", "carlos@example.com",
            "hashed123", null, null, null);
}
