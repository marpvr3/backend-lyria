# API de Registro Móvil

Endpoint dedicado al registro de usuarios desde la aplicación móvil de Lyria.

## Endpoint

```
POST /api/v1/mobile/registrations
```

No requiere autenticación. No emite tokens ni sesiones: el inicio de sesión, la
verificación de correo y la recuperación de contraseña quedan fuera de este alcance.

## Request

```json
{
  "name": "Andres",
  "lastName": "Perez",
  "email": "andres@email.com",
  "password": "Password123",
  "phone": "3001234567",
  "birthDate": "1978-12-25",
  "photoUrl": null,
  "restrictionIds": [
    "00000000-0000-0000-0000-000000000001",
    "00000000-0000-0000-0000-000000000002"
  ]
}
```

| Campo | Tipo | Obligatorio | Reglas |
|---|---|---|---|
| `name` | string | Sí | Entre 2 y 100 caracteres. |
| `lastName` | string | Sí | Entre 2 y 100 caracteres. |
| `email` | string | Sí | Máximo 254 caracteres. Se normaliza a minúsculas y sin espacios. |
| `password` | string | Sí | Entre 8 y 128 caracteres. Nunca se almacena en claro. |
| `phone` | string \| null | No | Máximo 30 caracteres. |
| `birthDate` | date \| null | No | Formato `yyyy-MM-dd`. |
| `photoUrl` | string \| null | No | Máximo 500 caracteres. |
| `restrictionIds` | uuid[] | No | Puede omitirse o ir vacío. Sin `Guid.Empty` ni duplicados. |

### Campos que el frontend móvil no puede enviar

La solicitud **no admite** `roleId`, `roleName`, `roleCode`, `scopeType`,
`establishmentId`, `branchId`, `isAdmin`, `status`, `passwordHash`,
`isEmailVerified` ni `importanceLevel`.

Estos campos no forman parte del contrato. Conforme a la política JSON vigente de la
API, un miembro desconocido en el cuerpo se **ignora silenciosamente**: enviar
`"roleId"` o `"importanceLevel"` no altera el rol asignado ni el nivel de importancia.
Esta política es global y no se modificó para este endpoint.

## Response

`201 Created`

```json
{
  "userId": "00000000-0000-0000-0000-000000000000",
  "name": "Andres",
  "lastName": "Perez",
  "email": "andres@email.com",
  "status": "Unverified",
  "restrictionIds": [
    "00000000-0000-0000-0000-000000000001",
    "00000000-0000-0000-0000-000000000002"
  ]
}
```

La cabecera `Location` apunta al recurso del usuario creado.

La respuesta **nunca** incluye la contraseña, el hash de la contraseña, el
identificador del rol asignado ni detalles internos de la transacción.

## Qué decide el backend

El móvil solo aporta datos personales, contraseña y restricciones alimenticias
seleccionadas. El backend determina, sin intervención del cliente:

| Valor | Decisión del backend |
|---|---|
| Estado inicial | `Unverified`. |
| `isEmailVerified` | `false`. |
| `lastLoginAtUtc` | `null`. |
| Hash de contraseña | ASP.NET Core Identity (`PasswordHasher<T>`, PBKDF2-HMAC-SHA512). |
| Rol asignado | El configurado en `MobileRegistration:DefaultRoleId`. |
| Alcance del rol | `Global`, con `establishmentId` y `branchId` en `null`. |
| Nivel de importancia | El configurado en `MobileRegistration:DefaultRestrictionImportanceLevel`. |
| Fechas | `TimeProvider` del servidor. |

## Configuración

```json
{
  "MobileRegistration": {
    "DefaultRoleId": "",
    "DefaultRestrictionImportanceLevel": "High"
  }
}
```

Variables de entorno a configurar en el servidor:

```
MobileRegistration__DefaultRoleId=B7E61E8B-7A94-4638-9068-CF364B81EE31
MobileRegistration__DefaultRestrictionImportanceLevel=High
```

`DefaultRoleId` no es un secreto, pero **no puede quedar fijo en el código**: se lee
siempre desde configuración. El repositorio no incluye el GUID de producción, y una
prueba de arquitectura lo verifica.

### Rol base

El rol se localiza **exclusivamente por su identificador**. Nunca por `Name`,
`Description`, código ni primera coincidencia: `Role.Name` no es único y no sirve como
identificador técnico. `Role.Code` fue eliminado del modelo y no se reintroduce.

El backend valida, en este orden, que `DefaultRoleId` sea un GUID válido y distinto de
`Guid.Empty`, que el rol exista y que esté activo.

### Nivel de importancia

`DefaultRestrictionImportanceLevel` se valida contra `UserRestrictionImportanceLevels`
(`Low`, `Medium`, `High`). No se duplican esas constantes.

### Momento de validación

La configuración **no** se valida durante el arranque (`ValidateOnStart`), a diferencia
de `BranchTimeZone` y `Database`. Se valida al invocar el endpoint.

**Motivo:** un ambiente que no expone el flujo móvil no debe quedar impedido de iniciar
la API por una opción que no utiliza. Una configuración ausente o inválida produce un
error controlado (`500`) únicamente en este endpoint, sin afectar al resto de la API.

## Restricciones alimenticias

Los `restrictionIds` corresponden exclusivamente a restricciones alimenticias del
catálogo (sin lactosa, sin gluten/TACC, vegano, vegetariano, alergias alimentarias).
No son permisos, bloqueos de seguridad ni restricciones administrativas.

- La lista puede ir vacía si el usuario no selecciona ninguna.
- Todas las restricciones enviadas deben existir.
- Deben estar **activas**: el catálogo público solo expone restricciones activas
  (`PublicCatalogReadService`), por lo que una restricción inactiva se rechaza como no
  disponible. Es el comportamiento coherente con el catálogo, no una regla nueva.
- Este endpoint **no crea ni modifica** restricciones maestras.

## Atomicidad

El usuario, su asignación de rol y sus restricciones alimenticias se persisten en una
**única transacción**:

```
BEGIN TRANSACTION
  INSERT Usuarios
  INSERT UsuarioRoles
  INSERT UsuarioRestricciones...
COMMIT
```

Ante cualquier fallo se ejecuta `ROLLBACK` y no queda ningún registro parcial:
ni usuario sin rol, ni usuario con restricciones incompletas, ni asociaciones huérfanas.

La transacción vive en `MobileRegistrationWriter` (Infrastructure), se abarca el caso de
uso completo y se envuelve en la *execution strategy* del proveedor, de modo que
reintentos y transacciones manuales sigan siendo compatibles si algún día se habilita
`EnableRetryOnFailure`. No se introdujo un Unit of Work genérico ni transacciones dentro
de cada repositorio.

## Comportamiento ante errores

| Situación | Código | Efecto |
|---|---|---|
| Datos de entrada inválidos | `400` | No se crea nada. |
| `restrictionId` vacío (`Guid.Empty`) | `400` | No se crea nada. |
| `restrictionIds` duplicados | `400` | No se crea nada. |
| Contraseña inválida | `400` | No se crea nada. |
| Restricción inexistente | `404` | No se crea nada. |
| Restricción inactiva | `404` | No se crea nada. |
| Correo ya registrado | `409` | No se crea nada. |
| `DefaultRoleId` ausente, mal formado o `Guid.Empty` | `500` | No se crea nada. |
| Rol configurado inexistente | `500` | No se crea nada. |
| Rol configurado inactivo | `500` | No se crea nada. |
| Nivel de importancia mal configurado | `500` | No se crea nada. |
| Fallo de `SaveChanges` o de SQL | `500` | `ROLLBACK`; sin datos parciales. |

Todas las respuestas de error usan Problem Details e incluyen la extensión `code`.

### Errores del rol configurado

Los cuatro problemas posibles con el rol base —identificador ausente, mal formado,
`Guid.Empty`; rol inexistente; rol inactivo— devuelven **exactamente el mismo error**:

```
500  MobileRegistrations.RoleConfigurationError
     "El registro móvil no está disponible en este momento. Intente nuevamente más tarde."
```

El cliente móvil no envía ni controla el rol, de modo que ninguno de estos casos le es
atribuible. Se responden de forma **indistinguible** y con un mensaje genérico: **no se
expone el `DefaultRoleId`**, ni cuál de los cuatro problemas ocurrió, ni ningún otro
detalle interno. Distinguirlos hacia afuera revelaría el estado de configuración del
servidor.

Los errores de restricción sí devuelven el identificador afectado, porque lo envió el
propio cliente y le permite corregir la solicitud.

La contraseña no se escribe en logs, excepciones ni respuestas en ningún caso.

## Separación respecto al flujo administrativo

El comportamiento automático se activa **únicamente** por la ruta dedicada. No existe un
campo `isMobileRegistration` ni una cabecera manipulable que lo determine.

| Flujo | Ruta | Rol |
|---|---|---|
| Registro móvil | `POST /api/v1/mobile/registrations` | Asigna el rol base automáticamente. |
| Creación administrativa | `POST /api/v1/users` | Conserva el manejo administrativo actual; no asigna rol. |

El flujo administrativo no fue modificado.

## Migraciones

Esta funcionalidad **no requiere migración**. Reutiliza las tablas existentes
`Usuarios`, `Roles`, `UsuarioRoles`, `Restricciones` y `UsuarioRestricciones`.
