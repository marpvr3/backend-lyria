using Lyria.Application;
using Lyria.Infrastructure;
using Microsoft.OpenApi;

namespace Lyria.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddLyriaServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
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
                    "⚠️ Esta API no cuenta con autenticación. " +
                    "No exponer en entornos de producción sin un gateway de seguridad."
            });

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

        return services;
    }
}
