using Lyria.Domain.Roles;
using Xunit;

namespace Lyria.Domain.UnitTests.Roles;

public sealed class RoleUpdateTests
{
    [Fact]
    public void Update_WithValidData_UpdatesNameAndDescription()
    {
        var role = CreateRole();

        role.Update("Nuevo Nombre", "Nueva descripción");

        Assert.Equal("Nuevo Nombre", role.Name);
        Assert.Equal("Nueva descripción", role.Description);
    }

    [Fact]
    public void Update_DoesNotChangeIdentity()
    {
        var role = CreateRole();
        RoleId originalId = role.Id;

        role.Update("Nuevo Nombre", null);

        Assert.Equal(originalId, role.Id);
    }

    [Fact]
    public void Update_WithEmptyName_ThrowsRoleException()
    {
        var role = CreateRole();

        Assert.Throws<RoleException>(() =>
            role.Update("", null));
    }

    [Fact]
    public void Update_NormalizesName()
    {
        var role = CreateRole();

        role.Update("  Super   Admin  ", null);

        Assert.Equal("Super Admin", role.Name);
    }

    [Fact]
    public void Update_WithNullDescription_SetsNull()
    {
        var role = CreateRole();

        role.Update("Administrador", null);

        Assert.Null(role.Description);
    }

    private static Role CreateRole() =>
        Role.Create(RoleId.New(), "Administrador", "Descripción original");
}
