-- =============================================================================
-- Consultas SQL de diagnóstico para dbo.LogsAplicacion
-- =============================================================================
-- Usar siempre parámetros. No construir consultas mediante concatenación.
-- =============================================================================

-- 1. Buscar por TraceId
-- Reemplace @TraceId con el valor del encabezado de respuesta.
DECLARE @TraceId varchar(64) = '<<TRACE_ID>>';

SELECT *
FROM dbo.LogsAplicacion
WHERE TraceId = @TraceId
ORDER BY FechaUtc;

-- 2. Últimos errores
SELECT TOP (100) *
FROM dbo.LogsAplicacion
WHERE Nivel IN ('Error', 'Fatal')
ORDER BY FechaUtc DESC;

-- 3. Errores HTTP recientes
SELECT TOP (100)
    FechaUtc,
    MetodoHttp,
    Ruta,
    CodigoRespuesta,
    DuracionMs,
    TraceId,
    RequestBody,
    ResponseBody
FROM dbo.LogsAplicacion
WHERE TipoEvento = 'HttpFailure'
ORDER BY FechaUtc DESC;
