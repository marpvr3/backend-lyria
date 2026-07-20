using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Lyria.Infrastructure.IntegrationTests;

public class DependencyInjectionTests
{
    [Fact]
    public void AddInfrastructure_RegistersServicesWithoutError()
    {
        var services = new ServiceCollection();

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:LyriaDatabase"] = "Server=(localdb)\\mssqllocaldb;Database=LyriaTests;Trusted_Connection=True"
            })
            .Build();

        IServiceCollection result = services.AddInfrastructure(configuration);

        Assert.Same(services, result);
    }

    [Fact]
    public void AddInfrastructure_ThrowsWhenConnectionStringMissing()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();

        var exception = Assert.Throws<InvalidOperationException>(
            () => services.AddInfrastructure(configuration));

        Assert.Contains("ConnectionStrings:LyriaDatabase", exception.Message);
    }

}
