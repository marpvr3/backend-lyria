using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lyria.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class AddUsersRolesAndUserRoles : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Roles",
            columns: table => new
            {
                RolId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Codigo = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                Nombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                Descripcion = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                Activo = table.Column<bool>(type: "bit", nullable: false),
                FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                FechaActualizacion = table.Column<DateTime>(type: "datetime2", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Roles", x => x.RolId);
            });

        migrationBuilder.CreateTable(
            name: "Usuarios",
            columns: table => new
            {
                UsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Nombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                Apellido = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                Email = table.Column<string>(type: "varchar(254)", unicode: false, maxLength: 254, nullable: false),
                HashContrasena = table.Column<string>(type: "varchar(256)", unicode: false, maxLength: 256, nullable: false),
                Telefono = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: true),
                FechaNacimiento = table.Column<DateOnly>(type: "date", nullable: true),
                FotoUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                Estado = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: false),
                EmailVerificado = table.Column<bool>(type: "bit", nullable: false),
                UltimaConexion = table.Column<DateTime>(type: "datetime2", nullable: true),
                FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                FechaActualizacion = table.Column<DateTime>(type: "datetime2", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Usuarios", x => x.UsuarioId);
            });

        migrationBuilder.CreateTable(
            name: "UsuarioRoles",
            columns: table => new
            {
                UsuarioRolId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                UsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                RolId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                AlcanceTipo = table.Column<string>(type: "varchar(13)", unicode: false, maxLength: 13, nullable: false),
                EstablecimientoId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                SedeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                Activo = table.Column<bool>(type: "bit", nullable: false),
                FechaAsignacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                FechaFinalizacion = table.Column<DateTime>(type: "datetime2", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_UsuarioRoles", x => x.UsuarioRolId);
                table.ForeignKey(
                    name: "FK_UsuarioRoles_Establecimientos_EstablecimientoId",
                    column: x => x.EstablecimientoId,
                    principalTable: "Establecimientos",
                    principalColumn: "EstablecimientoId",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_UsuarioRoles_Roles_RolId",
                    column: x => x.RolId,
                    principalTable: "Roles",
                    principalColumn: "RolId",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_UsuarioRoles_Sedes_SedeId",
                    column: x => x.SedeId,
                    principalTable: "Sedes",
                    principalColumn: "SedeId",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_UsuarioRoles_Usuarios_UsuarioId",
                    column: x => x.UsuarioId,
                    principalTable: "Usuarios",
                    principalColumn: "UsuarioId",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "UX_Roles_Codigo",
            table: "Roles",
            column: "Codigo",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_UsuarioRoles_EstablecimientoId",
            table: "UsuarioRoles",
            column: "EstablecimientoId",
            filter: "[EstablecimientoId] IS NOT NULL");

        migrationBuilder.CreateIndex(
            name: "IX_UsuarioRoles_RolId",
            table: "UsuarioRoles",
            column: "RolId");

        migrationBuilder.CreateIndex(
            name: "IX_UsuarioRoles_SedeId",
            table: "UsuarioRoles",
            column: "SedeId",
            filter: "[SedeId] IS NOT NULL");

        migrationBuilder.CreateIndex(
            name: "IX_UsuarioRoles_UsuarioId",
            table: "UsuarioRoles",
            column: "UsuarioId");

        migrationBuilder.CreateIndex(
            name: "UX_Usuarios_Email",
            table: "Usuarios",
            column: "Email",
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "UsuarioRoles");

        migrationBuilder.DropTable(
            name: "Roles");

        migrationBuilder.DropTable(
            name: "Usuarios");
    }
}
