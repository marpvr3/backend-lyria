using Lyria.Application.Abstractions.Persistence;
using Lyria.Infrastructure.Persistence;
using Lyria.Infrastructure.Persistence.ReadServices;
using Lyria.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Lyria.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        string connectionString = configuration.GetConnectionString("LyriaDatabase")
            ?? throw new InvalidOperationException(
                "No se configuró 'ConnectionStrings:LyriaDatabase'. " +
                "Defina la variable de entorno ConnectionStrings__LyriaDatabase " +
                "o configúrela en appsettings.Development.json / User Secrets.");

        services.AddSingleton(TimeProvider.System);

        services.AddDbContext<LyriaDbContext>(options =>
            options.UseSqlServer(connectionString, sqlOptions =>
                sqlOptions.MigrationsAssembly(typeof(LyriaDbContext).Assembly.FullName)));

        services.AddScoped<IEstablishmentCategoryRepository, EstablishmentCategoryRepository>();
        services.AddScoped<IEstablishmentCategoryReadService, EstablishmentCategoryReadService>();

        services.AddScoped<IEstablishmentRepository, EstablishmentRepository>();
        services.AddScoped<IEstablishmentReadService, EstablishmentReadService>();

        services.AddScoped<IEstablishmentBranchRepository, EstablishmentBranchRepository>();
        services.AddScoped<IEstablishmentBranchReadService, EstablishmentBranchReadService>();

        services.AddScoped<IRestrictionRepository, RestrictionRepository>();
        services.AddScoped<IRestrictionReadService, RestrictionReadService>();

        services.AddScoped<IServiceRepository, ServiceRepository>();
        services.AddScoped<IServiceReadService, ServiceReadService>();

        services.AddScoped<IEstablishmentBranchServiceRepository, EstablishmentBranchServiceRepository>();
        services.AddScoped<IEstablishmentBranchServiceReadService, EstablishmentBranchServiceReadService>();

        services.AddScoped<IEstablishmentBranchRestrictionRepository, EstablishmentBranchRestrictionRepository>();
        services.AddScoped<IEstablishmentBranchRestrictionReadService, EstablishmentBranchRestrictionReadService>();

        services.AddScoped<IBranchScheduleRepository, BranchScheduleRepository>();
        services.AddScoped<IBranchScheduleReadService, BranchScheduleReadService>();

        services.AddScoped<IBranchImageRepository, BranchImageRepository>();
        services.AddScoped<IBranchImageReadService, BranchImageReadService>();

        return services;
    }
}
