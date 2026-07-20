using Lyria.Application.Abstractions.Messaging;
using Lyria.Application.Features.EstablishmentCategories.Create;
using Lyria.Application.Features.EstablishmentCategories.Deactivate;
using Lyria.Application.Features.EstablishmentCategories.GetById;
using Lyria.Application.Features.EstablishmentCategories.ListActive;
using Lyria.Application.Features.EstablishmentCategories.Reactivate;
using Lyria.Application.Features.EstablishmentCategories.Update;
using Xunit;

namespace Lyria.Application.UnitTests.Common;

public sealed class CqrsContractTests
{
    [Fact]
    public void CreateCommand_ImplementsICommandOfT()
    {
        var interfaces = typeof(CreateEstablishmentCategoryCommand).GetInterfaces();

        Assert.Contains(interfaces, i =>
            i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ICommand<>));
    }

    [Fact]
    public void UpdateCommand_ImplementsICommand()
    {
        Assert.IsAssignableFrom<ICommand>(new UpdateEstablishmentCategoryCommand(
            Guid.NewGuid(), "Name", null, null, 0));
    }

    [Fact]
    public void DeactivateCommand_ImplementsICommand()
    {
        Assert.IsAssignableFrom<ICommand>(new DeactivateEstablishmentCategoryCommand(Guid.NewGuid()));
    }

    [Fact]
    public void ReactivateCommand_ImplementsICommand()
    {
        Assert.IsAssignableFrom<ICommand>(new ReactivateEstablishmentCategoryCommand(Guid.NewGuid()));
    }

    [Fact]
    public void GetByIdQuery_ImplementsIQuery()
    {
        var interfaces = typeof(GetEstablishmentCategoryByIdQuery).GetInterfaces();

        Assert.Contains(interfaces, i =>
            i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IQuery<>));
    }

    [Fact]
    public void ListActiveQuery_ImplementsIQuery()
    {
        var interfaces = typeof(ListActiveEstablishmentCategoriesQuery).GetInterfaces();

        Assert.Contains(interfaces, i =>
            i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IQuery<>));
    }

    [Fact]
    public void NoCustomMediatorImplementation_Exists()
    {
        var applicationAssembly = typeof(DependencyInjection).Assembly;

        var mediatorTypes = applicationAssembly.GetTypes()
            .Where(t => t.Name.Contains("Mediator", StringComparison.OrdinalIgnoreCase) &&
                        t is { IsInterface: false, IsAbstract: false } &&
                        !t.Name.Contains("Behavior", StringComparison.OrdinalIgnoreCase) &&
                        !t.Name.Contains("Validator", StringComparison.OrdinalIgnoreCase))
            .ToList();

        Assert.Empty(mediatorTypes);
    }
}
