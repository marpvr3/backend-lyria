# Verificación de correo — API

Confirmación del correo electrónico de las cuentas registradas desde la aplicación móvil,
mediante un código numérico de un solo uso.

> ⚠️ **Bloqueo de despliegue.** Esta funcionalidad no debe habilitarse en producción
> mientras la API pública siga expuesta únicamente sobre HTTP. Ver
> [HTTPS obligatorio](#https-obligatorio).

## Alcance

Incluye la emisión del código durante el registro móvil, su reenvío, su confirmación y el
cambio de estado `Unverified → Active`.

**No incluye**: recuperación ni cambio de contraseña, MFA, códigos por SMS o WhatsApp,
verificación por enlace, proveedores externos, administración de verificaciones,
reactivación de cuentas suspendidas ni plantillas administrables.

## Endpoints

| Método | Ruta | Autenticación | Rate limit |
|--------|------|---------------|------------|
| `POST` | `/api/v1/auth/email-verification/resend` | Anónimo | 3 / 15 min por IP |
| `POST` | `/api/v1/auth/email-verification/confirm` | Anónimo | 10 / 15 min por IP |

El **envío inicial no tiene endpoint propio**: forma parte de
`POST /api/v1/mobile/registrations`.

---

## Flujo completo

```
POST /api/v1/mobile/registrations
    → crea el usuario con Status = Unverified
    → genera un código de 6 dígitos
    → guarda solo su hash
    → 201 Created
    → envía el código por correo (ya fuera de la transacción)

POST /api/v1/auth/email-verification/confirm
    → 204 No Content
    → IsEmailVerified = true
    → Status = Unverified → Active

POST /api/v1/auth/login
    → 200 OK
```

Mientras la cuenta no esté verificada, `POST /api/v1/auth/login` devuelve `401`.

---

## El código

| Propiedad | Valor |
|-----------|-------|
| Longitud | Exactamente 6 dígitos |
| Rango | `000000` – `999999`, el cero inicial es significativo |
| Origen | `RandomNumberGenerator.GetInt32`, sin sesgo |
| Vigencia | 15 minutos (configurable) |
| Usos | Uno solo |
| Intentos fallidos | Máximo 5 (configurable) |
| Almacenamiento | Solo su HMAC-SHA256 en hexadecimal de 64 caracteres |

El código **nunca** se almacena en claro, aparece en logs, se devuelve en ninguna
respuesta de la API ni viaja a ningún destino distinto del correo del usuario.

### Por qué HMAC y no SHA-256 a secas

Seis dígitos son un millón de valores: recorrerlos por completo es instantáneo. Un digest
sin clave permitiría recuperar todos los códigos si la base de datos se filtrara. Con
HMAC-SHA256, quien obtenga las filas necesita además `EmailVerification:CodeSecret`, que
vive fuera de la base de datos.

Ese secreto es **independiente** de `Jwt:SigningKey`: reutilizar la clave de firma haría
que comprometer uno de los dos mecanismos comprometiera el otro.

---

## Reenviar código

`POST /api/v1/auth/email-verification/resend`

```json
{ "email": "andres@email.com" }
```

Respuesta `202 Accepted`, **siempre la misma**:

```json
{
  "message": "Si existe una cuenta pendiente de verificación, se enviará un nuevo código."
}
```

La respuesta es idéntica byte a byte cuando:

- el correo no existe;
- la cuenta ya está verificada;
- la cuenta está `Suspended`;
- la cuenta está `Deleted`;
- la cuenta está `Unverified` y aún no transcurrió el intervalo mínimo;
- la cuenta está `Unverified` y se envía un código nuevo.

**No revela si el correo existe.**

Para una cuenta `Unverified` fuera del intervalo mínimo, el backend:

1. Comprueba el intervalo mínimo desde el último envío (60 s por omisión, persistido y
   por usuario).
2. Revoca los códigos vigentes anteriores.
3. Genera un código nuevo y guarda su hash.
4. Confirma los pasos 2 y 3 **en una sola transacción**.
5. Envía el correo **después** de la confirmación.

Emitir un código invalida los anteriores: solo el último sirve.

---

## Confirmar código

`POST /api/v1/auth/email-verification/confirm`

```json
{
  "email": "andres@email.com",
  "code": "482731"
}
```

Respuesta correcta:

```
204 No Content
```

El backend valida, en este orden: correo normalizado, usuario existente, `Status` =
`Unverified`, `IsEmailVerified` = `false`, verificación vigente, no vencida, no usada, no
revocada, intentos por debajo del máximo y correspondencia del código con el hash
almacenado (comparación de tiempo constante).

Al confirmar, **en una sola transacción**:

```
BEGIN TRANSACTION
UPDATE UsuarioVerificacionesCorreo   -- FechaUso
UPDATE Usuarios                      -- IsEmailVerified, Estado
COMMIT
```

### Respuesta genérica ante fallo

```
400 Bad Request
```

```json
{
  "title": "Error de validación",
  "status": 400,
  "detail": "El código de verificación no es válido o ha vencido.",
  "code": "EmailVerification.InvalidCode"
}
```

Es **exactamente la misma** para: correo inexistente, código incorrecto, vencido,
revocado, ya usado, intentos agotados, cuenta ya verificada y cuenta en un estado no
permitido. No se revela cuál de las condiciones ocurrió.

### Manejo de intentos

| Situación | Efecto |
|-----------|--------|
| Código incorrecto | `IntentosFallidos` +1 y error genérico |
| Se alcanza el máximo (5) | El código queda revocado; hace falta un reenvío |
| Código vencido, usado o revocado | Error genérico **sin** incrementar el contador |

### Concurrencia

`FechaUso` actúa como token de concurrencia: EF incluye su valor original en el `WHERE` de
todo `UPDATE` de la fila. Dos confirmaciones simultáneas no pueden canjear el mismo
código: la segunda no afecta ninguna fila, su transacción se revierte y devuelve el error
genérico. El usuario nunca se activa dos veces. No hace falta ninguna columna adicional.

---

## Integración con el registro móvil

`POST /api/v1/mobile/registrations` conserva su contrato y su `201 Created`. Internamente
amplía su escritura atómica:

```
BEGIN TRANSACTION
INSERT Usuarios
INSERT UsuarioRoles
INSERT UsuarioRestricciones
INSERT UsuarioVerificacionesCorreo
COMMIT
```

Después del commit se envía el correo. La conexión con el proveedor SMTP **nunca** se
establece con una transacción SQL abierta.

| Qué falla | Qué ocurre |
|-----------|------------|
| La creación de la verificación | `ROLLBACK` de usuario, rol y restricciones. No queda un usuario incompleto |
| El envío SMTP, ya confirmada la transacción | El usuario permanece creado como `Unverified`. Se registra un error técnico sin datos sensibles. La respuesta del registro no cambia. El usuario puede usar `/resend` |

Un fallo de envío **nunca** revierte datos ya confirmados.

---

## Efecto sobre el inicio de sesión

| Estado | ¿Puede iniciar sesión? |
|--------|------------------------|
| `Unverified` | **No** |
| `Active` | Sí |
| `Suspended` | No |
| `Deleted` | No |

Todos los rechazos conservan la misma respuesta genérica:

```
401 Unauthorized
Authentication.InvalidCredentials
"El correo o la contraseña no son válidos."
```

No existe un mensaje del tipo "debe verificar su correo": revelaría que la cuenta existe.

### Sesiones anteriores al despliegue

Las sesiones existentes **no se eliminan** de forma automática.

> ⚠️ Un access token emitido antes de este despliegue a una cuenta `Unverified` **sigue
> siendo válido hasta que expire** (15 minutos por omisión). La renovación sí queda
> bloqueada de inmediato: `POST /api/v1/auth/refresh` exige `Active`, de modo que esas
> sesiones se extinguen solas al vencer su refresh token.

---

## Limitación de solicitudes

Políticas nombradas, de aplicación explícita. No hay límite global.

| Endpoint | Límite | Se suma a |
|----------|--------|-----------|
| `resend` | 3 solicitudes / 15 min por IP | Intervalo mínimo de 60 s por usuario, persistido |
| `confirm` | 10 solicitudes / 15 min por IP | Máximo de 5 intentos por código |

El exceso devuelve `429 Too Many Requests` sin cuerpo detallado, con cabecera
`Retry-After`. La respuesta es idéntica exista o no la cuenta.

La partición usa la dirección remota real de la conexión. **No** se usa `X-Forwarded-For`:
esa cabecera la puede falsificar cualquier cliente y hoy la aplicación no tiene
configurado `ForwardedHeaders` con una lista de proxies de confianza.

---

## El correo

Asunto: **Verifica tu correo electrónico en Lyria**

Se envía en texto plano y HTML. El nombre del usuario se escapa antes de insertarse en el
HTML. El mensaje contiene el saludo, el código, su vigencia y el aviso de que puede
ignorarse; **no** incluye contraseñas, tokens, enlaces ni datos personales adicionales.

La plantilla está centralizada en `EmailVerificationMessageTemplate`; los handlers no
construyen marcado.

### Proveedor

Abstracción `IEmailSender` en Application, sin ningún tipo SMTP. El adaptador
`SmtpEmailSender` vive en Infrastructure y usa **MailKit 4.17.0** (licencia MIT), la
biblioteca SMTP recomendada por Microsoft frente a `System.Net.Mail.SmtpClient`, que la
propia documentación desaconseja para código nuevo. Es el único paquete de correo del
proyecto.

Con `Email:UseTls` activo, la conexión se eleva a TLS con **STARTTLS**. Sin TLS las
credenciales viajarían en claro, por lo que debe permanecer activo fuera de desarrollo
local.

---

## Configuración

### Verificación

```json
{
  "EmailVerification": {
    "CodeSecret": "",
    "CodeLength": 6,
    "ExpirationMinutes": 15,
    "MaximumFailedAttempts": 5,
    "ResendCooldownSeconds": 60
  }
}
```

| Variable de entorno | Valor | Efecto |
|---------------------|-------|--------|
| `EmailVerification__CodeSecret` | *(secreto)* | Clave HMAC de los códigos. Mínimo 32 caracteres |
| `EmailVerification__CodeLength` | `6` | Debe ser exactamente 6 en esta versión |
| `EmailVerification__ExpirationMinutes` | `15` | Vigencia del código |
| `EmailVerification__MaximumFailedAttempts` | `5` | Intentos que invalidan un código |
| `EmailVerification__ResendCooldownSeconds` | `60` | Intervalo mínimo entre envíos |

Se valida **al arrancar**: sin `EmailVerification__CodeSecret` la API no inicia. Generarlo
con `openssl rand -base64 64`. Debe ser distinto de `Jwt__SigningKey`.

### Correo saliente

```json
{
  "Email": {
    "SmtpHost": "",
    "SmtpPort": 587,
    "UseTls": true,
    "Username": "",
    "Password": "",
    "FromAddress": "",
    "FromName": "Lyria"
  }
}
```

| Variable de entorno | Valor |
|---------------------|-------|
| `Email__SmtpHost` | Servidor SMTP |
| `Email__SmtpPort` | `587` |
| `Email__UseTls` | `true` |
| `Email__Username` | *(secreto)* |
| `Email__Password` | *(secreto)* |
| `Email__FromAddress` | `no-reply@dominio.com` |
| `Email__FromName` | `Lyria` |

La configuración SMTP **no** se valida al arrancar: un ambiente que no envía correo no
debe impedir el inicio de la API. Un problema de configuración produce un error técnico
registrado sin secretos en el momento del envío, nunca una respuesta con detalles SMTP.

Ningún secreto se versiona: `appsettings.json` solo contiene cadenas vacías.

---

## Seguridad

**Nunca se devuelve**: el código, `CodigoHash`, `CodeSecret`, credenciales SMTP, la
existencia de una cuenta, su estado ni trazas de pila.

**Nunca se registra**: el código, su hash, el secreto, la contraseña SMTP, el correo
completo del destinatario ni el cuerpo del mensaje. Los logs del flujo solo llevan el
identificador interno del usuario.

`SensitiveDataRedactor` redacta además `code`, `verificationCode`, `codeHash`,
`codeSecret` y `smtpPassword` en los cuerpos que se persisten al registrar fallos HTTP.

---

## Persistencia

Tabla `dbo.UsuarioVerificacionesCorreo`:

| Columna | Tipo | Nulo |
|---------|------|------|
| `VerificacionCorreoId` | `uniqueidentifier` | No |
| `UsuarioId` | `uniqueidentifier` | No |
| `CodigoHash` | `varchar(64)` | No |
| `FechaCreacion` | `datetime2` | No |
| `FechaExpiracion` | `datetime2` | No |
| `FechaUso` | `datetime2` | Sí |
| `FechaRevocacion` | `datetime2` | Sí |
| `IntentosFallidos` | `int` | No |

- PK: `PK_UsuarioVerificacionesCorreo` sobre `VerificacionCorreoId`.
- FK: `FK_UsuarioVerificacionesCorreo_Usuarios_UsuarioId` con `ON DELETE RESTRICT`. No
  hay borrado en cascada: eliminar un usuario no puede borrar en silencio su historial.
- Índice `IX_UsuarioVerificacionesCorreo_UsuarioId`.
- **Sin índice único sobre `CodigoHash`**: la relación con `Usuarios` es de uno a muchos
  porque cada reenvío deja una fila más, y dos usuarios distintos podrían generar el mismo
  código.
- La invalidación es lógica: `FechaUso` y `FechaRevocacion` se establecen y la fila se
  conserva como historial. No hay borrado físico.

### Migración

```bash
dotnet ef migrations add AddUserEmailVerifications \
  --project src/Lyria.Infrastructure/Lyria.Infrastructure.csproj \
  --startup-project src/Lyria.Api/Lyria.Api.csproj
```

`20260806011115_AddUserEmailVerifications` crea **únicamente** esa tabla con sus columnas,
PK, FK e índice. No modifica `Usuarios`, `UsuarioRoles`, `UsuarioRestricciones`,
`UsuarioRefreshTokens`, `Roles`, `Restricciones` ni ninguna otra tabla, y no inserta ni
modifica datos.

La estructura se aplica con el mecanismo existente de migraciones al iniciar la API
(`Database__ApplyMigrationsOnStartup=true`). Ver
[Migraciones automáticas](../operations/database-migrations-on-startup.md).

---

## HTTPS obligatorio

Aunque el código de verificación sea temporal y de un solo uso, el flujo completo sigue
exigiendo HTTPS:

- el inicio de sesión sigue enviando la contraseña en el cuerpo;
- los access tokens y refresh tokens siguen siendo credenciales;
- el código viaja en el cuerpo de `confirm` y, sobre HTTP plano, cualquiera en la ruta
  podría interceptarlo dentro de su ventana de 15 minutos.

El repositorio no incluye hoy configuración TLS (ni `UseHttpsRedirection`, ni
`ForwardedHeaders`, ni certificados). **El despliegue en producción queda bloqueado hasta
configurar HTTPS.** El desarrollo y las pruebas locales sí pueden realizarse sobre HTTP.
