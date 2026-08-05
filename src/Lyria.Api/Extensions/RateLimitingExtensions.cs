using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace Lyria.Api.Extensions;

/// <summary>
/// Opciones de limitación de solicitudes de los endpoints de autenticación.
/// Se configura bajo la sección "RateLimiting:Authentication".
/// </summary>
public sealed class AuthenticationRateLimitOptions
{
    /// <summary>
    /// Nombre de la sección en la configuración.
    /// </summary>
    public const string SectionName = "RateLimiting:Authentication";

    /// <summary>
    /// Solicitudes permitidas por ventana y por cliente.
    /// </summary>
    [Range(1, int.MaxValue, ErrorMessage =
        "El límite de solicitudes (RateLimiting:Authentication:PermitLimit) debe ser mayor que cero.")]
    public int PermitLimit { get; set; } = 10;

    /// <summary>
    /// Duración de la ventana, en segundos.
    /// </summary>
    [Range(1, int.MaxValue, ErrorMessage =
        "La ventana (RateLimiting:Authentication:WindowSeconds) debe ser mayor que cero.")]
    public int WindowSeconds { get; set; } = 60;
}

/// <summary>
/// Registro de la política de limitación de solicitudes de autenticación.
/// </summary>
/// <remarks>
/// La política es <b>nombrada y de aplicación explícita</b>: solo la usan los endpoints
/// que la declaran con <c>[EnableRateLimiting]</c>. No existe límite global, de modo que
/// health, Swagger, el catálogo público y el registro móvil no se ven afectados.
/// </remarks>
public static class RateLimitingExtensions
{
    /// <summary>
    /// Nombre de la única política de limitación registrada por la aplicación.
    /// </summary>
    public const string AuthenticationPolicyName = "LyriaAuthenticationRateLimit";

    /// <summary>
    /// Registra la política nombrada para el inicio de sesión y la renovación de tokens.
    /// </summary>
    public static IServiceCollection AddLyriaRateLimiting(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<AuthenticationRateLimitOptions>()
            .Bind(configuration.GetSection(AuthenticationRateLimitOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.OnRejected = static (context, cancellationToken) =>
            {
                if (context.Lease.TryGetMetadata(
                        MetadataName.RetryAfter, out TimeSpan retryAfter))
                {
                    context.HttpContext.Response.Headers.RetryAfter =
                        ((int)Math.Ceiling(retryAfter.TotalSeconds))
                        .ToString(NumberFormatInfo.InvariantInfo);
                }

                // Sin cuerpo detallado: la respuesta no debe revelar el estado interno
                // del contador ni ninguna información de la cuenta.
                return ValueTask.CompletedTask;
            };

            options.AddPolicy(AuthenticationPolicyName, httpContext =>
            {
                AuthenticationRateLimitOptions limits = httpContext.RequestServices
                    .GetRequiredService<
                        Microsoft.Extensions.Options.IOptions<AuthenticationRateLimitOptions>>()
                    .Value;

                return RateLimitPartition.GetFixedWindowLimiter(
                    ResolvePartitionKey(httpContext),
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = limits.PermitLimit,
                        Window = TimeSpan.FromSeconds(limits.WindowSeconds),

                        // Sin cola: el exceso se rechaza de inmediato con 429 en lugar
                        // de mantener abiertas conexiones de un posible ataque.
                        QueueLimit = 0,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        AutoReplenishment = true
                    });
            });
        });

        return services;
    }

    /// <summary>
    /// Clave de partición del limitador.
    /// </summary>
    /// <remarks>
    /// Se usa la dirección remota real de la conexión. <b>No</b> se particiona por
    /// <c>X-Forwarded-For</c>: esa cabecera la puede falsificar cualquier cliente y hoy
    /// la aplicación no tiene configurado <c>ForwardedHeaders</c> con una lista de
    /// proxies de confianza. Cuando se despliegue detrás de un proxy con TLS habrá que
    /// configurar <c>ForwardedHeaders</c> y revisar esta partición.
    /// </remarks>
    private static string ResolvePartitionKey(HttpContext httpContext)
    {
        return httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }
}
