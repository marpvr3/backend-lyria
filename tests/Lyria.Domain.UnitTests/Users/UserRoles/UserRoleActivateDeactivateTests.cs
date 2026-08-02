using Lyria.Domain.Roles;
using Lyria.Domain.Users;
using Lyria.Domain.Users.UserRoles;
using Xunit;

namespace Lyria.Domain.UnitTests.Users.UserRoles;

public sealed class UserRoleActivateDeactivateTests
{
    [Fact]
    public void Activate_WhenInactive_Activates()
    {
        var userRole = CreateUserRole();
        userRole.Deactivate();

        userRole.Activate();

        Assert.True(userRole.IsActive);
    }

    [Fact]
    public void Activate_WhenAlreadyActive_ThrowsUserRoleException()
    {
        var userRole = CreateUserRole();

        Assert.Throws<UserRoleException>(() =>
            userRole.Activate());
    }

    [Fact]
    public void Deactivate_WhenActive_Deactivates()
    {
        var userRole = CreateUserRole();

        userRole.Deactivate();

        Assert.False(userRole.IsActive);
    }

    [Fact]
    public void Deactivate_WhenAlreadyInactive_ThrowsUserRoleException()
    {
        var userRole = CreateUserRole();
        userRole.Deactivate();

        Assert.Throws<UserRoleException>(() =>
            userRole.Deactivate());
    }

    private static UserRole CreateUserRole() =>
        UserRole.Assign(
            UserRoleId.New(), UserId.New(), RoleId.New(),
            ScopeType.Global, null, null,
            new DateTime(2026, 7, 15, 12, 0, 0, DateTimeKind.Utc));
}
