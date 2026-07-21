namespace Lyria.Api.Extensions;

public static class WebApplicationExtensions
{
    public static WebApplication UseLyriaPipeline(this WebApplication app)
    {
        app.UseExceptionHandler();

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
