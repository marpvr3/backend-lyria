using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lyria.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class RemoveCodeFromRoles : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "UX_Roles_Codigo",
            table: "Roles");

        migrationBuilder.DropColumn(
            name: "Codigo",
            table: "Roles");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "Codigo",
            table: "Roles",
            type: "varchar(50)",
            unicode: false,
            maxLength: 50,
            nullable: false,
            defaultValue: "");

        migrationBuilder.CreateIndex(
            name: "UX_Roles_Codigo",
            table: "Roles",
            column: "Codigo",
            unique: true);
    }
}
