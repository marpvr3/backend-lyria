using Lyria.Domain.Roles;
using Xunit;

namespace Lyria.Domain.UnitTests.Roles;

public sealed class RoleActivateDeactivateTests
{
    [Fact]
    public void Activate_WhenInactive_Activates()
    {
        var role = CreateRole();
        role.Deactivate();

        role.Activate();

        Assert.True(role.IsActive);
    }

    [Fact]
    public void Activate_WhenAlreadyActive_ThrowsRoleException()
    {
        var role = CreateRole();

        Assert.Throws<RoleException>(() =>
            role.Activate());
    }

    [Fact]
    public void Deactivate_WhenActive_Deactivates()
    {
        var role = CreateRole();

        role.Deactivate();

        Assert.False(role.IsActive);
    }

    [Fact]
    public void Deactivate_WhenAlreadyInactive_ThrowsRoleException()
    {
        var role = CreateRole();
        role.Deactivate();

        Assert.Throws<RoleException>(() =>
            role.Deactivate());
    }

    private static Role CreateRole() =>
        Role.Create(RoleId.New(), "Administrador", null);
}
