# ADR-014: Galería de medios de sede

## Estado

Aceptado

## Contexto

El modelo original tenía `BranchImage` como aggregate root en el módulo de medios. Sin embargo, los invariantes "máximo una imagen primaria por sede" y "orden único por sede" requieren un único límite transaccional que abarque TODAS las imágenes de una sede. `BranchImage` como aggregate root individual no puede proteger estos invariantes entre imágenes.

## Decisión

- Se reemplaza `BranchImage` como aggregate root por `BranchMediaGallery` como aggregate root.
- `BranchImage` pasa a ser una entidad interna dentro de `BranchMediaGallery`.
- `BranchMediaGallery` tiene una relación conceptual uno a uno con `Branch` mediante `BranchId`.
- `BranchMediaGallery` es responsable de:
  - Gestionar la colección de imágenes.
  - Agregar imágenes.
  - Eliminar imágenes.
  - Establecer la imagen primaria.
  - Reordenar imágenes.
  - Gestionar el estado de publicación.
  - Garantizar máximo una imagen primaria activa.
  - Prevenir órdenes de clasificación duplicados.
- `BranchImage` contiene: `BranchImageId`, `StorageKey`, `PublicUrl`, `FileName`, `MimeType`, `AlternativeText`, `IsPrimary`, `SortOrder`, `Status`, `CreatedAt`, `UpdatedAt`.
- `BranchImage` NO es un aggregate root.
- Los archivos binarios NO se almacenan en SQL Server.
- La URL pública NO es la identidad permanente del archivo (`StorageKey` lo es).

## Razones

1. Los invariantes entre imágenes (una sola primaria, orden único) requieren un único límite transaccional.
2. Las operaciones de galería (reordenar, establecer primaria) afectan inherentemente a múltiples imágenes.
3. El modelo anterior no podía garantizar "máximo una primaria" sin cargar todas las imágenes de todas formas.

## Restricciones

- Todas las operaciones sobre imágenes pasan por `BranchMediaGallery`.
- No se puede acceder a `BranchImage` directamente sin pasar por la galería.
- El almacenamiento binario es externo (Azure Blob, S3, etc.).

## Consecuencias

- Protección de invariantes más robusta.
- La galería debe cargarse para cualquier operación sobre imágenes (aceptable dado el tamaño típico de una galería).
- Cambio en repositorios: `IBranchImageRepository` → `IBranchMediaGalleryRepository`.
