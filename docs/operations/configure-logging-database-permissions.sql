-- =============================================================================
-- Script documental: permisos mínimos para la cuenta de logging en SQL Server.
-- =============================================================================
-- Este script NO debe ejecutarse tal cual. Reemplace los placeholders
-- con los valores reales del ambiente destino.
--
-- Variable de ambiente esperada para la cadena de conexión:
--   ConnectionStrings__LoggingDatabase
--
-- La cuenta utilizada por LoggingDatabase solo necesita INSERT y SELECT
-- sobre la tabla dbo.LogsAplicacion.
-- =============================================================================

-- Placeholder: reemplace con el nombre real del login y usuario.
-- CREATE LOGIN [<<LOGGING_LOGIN>>] WITH PASSWORD = '<<CONTRASEÑA_SEGURA>>';

USE [<<NOMBRE_BASE_DATOS>>];

-- CREATE USER [<<LOGGING_USER>>] FOR LOGIN [<<LOGGING_LOGIN>>];

-- Permisos mínimos requeridos:
GRANT SELECT ON dbo.LogsAplicacion TO [<<LOGGING_USER>>];
GRANT INSERT ON dbo.LogsAplicacion TO [<<LOGGING_USER>>];

-- NO conceder:
-- DENY ALTER  ON dbo.LogsAplicacion TO [<<LOGGING_USER>>];
-- DENY DELETE ON dbo.LogsAplicacion TO [<<LOGGING_USER>>];
-- DENY UPDATE ON dbo.LogsAplicacion TO [<<LOGGING_USER>>];
-- DENY CREATE TABLE TO [<<LOGGING_USER>>];
-- DENY DROP   ON dbo.LogsAplicacion TO [<<LOGGING_USER>>];
