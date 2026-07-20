using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lyria.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class AddEstablishmentBranches : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Sedes",
            columns: table => new
            {
                SedeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                EstablecimientoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Nombre = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                Calle = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false),
                Numero = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: true),
                ComplementoDireccion = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: true),
                Barrio = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                Ciudad = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                Provincia = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                CodigoPostal = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: true),
                Pais = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                Latitud = table.Column<decimal>(type: "decimal(9,6)", nullable: true),
                Longitud = table.Column<decimal>(type: "decimal(9,6)", nullable: true),
                Telefono = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: true),
                Whatsapp = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: true),
                Email = table.Column<string>(type: "varchar(254)", unicode: false, maxLength: 254, nullable: true),
                RatingPromedio = table.Column<decimal>(type: "decimal(3,2)", nullable: false),
                TotalResenas = table.Column<int>(type: "int", nullable: false),
                Activo = table.Column<bool>(type: "bit", nullable: false),
                FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                FechaActualizacion = table.Column<DateTime>(type: "datetime2", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Sedes", x => x.SedeId);
                table.ForeignKey(
                    name: "FK_Sedes_Establecimientos_EstablecimientoId",
                    column: x => x.EstablecimientoId,
                    principalTable: "Establecimientos",
                    principalColumn: "EstablecimientoId",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_Sedes_Ciudad",
            table: "Sedes",
            column: "Ciudad");

        migrationBuilder.CreateIndex(
            name: "IX_Sedes_EstablecimientoId_Activo",
            table: "Sedes",
            columns: ["EstablecimientoId", "Activo"]);

        migrationBuilder.CreateIndex(
            name: "IX_Sedes_Provincia",
            table: "Sedes",
            column: "Provincia");

        migrationBuilder.CreateIndex(
            name: "UX_Sedes_EstablecimientoId_Nombre",
            table: "Sedes",
            columns: ["EstablecimientoId", "Nombre"],
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "Sedes");
    }
}
