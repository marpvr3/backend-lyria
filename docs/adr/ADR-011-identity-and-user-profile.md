# ADR-011: Identidad separada del perfil de usuario

## Estado

Aceptado

## Contexto

El modelo original de la tabla `USUARIO` mezclaba credenciales de acceso (correo electrónico, contraseña, estado de la cuenta) con información personal del usuario (nombre, apellido, teléfono, fecha de nacimiento, foto, preferencias dietéticas).

Esta mezcla genera acoplamiento entre dos preocupaciones conceptualmente distintas: la seguridad y autenticación por un lado, y el perfil personal por el otro. Cualquier cambio en el manejo de credenciales afecta indirectamente a los datos de perfil, y viceversa.

## Decisión

Se separan **identidad** y **perfil de usuario** en módulos y agregados distintos:

- `UserAccount` pertenece al módulo de **Identidad y Acceso** (`Identity & Access`): gestiona correo electrónico, credenciales, estado de la cuenta y datos de autenticación.
- `UserProfile` pertenece al módulo de **Perfiles de Usuario** (`User Profiles`): gestiona nombre, apellido, teléfono, fecha de nacimiento, fotografía y necesidades dietéticas.
- Ambos agregados se relacionan mediante el identificador `UserId`.
- Las credenciales no deben mezclarse con preferencias dietéticas ni con datos personales del perfil.
- Esta separación no implica bases de datos distintas: ambos módulos comparten el mismo SQL Server y el mismo esquema en la fase inicial.
- Cada módulo gestiona su propio agregado de forma independiente.

## Razones

1. La seguridad y el perfil personal evolucionan a ritmos distintos y por razones distintas; separarlos facilita el mantenimiento.
2. Los requisitos de auditoría y cumplimiento normativo (por ejemplo, GDPR) suelen aplicarse de forma diferenciada a datos de acceso y datos personales.
3. La separación permite, en el futuro, extraer el módulo de identidad hacia un proveedor externo sin afectar los perfiles de usuario.
4. Reduce el acoplamiento y mejora la cohesión dentro de cada módulo.

## Restricciones

- Prohibido colocar datos de autenticación dentro de `UserProfile`.
- Prohibido colocar preferencias dietéticas o datos personales dentro de `UserAccount`.
- Las consultas que requieran datos de ambos módulos deben resolverse a nivel de aplicación o mediante modelos de lectura.
- La separación lógica debe respetarse aunque la implementación física sea una única base de datos.

## Consecuencias

- Separación clara de responsabilidades entre seguridad y perfil personal.
- Evolución independiente de cada módulo sin afectar al otro.
- Las consultas que necesiten información combinada del usuario requieren coordinación entre módulos a nivel de aplicación.
- En una futura migración hacia un proveedor de identidad externo (OAuth2, OpenID Connect), el módulo `Identity & Access` puede reemplazarse sin impacto sobre `User Profiles`.
