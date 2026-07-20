using Lyria.Api.Extensions;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddLyriaServices(builder.Configuration);

WebApplication app = builder.Build();

app.UseLyriaPipeline();

app.Run();

namespace Lyria.Api
{
    public partial class Program;
}
