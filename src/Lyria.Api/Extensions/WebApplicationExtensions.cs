using Serilog;

namespace Lyria.Api.Extensions;

public static class WebApplicationExtensions
{
    public static WebApplication UseLyriaPipeline(this WebApplication app)
    {
        app.UseMiddleware<HttpFailureLoggingMiddleware>();

        app.UseExceptionHandler();

        app.UseSerilogRequestLogging(options =>
        {
            options.MessageTemplate =
                "HTTP {RequestMethod} {RequestPath} respondió {StatusCode} en {Elapsed:0.###} ms";

            options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
            {
                diagnosticContext.Set("TraceId", httpContext.TraceIdentifier);
                diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
                diagnosticContext.Set("RequestScheme", httpContext.Request.Scheme);
            };
        });

        app.UseRouting();

        // CORS debe ejecutarse después de UseRouting y antes de la autorización.
        app.UseCors(CorsExtensions.PolicyName);

        app.UseSwagger();

        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint(
                "/swagger/v1/swagger.json",
                "Lyria API v1");

            options.RoutePrefix = "swagger";
        });

        app.MapGet("/", () => Results.Redirect("/swagger"));

        app.MapControllers();

        return app;
    }
}
