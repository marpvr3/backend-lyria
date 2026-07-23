-- =============================================================================
-- Script operativo: limpieza de logs de aplicación con retención de 60 días.
-- =============================================================================
-- Elimina registros en lotes pequeños (5000 filas) para evitar transacciones
-- masivas prolongadas y bloqueos excesivos.
--
-- Programar mediante:
--   - SQL Server Agent (job recurrente diario o semanal).
--   - La herramienta operativa disponible en el servidor.
--
-- NO ejecutar durante pruebas ni durante el arranque de la aplicación.
-- =============================================================================

SET NOCOUNT ON;

DECLARE @RowsDeleted int = 1;

WHILE @RowsDeleted > 0
BEGIN
    DELETE TOP (5000)
    FROM dbo.LogsAplicacion
    WHERE FechaUtc < DATEADD(DAY, -60, SYSUTCDATETIME());

    SET @RowsDeleted = @@ROWCOUNT;
END;
