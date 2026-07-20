using FluentValidation;
using Lyria.Application.Features.EstablishmentCategories.Create;
using Lyria.Application.Features.EstablishmentCategories.Update;
using Lyria.Application.Features.EstablishmentCategories.Deactivate;
using Lyria.Application.Features.EstablishmentCategories.Reactivate;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Lyria.Application.UnitTests.Common;

public sealed class DependencyInjectionResolutionTests
{
    private readonly IServiceProvider _provider;

    public DependencyInjectionResolutionTests()
    {
        var services = new ServiceCollection();
        services.AddApplication();
        _provider = services.BuildServiceProvider();
    }

    [Fact]
    public void AddApplication_RegistersCreateValidator()
    {
        var validators = _provider.GetServices<IValidator<CreateEstablishmentCategoryCommand>>();
        Assert.Single(validators);
    }

    [Fact]
    public void AddApplication_RegistersUpdateValidator()
    {
        var validators = _provider.GetServices<IValidator<UpdateEstablishmentCategoryCommand>>();
        Assert.Single(validators);
    }

    [Fact]
    public void AddApplication_RegistersDeactivateValidator()
    {
        var validators = _provider.GetServices<IValidator<DeactivateEstablishmentCategoryCommand>>();
        Assert.Single(validators);
    }

    [Fact]
    public void AddApplication_RegistersReactivateValidator()
    {
        var validators = _provider.GetServices<IValidator<ReactivateEstablishmentCategoryCommand>>();
        Assert.Single(validators);
    }

    [Fact]
    public void AddApplication_RegistersServicesWithoutError()
    {
        var services = new ServiceCollection();

        IServiceCollection result = services.AddApplication();

        Assert.Same(services, result);
    }
}
