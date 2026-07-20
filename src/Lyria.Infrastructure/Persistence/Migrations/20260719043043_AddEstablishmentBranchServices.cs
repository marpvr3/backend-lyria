using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lyria.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class AddEstablishmentBranchServices : Migration
{
    private static readonly string[] SedeIdActivoColumns = ["SedeId", "Activo"];
    private static readonly string[] ServicioIdActivoColumns = ["ServicioId", "Activo"];

    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "SedesServicios",
            columns: table => new
            {
                SedeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                ServicioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Disponible = table.Column<bool>(type: "bit", nullable: false),
                Observacion = table.Column<string>(type: "varchar(500)", unicode: false, maxLength: 500, nullable: true),
                Activo = table.Column<bool>(type: "bit", nullable: false),
                FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                FechaActualizacion = table.Column<DateTime>(type: "datetime2", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_SedesServicios", x => new { x.SedeId, x.ServicioId });
                table.ForeignKey(
                    name: "FK_SedesServicios_Sedes_SedeId",
                    column: x => x.SedeId,
                    principalTable: "Sedes",
                    principalColumn: "SedeId",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_SedesServicios_Servicios_ServicioId",
                    column: x => x.ServicioId,
                    principalTable: "Servicios",
                    principalColumn: "ServicioId",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_SedesServicios_SedeId_Activo",
            table: "SedesServicios",
            columns: SedeIdActivoColumns);

        migrationBuilder.CreateIndex(
            name: "IX_SedesServicios_ServicioId",
            table: "SedesServicios",
            column: "ServicioId");

        migrationBuilder.CreateIndex(
            name: "IX_SedesServicios_ServicioId_Activo",
            table: "SedesServicios",
            columns: ServicioIdActivoColumns);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "SedesServicios");
    }
}
