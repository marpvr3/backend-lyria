using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Lyria.Infrastructure.IntegrationTests.Persistence;

/// <summary>
/// Contexto mínimo con migraciones propias, usado para ejercitar
/// <see cref="Lyria.Infrastructure.Persistence.DatabaseMigrator"/> sobre una base
/// relacional aislada (SQLite). No comparte modelo ni migraciones con
/// <c>LyriaDbContext</c>: las migraciones de Lyria son específicas de SQL Server.
/// </summary>
internal sealed class MigrationProbeDbContext(DbContextOptions<MigrationProbeDbContext> options)
    : DbContext(options);

[DbContext(typeof(MigrationProbeDbContext))]
[Migration("20260101000000_CreateProbeTable")]
internal sealed class CreateProbeTable : Migration
{
    public const string TableName = "SondasMigracion";

    protected override void Up(MigrationBuilder migrationBuilder)
    {
        ArgumentNullException.ThrowIfNull(migrationBuilder);

        migrationBuilder.CreateTable(
            name: TableName,
            columns: table => new
            {
                SondaId = table.Column<int>(type: "INTEGER", nullable: false),
                Nombre = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false)
            },
            constraints: table => table.PrimaryKey($"PK_{TableName}", x => x.SondaId));
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        ArgumentNullException.ThrowIfNull(migrationBuilder);

        migrationBuilder.DropTable(TableName);
    }
}

[DbContext(typeof(MigrationProbeDbContext))]
[Migration("20260101000001_AddProbeColumn")]
internal sealed class AddProbeColumn : Migration
{
    public const string ColumnName = "Descripcion";

    protected override void Up(MigrationBuilder migrationBuilder)
    {
        ArgumentNullException.ThrowIfNull(migrationBuilder);

        migrationBuilder.AddColumn<string>(
            name: ColumnName,
            table: CreateProbeTable.TableName,
            type: "TEXT",
            maxLength: 200,
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        ArgumentNullException.ThrowIfNull(migrationBuilder);

        migrationBuilder.DropColumn(ColumnName, CreateProbeTable.TableName);
    }
}
