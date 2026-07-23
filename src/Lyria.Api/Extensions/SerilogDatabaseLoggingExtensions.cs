using System.Data;
using System.Globalization;
using Serilog;
using Serilog.Events;
using Serilog.Filters;
using Serilog.Sinks.MSSqlServer;

namespace Lyria.Api.Extensions;

internal static class SerilogDatabaseLoggingExtensions
{
    public static LoggerConfiguration AddDatabaseLogging(
        this LoggerConfiguration loggerConfiguration,
        IConfiguration configuration)
    {
        bool enabled = configuration.GetValue<bool>("DatabaseLogging:Enabled");
        if (!enabled)
        {
            Log.Warning(
                "El sink de base de datos para logging está deshabilitado. " +
                "Establezca DatabaseLogging:Enabled=true para activarlo.");
            return loggerConfiguration;
        }

        string connectionStringName = configuration["DatabaseLogging:ConnectionStringName"] ?? "LoggingDatabase";
        string? connectionString = configuration.GetConnectionString(connectionStringName);

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            Log.Warning(
                "La cadena de conexión '{ConnectionStringName}' para logging no está configurada. " +
                "El sink de base de datos no se activará.",
                connectionStringName);
            return loggerConfiguration;
        }

        string tableName = configuration["DatabaseLogging:TableName"] ?? "LogsAplicacion";
        string schemaName = configuration["DatabaseLogging:SchemaName"] ?? "dbo";

        int batchPostingLimit = configuration.GetValue("DatabaseLogging:BatchPostingLimit", 50);
        int batchPeriodSeconds = configuration.GetValue("DatabaseLogging:BatchPeriodSeconds", 5);

        var columnOptions = BuildColumnOptions();

        loggerConfiguration.WriteTo.Logger(subLogger =>
            subLogger
                .Filter.ByIncludingOnly(evt =>
                    evt.Level >= LogEventLevel.Error ||
                    (evt.Properties.TryGetValue("PersistToDatabase", out LogEventPropertyValue? value) &&
                     value is ScalarValue { Value: true }))
                .WriteTo.MSSqlServer(
                    connectionString: connectionString,
                    sinkOptions: new MSSqlServerSinkOptions
                    {
                        TableName = tableName,
                        SchemaName = schemaName,
                        AutoCreateSqlDatabase = false,
                        AutoCreateSqlTable = false,
                        EnlistInTransaction = false,
                        BatchPostingLimit = batchPostingLimit,
                        BatchPeriod = TimeSpan.FromSeconds(batchPeriodSeconds),
                        UseSqlBulkCopy = true
                    },
                    formatProvider: CultureInfo.InvariantCulture,
                    columnOptions: columnOptions));

        return loggerConfiguration;
    }

    private static ColumnOptions BuildColumnOptions()
    {
        var columnOptions = new ColumnOptions();

        columnOptions.Store.Remove(StandardColumn.Properties);

        columnOptions.Id.ColumnName = "LogAplicacionId";
        columnOptions.Id.DataType = SqlDbType.BigInt;

        columnOptions.Message.ColumnName = "Mensaje";

        columnOptions.MessageTemplate.ColumnName = "PlantillaMensaje";

        columnOptions.Level.ColumnName = "Nivel";
        columnOptions.Level.DataLength = 32;
        columnOptions.Level.StoreAsEnum = false;

        columnOptions.TimeStamp.ColumnName = "FechaUtc";
        columnOptions.TimeStamp.ConvertToUtc = true;

        columnOptions.Exception.ColumnName = "Excepcion";

        columnOptions.LogEvent.ColumnName = "LogEvent";

        columnOptions.TraceId.ColumnName = "TraceId";
        columnOptions.SpanId.ColumnName = "SpanId";

        columnOptions.AdditionalColumns =
        [
            new SqlColumn
            {
                ColumnName = "TipoEvento",
                DataType = SqlDbType.VarChar,
                DataLength = 50,
                AllowNull = true
            },
            new SqlColumn
            {
                ColumnName = "MetodoHttp",
                DataType = SqlDbType.VarChar,
                DataLength = 10,
                AllowNull = true
            },
            new SqlColumn
            {
                ColumnName = "Ruta",
                DataType = SqlDbType.NVarChar,
                DataLength = 1024,
                AllowNull = true
            },
            new SqlColumn
            {
                ColumnName = "CodigoRespuesta",
                DataType = SqlDbType.Int,
                AllowNull = true
            },
            new SqlColumn
            {
                ColumnName = "DuracionMs",
                DataType = SqlDbType.Decimal,
                AllowNull = true
            },
            new SqlColumn
            {
                ColumnName = "RequestBody",
                DataType = SqlDbType.NVarChar,
                DataLength = -1,
                AllowNull = true
            },
            new SqlColumn
            {
                ColumnName = "ResponseBody",
                DataType = SqlDbType.NVarChar,
                DataLength = -1,
                AllowNull = true
            },
            new SqlColumn
            {
                ColumnName = "TipoExcepcion",
                DataType = SqlDbType.NVarChar,
                DataLength = 500,
                AllowNull = true
            },
            new SqlColumn
            {
                ColumnName = "SourceContext",
                DataType = SqlDbType.NVarChar,
                DataLength = 500,
                AllowNull = true
            },
            new SqlColumn
            {
                ColumnName = "Ambiente",
                DataType = SqlDbType.VarChar,
                DataLength = 50,
                AllowNull = true
            }
        ];

        return columnOptions;
    }
}
