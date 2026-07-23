using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lyria.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class AddApplicationLogs : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            CREATE TABLE dbo.LogsAplicacion
            (
                LogAplicacionId   bigint          IDENTITY(1,1) NOT NULL,
                Mensaje           nvarchar(max)   NULL,
                PlantillaMensaje  nvarchar(max)   NULL,
                Nivel             varchar(32)     NOT NULL,
                FechaUtc          datetime2(7)    NOT NULL,
                Excepcion         nvarchar(max)   NULL,
                LogEvent          nvarchar(max)   NULL,
                TraceId           varchar(64)     NULL,
                SpanId            varchar(32)     NULL,
                TipoEvento        varchar(50)     NULL,
                MetodoHttp        varchar(10)     NULL,
                Ruta              nvarchar(1024)  NULL,
                CodigoRespuesta   int             NULL,
                DuracionMs        decimal(18,4)   NULL,
                RequestBody       nvarchar(max)   NULL,
                ResponseBody      nvarchar(max)   NULL,
                TipoExcepcion     nvarchar(500)   NULL,
                SourceContext     nvarchar(500)   NULL,
                Ambiente          varchar(50)     NULL,

                CONSTRAINT PK_LogsAplicacion
                    PRIMARY KEY CLUSTERED (LogAplicacionId)
            );
            """);

        migrationBuilder.Sql(
            """
            CREATE NONCLUSTERED INDEX IX_LogsAplicacion_FechaUtc
                ON dbo.LogsAplicacion (FechaUtc DESC);
            """);

        migrationBuilder.Sql(
            """
            CREATE NONCLUSTERED INDEX IX_LogsAplicacion_TraceId
                ON dbo.LogsAplicacion (TraceId)
                WHERE TraceId IS NOT NULL;
            """);

        migrationBuilder.Sql(
            """
            CREATE NONCLUSTERED INDEX IX_LogsAplicacion_CodigoRespuesta_FechaUtc
                ON dbo.LogsAplicacion (CodigoRespuesta, FechaUtc DESC);
            """);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DROP TABLE IF EXISTS dbo.LogsAplicacion;");
    }
}
