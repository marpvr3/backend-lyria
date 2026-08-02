using Microsoft.AspNetCore.Cors.Infrastructure;

namespace Lyria.Api.Extensions;

/// <summary>
/// Opciones de CORS de Lyria. Se configura bajo la sección "Cors".
/// En el servidor publicado puede establecerse por variables de entorno,
/// por ejemplo: <c>Cors__AllowedOrigins__0=http://localhost:5173</c>.
/// Esta clase solo se usa en el composition root.
/// </summary>
public sealed class LyriaCorsOptions
{
    /// <summary>
    /// Nombre de la sección en appsettings.json.
    /// </summary>
    public const string SectionName = "Cors";

    /// <summary>
    /// Orígenes autorizados de forma explícita. Si está vacía, no se autoriza
    /// ningún origen cruzado (comportamiento seguro por defecto).
    /// </summary>
    public IList<string> AllowedOrigins { get; } = [];
}

/// <summary>
/// Registro de la política CORS de Lyria basada exclusivamente en orígenes
/// configurados de forma explícita. No se utiliza AllowAnyOrigin.
/// </summary>
public static class CorsExtensions
{
    /// <summary>
    /// Nombre de la única política CORS registrada por la aplicación.
    /// </summary>
    public const string PolicyName = "LyriaCorsPolicy";

    private static readonly string[] AllowedMethods =
        ["GET", "POST", "PUT", "PATCH", "DELETE"];

    private static readonly string[] AllowedHeaders =
        ["Accept", "Content-Type", "Authorization"];

    /// <summary>
    /// Registra la política CORS leyendo los orígenes desde "Cors:AllowedOrigins".
    /// </summary>
    public static IServiceCollection AddLyriaCors(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<LyriaCorsOptions>()
            .Bind(configuration.GetSection(LyriaCorsOptions.SectionName));

        services.AddCors();

        // La política se arma de forma diferida a partir de IConfiguration para
        // reflejar la configuración final del host (variables de entorno incluidas).
        services.AddOptions<CorsOptions>()
            .Configure<IConfiguration>((options, config) =>
            {
                string[] allowedOrigins = NormalizeOrigins(
                    config
                        .GetSection(LyriaCorsOptions.SectionName)
                        .GetSection(nameof(LyriaCorsOptions.AllowedOrigins))
                        .Get<string[]>());

                options.AddPolicy(PolicyName, policy =>
                {
                    // Sin orígenes configurados la política no autoriza ningún origen:
                    // el navegador no recibirá Access-Control-Allow-Origin.
                    policy
                        .WithOrigins(allowedOrigins)
                        .WithMethods(AllowedMethods)
                        .WithHeaders(AllowedHeaders);
                });
            });

        return services;
    }

    /// <summary>
    /// Descarta valores vacíos, normaliza espacios y barras finales, y elimina duplicados.
    /// </summary>
    public static string[] NormalizeOrigins(IEnumerable<string?>? origins)
    {
        if (origins is null)
        {
            return [];
        }

        return [.. origins
            .Where(origin => !string.IsNullOrWhiteSpace(origin))
            .Select(origin => origin!.Trim().TrimEnd('/'))
            .Where(origin => origin.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)];
    }
}
