using Lyria.Api.Security;
using Lyria.Application;
using Lyria.Application.Abstractions.Security;
using Lyria.Application.Features.BranchSpecialSchedules;
using Lyria.Application.Features.MobileRegistrations;
using Lyria.Infrastructure;
using Lyria.Infrastructure.Notifications;
using Lyria.Infrastructure.Persistence;
using Lyria.Infrastructure.Security;
using Microsoft.OpenApi;

namespace Lyria.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddLyriaServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<BranchTimeZoneOptions>()
            .Bind(configuration.GetSection(BranchTimeZoneOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<DatabaseStartupOptions>()
            .Bind(configuration.GetSection(DatabaseStartupOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // El registro móvil se valida al invocar el caso de uso, no al arrancar:
        // un ambiente que no expone ese flujo no debe impedir el inicio de la API.
        // Una configuración ausente o inválida produce un error controlado en el endpoint.
        services.AddOptions<MobileRegistrationOptions>()
            .Bind(configuration.GetSection(MobileRegistrationOptions.SectionName));

        // La configuración JWT sí se valida al arrancar: sin ella la API no puede
        // emitir ni validar access tokens, y arrancar sin validarla llevaría a emitir
        // tokens con una clave ausente o demasiado corta.
        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // La verificación de correo también se valida al arrancar: sin el secreto los
        // códigos se hashearían con una clave vacía, lo que equivaldría a almacenarlos
        // sin protección frente a una filtración de la base de datos.
        services.AddOptions<EmailVerificationOptions>()
            .Bind(configuration.GetSection(EmailVerificationOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // La configuración SMTP no se valida al arrancar: un ambiente que no envía correo
        // no debe impedir el inicio de la API. Un problema de configuración produce un
        // error técnico registrado en el momento del envío, sin exponer detalles al cliente.
        services.AddOptions<EmailOptions>()
            .Bind(configuration.GetSection(EmailOptions.SectionName));

        services.AddLyriaCors(configuration);
        services.AddLyriaAuthentication();
        services.AddLyriaRateLimiting(configuration);

        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, CurrentUser>();

        services.AddApplication();
        services.AddInfrastructure(configuration);
        services.AddMediator(options =>
            options.ServiceLifetime = ServiceLifetime.Scoped);

        services.AddControllers();
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Lyria API",
                Version = "v1",
                Description =
                    "API REST para la gestión de establecimientos gastronómicos " +
                    "y sus categorías en la plataforma Lyria.\n\n" +
                    "Los endpoints de la aplicación móvil marcados con candado requieren " +
                    "un access token JWT (`Authorization: Bearer {accessToken}`), que se " +
                    "obtiene en `POST /api/v1/auth/login`.\n\n" +
                    "⚠️ Los endpoints administrativos y públicos siguen sin autenticación. " +
                    "No exponer en producción sin un gateway de seguridad.\n\n" +
                    "⚠️ Credenciales y tokens solo deben transmitirse sobre HTTPS."
            });

            // Esquema Bearer para poder autorizar solicitudes desde la propia interfaz.
            options.AddSecurityDefinition(
                BearerSecurityOperationFilter.SchemeName,
                new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Description =
                        "Access token JWT obtenido en POST /api/v1/auth/login. " +
                        "Introduzca únicamente el token: el prefijo 'Bearer' se agrega automáticamente."
                });

            // El requisito de seguridad se aplica por operación, no de forma global:
            // login, refresh, logout, el registro móvil y el catálogo público son anónimos.
            options.OperationFilter<BearerSecurityOperationFilter>();

            string basePath = AppContext.BaseDirectory;

            string apiXml = Path.Combine(basePath, "Lyria.Api.xml");
            options.IncludeXmlComments(apiXml);

            string applicationXml = Path.Combine(basePath, "Lyria.Application.xml");
            if (File.Exists(applicationXml))
            {
                options.IncludeXmlComments(applicationXml);
            }
        });
        services.AddProblemDetails();
        services.AddExceptionHandler<UniqueConstraintExceptionHandler>();
        services.AddExceptionHandler<GlobalExceptionHandler>();

        return services;
    }
}
