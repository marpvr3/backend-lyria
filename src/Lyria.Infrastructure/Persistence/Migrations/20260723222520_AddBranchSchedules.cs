using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lyria.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class AddBranchSchedules : Migration
{
    private static readonly string[] SedeIdDiaSemanaColumns = ["SedeId", "DiaSemana"];
    private static readonly string[] SedeIdDiaSemanaEsActivoColumns = ["SedeId", "DiaSemana", "EsActivo"];

    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "HorariosSede",
            columns: table => new
            {
                HorarioSedeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                SedeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                DiaSemana = table.Column<byte>(type: "tinyint", nullable: false),
                HoraApertura = table.Column<TimeOnly>(type: "time", nullable: true),
                HoraCierre = table.Column<TimeOnly>(type: "time", nullable: true),
                CruzaMedianoche = table.Column<bool>(type: "bit", nullable: false),
                Cerrado = table.Column<bool>(type: "bit", nullable: false),
                EsActivo = table.Column<bool>(type: "bit", nullable: false),
                FechaCreacionUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                FechaActualizacionUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_HorariosSede", x => x.HorarioSedeId);
                table.ForeignKey(
                    name: "FK_HorariosSede_Sedes_SedeId",
                    column: x => x.SedeId,
                    principalTable: "Sedes",
                    principalColumn: "SedeId",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_HorariosSede_SedeId",
            table: "HorariosSede",
            column: "SedeId");

        migrationBuilder.CreateIndex(
            name: "IX_HorariosSede_SedeId_DiaSemana",
            table: "HorariosSede",
            columns: SedeIdDiaSemanaColumns);

        migrationBuilder.CreateIndex(
            name: "IX_HorariosSede_SedeId_DiaSemana_EsActivo",
            table: "HorariosSede",
            columns: SedeIdDiaSemanaEsActivoColumns);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "HorariosSede");
    }
}
