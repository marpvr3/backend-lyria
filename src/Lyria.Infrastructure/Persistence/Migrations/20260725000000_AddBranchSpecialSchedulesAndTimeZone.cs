using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lyria.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class AddBranchSpecialSchedulesAndTimeZone : Migration
{
    private static readonly string[] SedeIdFechaColumns = ["SedeId", "Fecha"];
    private static readonly string[] SedeIdFechaEsActivoColumns = ["SedeId", "Fecha", "EsActivo"];

    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // 1. Agregar ZonaHoraria como nullable
        migrationBuilder.AddColumn<string>(
            name: "ZonaHoraria",
            table: "Sedes",
            type: "nvarchar(100)",
            maxLength: 100,
            nullable: true);

        // 2. Backfill explícito para filas existentes
        migrationBuilder.Sql(
            "UPDATE [Sedes] SET [ZonaHoraria] = N'America/Argentina/Buenos_Aires' WHERE [ZonaHoraria] IS NULL;");

        // 3. Alterar a NOT NULL sin dejar un DEFAULT permanente
        migrationBuilder.AlterColumn<string>(
            name: "ZonaHoraria",
            table: "Sedes",
            type: "nvarchar(100)",
            maxLength: 100,
            nullable: false,
            defaultValue: "");

        // Eliminar la restricción DEFAULT residual generada por AlterColumn
        migrationBuilder.Sql(
            """
            DECLARE @defaultName NVARCHAR(256);
            SELECT @defaultName = d.name
            FROM sys.default_constraints d
            JOIN sys.columns c ON d.parent_object_id = c.object_id AND d.parent_column_id = c.column_id
            WHERE c.name = N'ZonaHoraria' AND OBJECT_NAME(c.object_id) = N'Sedes';
            IF @defaultName IS NOT NULL
                EXEC('ALTER TABLE [Sedes] DROP CONSTRAINT [' + @defaultName + ']');
            """);

        // Crear tabla dbo.HorariosEspecialesSede
        migrationBuilder.CreateTable(
            name: "HorariosEspecialesSede",
            columns: table => new
            {
                HorarioEspecialSedeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                SedeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Fecha = table.Column<DateOnly>(type: "date", nullable: false),
                HoraApertura = table.Column<TimeOnly>(type: "time", nullable: true),
                HoraCierre = table.Column<TimeOnly>(type: "time", nullable: true),
                CruzaMedianoche = table.Column<bool>(type: "bit", nullable: false),
                Cerrado = table.Column<bool>(type: "bit", nullable: false),
                Motivo = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                EsActivo = table.Column<bool>(type: "bit", nullable: false),
                FechaCreacionUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                FechaActualizacionUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_HorariosEspecialesSede", x => x.HorarioEspecialSedeId);
                table.ForeignKey(
                    name: "FK_HorariosEspecialesSede_Sedes_SedeId",
                    column: x => x.SedeId,
                    principalTable: "Sedes",
                    principalColumn: "SedeId",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_HorariosEspecialesSede_SedeId",
            table: "HorariosEspecialesSede",
            column: "SedeId");

        migrationBuilder.CreateIndex(
            name: "IX_HorariosEspecialesSede_SedeId_Fecha",
            table: "HorariosEspecialesSede",
            columns: SedeIdFechaColumns);

        migrationBuilder.CreateIndex(
            name: "IX_HorariosEspecialesSede_SedeId_Fecha_EsActivo",
            table: "HorariosEspecialesSede",
            columns: SedeIdFechaEsActivoColumns);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "HorariosEspecialesSede");

        migrationBuilder.DropColumn(
            name: "ZonaHoraria",
            table: "Sedes");
    }
}
