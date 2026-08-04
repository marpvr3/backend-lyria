using System.Reflection;
using Xunit;

namespace Lyria.ArchitectureTests;

/// <summary>
/// Valida estructuralmente que el concepto Code quedó eliminado del módulo de Roles
/// y que la identidad del rol sigue siendo RoleId.
/// </summary>
public class RoleCodeRemovalArchitectureTests
{
    private static readonly Assembly DomainAssembly = typeof(Domain.Abstractions.Entity<>).Assembly;
    private static readonly Assembly ApplicationAssembly = typeof(Application.DependencyInjection).Assembly;
    private static readonly Assembly InfrastructureAssembly = typeof(Infrastructure.DependencyInjection).Assembly;
    private static readonly Assembly ApiAssembly = typeof(Api.Program).Assembly;

    // --- Domain ---

    [Fact]
    public void Role_DoesNotDeclareCodeProperty()
    {
        Type role = DomainAssembly.GetTypes().First(t => t.Name == "Role");

        Assert.DoesNotContain(
            role.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance),
            p => p.Name.Contains("Code", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Role_DoesNotDeclareCodeRelatedMembers()
    {
        Type role = DomainAssembly.GetTypes().First(t => t.Name == "Role");

        MemberInfo[] members = role.GetMembers(
            BindingFlags.Public | BindingFlags.NonPublic |
            BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly);

        Assert.DoesNotContain(
            members,
            m => m.Name.Contains("Code", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Role_KeepsRoleIdAsIdentity()
    {
        Type role = DomainAssembly.GetTypes().First(t => t.Name == "Role");
        PropertyInfo id = role.GetProperty("Id")!;

        Assert.Equal("RoleId", id.PropertyType.Name);
    }

    // --- Application ---

    [Fact]
    public void RoleContracts_DoNotExposeCode()
    {
        foreach (string typeName in new[]
        {
            "CreateRoleCommand",
            "UpdateRoleCommand",
            "GetRolesQuery",
            "RoleListFilter",
            "RoleResponse",
            "RoleListItemResponse",
            "UserRoleResponse"
        })
        {
            Type type = ApplicationAssembly.GetTypes().First(t => t.Name == typeName);

            Assert.DoesNotContain(
                type.GetProperties(BindingFlags.Public | BindingFlags.Instance),
                p => p.Name.Contains("Code", StringComparison.OrdinalIgnoreCase));
        }
    }

    [Fact]
    public void RolePersistenceAbstractions_DoNotDeclareCodeLookups()
    {
        foreach (string typeName in new[] { "IRoleRepository", "IRoleReadService" })
        {
            Type type = ApplicationAssembly.GetTypes().First(t => t.Name == typeName);

            Assert.DoesNotContain(
                type.GetMethods(),
                m => m.Name.Contains("Code", StringComparison.OrdinalIgnoreCase));
        }
    }

    [Fact]
    public void RoleErrors_DoNotDeclareCodeAlreadyExists()
    {
        Type roleErrors = ApplicationAssembly.GetTypes().First(t => t.Name == "RoleErrors");

        Assert.DoesNotContain(
            roleErrors.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly),
            m => m.Name.Contains("Code", StringComparison.OrdinalIgnoreCase));
    }

    // --- Infrastructure ---

    [Fact]
    public void RoleImplementations_DoNotDeclareCodeLookups()
    {
        foreach (string typeName in new[] { "RoleRepository", "RoleReadService" })
        {
            Type type = InfrastructureAssembly.GetTypes().First(t => t.Name == typeName);

            Assert.DoesNotContain(
                type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly),
                m => m.Name.Contains("Code", StringComparison.OrdinalIgnoreCase));
        }
    }

    [Fact]
    public void RoleConfiguration_DoesNotMapCode()
    {
        Type configuration = InfrastructureAssembly.GetTypes()
            .First(t => t.Name == "RoleConfiguration");

        Assert.NotNull(configuration);

        // La ausencia de mapeo se valida sobre el modelo materializado en
        // Lyria.Infrastructure.IntegrationTests (RoleConfigurationTests);
        // aquí se garantiza que la clase no declara constantes de Code.
        Assert.DoesNotContain(
            configuration.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static),
            f => f.Name.Contains("Code", StringComparison.OrdinalIgnoreCase));
    }

    // --- API ---

    [Fact]
    public void RoleRequests_DoNotExposeCode()
    {
        foreach (string typeName in new[] { "CreateRoleRequest", "UpdateRoleRequest" })
        {
            Type type = ApiAssembly.GetTypes().First(t => t.Name == typeName);

            Assert.DoesNotContain(
                type.GetProperties(BindingFlags.Public | BindingFlags.Instance),
                p => p.Name.Contains("Code", StringComparison.OrdinalIgnoreCase));
        }
    }

    [Fact]
    public void CreateRoleRequest_ExposesExactlyNameAndDescription()
    {
        Type type = ApiAssembly.GetTypes().First(t => t.Name == "CreateRoleRequest");

        string[] properties = type
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Select(p => p.Name)
            .ToArray();

        Assert.Equal(["Name", "Description"], properties);
    }

    // --- UserRole ---

    [Fact]
    public void UserRole_ReferencesRoleByRoleId()
    {
        Type userRole = DomainAssembly.GetTypes().First(t => t.Name == "UserRole");
        PropertyInfo roleId = userRole.GetProperty("RoleId")!;

        Assert.NotNull(roleId);
        Assert.Equal("RoleId", roleId.PropertyType.Name);

        Assert.DoesNotContain(
            userRole.GetProperties(BindingFlags.Public | BindingFlags.Instance),
            p => p.Name.Contains("Code", StringComparison.OrdinalIgnoreCase));
    }

    // --- Migración ---

    [Fact]
    public void Migration_RemoveCodeFromRoles_Exists()
    {
        Assert.Contains(
            InfrastructureAssembly.GetTypes(),
            t => t.Name == "RemoveCodeFromRoles");
    }
}
