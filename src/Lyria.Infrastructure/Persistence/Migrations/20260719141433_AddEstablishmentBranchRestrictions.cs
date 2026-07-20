using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lyria.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class AddEstablishmentBranchRestrictions : Migration
{
    private static readonly string[] SedeIdActivoColumns = ["SedeId", "Activo"];
    private static readonly string[] RestriccionIdActivoColumns = ["RestriccionId", "Activo"];
    private static readonly string[] SedeIdNivelCumplimientoColumns = ["SedeId", "NivelCumplimiento"];

    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "SedesRestricciones",
            columns: table => new
            {
                SedeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                RestriccionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                NivelCumplimiento = table.Column<byte>(type: "tinyint", nullable: false),
                Certificado = table.Column<bool>(type: "bit", nullable: false),
                Observacion = table.Column<string>(type: "varchar(500)", unicode: false, maxLength: 500, nullable: true),
                Activo = table.Column<bool>(type: "bit", nullable: false),
                FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                FechaActualizacion = table.Column<DateTime>(type: "datetime2", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_SedesRestricciones", x => new { x.SedeId, x.RestriccionId });
                table.ForeignKey(
                    name: "FK_SedesRestricciones_Restricciones_RestriccionId",
                    column: x => x.RestriccionId,
                    principalTable: "Restricciones",
                    principalColumn: "RestriccionId",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_SedesRestricciones_Sedes_SedeId",
                    column: x => x.SedeId,
                    principalTable: "Sedes",
                    principalColumn: "SedeId",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_SedesRestricciones_RestriccionId",
            table: "SedesRestricciones",
            column: "RestriccionId");

        migrationBuilder.CreateIndex(
            name: "IX_SedesRestricciones_RestriccionId_Activo",
            table: "SedesRestricciones",
            columns: RestriccionIdActivoColumns);

        migrationBuilder.CreateIndex(
            name: "IX_SedesRestricciones_SedeId_Activo",
            table: "SedesRestricciones",
            columns: SedeIdActivoColumns);

        migrationBuilder.CreateIndex(
            name: "IX_SedesRestricciones_SedeId_NivelCumplimiento",
            table: "SedesRestricciones",
            columns: SedeIdNivelCumplimientoColumns);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "SedesRestricciones");
    }
}
