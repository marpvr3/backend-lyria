using Lyria.Infrastructure.Persistence;
using Microsoft.Extensions.Options;
using Serilog;

namespace Lyria.Api.Extensions;

public static class WebApplicationExtensions
{
    private const string MigrationsSettingKey =
        $"{DatabaseStartupOptions.SectionName}:{nameof(DatabaseStartupOptions.ApplyMigrationsOnStartup)}";

    /// <summary>
    /// Aplica las migraciones EF Core pendientes antes de que la API acepte solicitudes,
    /// siempre que <c>Database:ApplyMigrationsOnStartup</c> esté habilitado.
    /// Si está deshabilitado (valor predeterminado) no se toca la base de datos.
    /// </summary>
    public static async Task<WebApplication> ApplyPendingMigrationsAsync(
        this WebApplication app,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(app);

        DatabaseStartupOptions options = app.Services
            .GetRequiredService<IOptions<DatabaseStartupOptions>>()
            .Value;

        if (!options.ApplyMigrationsOnStartup)
        {
            Log.Information(
                "La aplicación automática de migraciones está deshabilitada ({SettingKey}=false). " +
                "No se modificará la base de datos durante el inicio.",
                MigrationsSettingKey);

            return app;
        }

        Log.Information(
            "La aplicación automática de migraciones está habilitada ({SettingKey}=true). " +
            "Verificando migraciones pendientes.",
            MigrationsSettingKey);

        await DatabaseMigrator.ApplyPendingMigrationsAsync(app.Services, cancellationToken);

        return app;
    }

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
