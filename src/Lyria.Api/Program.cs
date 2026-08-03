using System.Globalization;
using Lyria.Api.Extensions;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console(formatProvider: CultureInfo.InvariantCulture)
    .CreateBootstrapLogger();

try
{
    Log.Information("Iniciando Lyria API");

    WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

    builder.Services.AddLyriaServices(builder.Configuration);

    builder.Services.AddSerilog((services, configuration) =>
    {
        IConfiguration config = services.GetRequiredService<IConfiguration>();
        configuration
            .ReadFrom.Configuration(config)
            .ReadFrom.Services(services)
            .AddDatabaseLogging(config);
    });

    WebApplication app = builder.Build();

    // Las migraciones pendientes se aplican antes de configurar el pipeline HTTP,
    // de modo que la API nunca atienda solicitudes con una estructura incompleta.
    await app.ApplyPendingMigrationsAsync();

    app.UseLyriaPipeline();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Lyria API terminó de forma inesperada");

    // Se relanza para que el arranque falle con un código de salida distinto de cero:
    // un fallo de migración o de configuración no puede pasar inadvertido.
    throw;
}
finally
{
    Log.Information("Finalizando Lyria API");
    Log.CloseAndFlush();
}

namespace Lyria.Api
{
    public partial class Program;
}
