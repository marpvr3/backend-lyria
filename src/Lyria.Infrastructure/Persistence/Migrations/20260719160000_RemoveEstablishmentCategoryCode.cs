using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lyria.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class RemoveEstablishmentCategoryCode : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "UX_CategoriasEstablecimiento_Codigo",
            schema: "dbo",
            table: "CategoriasEstablecimiento");

        migrationBuilder.DropColumn(
            name: "Codigo",
            schema: "dbo",
            table: "CategoriasEstablecimiento");

        migrationBuilder.Sql(
            """
            IF EXISTS
            (
                SELECT 1
                FROM dbo.CategoriasEstablecimiento
                GROUP BY Nombre COLLATE SQL_Latin1_General_CP1_CI_AS
                HAVING COUNT(*) > 1
            )
            BEGIN
                THROW 50001,
                    N'Existen categorías con nombres duplicados. Corrija los datos antes de continuar.',
                    1;
            END;
            """);

        migrationBuilder.Sql(
            """
            ALTER TABLE dbo.CategoriasEstablecimiento
            ALTER COLUMN Nombre
                varchar(100)
                COLLATE SQL_Latin1_General_CP1_CI_AS
                NOT NULL;
            """);

        migrationBuilder.CreateIndex(
            name: "UX_CategoriasEstablecimiento_Nombre",
            schema: "dbo",
            table: "CategoriasEstablecimiento",
            column: "Nombre",
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "UX_CategoriasEstablecimiento_Nombre",
            schema: "dbo",
            table: "CategoriasEstablecimiento");

        migrationBuilder.Sql(
            """
            ALTER TABLE dbo.CategoriasEstablecimiento
            ALTER COLUMN Nombre nvarchar(100) NOT NULL;
            """);

        migrationBuilder.AddColumn<string>(
            name: "Codigo",
            table: "CategoriasEstablecimiento",
            type: "varchar(50)",
            unicode: false,
            maxLength: 50,
            nullable: true);

        migrationBuilder.Sql(
            """
            UPDATE dbo.CategoriasEstablecimiento
            SET Codigo = CONCAT(
                'CAT-',
                UPPER(
                    REPLACE(
                        CONVERT(varchar(36), CategoriaEstablecimientoId),
                        '-',
                        ''
                    )
                )
            );
            """);

        migrationBuilder.AlterColumn<string>(
            name: "Codigo",
            table: "CategoriasEstablecimiento",
            type: "varchar(50)",
            unicode: false,
            maxLength: 50,
            nullable: false,
            oldClrType: typeof(string),
            oldType: "varchar(50)",
            oldUnicode: false,
            oldMaxLength: 50,
            oldNullable: true);

        migrationBuilder.CreateIndex(
            name: "UX_CategoriasEstablecimiento_Codigo",
            table: "CategoriasEstablecimiento",
            column: "Codigo",
            unique: true);
    }
}
