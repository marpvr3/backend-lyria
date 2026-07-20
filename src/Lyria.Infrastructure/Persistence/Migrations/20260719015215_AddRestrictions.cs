using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lyria.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class AddRestrictions : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Restricciones",
            columns: table => new
            {
                RestriccionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Nombre = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false, collation: "SQL_Latin1_General_CP1_CI_AS"),
                Descripcion = table.Column<string>(type: "varchar(500)", unicode: false, maxLength: 500, nullable: true),
                Activo = table.Column<bool>(type: "bit", nullable: false),
                FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                FechaActualizacion = table.Column<DateTime>(type: "datetime2", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Restricciones", x => x.RestriccionId);
            });

        migrationBuilder.CreateIndex(
            name: "IX_Restricciones_Activo",
            table: "Restricciones",
            column: "Activo");

        migrationBuilder.CreateIndex(
            name: "UX_Restricciones_Nombre",
            table: "Restricciones",
            column: "Nombre",
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "Restricciones");
    }
}
