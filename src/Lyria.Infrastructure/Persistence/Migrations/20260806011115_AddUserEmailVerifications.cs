using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lyria.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class AddUserEmailVerifications : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "UsuarioVerificacionesCorreo",
            columns: table => new
            {
                VerificacionCorreoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                UsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                CodigoHash = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: false),
                FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                FechaExpiracion = table.Column<DateTime>(type: "datetime2", nullable: false),
                FechaUso = table.Column<DateTime>(type: "datetime2", nullable: true),
                FechaRevocacion = table.Column<DateTime>(type: "datetime2", nullable: true),
                IntentosFallidos = table.Column<int>(type: "int", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_UsuarioVerificacionesCorreo", x => x.VerificacionCorreoId);
                table.ForeignKey(
                    name: "FK_UsuarioVerificacionesCorreo_Usuarios_UsuarioId",
                    column: x => x.UsuarioId,
                    principalTable: "Usuarios",
                    principalColumn: "UsuarioId",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_UsuarioVerificacionesCorreo_UsuarioId",
            table: "UsuarioVerificacionesCorreo",
            column: "UsuarioId");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "UsuarioVerificacionesCorreo");
    }
}
