using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lyria.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class AddUserRestrictions : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema(
            name: "dbo");

        migrationBuilder.CreateTable(
            name: "UsuarioRestricciones",
            schema: "dbo",
            columns: table => new
            {
                UsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                RestriccionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                NivelImportancia = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: false),
                FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_UsuarioRestricciones", x => new { x.UsuarioId, x.RestriccionId });
                table.ForeignKey(
                    name: "FK_UsuarioRestricciones_Restricciones_RestriccionId",
                    column: x => x.RestriccionId,
                    principalTable: "Restricciones",
                    principalColumn: "RestriccionId",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_UsuarioRestricciones_Usuarios_UsuarioId",
                    column: x => x.UsuarioId,
                    principalTable: "Usuarios",
                    principalColumn: "UsuarioId",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_UsuarioRestricciones_RestriccionId",
            schema: "dbo",
            table: "UsuarioRestricciones",
            column: "RestriccionId");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "UsuarioRestricciones",
            schema: "dbo");
    }
}
