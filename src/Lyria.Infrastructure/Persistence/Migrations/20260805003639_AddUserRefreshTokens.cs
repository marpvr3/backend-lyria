using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lyria.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class AddUserRefreshTokens : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "UsuarioRefreshTokens",
            columns: table => new
            {
                RefreshTokenId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                UsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                TokenHash = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: false),
                FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                FechaExpiracion = table.Column<DateTime>(type: "datetime2", nullable: false),
                FechaRevocacion = table.Column<DateTime>(type: "datetime2", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_UsuarioRefreshTokens", x => x.RefreshTokenId);
                table.ForeignKey(
                    name: "FK_UsuarioRefreshTokens_Usuarios_UsuarioId",
                    column: x => x.UsuarioId,
                    principalTable: "Usuarios",
                    principalColumn: "UsuarioId",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_UsuarioRefreshTokens_UsuarioId",
            table: "UsuarioRefreshTokens",
            column: "UsuarioId");

        migrationBuilder.CreateIndex(
            name: "UX_UsuarioRefreshTokens_TokenHash",
            table: "UsuarioRefreshTokens",
            column: "TokenHash",
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "UsuarioRefreshTokens");
    }
}
