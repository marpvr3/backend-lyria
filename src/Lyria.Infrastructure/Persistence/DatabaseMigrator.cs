using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lyria.Infrastructure.Persistence;

/// <summary>
/// Aplica las migraciones EF Core pendientes que vienen en el ensamblado publicado.
///
/// La fuente de verdad es el historial de migraciones de EF Core
/// (<c>__EFMigrationsHistory</c>): solo se ejecutan las migraciones que aún no
/// figuran aplicadas. No recrea la base, no restaura respaldos y no repara
/// objetos eliminados manualmente fuera de una migración.
/// </summary>
public static partial class DatabaseMigrator
{
    private const string LoggerCategory = "Lyria.Infrastructure.Persistence.DatabaseMigrator";

    /// <summary>
    /// Crea un scope, resuelve el <see cref="DbContext"/> de Lyria y aplica las
    /// migraciones pendientes. Cualquier error se registra y se relanza para que
    /// la aplicación no continúe con una estructura incompleta.
    /// </summary>
    /// <param name="serviceProvider">Proveedor de servicios raíz de la aplicación.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    public static async Task ApplyPendingMigrationsAsync(
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);

        await using AsyncServiceScope scope = serviceProvider.CreateAsyncScope();

        LyriaDbContext context = scope.ServiceProvider.GetRequiredService<LyriaDbContext>();

        ILogger logger = scope.ServiceProvider
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger(LoggerCategory);

        await ApplyPendingMigrationsAsync(context, logger, cancellationToken);
    }

    /// <summary>
    /// Núcleo del mecanismo, independiente del hosting: consulta el historial de
    /// migraciones y aplica únicamente las pendientes.
    /// </summary>
    internal static async Task ApplyPendingMigrationsAsync(
        DbContext context,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(logger);

        List<string> pendingMigrations;

        try
        {
            pendingMigrations =
                [.. await context.Database.GetPendingMigrationsAsync(cancellationToken)];
        }
        catch (Exception exception)
        {
            LogPendingMigrationsQueryFailed(logger, exception);
            throw;
        }

        if (pendingMigrations.Count == 0)
        {
            LogNoPendingMigrations(logger);
            return;
        }

        string migrationIds = string.Join(", ", pendingMigrations);

        LogPendingMigrationsDetected(logger, pendingMigrations.Count, migrationIds);

        try
        {
            await context.Database.MigrateAsync(cancellationToken);
        }
        catch (Exception exception)
        {
            LogMigrationFailed(logger, migrationIds, exception);
            throw;
        }

        LogMigrationsApplied(logger, pendingMigrations.Count, migrationIds);
    }

    [LoggerMessage(
        EventId = 1000,
        Level = LogLevel.Critical,
        Message = "No fue posible consultar las migraciones pendientes de la base de datos. " +
                  "La API no iniciará.")]
    private static partial void LogPendingMigrationsQueryFailed(ILogger logger, Exception exception);

    [LoggerMessage(
        EventId = 1001,
        Level = LogLevel.Information,
        Message = "No hay migraciones pendientes. " +
                  "La estructura de la base de datos está actualizada.")]
    private static partial void LogNoPendingMigrations(ILogger logger);

    [LoggerMessage(
        EventId = 1002,
        Level = LogLevel.Information,
        Message = "Se detectaron {PendingMigrationCount} migraciones pendientes: " +
                  "{PendingMigrations}. Iniciando su aplicación.")]
    private static partial void LogPendingMigrationsDetected(
        ILogger logger,
        int pendingMigrationCount,
        string pendingMigrations);

    [LoggerMessage(
        EventId = 1003,
        Level = LogLevel.Critical,
        Message = "Falló la aplicación de las migraciones pendientes ({PendingMigrations}). " +
                  "La API no iniciará para no operar sobre una estructura incompleta.")]
    private static partial void LogMigrationFailed(
        ILogger logger,
        string pendingMigrations,
        Exception exception);

    [LoggerMessage(
        EventId = 1004,
        Level = LogLevel.Information,
        Message = "Se aplicaron {AppliedMigrationCount} migraciones correctamente: " +
                  "{AppliedMigrations}.")]
    private static partial void LogMigrationsApplied(
        ILogger logger,
        int appliedMigrationCount,
        string appliedMigrations);
}
