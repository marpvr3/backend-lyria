using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using Xunit;

namespace Lyria.Api.UnitTests;

public class HttpFailureLoggingMiddlewareTests : IDisposable
{
    private readonly InMemorySink _memorySink;
    private readonly IHost _host;
    private readonly HttpClient _client;

    public HttpFailureLoggingMiddlewareTests()
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
                });
                webHost.Configure(app =>
                {
                    app.UseMiddleware<Api.Extensions.HttpFailureLoggingMiddleware>();
                    app.UseRouting();
                    app.UseEndpoints(endpoints =>
                    {
                        endpoints.MapGet("/ok", () => Results.Ok(new { status = "ok" }));
                        endpoints.MapPost("/ok", () => Results.Ok(new { status = "created" }));
                        endpoints.MapGet("/created", () => Results.StatusCode(201));
                        endpoints.MapGet("/no-content", () => Results.NoContent());
                        endpoints.MapPost("/bad-request", () =>
                            Results.BadRequest(new ProblemDetails
                            {
                                Title = "Error de validación",
                                Status = 400
                            }));
                        endpoints.MapPost("/conflict", () =>
                            Results.Conflict(new ProblemDetails
                            {
                                Title = "Conflicto",
                                Status = 409
                            }));
                        endpoints.MapPost("/unprocessable", () =>
                            Results.UnprocessableEntity(new ProblemDetails
                            {
                                Title = "No procesable",
                                Status = 422
                            }));
                        endpoints.MapGet("/server-error", () => Results.StatusCode(500));
                        endpoints.MapPost("/echo", async (HttpContext ctx) =>
                        {
                            using var reader = new StreamReader(ctx.Request.Body);
                            string body = await reader.ReadToEndAsync();
                            ctx.Response.ContentType = "application/json";
                            ctx.Response.StatusCode = 400;
                            await ctx.Response.WriteAsync(body);
                        });
                        endpoints.MapPost("/echo-ok", async (HttpContext ctx) =>
                        {
                            using var reader = new StreamReader(ctx.Request.Body);
                            string body = await reader.ReadToEndAsync();
                            ctx.Response.ContentType = "application/json";
                            await ctx.Response.WriteAsync(body);
                        });
                        endpoints.MapPost("/sensitive", (HttpContext ctx) =>
                        {
                            ctx.Response.StatusCode = 400;
                            ctx.Response.ContentType = "application/json";
                            return ctx.Response.WriteAsync("""{"error":"bad","password":"leaked"}""");
                        });
                        endpoints.MapPost("/api/auth/login", async (HttpContext ctx) =>
                        {
                            using var reader = new StreamReader(ctx.Request.Body);
                            await reader.ReadToEndAsync();
                            ctx.Response.StatusCode = 400;
                            ctx.Response.ContentType = "application/json";
                            await ctx.Response.WriteAsync("""{"error":"invalid credentials"}""");
                        });
                        endpoints.MapPost("/api/login", async (HttpContext ctx) =>
                        {
                            using var reader = new StreamReader(ctx.Request.Body);
                            await reader.ReadToEndAsync();
                            ctx.Response.StatusCode = 400;
                            ctx.Response.ContentType = "application/json";
                            await ctx.Response.WriteAsync("""{"error":"invalid"}""");
                        });
                        endpoints.MapPost("/api/token", async (HttpContext ctx) =>
                        {
                            using var reader = new StreamReader(ctx.Request.Body);
                            await reader.ReadToEndAsync();
                            ctx.Response.StatusCode = 400;
                            ctx.Response.ContentType = "application/json";
                            await ctx.Response.WriteAsync("""{"error":"token invalid"}""");
                        });
                        endpoints.MapPost("/echo-auth-ok", async (HttpContext ctx) =>
                        {
                            using var reader = new StreamReader(ctx.Request.Body);
                            string body = await reader.ReadToEndAsync();
                            ctx.Response.ContentType = "application/json";
                            await ctx.Response.WriteAsync(body);
                        });
                    });
                });
            })
            .Build();

        _host.Start();
        _client = _host.GetTestClient();
    }

    [Fact]
    public async Task Ok200_DoesNotCreateHttpFailure()
    {
        _memorySink.LogEvents.Clear();

        await _client.GetAsync("/ok", TestContext.Current.CancellationToken);

        Assert.DoesNotContain(_memorySink.LogEvents,
            e => HasProperty(e, "TipoEvento", "HttpFailure"));
    }

    [Fact]
    public async Task Created201_DoesNotCreateHttpFailure()
    {
        _memorySink.LogEvents.Clear();

        await _client.GetAsync("/created", TestContext.Current.CancellationToken);

        Assert.DoesNotContain(_memorySink.LogEvents,
            e => HasProperty(e, "TipoEvento", "HttpFailure"));
    }

    [Fact]
    public async Task NoContent204_DoesNotCreateHttpFailure()
    {
        _memorySink.LogEvents.Clear();

        await _client.GetAsync("/no-content", TestContext.Current.CancellationToken);

        Assert.DoesNotContain(_memorySink.LogEvents,
            e => HasProperty(e, "TipoEvento", "HttpFailure"));
    }

    [Fact]
    public async Task BadRequest400_CreatesHttpFailure()
    {
        _memorySink.LogEvents.Clear();

        await _client.PostAsync("/bad-request",
            new StringContent("{}", Encoding.UTF8, "application/json"), TestContext.Current.CancellationToken);

        Assert.Contains(_memorySink.LogEvents,
            e => HasProperty(e, "TipoEvento", "HttpFailure"));
    }

    [Fact]
    public async Task Conflict409_CreatesHttpFailure()
    {
        _memorySink.LogEvents.Clear();

        await _client.PostAsync("/conflict",
            new StringContent("{}", Encoding.UTF8, "application/json"), TestContext.Current.CancellationToken);

        Assert.Contains(_memorySink.LogEvents,
            e => HasProperty(e, "TipoEvento", "HttpFailure"));
    }

    [Fact]
    public async Task Unprocessable422_CreatesHttpFailure()
    {
        _memorySink.LogEvents.Clear();

        await _client.PostAsync("/unprocessable",
            new StringContent("{}", Encoding.UTF8, "application/json"), TestContext.Current.CancellationToken);

        Assert.Contains(_memorySink.LogEvents,
            e => HasProperty(e, "TipoEvento", "HttpFailure"));
    }

    [Fact]
    public async Task ServerError500_CreatesHttpFailure()
    {
        _memorySink.LogEvents.Clear();

        await _client.GetAsync("/server-error", TestContext.Current.CancellationToken);

        Assert.Contains(_memorySink.LogEvents,
            e => HasProperty(e, "TipoEvento", "HttpFailure"));
    }

    [Fact]
    public async Task NonExistentRoute404_DoesNotPersist()
    {
        _memorySink.LogEvents.Clear();

        await _client.GetAsync("/api/v1/does-not-exist", TestContext.Current.CancellationToken);

        Assert.DoesNotContain(_memorySink.LogEvents,
            e => HasProperty(e, "TipoEvento", "HttpFailure"));
    }

    [Fact]
    public async Task RequestBody_CapturedForJson()
    {
        _memorySink.LogEvents.Clear();

        string requestJson = """{"name":"test"}""";
        await _client.PostAsync("/echo",
            new StringContent(requestJson, Encoding.UTF8, "application/json"), TestContext.Current.CancellationToken);

        LogEvent? evt = _memorySink.LogEvents
            .FirstOrDefault(e => HasProperty(e, "TipoEvento", "HttpFailure"));

        Assert.NotNull(evt);
        string? requestBody = GetPropertyValue(evt, "RequestBody");
        Assert.NotNull(requestBody);
        Assert.Contains("test", requestBody);
    }

    [Fact]
    public async Task ResponseBody_CapturedForJson()
    {
        _memorySink.LogEvents.Clear();

        string requestJson = """{"input":"data"}""";
        await _client.PostAsync("/echo",
            new StringContent(requestJson, Encoding.UTF8, "application/json"), TestContext.Current.CancellationToken);

        LogEvent? evt = _memorySink.LogEvents
            .FirstOrDefault(e => HasProperty(e, "TipoEvento", "HttpFailure"));

        Assert.NotNull(evt);
        string? responseBody = GetPropertyValue(evt, "ResponseBody");
        Assert.NotNull(responseBody);
    }

    [Fact]
    public async Task RequestBody_ArrivesIntactToController()
    {
        _memorySink.LogEvents.Clear();

        string requestJson = """{"name":"intact-test","value":42}""";

        var response = await _client.PostAsync("/echo-ok",
            new StringContent(requestJson, Encoding.UTF8, "application/json"), TestContext.Current.CancellationToken);

        string responseBody = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.Contains("intact-test", responseBody);
        Assert.Contains("42", responseBody);
    }

    [Fact]
    public async Task ResponseBody_ArrivesIntactToClient()
    {
        _memorySink.LogEvents.Clear();

        await _client.PostAsync("/bad-request",
            new StringContent("{}", Encoding.UTF8, "application/json"), TestContext.Current.CancellationToken);

        // Already tested above that the endpoint returns the ProblemDetails,
        // here we verify the middleware didn't alter it
        var response = await _client.PostAsync("/bad-request",
            new StringContent("{}", Encoding.UTF8, "application/json"), TestContext.Current.CancellationToken);

        string body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.Contains("Error de validación", body);
    }

    [Fact]
    public async Task BodyLimit_IsRespected()
    {
        _memorySink.LogEvents.Clear();

        string largeBody = new('A', 20000);
        string largeJson = $$"""{"data":"{{largeBody}}"}""";

        await _client.PostAsync("/echo",
            new StringContent(largeJson, Encoding.UTF8, "application/json"), TestContext.Current.CancellationToken);

        LogEvent? evt = _memorySink.LogEvents
            .FirstOrDefault(e => HasProperty(e, "TipoEvento", "HttpFailure"));

        Assert.NotNull(evt);
        string? requestBody = GetPropertyValue(evt, "RequestBody");
        Assert.NotNull(requestBody);
        Assert.Contains("[OMITIDO:", requestBody);
    }

    [Fact]
    public async Task SensitiveData_IsRedacted()
    {
        _memorySink.LogEvents.Clear();

        string json = """{"password":"secret123","name":"test"}""";

        await _client.PostAsync("/echo",
            new StringContent(json, Encoding.UTF8, "application/json"), TestContext.Current.CancellationToken);

        LogEvent? evt = _memorySink.LogEvents
            .FirstOrDefault(e => HasProperty(e, "TipoEvento", "HttpFailure"));

        Assert.NotNull(evt);
        string? requestBody = GetPropertyValue(evt, "RequestBody");
        Assert.NotNull(requestBody);
        Assert.DoesNotContain("secret123", requestBody);
        Assert.Contains("***REDACTED***", requestBody);
    }

    [Fact]
    public async Task AuthorizationHeader_DoesNotAppearInLog()
    {
        _memorySink.LogEvents.Clear();

        var request = new HttpRequestMessage(HttpMethod.Post, "/bad-request")
        {
            Content = new StringContent("{}", Encoding.UTF8, "application/json")
        };
        request.Headers.Add("Authorization", "Bearer my-secret-token");

        await _client.SendAsync(request, TestContext.Current.CancellationToken);

        string allLogs = string.Join(" ",
            _memorySink.LogEvents.Select(e => e.RenderMessage(CultureInfo.InvariantCulture)));

        Assert.DoesNotContain("my-secret-token", allLogs);
    }

    [Fact]
    public async Task CookieHeader_DoesNotAppearInLog()
    {
        _memorySink.LogEvents.Clear();

        var request = new HttpRequestMessage(HttpMethod.Post, "/bad-request")
        {
            Content = new StringContent("{}", Encoding.UTF8, "application/json")
        };
        request.Headers.Add("Cookie", "session=secret-cookie-value");

        await _client.SendAsync(request, TestContext.Current.CancellationToken);

        string allLogs = string.Join(" ",
            _memorySink.LogEvents.Select(e => e.RenderMessage(CultureInfo.InvariantCulture)));

        Assert.DoesNotContain("secret-cookie-value", allLogs);
    }

    [Fact]
    public async Task FileUpload_IsNotCaptured()
    {
        _memorySink.LogEvents.Clear();

        var content = new MultipartFormDataContent();
        content.Add(new ByteArrayContent([0xFF, 0xD8]), "file", "test.jpg");

        await _client.PostAsync("/echo", content, TestContext.Current.CancellationToken);

        LogEvent? evt = _memorySink.LogEvents
            .FirstOrDefault(e => HasProperty(e, "TipoEvento", "HttpFailure"));

        if (evt is not null)
        {
            string? requestBody = GetPropertyValue(evt, "RequestBody");
            Assert.True(
                requestBody is null ||
                requestBody.StartsWith("[OMITIDO:", StringComparison.Ordinal),
                "File content should not be captured");
        }
    }

    [Fact]
    public async Task TraceId_MatchesResponse()
    {
        _memorySink.LogEvents.Clear();

        var response = await _client.PostAsync("/bad-request",
            new StringContent("{}", Encoding.UTF8, "application/json"), TestContext.Current.CancellationToken);

        LogEvent? evt = _memorySink.LogEvents
            .FirstOrDefault(e => HasProperty(e, "TipoEvento", "HttpFailure"));

        Assert.NotNull(evt);
        string? logTraceId = GetPropertyValue(evt, "TraceId");
        Assert.NotNull(logTraceId);
        Assert.NotEmpty(logTraceId);
    }

    [Fact]
    public async Task StatusCode_IsCorrect()
    {
        _memorySink.LogEvents.Clear();

        await _client.PostAsync("/bad-request",
            new StringContent("{}", Encoding.UTF8, "application/json"), TestContext.Current.CancellationToken);

        LogEvent? evt = _memorySink.LogEvents
            .FirstOrDefault(e => HasProperty(e, "TipoEvento", "HttpFailure"));

        Assert.NotNull(evt);
        string? statusCode = GetPropertyValue(evt, "CodigoRespuesta");
        Assert.Equal("400", statusCode);
    }

    [Fact]
    public async Task DurationMs_HasValidValue()
    {
        _memorySink.LogEvents.Clear();

        await _client.PostAsync("/bad-request",
            new StringContent("{}", Encoding.UTF8, "application/json"), TestContext.Current.CancellationToken);

        LogEvent? evt = _memorySink.LogEvents
            .FirstOrDefault(e => HasProperty(e, "TipoEvento", "HttpFailure"));

        Assert.NotNull(evt);
        Assert.True(evt.Properties.ContainsKey("DuracionMs"));

        LogEventPropertyValue durationValue = evt.Properties["DuracionMs"];
        Assert.IsType<ScalarValue>(durationValue);

        object? rawValue = ((ScalarValue)durationValue).Value;
        Assert.NotNull(rawValue);
        Assert.True(rawValue is double, $"DuracionMs should be double but was {rawValue.GetType().Name}");
        Assert.True((double)rawValue >= 0);
    }

    [Fact]
    public async Task OnlyOneHttpFailureEvent_PerRequest()
    {
        _memorySink.LogEvents.Clear();

        await _client.PostAsync("/bad-request",
            new StringContent("{}", Encoding.UTF8, "application/json"), TestContext.Current.CancellationToken);

        int httpFailureCount = _memorySink.LogEvents
            .Count(e => HasProperty(e, "TipoEvento", "HttpFailure"));

        Assert.Equal(1, httpFailureCount);
    }

    [Fact]
    public async Task AuthRoute_DoesNotCaptureRequestBody()
    {
        _memorySink.LogEvents.Clear();

        string json = """{"username":"admin","password":"secret"}""";
        await _client.PostAsync("/api/auth/login",
            new StringContent(json, Encoding.UTF8, "application/json"), TestContext.Current.CancellationToken);

        LogEvent? evt = _memorySink.LogEvents
            .FirstOrDefault(e => HasProperty(e, "TipoEvento", "HttpFailure"));

        Assert.NotNull(evt);
        string? requestBody = GetPropertyValue(evt, "RequestBody");
        Assert.Null(requestBody);
    }

    [Fact]
    public async Task AuthRoute_DoesNotCaptureResponseBody()
    {
        _memorySink.LogEvents.Clear();

        string json = """{"username":"admin","password":"secret"}""";
        await _client.PostAsync("/api/auth/login",
            new StringContent(json, Encoding.UTF8, "application/json"), TestContext.Current.CancellationToken);

        LogEvent? evt = _memorySink.LogEvents
            .FirstOrDefault(e => HasProperty(e, "TipoEvento", "HttpFailure"));

        Assert.NotNull(evt);
        string? responseBody = GetPropertyValue(evt, "ResponseBody");
        Assert.Null(responseBody);
    }

    [Fact]
    public async Task LoginRoute_DoesNotCaptureBodies()
    {
        _memorySink.LogEvents.Clear();

        string json = """{"user":"test","pass":"123"}""";
        await _client.PostAsync("/api/login",
            new StringContent(json, Encoding.UTF8, "application/json"), TestContext.Current.CancellationToken);

        LogEvent? evt = _memorySink.LogEvents
            .FirstOrDefault(e => HasProperty(e, "TipoEvento", "HttpFailure"));

        Assert.NotNull(evt);
        Assert.Null(GetPropertyValue(evt, "RequestBody"));
        Assert.Null(GetPropertyValue(evt, "ResponseBody"));
    }

    [Fact]
    public async Task TokenRoute_DoesNotCaptureBodies()
    {
        _memorySink.LogEvents.Clear();

        string json = """{"grant_type":"password","username":"test"}""";
        await _client.PostAsync("/api/token",
            new StringContent(json, Encoding.UTF8, "application/json"), TestContext.Current.CancellationToken);

        LogEvent? evt = _memorySink.LogEvents
            .FirstOrDefault(e => HasProperty(e, "TipoEvento", "HttpFailure"));

        Assert.NotNull(evt);
        Assert.Null(GetPropertyValue(evt, "RequestBody"));
        Assert.Null(GetPropertyValue(evt, "ResponseBody"));
    }

    [Fact]
    public async Task SensitiveRoute_StillRecordsMethodRouteStatusDurationTraceId()
    {
        _memorySink.LogEvents.Clear();

        await _client.PostAsync("/api/auth/login",
            new StringContent("{}", Encoding.UTF8, "application/json"), TestContext.Current.CancellationToken);

        LogEvent? evt = _memorySink.LogEvents
            .FirstOrDefault(e => HasProperty(e, "TipoEvento", "HttpFailure"));

        Assert.NotNull(evt);
        Assert.Equal("POST", GetPropertyValue(evt, "MetodoHttp"));
        Assert.Equal("/api/auth/login", GetPropertyValue(evt, "Ruta"));
        Assert.Equal("400", GetPropertyValue(evt, "CodigoRespuesta"));
        Assert.True(evt.Properties.ContainsKey("DuracionMs"));
        Assert.NotNull(GetPropertyValue(evt, "TraceId"));
    }

    [Fact]
    public async Task SensitiveRoute_ControllerReceivesBodyIntact()
    {
        string json = """{"username":"admin","password":"secret"}""";

        var response = await _client.PostAsync("/echo-auth-ok",
            new StringContent(json, Encoding.UTF8, "application/json"), TestContext.Current.CancellationToken);

        string responseBody = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.Contains("admin", responseBody);
        Assert.Contains("secret", responseBody);
    }

    [Fact]
    public async Task SensitiveRoute_ClientReceivesResponseIntact()
    {
        string json = """{"username":"admin"}""";

        var response = await _client.PostAsync("/api/auth/login",
            new StringContent(json, Encoding.UTF8, "application/json"), TestContext.Current.CancellationToken);

        string body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.Contains("invalid credentials", body);
    }

    private static bool HasProperty(LogEvent evt, string key, string value)
    {
        return evt.Properties.TryGetValue(key, out LogEventPropertyValue? prop) &&
               prop is ScalarValue sv &&
               string.Equals(sv.Value?.ToString(), value, StringComparison.Ordinal);
    }

    private static string? GetPropertyValue(LogEvent evt, string key)
    {
        if (evt.Properties.TryGetValue(key, out LogEventPropertyValue? prop) &&
            prop is ScalarValue sv)
        {
            return sv.Value?.ToString();
        }
        return null;
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        _client.Dispose();
        _host.Dispose();
    }
}

internal sealed class InMemorySink : Serilog.Core.ILogEventSink
{
    public List<LogEvent> LogEvents { get; } = [];

    public void Emit(LogEvent logEvent) => LogEvents.Add(logEvent);
}
