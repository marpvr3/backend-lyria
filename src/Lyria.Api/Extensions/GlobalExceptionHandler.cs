using System.Diagnostics;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Serilog;

namespace Lyria.Api.Extensions;

internal sealed class GlobalExceptionHandler(IHostEnvironment environment)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        string traceId = httpContext.TraceIdentifier;
        string method = httpContext.Request.Method;
        string path = httpContext.Request.Path.Value ?? string.Empty;

        Log.ForContext("TipoEvento", "UnhandledException")
           .ForContext("PersistToDatabase", true)
           .ForContext("TipoExcepcion", exception.GetType().FullName)
           .ForContext("TraceId", traceId)
           .ForContext("MetodoHttp", method)
           .ForContext("Ruta", path)
           .ForContext("SpanId", Activity.Current?.SpanId.ToString())
           .ForContext("Ambiente", environment.EnvironmentName)
           .Error(exception,
               "Excepción no controlada en {MetodoHttp} {Ruta}",
               method, path);

        var problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "Ocurrió un error interno.",
            Detail = "No fue posible completar la operación.",
            Instance = path,
            Type = "about:blank",
            Extensions = { ["traceId"] = traceId }
        };

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }
}
