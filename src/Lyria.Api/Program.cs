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
        configuration
            .ReadFrom.Configuration(services.GetRequiredService<IConfiguration>())
            .ReadFrom.Services(services));

    WebApplication app = builder.Build();

    app.UseLyriaPipeline();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Lyria API terminó de forma inesperada");
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
