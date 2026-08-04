using Lyria.Domain.Roles;
using Xunit;

namespace Lyria.Domain.UnitTests.Roles;

public sealed class RoleCreateTests
{
    private static readonly RoleId DefaultId = RoleId.New();

    [Fact]
    public void Create_WithValidData_CreatesRole()
    {
        var role = Role.Create(DefaultId, "Administrador", "Rol de administración");

        Assert.Equal(DefaultId, role.Id);
        Assert.Equal("Administrador", role.Name);
        Assert.Equal("Rol de administración", role.Description);
        Assert.True(role.IsActive);
    }

    [Fact]
    public void Create_WithEmptyName_ThrowsRoleException()
    {
        Assert.Throws<RoleException>(() =>
            Role.Create(DefaultId, "", null));
    }

    [Fact]
    public void Create_WithNameTooShort_ThrowsRoleException()
    {
        Assert.Throws<RoleException>(() =>
            Role.Create(DefaultId, "A", null));
    }

    [Fact]
    public void Create_WithNameTooLong_ThrowsRoleException()
    {
        string longName = new('A', Role.NameMaxLength + 1);

        Assert.Throws<RoleException>(() =>
            Role.Create(DefaultId, longName, null));
    }

    [Fact]
    public void Create_NormalizesName()
    {
        var role = Role.Create(DefaultId, "  Super   Admin  ", null);

        Assert.Equal("Super Admin", role.Name);
    }

    [Fact]
    public void Create_WithNullDescription_SetsDescriptionToNull()
    {
        var role = Role.Create(DefaultId, "Administrador", null);

        Assert.Null(role.Description);
    }

    [Fact]
    public void Create_WithValidDescription_SetsDescription()
    {
        var role = Role.Create(DefaultId, "Administrador", "Rol con todos los permisos");

        Assert.Equal("Rol con todos los permisos", role.Description);
    }

    [Fact]
    public void Create_WithDescriptionTooLong_ThrowsRoleException()
    {
        string longDesc = new('A', Role.DescriptionMaxLength + 1);

        Assert.Throws<RoleException>(() =>
            Role.Create(DefaultId, "Administrador", longDesc));
    }
}
