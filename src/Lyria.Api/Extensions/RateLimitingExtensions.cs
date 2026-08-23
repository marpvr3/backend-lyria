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
/// Opciones de limitación de solicitudes del reenvío de códigos de verificación.
/// Se configura bajo la sección "RateLimiting:EmailVerificationResend".
/// </summary>
/// <remarks>
/// Es el límite por dirección IP. Se suma —no sustituye— al intervalo mínimo persistido
/// por usuario, que sobrevive a un cambio de dirección del solicitante.
/// </remarks>
public sealed class EmailVerificationResendRateLimitOptions
{
    /// <summary>
    /// Nombre de la sección en la configuración.
    /// </summary>
    public const string SectionName = "RateLimiting:EmailVerificationResend";

    /// <summary>
    /// Solicitudes permitidas por ventana y por cliente.
    /// </summary>
    [Range(1, int.MaxValue, ErrorMessage =
        "El límite de solicitudes (RateLimiting:EmailVerificationResend:PermitLimit) debe ser mayor que cero.")]
    public int PermitLimit { get; set; } = 3;

    /// <summary>
    /// Duración de la ventana, en segundos.
    /// </summary>
    [Range(1, int.MaxValue, ErrorMessage =
        "La ventana (RateLimiting:EmailVerificationResend:WindowSeconds) debe ser mayor que cero.")]
    public int WindowSeconds { get; set; } = 900;
}

/// <summary>
/// Opciones de limitación de solicitudes de la confirmación de códigos.
/// Se configura bajo la sección "RateLimiting:EmailVerificationConfirm".
/// </summary>
/// <remarks>
/// Es el límite por dirección IP. Se suma al máximo de intentos fallidos que admite cada
/// código, que es la defensa real contra el recorrido del espacio de seis dígitos.
/// </remarks>
public sealed class EmailVerificationConfirmRateLimitOptions
{
    /// <summary>
    /// Nombre de la sección en la configuración.
    /// </summary>
    public const string SectionName = "RateLimiting:EmailVerificationConfirm";

    /// <summary>
    /// Solicitudes permitidas por ventana y por cliente.
    /// </summary>
    [Range(1, int.MaxValue, ErrorMessage =
        "El límite de solicitudes (RateLimiting:EmailVerificationConfirm:PermitLimit) debe ser mayor que cero.")]
    public int PermitLimit { get; set; } = 10;

    /// <summary>
    /// Duración de la ventana, en segundos.
    /// </summary>
    [Range(1, int.MaxValue, ErrorMessage =
        "La ventana (RateLimiting:EmailVerificationConfirm:WindowSeconds) debe ser mayor que cero.")]
    public int WindowSeconds { get; set; } = 900;
}

/// <summary>
/// Registro de las políticas de limitación de solicitudes.
/// </summary>
/// <remarks>
/// Todas las políticas son <b>nombradas y de aplicación explícita</b>: solo las usan los
/// endpoints que las declaran con <c>[EnableRateLimiting]</c>. No existe límite global,
/// de modo que health, Swagger, el catálogo público y el registro móvil no se ven
/// afectados.
/// </remarks>
public static class RateLimitingExtensions
{
    /// <summary>
    /// Política de limitación del inicio de sesión y la renovación de tokens.
    /// </summary>
    public const string AuthenticationPolicyName = "LyriaAuthenticationRateLimit";

    /// <summary>
    /// Política de limitación del reenvío de códigos de verificación.
    /// </summary>
    public const string EmailVerificationResendPolicyName =
        "LyriaEmailVerificationResendRateLimit";

    /// <summary>
    /// Política de limitación de la confirmación de códigos de verificación.
    /// </summary>
    public const string EmailVerificationConfirmPolicyName =
        "LyriaEmailVerificationConfirmRateLimit";

    /// <summary>
    /// Registra las políticas nombradas de autenticación y verificación de correo.
    /// </summary>
    public static IServiceCollection AddLyriaRateLimiting(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<AuthenticationRateLimitOptions>()
            .Bind(configuration.GetSection(AuthenticationRateLimitOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<EmailVerificationResendRateLimitOptions>()
            .Bind(configuration.GetSection(EmailVerificationResendRateLimitOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<EmailVerificationConfirmRateLimitOptions>()
            .Bind(configuration.GetSection(EmailVerificationConfirmRateLimitOptions.SectionName))
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
                AuthenticationRateLimitOptions limits = Resolve<AuthenticationRateLimitOptions>(
                    httpContext);

                return CreatePartition(
                    httpContext, limits.PermitLimit, limits.WindowSeconds);
            });

            options.AddPolicy(EmailVerificationResendPolicyName, httpContext =>
            {
                EmailVerificationResendRateLimitOptions limits =
                    Resolve<EmailVerificationResendRateLimitOptions>(httpContext);

                return CreatePartition(
                    httpContext, limits.PermitLimit, limits.WindowSeconds);
            });

            options.AddPolicy(EmailVerificationConfirmPolicyName, httpContext =>
            {
                EmailVerificationConfirmRateLimitOptions limits =
                    Resolve<EmailVerificationConfirmRateLimitOptions>(httpContext);

                return CreatePartition(
                    httpContext, limits.PermitLimit, limits.WindowSeconds);
            });
        });

        return services;
    }

    private static TOptions Resolve<TOptions>(HttpContext httpContext)
        where TOptions : class =>
        httpContext.RequestServices
            .GetRequiredService<Microsoft.Extensions.Options.IOptions<TOptions>>()
            .Value;

    /// <summary>
    /// Ventana fija particionada por cliente, común a todas las políticas.
    /// </summary>
    private static RateLimitPartition<string> CreatePartition(
        HttpContext httpContext,
        int permitLimit,
        int windowSeconds) =>
        RateLimitPartition.GetFixedWindowLimiter(
            ResolvePartitionKey(httpContext),
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = permitLimit,
                Window = TimeSpan.FromSeconds(windowSeconds),

                // Sin cola: el exceso se rechaza de inmediato con 429 en lugar
                // de mantener abiertas conexiones de un posible ataque.
                QueueLimit = 0,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                AutoReplenishment = true
            });

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
