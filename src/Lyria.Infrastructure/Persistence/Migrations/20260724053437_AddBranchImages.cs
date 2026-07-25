using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lyria.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class AddBranchImages : Migration
{
    private static readonly string[] SedeIdEsActivoOrdenColumns = ["SedeId", "EsActivo", "Orden"];
    private static readonly string[] SedeIdEsPrincipalColumns = ["SedeId", "EsPrincipal"];

    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "ImagenesSede",
            columns: table => new
            {
                ImagenSedeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                SedeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Url = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                NombreArchivo = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                TextoAlternativo = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                EsPrincipal = table.Column<bool>(type: "bit", nullable: false),
                Orden = table.Column<int>(type: "int", nullable: false),
                EsActivo = table.Column<bool>(type: "bit", nullable: false),
                FechaCreacionUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                FechaActualizacionUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ImagenesSede", x => x.ImagenSedeId);
                table.ForeignKey(
                    name: "FK_ImagenesSede_Sedes_SedeId",
                    column: x => x.SedeId,
                    principalTable: "Sedes",
                    principalColumn: "SedeId",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_ImagenesSede_SedeId",
            table: "ImagenesSede",
            column: "SedeId");

        migrationBuilder.CreateIndex(
            name: "IX_ImagenesSede_SedeId_EsActivo_Orden",
            table: "ImagenesSede",
            columns: SedeIdEsActivoOrdenColumns);

        migrationBuilder.CreateIndex(
            name: "IX_ImagenesSede_SedeId_EsPrincipal",
            table: "ImagenesSede",
            columns: SedeIdEsPrincipalColumns);

        // Índice único filtrado: garantiza una sola imagen principal activa por sede.
        // Específico de SQL Server (WHERE no es portátil a SQLite).
        // La protección transaccional en Application cubre todos los proveedores.
        migrationBuilder.Sql("""
            CREATE UNIQUE INDEX UX_ImagenesSede_Sede_PrincipalActiva
            ON dbo.ImagenesSede (SedeId)
            WHERE EsPrincipal = 1 AND EsActivo = 1;
            """);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "UX_ImagenesSede_Sede_PrincipalActiva",
            table: "ImagenesSede");

        migrationBuilder.DropTable(
            name: "ImagenesSede");
    }
}
