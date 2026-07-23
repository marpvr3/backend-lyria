using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Lyria.Api.FunctionalTests;

[Collection(LyriaApiTestGroup.Name)]
public class SerilogConfigurationTests
{
    private readonly WebApplicationFactory<Program> _factory;

    public SerilogConfigurationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:LyriaDatabase"] =
                        "Server=(localdb)\\mssqllocaldb;Database=LyriaTest;Trusted_Connection=True",
                    ["Serilog:WriteTo:1:Args:path"] =
                        Path.Combine(Path.GetTempPath(), "lyria-test-logs", "lyria-.log")
                });
            });
        });
    }

    [Fact]
    public async Task Api_StartsSuccessfully_WithSerilogConfiguration()
    {
        HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync(
            "/api/health", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ExistingEndpoints_ContinueResponding_WithSerilog()
    {
        HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync(
            "/api/v1/establishment-categories", TestContext.Current.CancellationToken);

        Assert.True(response.IsSuccessStatusCode);
    }

    [Fact]
    public async Task SwaggerEndpoint_RemainsAvailable_WithSerilog()
    {
        HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync(
            "/swagger/v1/swagger.json", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public void Serilog_IsRegistered_AsLoggingProvider()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        var loggerFactory = scope.ServiceProvider.GetRequiredService<ILoggerFactory>();
        ILogger logger = loggerFactory.CreateLogger("TestLogger");

        Assert.NotNull(logger);
    }

    [Fact]
    public void Configuration_ContainsConsoleSink()
    {
        IConfiguration configuration = _factory.Services.GetRequiredService<IConfiguration>();

        string? consoleSinkName = configuration["Serilog:WriteTo:0:Name"];

        Assert.Equal("Console", consoleSinkName);
    }

    [Fact]
    public void Configuration_ContainsFileSink()
    {
        IConfiguration configuration = _factory.Services.GetRequiredService<IConfiguration>();

        string? fileSinkName = configuration["Serilog:WriteTo:1:Name"];

        Assert.Equal("File", fileSinkName);
    }

    [Fact]
    public void Configuration_HasDailyRollingInterval()
    {
        IConfiguration configuration = _factory.Services.GetRequiredService<IConfiguration>();

        string? rollingInterval = configuration["Serilog:WriteTo:1:Args:rollingInterval"];

        Assert.Equal("Day", rollingInterval);
    }

    [Fact]
    public void Configuration_HasRetainedFileCountLimit_Of30()
    {
        IConfiguration configuration = _factory.Services.GetRequiredService<IConfiguration>();

        string? retainedFileCountLimit = configuration["Serilog:WriteTo:1:Args:retainedFileCountLimit"];

        Assert.Equal("30", retainedFileCountLimit);
    }

    [Fact]
    public void Configuration_HasFileSizeLimit_Of10MB()
    {
        IConfiguration configuration = _factory.Services.GetRequiredService<IConfiguration>();

        string? fileSizeLimitBytes = configuration["Serilog:WriteTo:1:Args:fileSizeLimitBytes"];

        Assert.Equal("10485760", fileSizeLimitBytes);
    }

    [Fact]
    public void Configuration_DoesNotContain_ConnectionStringSecrets()
    {
        IConfiguration configuration = _factory.Services.GetRequiredService<IConfiguration>();

        IConfigurationSection serilogSection = configuration.GetSection("Serilog");
        string serilogJson = SerializeSectionToString(serilogSection);

        Assert.DoesNotContain("Password", serilogJson, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("pwd", serilogJson, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("secret", serilogJson, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("token", serilogJson, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("connectionstring", serilogJson, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Configuration_HasRollOnFileSizeLimit_Enabled()
    {
        IConfiguration configuration = _factory.Services.GetRequiredService<IConfiguration>();

        string? rollOnFileSizeLimit = configuration["Serilog:WriteTo:1:Args:rollOnFileSizeLimit"];

        Assert.Equal("true", rollOnFileSizeLimit, ignoreCase: true);
    }

    [Fact]
    public void Configuration_DefaultLevel_IsInformation()
    {
        IConfiguration configuration = _factory.Services.GetRequiredService<IConfiguration>();

        string? defaultLevel = configuration["Serilog:MinimumLevel:Default"];

        Assert.Equal("Information", defaultLevel);
    }

    [Fact]
    public void Configuration_MicrosoftOverride_IsWarning()
    {
        IConfiguration configuration = _factory.Services.GetRequiredService<IConfiguration>();

        string? microsoftLevel = configuration["Serilog:MinimumLevel:Override:Microsoft"];

        Assert.Equal("Warning", microsoftLevel);
    }

    [Fact]
    public void Configuration_AspNetCoreOverride_IsWarning()
    {
        IConfiguration configuration = _factory.Services.GetRequiredService<IConfiguration>();

        string? aspNetCoreLevel = configuration["Serilog:MinimumLevel:Override:Microsoft.AspNetCore"];

        Assert.Equal("Warning", aspNetCoreLevel);
    }

    [Fact]
    public void OutputTemplate_ContainsTraceId()
    {
        IConfiguration configuration = _factory.Services.GetRequiredService<IConfiguration>();

        string? outputTemplate = configuration["Serilog:WriteTo:1:Args:outputTemplate"];

        Assert.NotNull(outputTemplate);
        Assert.Contains("{TraceId}", outputTemplate);
    }

    [Fact]
    public void OutputTemplate_ContainsSourceContext()
    {
        IConfiguration configuration = _factory.Services.GetRequiredService<IConfiguration>();

        string? outputTemplate = configuration["Serilog:WriteTo:1:Args:outputTemplate"];

        Assert.NotNull(outputTemplate);
        Assert.Contains("{SourceContext}", outputTemplate);
    }

    [Fact]
    public void OutputTemplate_ContainsTimestampAndLevel()
    {
        IConfiguration configuration = _factory.Services.GetRequiredService<IConfiguration>();

        string? outputTemplate = configuration["Serilog:WriteTo:1:Args:outputTemplate"];

        Assert.NotNull(outputTemplate);
        Assert.Contains("{Timestamp:", outputTemplate);
        Assert.Contains("{Level:", outputTemplate);
    }

    [Fact]
    public void OutputTemplate_ContainsExceptionPlaceholder()
    {
        IConfiguration configuration = _factory.Services.GetRequiredService<IConfiguration>();

        string? outputTemplate = configuration["Serilog:WriteTo:1:Args:outputTemplate"];

        Assert.NotNull(outputTemplate);
        Assert.Contains("{Exception}", outputTemplate);
    }

    [Fact]
    public void Configuration_EnrichesFromLogContext()
    {
        IConfiguration configuration = _factory.Services.GetRequiredService<IConfiguration>();

        IConfigurationSection enrichSection = configuration.GetSection("Serilog:Enrich");
        List<string?> enrichers = enrichSection.GetChildren().Select(c => c.Value).ToList();

        Assert.Contains("FromLogContext", enrichers);
    }

    private static string SerializeSectionToString(IConfigurationSection section)
    {
        var dict = new Dictionary<string, string?>();
        foreach (IConfigurationSection child in section.GetChildren())
        {
            CollectValues(child, child.Key, dict);
        }
        return JsonSerializer.Serialize(dict);
    }

    private static void CollectValues(
        IConfigurationSection section,
        string prefix,
        Dictionary<string, string?> dict)
    {
        if (section.Value is not null)
        {
            dict[prefix] = section.Value;
            return;
        }

        foreach (IConfigurationSection child in section.GetChildren())
        {
            CollectValues(child, prefix + ":" + child.Key, dict);
        }
    }
}
