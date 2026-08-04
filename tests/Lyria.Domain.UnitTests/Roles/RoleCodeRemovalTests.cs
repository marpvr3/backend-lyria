using System.Reflection;
using Lyria.Domain.Roles;
using Xunit;

namespace Lyria.Domain.UnitTests.Roles;

/// <summary>
/// Verifica que el concepto de código quedó completamente eliminado del agregado Role.
/// </summary>
public sealed class RoleCodeRemovalTests
{
    [Fact]
    public void Role_DoesNotExposeCodeProperty()
    {
        PropertyInfo[] properties = typeof(Role).GetProperties(
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

        Assert.DoesNotContain(properties, p => p.Name.Contains("Code", StringComparison.Ordinal));
    }

    [Fact]
    public void Role_ExposesOnlyExpectedStateProperties()
    {
        string[] actual = typeof(Role)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Select(p => p.Name)
            .Where(name => name != "DomainEvents")
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        string[] expected =
        [
            "CreatedAtUtc",
            "Description",
            "Id",
            "IsActive",
            "Name",
            "UpdatedAtUtc"
        ];

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Create_DoesNotAcceptCodeParameter()
    {
        MethodInfo factory = typeof(Role).GetMethod(
            nameof(Role.Create),
            BindingFlags.Public | BindingFlags.Static)!;

        string[] parameters = factory.GetParameters().Select(p => p.Name!).ToArray();

        Assert.Equal(["id", "name", "description"], parameters);
    }

    [Fact]
    public void Update_DoesNotAcceptCodeParameter()
    {
        MethodInfo update = typeof(Role).GetMethod(
            nameof(Role.Update),
            BindingFlags.Public | BindingFlags.Instance)!;

        string[] parameters = update.GetParameters().Select(p => p.Name!).ToArray();

        Assert.Equal(["name", "description"], parameters);
    }

    [Fact]
    public void Role_HasNoCodeRelatedMembers()
    {
        MemberInfo[] members = typeof(Role).GetMembers(
            BindingFlags.Public | BindingFlags.NonPublic |
            BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly);

        Assert.DoesNotContain(
            members,
            m => m.Name.Contains("Code", StringComparison.OrdinalIgnoreCase));
    }
}
