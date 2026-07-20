using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lyria.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class InitialCreate : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "CategoriasEstablecimiento",
            columns: table => new
            {
                CategoriaEstablecimientoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Codigo = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                Nombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                Descripcion = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                Orden = table.Column<int>(type: "int", nullable: false),
                Activo = table.Column<bool>(type: "bit", nullable: false),
                FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                FechaActualizacion = table.Column<DateTime>(type: "datetime2", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_CategoriasEstablecimiento", x => x.CategoriaEstablecimientoId);
            });

        migrationBuilder.CreateTable(
            name: "Establecimientos",
            columns: table => new
            {
                EstablecimientoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                CategoriaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Nombre = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                Slug = table.Column<string>(type: "varchar(160)", unicode: false, maxLength: 160, nullable: false),
                Descripcion = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                SitioWeb = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                Instagram = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                Verificado = table.Column<bool>(type: "bit", nullable: false),
                Activo = table.Column<bool>(type: "bit", nullable: false),
                FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                FechaActualizacion = table.Column<DateTime>(type: "datetime2", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Establecimientos", x => x.EstablecimientoId);
                table.ForeignKey(
                    name: "FK_Establecimientos_CategoriasEstablecimiento_CategoriaId",
                    column: x => x.CategoriaId,
                    principalTable: "CategoriasEstablecimiento",
                    principalColumn: "CategoriaEstablecimientoId",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "UX_CategoriasEstablecimiento_Codigo",
            table: "CategoriasEstablecimiento",
            column: "Codigo",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Establecimientos_Activo",
            table: "Establecimientos",
            column: "Activo");

        migrationBuilder.CreateIndex(
            name: "IX_Establecimientos_CategoriaId",
            table: "Establecimientos",
            column: "CategoriaId");

        migrationBuilder.CreateIndex(
            name: "IX_Establecimientos_Verificado",
            table: "Establecimientos",
            column: "Verificado");

        migrationBuilder.CreateIndex(
            name: "UX_Establecimientos_Slug",
            table: "Establecimientos",
            column: "Slug",
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "Establecimientos");

        migrationBuilder.DropTable(
            name: "CategoriasEstablecimiento");
    }
}
