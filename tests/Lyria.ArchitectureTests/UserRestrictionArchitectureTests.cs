using System.Reflection;
using Xunit;

namespace Lyria.ArchitectureTests;

public class UserRestrictionArchitectureTests
{
    private static readonly Assembly DomainAssembly = typeof(Domain.Abstractions.Entity<>).Assembly;
    private static readonly Assembly ApplicationAssembly = typeof(Application.DependencyInjection).Assembly;
    private static readonly Assembly InfrastructureAssembly = typeof(Infrastructure.DependencyInjection).Assembly;
    private static readonly Assembly ApiAssembly = typeof(Api.Program).Assembly;

    // --- Domain ---

    [Fact]
    public void UserRestriction_IsInDomainLayer()
    {
        var type = DomainAssembly.GetTypes()
            .FirstOrDefault(t => t.Name == "UserRestriction");

        Assert.NotNull(type);
        Assert.Equal("Lyria.Domain.Users.UserRestrictions", type.Namespace);
    }

    [Fact]
    public void UserRestrictionImportanceLevels_IsInDomainLayer()
    {
        var type = DomainAssembly.GetTypes()
            .FirstOrDefault(t => t.Name == "UserRestrictionImportanceLevels");

        Assert.NotNull(type);
        Assert.Equal("Lyria.Domain.Users.UserRestrictions", type.Namespace);
    }

    [Fact]
    public void UserRestriction_HasNoPublicConstructors()
    {
        var type = DomainAssembly.GetTypes()
            .First(t => t.Name == "UserRestriction");

        Assert.Empty(type.GetConstructors(BindingFlags.Public | BindingFlags.Instance));
    }

    [Fact]
    public void UserRestriction_IsNotDefinedOutsideDomain()
    {
        foreach (Assembly assembly in new[]
        {
            ApplicationAssembly, InfrastructureAssembly, ApiAssembly
        })
        {
            Assert.DoesNotContain(
                assembly.GetTypes(),
                t => t.Name == "UserRestriction");
        }
    }

    // --- Application ---

    [Fact]
    public void UserRestrictionPersistenceAbstractions_AreInApplicationLayer()
    {
        foreach (string interfaceName in new[]
        {
            "IUserRestrictionRepository", "IUserRestrictionReadService"
        })
        {
            var type = ApplicationAssembly.GetTypes()
                .FirstOrDefault(t => t.Name == interfaceName);

            Assert.NotNull(type);
            Assert.True(type.IsInterface);
            Assert.Equal("Lyria.Application.Abstractions.Persistence", type.Namespace);
        }
    }

    [Fact]
    public void UserRestrictionFeatures_ResideIn_FeaturesNamespace()
    {
        var featureTypes = ApplicationAssembly.GetTypes()
            .Where(t => t.Name.Contains("UserRestriction", StringComparison.Ordinal) &&
                        !t.Name.Contains("Repository", StringComparison.Ordinal) &&
                        !t.Name.Contains("ReadService", StringComparison.Ordinal))
            .ToList();

        Assert.NotEmpty(featureTypes);
        Assert.All(featureTypes, t =>
            Assert.StartsWith("Lyria.Application.Features.UserRestrictions", t.Namespace!));
    }

    [Fact]
    public void UserRestrictionQueryHandlers_DoNotDependOn_WriteRepositories()
    {
        var queryHandlers = ApplicationAssembly.GetTypes()
            .Where(t => t.Name.Contains("UserRestriction", StringComparison.Ordinal) &&
                        t.Name.EndsWith("QueryHandler", StringComparison.Ordinal))
            .ToList();

        Assert.NotEmpty(queryHandlers);

        foreach (var handler in queryHandlers)
        {
            foreach (var ctor in handler.GetConstructors())
            {
                Assert.DoesNotContain(ctor.GetParameters(), p =>
                    p.ParameterType.Name.Contains("Repository", StringComparison.Ordinal));
            }
        }
    }

    // --- Infrastructure ---

    [Fact]
    public void UserRestrictionPersistenceImplementations_AreInInfrastructureLayer()
    {
        foreach (string typeName in new[]
        {
            "UserRestrictionRepository", "UserRestrictionReadService", "UserRestrictionConfiguration"
        })
        {
            var type = InfrastructureAssembly.GetTypes()
                .FirstOrDefault(t => t.Name == typeName);

            Assert.NotNull(type);
            Assert.False(type.IsInterface);
            Assert.StartsWith("Lyria.Infrastructure.Persistence", type.Namespace!);
        }
    }

    // --- API ---

    [Fact]
    public void UserRestrictionsController_IsInApiLayer()
    {
        var type = ApiAssembly.GetTypes()
            .FirstOrDefault(t => t.Name == "UserRestrictionsController");

        Assert.NotNull(type);
        Assert.StartsWith("Lyria.Api.Controllers", type.Namespace!);
    }

    [Fact]
    public void UserRestrictionsController_DoesNotDependOn_DomainEntities()
    {
        var controller = ApiAssembly.GetTypes()
            .First(t => t.Name == "UserRestrictionsController");

        var domainEntityNames = new HashSet<string>(StringComparer.Ordinal)
        {
            "User", "Restriction", "UserRestriction"
        };

        foreach (var method in controller.GetMethods(
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
        {
            Assert.DoesNotContain(domainEntityNames, name =>
                method.ReturnType.FullName?.StartsWith("Lyria.Domain.", StringComparison.Ordinal) == true &&
                method.ReturnType.Name == name);

            foreach (var parameter in method.GetParameters())
            {
                Assert.DoesNotContain(domainEntityNames, name =>
                    parameter.ParameterType.FullName?.StartsWith("Lyria.Domain.", StringComparison.Ordinal) == true &&
                    parameter.ParameterType.Name == name);
            }
        }

        foreach (var ctor in controller.GetConstructors())
        {
            foreach (var parameter in ctor.GetParameters())
            {
                Assert.DoesNotContain(domainEntityNames, name =>
                    parameter.ParameterType.FullName?.StartsWith("Lyria.Domain.", StringComparison.Ordinal) == true &&
                    parameter.ParameterType.Name == name);
            }
        }
    }

    [Fact]
    public void UserRestrictionsController_DoesNotInject_Repositories()
    {
        var controller = ApiAssembly.GetTypes()
            .First(t => t.Name == "UserRestrictionsController");

        foreach (var ctor in controller.GetConstructors())
        {
            Assert.DoesNotContain(ctor.GetParameters(), p =>
                p.ParameterType.Name.Contains("Repository", StringComparison.Ordinal) ||
                p.ParameterType.Name.EndsWith("DbContext", StringComparison.Ordinal));
        }
    }

    [Fact]
    public void UserRestrictionsController_HasExactlyFiveEndpoints()
    {
        var controller = ApiAssembly.GetTypes()
            .First(t => t.Name == "UserRestrictionsController");

        var httpAttributeNames = new HashSet<string>(StringComparer.Ordinal)
        {
            "HttpGetAttribute", "HttpPostAttribute", "HttpPutAttribute",
            "HttpPatchAttribute", "HttpDeleteAttribute"
        };

        var actions = controller
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(m => m.GetCustomAttributes(true).Any(a =>
                httpAttributeNames.Contains(a.GetType().Name)))
            .ToList();

        Assert.Equal(5, actions.Count);
    }
}
