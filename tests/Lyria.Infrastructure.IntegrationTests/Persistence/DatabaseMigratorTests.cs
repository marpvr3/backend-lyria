using System.Globalization;
using Lyria.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Lyria.Infrastructure.IntegrationTests.Persistence;

/// <summary>
/// Verifica el mecanismo de aplicación automática de migraciones sobre una base
/// relacional aislada (SQLite en memoria) usando un contexto de sondeo con
/// migraciones propias.
/// </summary>
public sealed class DatabaseMigratorTests : IDisposable
{
    private const string FirstMigration = "20260101000000_CreateProbeTable";
    private const string SecondMigration = "20260101000001_AddProbeColumn";

    private readonly SqliteConnection _connection;

    public DatabaseMigratorTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
    }

    [Fact]
    public async Task ApplyPendingMigrations_AppliesAllPendingMigrations()
    {
        await using MigrationProbeDbContext context = CreateContext();
        var logger = new RecordingLogger();

        await DatabaseMigrator.ApplyPendingMigrationsAsync(
            context, logger, TestContext.Current.CancellationToken);

        IEnumerable<string> applied = await context.Database.GetAppliedMigrationsAsync(
            TestContext.Current.CancellationToken);

        Assert.Equal([FirstMigration, SecondMigration], applied);
        Assert.True(await TableExistsAsync(CreateProbeTable.TableName));
    }

    [Fact]
    public async Task ApplyPendingMigrations_LogsPendingMigrationIdentifiers()
    {
        await using MigrationProbeDbContext context = CreateContext();
        var logger = new RecordingLogger();

        await DatabaseMigrator.ApplyPendingMigrationsAsync(
            context, logger, TestContext.Current.CancellationToken);

        Assert.True(logger.ContainsMessage(FirstMigration), logger.ToString());
        Assert.True(logger.ContainsMessage(SecondMigration), logger.ToString());
        Assert.DoesNotContain(logger.Entries, entry => entry.Level >= LogLevel.Error);
    }

    [Fact]
    public async Task ApplyPendingMigrations_WithoutPendingMigrations_DoesNothing()
    {
        await using MigrationProbeDbContext firstRun = CreateContext();
        await DatabaseMigrator.ApplyPendingMigrationsAsync(
            firstRun, new RecordingLogger(), TestContext.Current.CancellationToken);

        await using MigrationProbeDbContext secondRun = CreateContext();
        var logger = new RecordingLogger();

        await DatabaseMigrator.ApplyPendingMigrationsAsync(
            secondRun, logger, TestContext.Current.CancellationToken);

        IEnumerable<string> pending = await secondRun.Database.GetPendingMigrationsAsync(
            TestContext.Current.CancellationToken);

        Assert.Empty(pending);
        Assert.True(logger.ContainsMessage("No hay migraciones pendientes"), logger.ToString());
        Assert.Contains(logger.Entries, entry => entry.EventId.Id == 1001);
    }

    [Fact]
    public async Task ApplyPendingMigrations_OnSecondStartup_DoesNotReapplyHistory()
    {
        await using MigrationProbeDbContext firstRun = CreateContext();
        await DatabaseMigrator.ApplyPendingMigrationsAsync(
            firstRun, new RecordingLogger(), TestContext.Current.CancellationToken);

        await using MigrationProbeDbContext secondRun = CreateContext();
        await DatabaseMigrator.ApplyPendingMigrationsAsync(
            secondRun, new RecordingLogger(), TestContext.Current.CancellationToken);

        IEnumerable<string> applied = await secondRun.Database.GetAppliedMigrationsAsync(
            TestContext.Current.CancellationToken);

        Assert.Equal([FirstMigration, SecondMigration], applied);
    }

    [Fact]
    public async Task ApplyPendingMigrations_PreservesExistingData()
    {
        await using MigrationProbeDbContext initial = CreateContext();

        // Deja la base en el estado de la primera migración, como una instalación
        // productiva que aún no recibió la migración nueva.
        await initial.GetService<IMigrator>().MigrateAsync(
            FirstMigration, TestContext.Current.CancellationToken);

        await ExecuteAsync(
            $"INSERT INTO {CreateProbeTable.TableName} (SondaId, Nombre) VALUES (1, 'dato existente')");

        await using MigrationProbeDbContext startup = CreateContext();
        var logger = new RecordingLogger();

        await DatabaseMigrator.ApplyPendingMigrationsAsync(
            startup, logger, TestContext.Current.CancellationToken);

        IEnumerable<string> applied = await startup.Database.GetAppliedMigrationsAsync(
            TestContext.Current.CancellationToken);

        Assert.Equal([FirstMigration, SecondMigration], applied);
        Assert.Equal(
            "dato existente",
            await QueryScalarAsync($"SELECT Nombre FROM {CreateProbeTable.TableName} WHERE SondaId = 1"));
        Assert.True(logger.ContainsMessage(SecondMigration), logger.ToString());
    }

    [Fact]
    public async Task ApplyPendingMigrations_WhenMigrationFails_PropagatesException()
    {
        // Un objeto preexistente hace fallar la migración, igual que ocurriría con
        // una migración inválida o con permisos insuficientes en SQL Server.
        await ExecuteAsync($"CREATE TABLE {CreateProbeTable.TableName} (SondaId INTEGER)");

        await using MigrationProbeDbContext context = CreateContext();
        var logger = new RecordingLogger();

        await Assert.ThrowsAsync<SqliteException>(() =>
            DatabaseMigrator.ApplyPendingMigrationsAsync(
                context, logger, TestContext.Current.CancellationToken));

        RecordedLog failure = Assert.Single(
            logger.Entries,
            entry => entry.Level == LogLevel.Critical);

        Assert.Equal(1003, failure.EventId.Id);
        Assert.NotNull(failure.Exception);
        Assert.Contains(FirstMigration, failure.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ApplyPendingMigrations_WhenMigrationFails_DoesNotRecordHistory()
    {
        await ExecuteAsync($"CREATE TABLE {CreateProbeTable.TableName} (SondaId INTEGER)");

        await using MigrationProbeDbContext context = CreateContext();

        await Assert.ThrowsAsync<SqliteException>(() =>
            DatabaseMigrator.ApplyPendingMigrationsAsync(
                context, new RecordingLogger(), TestContext.Current.CancellationToken));

        await using MigrationProbeDbContext verification = CreateContext();
        IEnumerable<string> applied = await verification.Database.GetAppliedMigrationsAsync(
            TestContext.Current.CancellationToken);

        Assert.Empty(applied);
    }

    public void Dispose() => _connection.Dispose();

    private MigrationProbeDbContext CreateContext()
    {
        DbContextOptions<MigrationProbeDbContext> options =
            new DbContextOptionsBuilder<MigrationProbeDbContext>()
                .UseSqlite(_connection, sqlite =>
                    sqlite.MigrationsAssembly(typeof(CreateProbeTable).Assembly.FullName))
                .Options;

        return new MigrationProbeDbContext(options);
    }

    private async Task ExecuteAsync(string sql)
    {
        await using SqliteCommand command = _connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);
    }

    private async Task<string?> QueryScalarAsync(string sql)
    {
        await using SqliteCommand command = _connection.CreateCommand();
        command.CommandText = sql;

        object? result = await command.ExecuteScalarAsync(TestContext.Current.CancellationToken);

        return result is null or DBNull
            ? null
            : Convert.ToString(result, CultureInfo.InvariantCulture);
    }

    private async Task<bool> TableExistsAsync(string tableName)
    {
        string? found = await QueryScalarAsync(
            $"SELECT name FROM sqlite_master WHERE type = 'table' AND name = '{tableName}'");

        return found is not null;
    }
}
