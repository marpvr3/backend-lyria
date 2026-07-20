using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lyria.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class AddCategoryAndEstablishmentOptionalFields : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "EmailContacto",
            table: "Establecimientos",
            type: "varchar(254)",
            unicode: false,
            maxLength: 254,
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "FechaVerificacion",
            table: "Establecimientos",
            type: "datetime2",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "LogoUrl",
            table: "Establecimientos",
            type: "varchar(500)",
            unicode: false,
            maxLength: 500,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "TelefonoContacto",
            table: "Establecimientos",
            type: "varchar(30)",
            unicode: false,
            maxLength: 30,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "IconoUrl",
            table: "CategoriasEstablecimiento",
            type: "varchar(500)",
            unicode: false,
            maxLength: 500,
            nullable: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "EmailContacto",
            table: "Establecimientos");

        migrationBuilder.DropColumn(
            name: "FechaVerificacion",
            table: "Establecimientos");

        migrationBuilder.DropColumn(
            name: "LogoUrl",
            table: "Establecimientos");

        migrationBuilder.DropColumn(
            name: "TelefonoContacto",
            table: "Establecimientos");

        migrationBuilder.DropColumn(
            name: "IconoUrl",
            table: "CategoriasEstablecimiento");
    }
}
