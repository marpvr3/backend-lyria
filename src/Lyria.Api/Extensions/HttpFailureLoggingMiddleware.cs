using System.Diagnostics;
using System.Text;
using Serilog;
using Serilog.Events;

namespace Lyria.Api.Extensions;

internal sealed class HttpFailureLoggingMiddleware
{
    private const string OmittedBinaryMarker = "[OMITIDO: contenido no JSON]";
    private const string EventType = "HttpFailure";

    private static readonly HashSet<int> PersistableClientStatusCodes = [400, 409, 422];

    private static readonly HashSet<string> ExcludedPathPrefixes =
    [
        "/swagger",
        "/api/health"
    ];

    private static readonly HashSet<string> JsonContentTypes =
    [
        "application/json",
        "application/problem+json"
    ];

    private static readonly HashSet<string> NonCapturableMethods =
    [
        "GET", "HEAD", "OPTIONS"
    ];

    private static readonly string[] SensitiveRouteSegments =
    [
        "/auth",
        "/login",
        "/token",
        "/refresh-token",
        "/password",
        "/reset-password"
    ];

    private readonly RequestDelegate _next;
    private readonly int _maxBodyBytes;
    private readonly string _omittedSizeMessage;
    private readonly IHostEnvironment _environment;

    public HttpFailureLoggingMiddleware(
        RequestDelegate next,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        _next = next;
        _environment = environment;

        string? maxBytesConfig = configuration["DatabaseLogging:MaxBodyBytes"];
        _maxBodyBytes = int.TryParse(maxBytesConfig, out int parsed) ? parsed : 16384;
        _omittedSizeMessage = $"[OMITIDO: cuerpo superior a {_maxBodyBytes} bytes]";
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (ShouldSkipCapture(context))
        {
            await _next(context);
            return;
        }

        bool sensitiveRoute = IsSensitiveRoute(context.Request.Path.Value ?? string.Empty);

        string? requestBody = sensitiveRoute ? null : await CaptureRequestBodyAsync(context);

        Stream originalBodyStream = context.Response.Body;
        LimitedCaptureResponseStream? captureStream = sensitiveRoute
            ? null
            : new LimitedCaptureResponseStream(originalBodyStream, _maxBodyBytes);

        if (captureStream is not null)
        {
            context.Response.Body = captureStream;
        }

        var stopwatch = Stopwatch.StartNew();

        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();

            if (captureStream is not null)
            {
                context.Response.Body = originalBodyStream;
            }

            int statusCode = context.Response.StatusCode;

            if (ShouldPersist(statusCode))
            {
                string? responseBody = sensitiveRoute ? null : GetResponseBody(context, captureStream!);

                string? sanitizedRequest = IsMarker(requestBody) ? requestBody : SensitiveDataRedactor.Redact(requestBody);
                string? sanitizedResponse = IsMarker(responseBody) ? responseBody : SensitiveDataRedactor.Redact(responseBody);

                LogLevel logLevel = statusCode >= 500
                    ? LogLevel.Error
                    : LogLevel.Warning;

                EmitHttpFailureEvent(
                    context,
                    logLevel,
                    stopwatch.Elapsed.TotalMilliseconds,
                    sanitizedRequest,
                    sanitizedResponse);
            }

            captureStream?.Dispose();
        }
    }

    private static bool ShouldSkipCapture(HttpContext context)
    {
        string method = context.Request.Method;
        if (string.Equals(method, "OPTIONS", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        string path = context.Request.Path.Value ?? string.Empty;

        foreach (string prefix in ExcludedPathPrefixes)
        {
            if (path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private async Task<string?> CaptureRequestBodyAsync(HttpContext context)
    {
        if (NonCapturableMethods.Contains(context.Request.Method.ToUpperInvariant()))
        {
            return null;
        }

        if (!IsJsonContentType(context.Request.ContentType))
        {
            return context.Request.ContentType is not null
                ? OmittedBinaryMarker
                : null;
        }

        if (context.Request.ContentLength.HasValue && context.Request.ContentLength.Value > _maxBodyBytes)
        {
            return _omittedSizeMessage;
        }

        context.Request.EnableBuffering();

        using var reader = new StreamReader(
            context.Request.Body,
            Encoding.UTF8,
            detectEncodingFromByteOrderMarks: false,
            bufferSize: 4096,
            leaveOpen: true);

        string body = await reader.ReadToEndAsync();

        context.Request.Body.Position = 0;

        if (body.Length > _maxBodyBytes)
        {
            return _omittedSizeMessage;
        }

        return body;
    }

    private string? GetResponseBody(HttpContext context, LimitedCaptureResponseStream captureStream)
    {
        if (!IsJsonContentType(context.Response.ContentType))
        {
            return null;
        }

        if (captureStream.LimitExceeded)
        {
            return _omittedSizeMessage;
        }

        return captureStream.GetCapturedContent();
    }

    private static bool ShouldPersist(int statusCode)
    {
        if (statusCode >= 500 && statusCode <= 599)
        {
            return true;
        }

        return PersistableClientStatusCodes.Contains(statusCode);
    }

    private void EmitHttpFailureEvent(
        HttpContext context,
        LogLevel logLevel,
        double durationMs,
        string? requestBody,
        string? responseBody)
    {
        var serilogLevel = logLevel switch
        {
            LogLevel.Error => LogEventLevel.Error,
            LogLevel.Critical => LogEventLevel.Fatal,
            _ => LogEventLevel.Warning
        };

        string method = context.Request.Method;
        string path = context.Request.Path.Value ?? string.Empty;
        int statusCode = context.Response.StatusCode;

        Log.ForContext("TipoEvento", EventType)
           .ForContext("PersistToDatabase", true)
           .ForContext("TraceId", context.TraceIdentifier)
           .ForContext("SpanId", Activity.Current?.SpanId.ToString())
           .ForContext("RequestId", context.TraceIdentifier)
           .ForContext("MetodoHttp", method)
           .ForContext("Ruta", path)
           .ForContext("CodigoRespuesta", statusCode)
           .ForContext("DuracionMs", Math.Round(durationMs, 4))
           .ForContext("RequestBody", requestBody)
           .ForContext("ResponseBody", responseBody)
           .ForContext("Ambiente", _environment.EnvironmentName)
           .Write(serilogLevel,
               "HTTP {MetodoHttp} {Ruta} respondió {CodigoRespuesta} en {DuracionMs:0.####} ms",
               method, path, statusCode, Math.Round(durationMs, 4));
    }

    private static bool IsSensitiveRoute(string path)
    {
        foreach (string segment in SensitiveRouteSegments)
        {
            int index = path.IndexOf(segment, StringComparison.OrdinalIgnoreCase);
            if (index < 0)
            {
                continue;
            }

            int afterSegment = index + segment.Length;
            bool endsAtBoundary = afterSegment >= path.Length ||
                                  path[afterSegment] == '/' ||
                                  path[afterSegment] == '?';

            if (endsAtBoundary)
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsMarker(string? value)
    {
        return value is not null && value.StartsWith("[OMITIDO:", StringComparison.Ordinal);
    }

    private static bool IsJsonContentType(string? contentType)
    {
        if (string.IsNullOrEmpty(contentType))
        {
            return false;
        }

        foreach (string jsonType in JsonContentTypes)
        {
            if (contentType.Contains(jsonType, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        if (contentType.Contains("+json", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return false;
    }
}
