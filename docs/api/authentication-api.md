# Autenticación móvil — API

Primera versión de la autenticación para usuarios de la aplicación móvil de Lyria.

> ⚠️ **Bloqueo de despliegue.** Esta funcionalidad no debe habilitarse en producción
> mientras la API pública siga expuesta únicamente sobre HTTP. Ver
> [HTTPS obligatorio](#https-obligatorio).

## Alcance

Incluye inicio de sesión, emisión y renovación de tokens con rotación, cierre de sesión
y consulta del perfil autenticado.

**No incluye** (requerimientos posteriores): confirmación de correo, recuperación y
cambio de contraseña, MFA, inicio con proveedores externos, cookies, autorización por
roles, administración de sesiones ni las restricciones alimenticias dentro del perfil.

## Endpoints

| Método | Ruta | Autenticación | Rate limit |
|--------|------|---------------|------------|
| `POST` | `/api/v1/auth/login` | Anónimo | Sí |
| `POST` | `/api/v1/auth/refresh` | Anónimo | Sí |
| `POST` | `/api/v1/auth/logout` | Anónimo | No |
| `GET` | `/api/v1/users/me` | `Bearer` | No |

El registro móvil (`POST /api/v1/mobile/registrations`), los endpoints administrativos
y el catálogo público conservan su comportamiento anónimo.

---

## Flujo de inicio de sesión

`POST /api/v1/auth/login`

```json
{
  "email": "andres@email.com",
  "password": "Password123"
}
```

Respuesta `200 OK`:

```json
{
  "tokenType": "Bearer",
  "accessToken": "eyJhbGciOi...",
  "accessTokenExpiresAtUtc": "2026-08-04T23:15:00Z",
  "refreshToken": "9Kt3...",
  "refreshTokenExpiresAtUtc": "2026-09-03T23:00:00Z",
  "user": {
    "userId": "0f8f...",
    "name": "Andres",
    "lastName": "Perez",
    "email": "andres@email.com",
    "status": "Unverified"
  }
}
```

Pasos que ejecuta el backend:

1. Normaliza el correo con las mismas reglas del registro (`trim` + minúsculas).
2. Busca al usuario por correo normalizado.
3. Verifica la contraseña contra el hash almacenado. **Si el usuario no existe, la
   verificación se ejecuta igualmente contra un hash señuelo**, para que el tiempo de
   respuesta no revele si la cuenta existe.
4. Valida que el estado de la cuenta permita autenticarse.
5. Si el hash quedó obsoleto (`SuccessRehashNeeded`), lo regenera con el algoritmo vigente.
6. Registra la última conexión.
7. Genera el refresh token y crea la sesión.
8. Confirma los pasos 5, 6 y 7 **en una sola transacción**.
9. Emite el access token.

### Estados que pueden iniciar sesión

| Estado | ¿Puede iniciar sesión? |
|--------|------------------------|
| `Unverified` | Sí |
| `Active` | Sí |
| `Suspended` | No |
| `Deleted` | No |

`Unverified` se admite de forma **temporal**: es el estado con el que el registro móvil
crea las cuentas y todavía no existe un flujo de confirmación de correo. El inicio de
sesión no modifica el estado de la cuenta.

### Respuesta genérica ante fallo

Correo inexistente, contraseña incorrecta, cuenta suspendida y cuenta eliminada
producen **exactamente la misma respuesta**:

```
401 Unauthorized
```

```json
{
  "title": "No autenticado",
  "status": 401,
  "detail": "El correo o la contraseña no son válidos.",
  "code": "Authentication.InvalidCredentials"
}
```

La respuesta no revela si el correo existe, si la contraseña era incorrecta, ni el
estado almacenado de la cuenta.

---

## Flujo de renovación

`POST /api/v1/auth/refresh`

```json
{ "refreshToken": "9Kt3..." }
```

Devuelve un par de tokens nuevo con la misma forma que el inicio de sesión.

El backend calcula el hash del token recibido, localiza la sesión y comprueba que
exista, no esté revocada, no haya vencido, y que el usuario siga existiendo y en un
estado que permita autenticarse. Después **revoca la sesión anterior y crea la nueva en
la misma transacción**.

**La rotación es obligatoria**: un refresh token solo puede usarse una vez. Reenviar un
token ya rotado devuelve `401`.

Token inexistente, vencido, revocado o reutilizado producen la misma respuesta:

```
401 Unauthorized
```

```json
{
  "title": "No autenticado",
  "status": 401,
  "detail": "La sesión no es válida o ha expirado.",
  "code": "Authentication.InvalidRefreshToken"
}
```

---

## Flujo de cierre de sesión

`POST /api/v1/auth/logout`

```json
{ "refreshToken": "9Kt3..." }
```

Respuesta: `204 No Content`.

La operación es **idempotente**: un token inexistente o ya revocado devuelve igualmente
`204`, sin revelar si la sesión existía. La revocación es **lógica**: la fila conserva su
historial y solo se establece `FechaRevocacion`. No hay borrado físico de sesiones.

---

## Perfil autenticado

`GET /api/v1/users/me`

```
Authorization: Bearer {accessToken}
```

Respuesta `200 OK`:

```json
{
  "userId": "0f8f...",
  "name": "Andres",
  "lastName": "Perez",
  "email": "andres@email.com",
  "phone": "3001234567",
  "birthDate": "1978-12-25",
  "photoUrl": null,
  "status": "Unverified",
  "isEmailVerified": false
}
```

La identidad se resuelve **exclusivamente** desde el claim `sub` del access token
validado. El endpoint no admite identificadores enviados por el cliente en el cuerpo, la
query string ni cabeceras personalizadas.

Sin token, con token inválido, vencido, con firma incorrecta, con emisor o audiencia no
esperados, o sin `sub` válido: `401 Unauthorized`.

---

## Access token

JWT firmado con HMAC-SHA256. Vigencia predeterminada: **15 minutos**.

Claims emitidos, y solo estos:

| Claim | Contenido |
|-------|-----------|
| `sub` | Identificador del usuario |
| `jti` | Identificador único del token |
| `iat` | Emisión |
| `nbf` | Inicio de validez |
| `exp` | Expiración |
| `iss` | Emisor |
| `aud` | Audiencia |

**No** transporta correo, teléfono, fecha de nacimiento, foto, roles, hash de contraseña
ni el refresh token.

Validaciones obligatorias al recibirlo: firma, emisor, audiencia y expiración.
`ClockSkew` está fijado en cero, de modo que el token expira exactamente cuando indica
su claim `exp` (sin los 5 minutos de tolerancia predeterminados de ASP.NET Core).

Los roles **no** participan en la autenticación de esta versión. No se usa `Role.Name`
como identificador —no es único— ni se reincorpora `Role.Code`.

## Refresh token

Token **opaco**, no un JWT. 32 bytes de `RandomNumberGenerator` codificados en Base64Url.
Vigencia predeterminada: **30 días**.

- Se entrega al cliente **una única vez**. El servidor no puede volver a mostrarlo.
- Se almacena **solo su hash SHA-256** en hexadecimal (64 caracteres).
- La búsqueda de sesiones se hace siempre por hash: el token en claro nunca llega a la
  base de datos.

> Usar SHA-256 aquí es correcto y no contradice el uso de un password hasher lento: el
> token ya tiene 256 bits de entropía, así que no existe un espacio de búsqueda que un
> atacante pueda recorrer, que es justamente el riesgo del que protege un hasher con
> factor de trabajo. **SHA-256 no se usa —ni debe usarse— para contraseñas.**

## Contraseñas

La verificación usa el password hasher existente (PBKDF2 de ASP.NET Core Identity, con
sal por contraseña), confinado en Infrastructure. Nunca se comparan hashes como cadenas
ni se intenta revertirlos.

Cuando la verificación indica que el hash almacenado quedó obsoleto, el inicio de sesión
lo regenera con el algoritmo vigente y confirma ese cambio junto con el resto de la
operación.

---

## Almacenamiento seguro en la aplicación móvil

- El **refresh token** debe guardarse en el almacén seguro del sistema operativo:
  Keychain en iOS, EncryptedSharedPreferences o Keystore en Android.
- **No** guardarlo en `UserDefaults`, `SharedPreferences` sin cifrar, `localStorage`,
  archivos de texto ni bases de datos sin cifrar.
- **No** registrarlo en logs, trazas, informes de fallos ni herramientas de analítica.
- El **access token** puede mantenerse en memoria durante la sesión; no necesita
  persistirse, dado que se renueva con el refresh token.
- Al cerrar sesión, invocar `logout` y borrar ambos tokens del dispositivo.

---

## Configuración

Sección `Jwt` en `appsettings.json` (valores no sensibles):

```json
{
  "Jwt": {
    "Issuer": "Lyria.Api",
    "Audience": "Lyria.Mobile",
    "SigningKey": "",
    "AccessTokenMinutes": 15,
    "RefreshTokenDays": 30
  }
}
```

### Variables de entorno de producción

```
Jwt__Issuer=Lyria.Api
Jwt__Audience=Lyria.Mobile
Jwt__SigningKey=VALOR_SEGURO
Jwt__AccessTokenMinutes=15
Jwt__RefreshTokenDays=30
```

**La clave de firma se configura únicamente por variable de entorno.** No debe aparecer
en `appsettings.json`, en `appsettings.Development.json` versionado, en el código, en
pruebas compartidas, en el README ni en los logs. Debe ser criptográficamente fuerte y
tener al menos 32 caracteres.

Generar una clave adecuada:

```bash
openssl rand -base64 64
```

La configuración se valida **al arrancar** (`ValidateOnStart`): emisor, audiencia, clave
presente y suficientemente larga, y vigencias mayores que cero. Una configuración
incompleta impide el inicio de la API en lugar de emitir tokens inseguros.

### Rate limiting

```json
{
  "RateLimiting": {
    "Authentication": {
      "PermitLimit": 10,
      "WindowSeconds": 60
    }
  }
}
```

Ventana fija por dirección IP de la conexión, aplicada **solo** a `login` y `refresh`
mediante una política nombrada. No existe límite global: health, Swagger, el catálogo
público, el registro móvil y `logout` no se ven afectados. Al superarse se responde
`429 Too Many Requests` con cabecera `Retry-After`.

> La partición usa `RemoteIpAddress`, **no** `X-Forwarded-For`: esa cabecera la puede
> falsificar cualquier cliente y hoy la aplicación no tiene configurado
> `ForwardedHeaders` con una lista de proxies de confianza. Al desplegar detrás de un
> proxy con TLS habrá que configurar `ForwardedHeaders` y revisar esta partición.

---

## HTTPS obligatorio

Credenciales, access tokens y refresh tokens **no deben viajar por HTTP plano**. Quien
observe la red obtiene la contraseña y la sesión completa del usuario.

**Estado actual del repositorio:** no hay configuración HTTPS. No existen
`UseHttpsRedirection`, `UseHsts`, `ForwardedHeaders`, endpoints HTTPS de Kestrel,
`web.config` ni certificados. El perfil de `launchSettings.json` llamado `https` escucha
en realidad sobre `http://localhost:50003`.

**Requisitos antes de habilitar el inicio de sesión real:**

- **Desarrollo local**: puede usarse el certificado de desarrollo
  (`dotnet dev-certs https --trust`).
- **Producción**: TLS terminado en IIS, en un reverse proxy o directamente en Kestrel.
  Si se termina TLS en un proxy, configurar además `ForwardedHeaders` con los proxies de
  confianza para que la aplicación reconozca el esquema original.
- No basta con que la aplicación móvil use HTTPS: el servidor debe rechazar o redirigir
  el tráfico en claro.
- No imprimir tokens en Swagger ni en los logs.

Mientras la API pública siga disponible solo en `http://186.31.68.74:3011`, **el
despliegue de la autenticación queda bloqueado**. La funcionalidad puede desarrollarse y
probarse localmente.

---

## Registro de eventos

Se registran: inicio de sesión, renovación y cierre de sesión correctos, cada uno con el
`UserId`; los rehashes de contraseña; y los fallos de configuración o técnicos.

**Nunca** se registran: contraseñas, hashes de contraseña, access tokens, refresh tokens,
hashes de refresh token, la clave de firma, la cabecera `Authorization` ni el correo en
intentos fallidos. Los intentos fallidos usan mensajes genéricos, para que los logs no
permitan reconstruir qué cuentas existen.

---

## Persistencia

Tabla `dbo.UsuarioRefreshTokens`:

| Columna | Tipo | Nulo |
|---------|------|------|
| `RefreshTokenId` | `uniqueidentifier` | No |
| `UsuarioId` | `uniqueidentifier` | No |
| `TokenHash` | `varchar(64)` | No |
| `FechaCreacion` | `datetime2` | No |
| `FechaExpiracion` | `datetime2` | No |
| `FechaRevocacion` | `datetime2` | Sí |

- PK: `PK_UsuarioRefreshTokens` sobre `RefreshTokenId`.
- FK: `FK_UsuarioRefreshTokens_Usuarios_UsuarioId` con `ON DELETE RESTRICT`. No hay
  borrado en cascada: eliminar un usuario no puede borrar en silencio su historial de
  sesiones.
- Índice único `UX_UsuarioRefreshTokens_TokenHash`.
- Índice `IX_UsuarioRefreshTokens_UsuarioId`.

### Migración

Nombre: `AddUserRefreshTokens`. Crea únicamente esta tabla con sus columnas, clave
primaria, clave foránea e índices. No modifica ninguna tabla existente ni ningún dato.

```bash
dotnet ef migrations add AddUserRefreshTokens \
  --project src/Lyria.Infrastructure/Lyria.Infrastructure.csproj \
  --startup-project src/Lyria.Api/Lyria.Api.csproj \
  --output-dir Persistence/Migrations
```

Se aplica con el mecanismo habitual del proyecto: las migraciones pendientes se ejecutan
durante el inicio de la API cuando `Database__ApplyMigrationsOnStartup=true`. Ver
[Migraciones automáticas al iniciar la API](../operations/database-migrations-on-startup.md).

## Atomicidad

| Operación | Se confirma en una transacción |
|-----------|-------------------------------|
| Login | Última conexión + rehash de contraseña (si procede) + creación de la sesión |
| Refresh | Revocación de la sesión anterior + creación de la nueva |
| Logout | Revocación de la sesión |

Si cualquier escritura falla, ninguna del grupo queda confirmada. La transacción vive
completamente en Infrastructure, siguiendo el mismo patrón que el registro móvil: ningún
repositorio abre la suya y Application nunca controla el commit.
