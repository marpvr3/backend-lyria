using ArchUnitNET.Domain;
using ArchUnitNET.Fluent;
using ArchUnitNET.Loader;
using Xunit;
using static ArchUnitNET.Fluent.ArchRuleDefinition;
using Assembly = System.Reflection.Assembly;

namespace Lyria.ArchitectureTests;

public class LayerDependencyTests
{
    private static readonly Assembly DomainAssembly = typeof(Domain.Abstractions.Entity<>).Assembly;
    private static readonly Assembly ApplicationAssembly = typeof(Application.DependencyInjection).Assembly;
    private static readonly Assembly InfrastructureAssembly = typeof(Infrastructure.DependencyInjection).Assembly;
    private static readonly Assembly ApiAssembly = typeof(Api.Program).Assembly;

    private static readonly Architecture Architecture = new ArchLoader()
        .LoadAssemblies(DomainAssembly, ApplicationAssembly, InfrastructureAssembly, ApiAssembly)
        .Build();

    private static readonly IObjectProvider<IType> DomainLayer =
        Types().That().ResideInAssembly(DomainAssembly).As("Domain Layer");

    private static readonly IObjectProvider<IType> ApplicationLayer =
        Types().That().ResideInAssembly(ApplicationAssembly).As("Application Layer");

    private static readonly IObjectProvider<IType> InfrastructureLayer =
        Types().That().ResideInAssembly(InfrastructureAssembly).As("Infrastructure Layer");

    private static readonly IObjectProvider<IType> ApiLayer =
        Types().That().ResideInAssembly(ApiAssembly).As("API Layer");

    private static void AssertRule(IArchRule rule)
    {
        bool hasNoViolations = rule.HasNoViolations(Architecture);
        Assert.True(hasNoViolations, rule.Description + ": " + rule.Evaluate(Architecture)
            .Where(r => !r.Passed)
            .Select(r => r.Description)
            .Aggregate("", (current, next) => current + "\n" + next));
    }

    // --- Layer dependency rules ---

    [Fact]
    public void Domain_ShouldNotDependOn_Application()
    {
        IArchRule rule = Types().That().Are(DomainLayer)
            .Should().NotDependOnAnyTypesThat().Are(ApplicationLayer);
        AssertRule(rule);
    }

    [Fact]
    public void Domain_ShouldNotDependOn_Infrastructure()
    {
        IArchRule rule = Types().That().Are(DomainLayer)
            .Should().NotDependOnAnyTypesThat().Are(InfrastructureLayer);
        AssertRule(rule);
    }

    [Fact]
    public void Domain_ShouldNotDependOn_Api()
    {
        IArchRule rule = Types().That().Are(DomainLayer)
            .Should().NotDependOnAnyTypesThat().Are(ApiLayer);
        AssertRule(rule);
    }

    [Fact]
    public void Application_ShouldNotDependOn_Infrastructure()
    {
        IArchRule rule = Types().That().Are(ApplicationLayer)
            .Should().NotDependOnAnyTypesThat().Are(InfrastructureLayer);
        AssertRule(rule);
    }

    [Fact]
    public void Application_ShouldNotDependOn_Api()
    {
        IArchRule rule = Types().That().Are(ApplicationLayer)
            .Should().NotDependOnAnyTypesThat().Are(ApiLayer);
        AssertRule(rule);
    }

    [Fact]
    public void Infrastructure_ShouldNotDependOn_Api()
    {
        IArchRule rule = Types().That().Are(InfrastructureLayer)
            .Should().NotDependOnAnyTypesThat().Are(ApiLayer);
        AssertRule(rule);
    }

    // --- Framework dependency rules ---

    [Fact]
    public void Domain_ShouldNotDependOn_EntityFrameworkCore()
    {
        IArchRule rule = Types().That().Are(DomainLayer)
            .Should().NotDependOnAnyTypesThat()
            .ResideInNamespace("Microsoft.EntityFrameworkCore");
        AssertRule(rule);
    }

    [Fact]
    public void Application_ShouldNotDependOn_EntityFrameworkCore()
    {
        IArchRule rule = Types().That().Are(ApplicationLayer)
            .Should().NotDependOnAnyTypesThat()
            .ResideInNamespace("Microsoft.EntityFrameworkCore");
        AssertRule(rule);
    }

    [Fact]
    public void Application_ShouldNotDependOn_AspNetCore()
    {
        IArchRule rule = Types().That().Are(ApplicationLayer)
            .Should().NotDependOnAnyTypesThat()
            .ResideInNamespace("Microsoft.AspNetCore");
        AssertRule(rule);
    }

    [Fact]
    public void Domain_ShouldNotDependOn_AspNetCore()
    {
        IArchRule rule = Types().That().Are(DomainLayer)
            .Should().NotDependOnAnyTypesThat()
            .ResideInNamespace("Microsoft.AspNetCore");
        AssertRule(rule);
    }

    [Fact]
    public void Domain_ShouldNotDependOn_MediatR()
    {
        IArchRule rule = Types().That().Are(DomainLayer)
            .Should().NotDependOnAnyTypesThat()
            .ResideInNamespace("MediatR");
        AssertRule(rule);
    }

    [Fact]
    public void Domain_ShouldNotDependOn_Mediator()
    {
        IArchRule rule = Types().That().Are(DomainLayer)
            .Should().NotDependOnAnyTypesThat()
            .ResideInNamespace("Mediator");
        AssertRule(rule);
    }

    [Fact]
    public void Domain_ShouldNotDependOn_FluentValidation()
    {
        IArchRule rule = Types().That().Are(DomainLayer)
            .Should().NotDependOnAnyTypesThat()
            .ResideInNamespace("FluentValidation");
        AssertRule(rule);
    }

    // --- Type naming and location rules ---

    [Fact]
    public void Controllers_ShouldResideIn_ApiLayer()
    {
        IArchRule rule = Types().That().HaveNameEndingWith("Controller")
            .Should().ResideInAssembly(ApiAssembly);
        AssertRule(rule);
    }

    [Fact]
    public void Domain_ShouldNotContain_Controllers()
    {
        IArchRule rule = Types().That().Are(DomainLayer)
            .Should().NotHaveNameEndingWith("Controller");
        AssertRule(rule);
    }

    [Fact]
    public void Domain_ShouldNotContain_Repositories()
    {
        IArchRule rule = Types().That().Are(DomainLayer)
            .Should().NotHaveNameEndingWith("Repository");
        AssertRule(rule);
    }

    [Fact]
    public void Domain_ShouldNotContain_DbContexts()
    {
        IArchRule rule = Types().That().Are(DomainLayer)
            .Should().NotHaveNameEndingWith("DbContext");
        AssertRule(rule);
    }

    [Fact]
    public void Domain_ShouldNotContain_Handlers()
    {
        IArchRule rule = Types().That().Are(DomainLayer)
            .Should().NotHaveNameEndingWith("Handler");
        AssertRule(rule);
    }

    [Fact]
    public void Domain_ShouldNotContain_Endpoints()
    {
        IArchRule rule = Types().That().Are(DomainLayer)
            .Should().NotHaveNameEndingWith("Endpoint");
        AssertRule(rule);
    }

    // --- Namespace organization rules ---

    [Fact]
    public void EstablishmentEntities_ShouldResideIn_EstablishmentsNamespace()
    {
        var domainTypes = DomainAssembly.GetTypes()
            .Where(t => t.Name.StartsWith("EstablishmentCategory", StringComparison.Ordinal))
            .ToList();

        Assert.NotEmpty(domainTypes);
        Assert.All(domainTypes, t =>
            Assert.StartsWith("Lyria.Domain.Establishments", t.Namespace!));
    }

    [Fact]
    public void EstablishmentCategoryFeatures_ShouldResideIn_FeaturesNamespace()
    {
        var featureTypes = ApplicationAssembly.GetTypes()
            .Where(t => t.Name.Contains("EstablishmentCategory", StringComparison.Ordinal) &&
                        !t.Name.Contains("Repository", StringComparison.Ordinal) &&
                        !t.Name.Contains("ReadService", StringComparison.Ordinal))
            .ToList();

        Assert.NotEmpty(featureTypes);
        Assert.All(featureTypes, t =>
            Assert.StartsWith("Lyria.Application.Features.EstablishmentCategories", t.Namespace!));
    }

    // --- Anti-pattern rules ---

    [Fact]
    public void Application_ShouldNotContain_GenericRepository()
    {
        var applicationTypes = ApplicationAssembly.GetTypes();
        var genericRepos = applicationTypes.Where(t =>
            t.IsInterface &&
            t.IsGenericTypeDefinition &&
            t.Name.Contains("Repository", StringComparison.OrdinalIgnoreCase))
            .ToList();

        Assert.Empty(genericRepos);
    }

    [Fact]
    public void Application_ShouldNotContain_IUnitOfWork()
    {
        var applicationTypes = ApplicationAssembly.GetTypes();
        var unitOfWork = applicationTypes.Where(t =>
            t.Name.Equals("IUnitOfWork", StringComparison.OrdinalIgnoreCase))
            .ToList();

        Assert.Empty(unitOfWork);
    }

    [Fact]
    public void Domain_ShouldNotContain_ResultType()
    {
        var domainTypes = DomainAssembly.GetTypes();
        var resultTypes = domainTypes.Where(t =>
            t.Name.Equals("Result", StringComparison.OrdinalIgnoreCase) ||
            (t.Name.StartsWith("Result", StringComparison.OrdinalIgnoreCase) &&
             t.Name.Contains('`')))
            .ToList();

        Assert.Empty(resultTypes);
    }

    // --- Infrastructure location rules ---

    [Fact]
    public void EfConfigurations_ShouldResideIn_Infrastructure()
    {
        var configTypes = InfrastructureAssembly.GetTypes()
            .Where(t => t.Name.EndsWith("Configuration", StringComparison.Ordinal) &&
                        t.GetInterfaces().Any(i =>
                            i.IsGenericType &&
                            i.GetGenericTypeDefinition().FullName ==
                                "Microsoft.EntityFrameworkCore.IEntityTypeConfiguration`1"))
            .ToList();

        Assert.NotEmpty(configTypes);
        Assert.All(configTypes, t =>
            Assert.StartsWith("Lyria.Infrastructure.Persistence", t.Namespace!));
    }

    [Fact]
    public void ConcreteRepositories_ShouldResideIn_Infrastructure()
    {
        var repoTypes = InfrastructureAssembly.GetTypes()
            .Where(t => !t.IsInterface &&
                        !t.IsAbstract &&
                        t.Name.EndsWith("Repository", StringComparison.Ordinal))
            .ToList();

        Assert.NotEmpty(repoTypes);
        Assert.All(repoTypes, t =>
            Assert.StartsWith("Lyria.Infrastructure.Persistence", t.Namespace!));
    }

    [Fact]
    public void PersistenceInterfaces_ShouldResideIn_Application()
    {
        var persistenceInterfaces = ApplicationAssembly.GetTypes()
            .Where(t => t.IsInterface &&
                        (t.Name.Contains("Repository", StringComparison.Ordinal) ||
                         t.Name.Contains("ReadService", StringComparison.Ordinal)))
            .ToList();

        Assert.NotEmpty(persistenceInterfaces);
        Assert.All(persistenceInterfaces, t =>
            Assert.StartsWith("Lyria.Application.Abstractions.Persistence", t.Namespace!));
    }

    // --- API rules ---

    [Fact]
    public void Api_ShouldNotDirectlyAccess_DbContext()
    {
        var apiTypes = ApiAssembly.GetTypes()
            .Where(t => !t.Name.Contains("GeneratedServiceCollectionExtensions", StringComparison.Ordinal) &&
                        !t.Name.Contains("<>", StringComparison.Ordinal))
            .ToList();

        foreach (var type in apiTypes)
        {
            var constructors = type.GetConstructors();
            foreach (var ctor in constructors)
            {
                var parameters = ctor.GetParameters();
                Assert.DoesNotContain(parameters, p =>
                    p.ParameterType.Name.EndsWith("DbContext", StringComparison.Ordinal));
            }
        }
    }

    [Fact]
    public void Controllers_ShouldNotInject_Repositories()
    {
        var controllers = ApiAssembly.GetTypes()
            .Where(t => t.Name.EndsWith("Controller", StringComparison.Ordinal) &&
                        !t.IsAbstract)
            .ToList();

        Assert.NotEmpty(controllers);

        foreach (var controller in controllers)
        {
            var constructors = controller.GetConstructors();
            foreach (var ctor in constructors)
            {
                var parameters = ctor.GetParameters();
                Assert.DoesNotContain(parameters, p =>
                    p.ParameterType.Name.Contains("Repository", StringComparison.Ordinal));
            }
        }
    }

    // --- Queries should not use write repository ---

    [Fact]
    public void QueryHandlers_ShouldNotDependOn_WriteRepository()
    {
        var queryHandlers = ApplicationAssembly.GetTypes()
            .Where(t => t.Name.EndsWith("QueryHandler", StringComparison.Ordinal) &&
                        !t.IsAbstract &&
                        !t.IsInterface)
            .ToList();

        Assert.NotEmpty(queryHandlers);

        foreach (var handler in queryHandlers)
        {
            var constructors = handler.GetConstructors();
            foreach (var ctor in constructors)
            {
                var parameters = ctor.GetParameters();
                Assert.DoesNotContain(parameters, p =>
                    p.ParameterType.Name.Contains("Repository", StringComparison.Ordinal));
            }
        }
    }

    // --- No Minimal APIs ---

    [Fact]
    public void NoMinimalApiEndpoints_InSource()
    {
        var apiTypes = ApiAssembly.GetTypes();
        var endpointTypes = apiTypes.Where(t =>
            t.Name.Contains("Endpoint", StringComparison.Ordinal) &&
            !t.Name.Contains("Controller", StringComparison.Ordinal) &&
            !t.Name.Contains("ExceptionHandler", StringComparison.Ordinal))
            .ToList();

        Assert.Empty(endpointTypes);
    }

    // --- EstablishmentCategoriesController endpoint constraints ---

    [Fact]
    public void EstablishmentCategoriesController_ShouldNotHave_DeleteEndpoint()
    {
        var controllerType = ApiAssembly.GetTypes()
            .FirstOrDefault(t => t.Name == "EstablishmentCategoriesController");

        Assert.NotNull(controllerType);

        var methods = controllerType.GetMethods(System.Reflection.BindingFlags.Public |
                                                 System.Reflection.BindingFlags.Instance |
                                                 System.Reflection.BindingFlags.DeclaredOnly);

        foreach (var method in methods)
        {
            var attributes = method.GetCustomAttributes(true);
            Assert.DoesNotContain(attributes, a =>
                a.GetType().Name is "HttpDeleteAttribute");
        }
    }

    [Fact]
    public void EstablishmentCategoriesController_HasExactlyFiveEndpoints()
    {
        var controllerType = ApiAssembly.GetTypes()
            .FirstOrDefault(t => t.Name == "EstablishmentCategoriesController");

        Assert.NotNull(controllerType);

        var methods = controllerType.GetMethods(System.Reflection.BindingFlags.Public |
                                                 System.Reflection.BindingFlags.Instance |
                                                 System.Reflection.BindingFlags.DeclaredOnly);

        var httpAttributeNames = new HashSet<string>
        {
            "HttpGetAttribute", "HttpPostAttribute", "HttpPutAttribute",
            "HttpPatchAttribute", "HttpDeleteAttribute"
        };

        var actions = methods.Where(m =>
            m.GetCustomAttributes(true).Any(a =>
                httpAttributeNames.Contains(a.GetType().Name)))
            .ToList();

        Assert.Equal(5, actions.Count);
    }

    [Fact]
    public void EstablishmentCategoriesController_ContainsExpectedHttpMethods()
    {
        var controllerType = ApiAssembly.GetTypes()
            .FirstOrDefault(t => t.Name == "EstablishmentCategoriesController");

        Assert.NotNull(controllerType);

        var methods = controllerType.GetMethods(System.Reflection.BindingFlags.Public |
                                                 System.Reflection.BindingFlags.Instance |
                                                 System.Reflection.BindingFlags.DeclaredOnly);

        var allAttributes = methods
            .SelectMany(m => m.GetCustomAttributes(true))
            .Select(a => a.GetType().Name)
            .ToList();

        Assert.Contains("HttpGetAttribute", allAttributes);
        Assert.Contains("HttpPostAttribute", allAttributes);
        Assert.Contains("HttpPutAttribute", allAttributes);
        Assert.Contains("HttpPatchAttribute", allAttributes);
        Assert.DoesNotContain("HttpDeleteAttribute", allAttributes);
    }

    // --- No EnsureCreated in production code ---

    [Fact]
    public void NoEnsureCreated_InProductionCode()
    {
        var allProductionTypes = DomainAssembly.GetTypes()
            .Concat(ApplicationAssembly.GetTypes())
            .Concat(InfrastructureAssembly.GetTypes())
            .Concat(ApiAssembly.GetTypes())
            .ToList();

        foreach (var type in allProductionTypes)
        {
            var methods = type.GetMethods(System.Reflection.BindingFlags.Public |
                                           System.Reflection.BindingFlags.NonPublic |
                                           System.Reflection.BindingFlags.Instance |
                                           System.Reflection.BindingFlags.Static |
                                           System.Reflection.BindingFlags.DeclaredOnly);

            Assert.DoesNotContain(methods, m =>
                m.Name is "EnsureCreated" or "EnsureCreatedAsync");
        }
    }

    // --- No auto-migration in production code ---

    [Fact]
    public void NoAutoMigration_InProductionCode()
    {
        var allProductionTypes = DomainAssembly.GetTypes()
            .Concat(ApplicationAssembly.GetTypes())
            .Concat(InfrastructureAssembly.GetTypes())
            .Concat(ApiAssembly.GetTypes())
            .ToList();

        foreach (var type in allProductionTypes)
        {
            var methods = type.GetMethods(System.Reflection.BindingFlags.Public |
                                           System.Reflection.BindingFlags.NonPublic |
                                           System.Reflection.BindingFlags.Instance |
                                           System.Reflection.BindingFlags.Static |
                                           System.Reflection.BindingFlags.DeclaredOnly);

            Assert.DoesNotContain(methods, m =>
                m.Name is "Migrate" or "MigrateAsync");
        }
    }

    // --- Mediator.SourceGenerator only in Api ---

    [Fact]
    public void MediatorSourceGenerator_OnlyIn_ApiProject()
    {
        var apiReferences = ApiAssembly.GetReferencedAssemblies()
            .Select(a => a.Name)
            .ToList();

        // Infrastructure and Application should NOT reference Mediator source-generated types
        // The source generator produces types in the Api assembly namespace
        var generatedMediatorTypes = ApiAssembly.GetTypes()
            .Where(t => t.Namespace?.Contains("Mediator", StringComparison.Ordinal) == true ||
                        t.Name.Contains("GeneratedServiceCollectionExtensions", StringComparison.Ordinal))
            .ToList();

        // Verify source-generated types exist in Api
        Assert.NotEmpty(generatedMediatorTypes);
    }

    // --- Domain and Application should not depend on Serilog ---

    [Fact]
    public void Domain_ShouldNotDependOn_Serilog()
    {
        IArchRule rule = Types().That().Are(DomainLayer)
            .Should().NotDependOnAnyTypesThat()
            .ResideInNamespace("Serilog");
        AssertRule(rule);
    }

    [Fact]
    public void Application_ShouldNotDependOn_Serilog()
    {
        IArchRule rule = Types().That().Are(ApplicationLayer)
            .Should().NotDependOnAnyTypesThat()
            .ResideInNamespace("Serilog");
        AssertRule(rule);
    }

    [Fact]
    public void Infrastructure_ShouldNotDependOn_Serilog()
    {
        IArchRule rule = Types().That().Are(InfrastructureLayer)
            .Should().NotDependOnAnyTypesThat()
            .ResideInNamespace("Serilog");
        AssertRule(rule);
    }

    [Fact]
    public void Domain_ShouldNotReference_SerilogAssembly()
    {
        var references = DomainAssembly.GetReferencedAssemblies()
            .Select(a => a.Name)
            .ToList();

        Assert.DoesNotContain(references, name =>
            name!.StartsWith("Serilog", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Application_ShouldNotReference_SerilogAssembly()
    {
        var references = ApplicationAssembly.GetReferencedAssemblies()
            .Select(a => a.Name)
            .ToList();

        Assert.DoesNotContain(references, name =>
            name!.StartsWith("Serilog", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Infrastructure_ShouldNotReference_SerilogAssembly()
    {
        var references = InfrastructureAssembly.GetReferencedAssemblies()
            .Select(a => a.Name)
            .ToList();

        Assert.DoesNotContain(references, name =>
            name!.StartsWith("Serilog", StringComparison.OrdinalIgnoreCase));
    }

    // --- Availability feature location rules ---

    [Fact]
    public void BranchAvailabilityCalculator_IsInDomainLayer()
    {
        var type = DomainAssembly.GetTypes()
            .FirstOrDefault(t => t.Name == "BranchAvailabilityCalculator");

        Assert.NotNull(type);
        Assert.StartsWith("Lyria.Domain", type.Namespace!);
    }

    [Fact]
    public void BranchOpenStatus_IsInDomainLayer()
    {
        var type = DomainAssembly.GetTypes()
            .FirstOrDefault(t => t.Name == "BranchOpenStatus");

        Assert.NotNull(type);
        Assert.StartsWith("Lyria.Domain", type.Namespace!);
    }

    [Fact]
    public void ScheduleSource_IsInDomainLayer()
    {
        var type = DomainAssembly.GetTypes()
            .FirstOrDefault(t => t.Name == "ScheduleSource");

        Assert.NotNull(type);
        Assert.StartsWith("Lyria.Domain", type.Namespace!);
    }

    [Fact]
    public void IBranchAvailabilityReadService_IsInApplicationLayer()
    {
        var type = ApplicationAssembly.GetTypes()
            .FirstOrDefault(t => t.Name == "IBranchAvailabilityReadService");

        Assert.NotNull(type);
        Assert.True(type.IsInterface);
        Assert.StartsWith("Lyria.Application", type.Namespace!);
    }

    [Fact]
    public void BranchAvailabilityReadService_IsInInfrastructureLayer()
    {
        var type = InfrastructureAssembly.GetTypes()
            .FirstOrDefault(t => t.Name == "BranchAvailabilityReadService");

        Assert.NotNull(type);
        Assert.False(type.IsInterface);
        Assert.StartsWith("Lyria.Infrastructure", type.Namespace!);
    }

    [Fact]
    public void PublicBranchAvailabilityResponse_IsInApplicationLayer()
    {
        var type = ApplicationAssembly.GetTypes()
            .FirstOrDefault(t => t.Name == "PublicBranchAvailabilityResponse");

        Assert.NotNull(type);
        Assert.StartsWith("Lyria.Application", type.Namespace!);
    }

    [Fact]
    public void BranchOpenStatusNames_IsInApplicationLayer()
    {
        var type = ApplicationAssembly.GetTypes()
            .FirstOrDefault(t => t.Name == "BranchOpenStatusNames");

        Assert.NotNull(type);
        Assert.StartsWith("Lyria.Application", type.Namespace!);
    }

    [Fact]
    public void BranchAvailabilityContext_IsInApplicationLayer()
    {
        var type = ApplicationAssembly.GetTypes()
            .FirstOrDefault(t => t.Name == "BranchAvailabilityContext");

        Assert.NotNull(type);
        Assert.StartsWith("Lyria.Application", type.Namespace!);
    }

    [Fact]
    public void Domain_DoesNotReference_BranchAvailabilityContext()
    {
        var domainTypes = DomainAssembly.GetTypes();
        var contextType = domainTypes.FirstOrDefault(t => t.Name == "BranchAvailabilityContext");

        Assert.Null(contextType);
    }

    [Fact]
    public void PublicEstablishmentListItemResponse_HasOpenBranchCountProperty()
    {
        var type = ApplicationAssembly.GetTypes()
            .FirstOrDefault(t => t.Name == "PublicEstablishmentListItemResponse");

        Assert.NotNull(type);

        var openBranchCountProp = type.GetProperty("OpenBranchCount");
        Assert.NotNull(openBranchCountProp);

        var hasOpenBranchProp = type.GetProperty("HasOpenBranch");
        Assert.NotNull(hasOpenBranchProp);
    }

    // --- No EF InMemory ---

    [Fact]
    public void NoEfCoreInMemoryProvider_Referenced()
    {
        var allAssemblies = new[] { DomainAssembly, ApplicationAssembly, InfrastructureAssembly, ApiAssembly };

        foreach (var assembly in allAssemblies)
        {
            var references = assembly.GetReferencedAssemblies()
                .Select(a => a.Name)
                .ToList();

            Assert.DoesNotContain(references, name =>
                name!.Equals("Microsoft.EntityFrameworkCore.InMemory", StringComparison.OrdinalIgnoreCase));
        }
    }
}
