using Lyria.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Lyria.Infrastructure.IntegrationTests;

internal sealed class SqliteFixture : IDisposable
{
    private readonly SqliteConnection _connection;

    public SqliteFixture()
        : this(TimeProvider.System)
    {
    }

    public SqliteFixture(TimeProvider timeProvider)
    {
        TimeProvider = timeProvider;
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        _connection.CreateCollation("SQL_Latin1_General_CP1_CI_AS",
            (x, y) => string.Compare(x, y, StringComparison.OrdinalIgnoreCase));

        var options = new DbContextOptionsBuilder<LyriaDbContext>()
            .UseSqlite(_connection)
            .Options;

        Options = options;

        using var context = new LyriaDbContext(options, timeProvider);
        context.Database.EnsureCreated();
    }

    public DbContextOptions<LyriaDbContext> Options { get; }
    public TimeProvider TimeProvider { get; }

    public LyriaDbContext CreateContext() => new(Options, TimeProvider);

    public void Dispose()
    {
        _connection.Dispose();
    }
}
