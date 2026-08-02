using Lyria.Domain.Roles;
using Lyria.Domain.Users;
using Lyria.Domain.Users.UserRoles;
using Xunit;

namespace Lyria.Domain.UnitTests.Users.UserRoles;

public sealed class UserRoleFinalizeTests
{
    [Fact]
    public void Finalize_SetsEndedAtUtcAndDeactivates()
    {
        var userRole = CreateUserRole();
        var endedAt = new DateTime(2026, 7, 25, 18, 0, 0, DateTimeKind.Utc);

        userRole.Finalize(endedAt);

        Assert.Equal(endedAt, userRole.EndedAtUtc);
        Assert.False(userRole.IsActive);
    }

    [Fact]
    public void Finalize_WhenAlreadyFinalized_ThrowsUserRoleException()
    {
        var userRole = CreateUserRole();
        var endedAt = new DateTime(2026, 7, 25, 18, 0, 0, DateTimeKind.Utc);
        userRole.Finalize(endedAt);

        Assert.Throws<UserRoleException>(() =>
            userRole.Finalize(new DateTime(2026, 7, 26, 18, 0, 0, DateTimeKind.Utc)));
    }

    private static UserRole CreateUserRole() =>
        UserRole.Assign(
            UserRoleId.New(), UserId.New(), RoleId.New(),
            ScopeType.Global, null, null,
            new DateTime(2026, 7, 15, 12, 0, 0, DateTimeKind.Utc));
}
