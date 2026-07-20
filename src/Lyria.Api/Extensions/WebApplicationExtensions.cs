namespace Lyria.Api.Extensions;

public static class WebApplicationExtensions
{
    public static WebApplication UseLyriaPipeline(this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint(
                    "/swagger/v1/swagger.json",
                    "Lyria API v1");

                options.RoutePrefix = "swagger";
            });
        }

        app.UseExceptionHandler();

        app.MapControllers();

        return app;
    }
}
