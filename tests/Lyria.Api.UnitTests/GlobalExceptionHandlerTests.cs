using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using Xunit;

namespace Lyria.Api.UnitTests;

public class GlobalExceptionHandlerTests : IDisposable
{
    private readonly InMemorySink _memorySink;
    private readonly IHost _host;
    private readonly HttpClient _client;

    public GlobalExceptionHandlerTests()
    {
        _memorySink = new InMemorySink();

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Sink(_memorySink)
            .CreateLogger();

        _host = new HostBuilder()
            .ConfigureWebHost(webHost =>
            {
                webHost.UseTestServer();
                webHost.ConfigureAppConfiguration((_, config) =>
                {
                    config.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["DatabaseLogging:MaxBodyBytes"] = "16384"
                    });
                });
                webHost.ConfigureServices(services =>
                {
                    services.AddRouting();
                    services.AddProblemDetails();
                    services.AddExceptionHandler<Api.Extensions.UniqueConstraintExceptionHandler>();
                    services.AddExceptionHandler<Api.Extensions.GlobalExceptionHandler>();
                });
                webHost.Configure(app =>
                {
                    app.UseExceptionHandler();
                    app.UseRouting();
                    app.UseEndpoints(endpoints =>
                    {
                        endpoints.MapGet("/throw", _ =>
                            throw new InvalidOperationException("Test exception message"));
                        endpoints.MapGet("/throw-sql", _ =>
                            throw new InvalidOperationException("An error occurred",
                                new InvalidOperationException("Connection string: Server=secret;Password=pass123")));
                        endpoints.MapGet("/ok", () => Results.Ok());
                    });
                });
            })
            .Build();

        _host.Start();
        _client = _host.GetTestClient();
    }

    [Fact]
    public async Task UniqueConstraintExceptionHandler_ConservesItsBehavior()
    {
        _memorySink.LogEvents.Clear();

        // UniqueConstraintExceptionHandler only handles DbUpdateException with SqlException,
        // so a generic exception should pass through to GlobalExceptionHandler
        var response = await _client.GetAsync("/throw", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
    }

    [Fact]
    public async Task GlobalExceptionHandler_Returns500()
    {
        _memorySink.LogEvents.Clear();

        var response = await _client.GetAsync("/throw", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
    }

    [Fact]
    public async Task GlobalExceptionHandler_ReturnsProblemDetails()
    {
        _memorySink.LogEvents.Clear();

        var response = await _client.GetAsync("/throw", TestContext.Current.CancellationToken);

        string body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        using var doc = JsonDocument.Parse(body);

        Assert.Equal("Ocurrió un error interno.", doc.RootElement.GetProperty("title").GetString());
        Assert.Equal(500, doc.RootElement.GetProperty("status").GetInt32());
        Assert.Equal("No fue posible completar la operación.", doc.RootElement.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task GlobalExceptionHandler_IncludesTraceId()
    {
        _memorySink.LogEvents.Clear();

        var response = await _client.GetAsync("/throw", TestContext.Current.CancellationToken);

        string body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        using var doc = JsonDocument.Parse(body);

        Assert.True(doc.RootElement.TryGetProperty("traceId", out JsonElement traceElement));
        Assert.NotNull(traceElement.GetString());
        Assert.NotEmpty(traceElement.GetString()!);
    }

    [Fact]
    public async Task GlobalExceptionHandler_DoesNotExposeStackTrace()
    {
        _memorySink.LogEvents.Clear();

        var response = await _client.GetAsync("/throw", TestContext.Current.CancellationToken);

        string body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.DoesNotContain("at ", body);
        Assert.DoesNotContain("StackTrace", body);
        Assert.DoesNotContain("InvalidOperationException", body);
    }

    [Fact]
    public async Task GlobalExceptionHandler_DoesNotExposeSqlMessage()
    {
        _memorySink.LogEvents.Clear();

        var response = await _client.GetAsync("/throw-sql", TestContext.Current.CancellationToken);

        string body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.DoesNotContain("Connection string", body);
        Assert.DoesNotContain("Password", body);
        Assert.DoesNotContain("pass123", body);
    }

    [Fact]
    public async Task GlobalExceptionHandler_LogsError()
    {
        _memorySink.LogEvents.Clear();

        await _client.GetAsync("/throw", TestContext.Current.CancellationToken);

        Assert.Contains(_memorySink.LogEvents,
            e => e.Level == LogEventLevel.Error &&
                 e.Exception is not null);
    }

    [Fact]
    public async Task GlobalExceptionHandler_MarksPersistToDatabase()
    {
        _memorySink.LogEvents.Clear();

        await _client.GetAsync("/throw", TestContext.Current.CancellationToken);

        LogEvent? evt = _memorySink.LogEvents
            .FirstOrDefault(e =>
                e.Properties.TryGetValue("PersistToDatabase", out LogEventPropertyValue? val) &&
                val is ScalarValue { Value: true });

        Assert.NotNull(evt);
    }

    [Fact]
    public async Task GlobalExceptionHandler_DoesNotLogExceptionTwice()
    {
        _memorySink.LogEvents.Clear();

        await _client.GetAsync("/throw", TestContext.Current.CancellationToken);

        int errorCount = _memorySink.LogEvents
            .Count(e => e.Level >= LogEventLevel.Error &&
                        e.Properties.TryGetValue("TipoEvento", out LogEventPropertyValue? val) &&
                        val is ScalarValue { Value: "UnhandledException" });

        Assert.Equal(1, errorCount);
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        _client.Dispose();
        _host.Dispose();
    }
}
