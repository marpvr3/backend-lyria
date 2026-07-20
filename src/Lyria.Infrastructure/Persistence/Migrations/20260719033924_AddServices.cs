using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lyria.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class AddServices : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Servicios",
            columns: table => new
            {
                ServicioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Nombre = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false, collation: "SQL_Latin1_General_CP1_CI_AS"),
                Descripcion = table.Column<string>(type: "varchar(500)", unicode: false, maxLength: 500, nullable: true),
                IconoUrl = table.Column<string>(type: "varchar(500)", unicode: false, maxLength: 500, nullable: true),
                Activo = table.Column<bool>(type: "bit", nullable: false),
                FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                FechaActualizacion = table.Column<DateTime>(type: "datetime2", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Servicios", x => x.ServicioId);
            });

        migrationBuilder.CreateIndex(
            name: "IX_Servicios_Activo",
            table: "Servicios",
            column: "Activo");

        migrationBuilder.CreateIndex(
            name: "UX_Servicios_Nombre",
            table: "Servicios",
            column: "Nombre",
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "Servicios");
    }
}
