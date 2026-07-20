# Value Objects — Lyria

Documento de referencia de los value objects del dominio de Lyria, plataforma gastronómica.

---

## Tabla resumen

| Value Object       | Representa                                              | Invariantes clave                                                             | Módulos                                   | MVP            |
|--------------------|---------------------------------------------------------|-------------------------------------------------------------------------------|-------------------------------------------|----------------|
| `Email`            | Dirección de correo electrónico                         | Formato válido, no vacío, normalizado a minúsculas                            | Identity & Access                         | Sí             |
| `FullName`         | Nombre y apellido de una persona                        | Al menos el nombre requerido, longitud máxima por componente                  | User Profiles                             | Sí             |
| `PhoneNumber`      | Número telefónico con código de país opcional           | Formato válido cuando se provee                                               | User Profiles, Establishments             | Sí             |
| `Slug`             | Identificador amigable para URL de un establecimiento   | No vacío, solo letras minúsculas, dígitos y guiones                           | Establishments                            | Sí             |
| `Address`          | Ubicación física de una sucursal                        | Calle y ciudad requeridas como mínimo                                         | Establishments                            | Sí             |
| `GeoLocation`      | Coordenadas geográficas                                 | Ambos valores requeridos, latitud ∈ [-90, 90], longitud ∈ [-180, 180]        | Establishments                            | Sí             |
| `Rating`           | Puntaje de una reseña                                   | Entero entre 1 y 5 (ambos inclusive)                                          | Reviews                                   | Sí             |
| `OpeningPeriod`    | Rango horario dentro de un día de la semana             | Día válido, horas válidas; soporta cruce de medianoche                        | Establishments                            | Sí             |
| `RoleScope`        | Alcance de autorización de un rol asignado              | El tipo de alcance debe ser consistente con la presencia o ausencia de IDs    | Identity & Access                         | Sí             |
| `DietarySuitability` | Evaluación compuesta del manejo de una necesidad dietaria | Estado consistente entre nivel, fuente e información de verificación        | Establishments, Dietary Catalog           | Secundario     |
| `CountryCode`      | Código ISO de país                                      | Exactamente 2 letras mayúsculas (ISO 3166-1 alpha-2)                          | Establishments (dentro de `Address`)      | Sí             |
| `TimeZoneId`       | Identificador de zona horaria IANA                      | Debe ser una zona horaria reconocida por la base de datos IANA                | Establishments                            | Sí             |

---

## Definiciones detalladas

### 1. `Email`

**Representa**
La dirección de correo electrónico de un usuario. Es el identificador principal de autenticación en la plataforma.

**Valida**
- Formato de correo electrónico válido (estructura `local@domain.tld`).
- Longitud máxima de 254 caracteres (límite definido en RFC 5321).
- Normalización a minúsculas al momento de creación.

**Tiene identidad**
No.

**Es inmutable**
Sí.

**Invariantes que protege**
- No puede estar vacío ni ser nulo.
- Debe respetar el formato de correo electrónico estándar.
- Siempre se almacena y compara en minúsculas, eliminando ambigüedades de comparación.

**Módulos donde puede usarse**
- Identity & Access

**Debe implementarse en MVP**
Sí.

---

### 2. `FullName`

**Representa**
El nombre completo de una persona, compuesto por nombre de pila y apellido. Se utiliza para mostrar información de perfil en la plataforma.

**Valida**
- El nombre (`FirstName`) no puede estar vacío.
- Longitud máxima por componente (por ejemplo, 100 caracteres por campo).
- El apellido (`LastName`) es opcional pero, si se provee, debe respetar la longitud máxima.

**Tiene identidad**
No.

**Es inmutable**
Sí.

**Invariantes que protege**
- Al menos el nombre de pila es obligatorio.
- Ningún componente puede exceder la longitud máxima permitida.

**Módulos donde puede usarse**
- User Profiles

**Debe implementarse en MVP**
Sí.

---

### 3. `PhoneNumber`

**Representa**
Un número de teléfono de contacto, opcionalmente prefijado con el código de país en formato E.164. Puede ser el número personal de un usuario o el número de contacto de una sucursal.

**Valida**
- Longitud mínima y máxima según el estándar E.164 (máximo 15 dígitos).
- Solo caracteres numéricos más el símbolo `+` como prefijo opcional.
- No se realizan llamadas a APIs externas para validación: la validación es estructural.

**Tiene identidad**
No.

**Es inmutable**
Sí.

**Invariantes que protege**
- Cuando se provee un número, debe tener un formato válido.
- No puede contener caracteres que no sean dígitos o el prefijo `+`.

**Módulos donde puede usarse**
- User Profiles
- Establishments (como dato de contacto de una sucursal, `Branch`)

**Debe implementarse en MVP**
Sí.

---

### 4. `Slug`

**Representa**
Un identificador textual legible por humanos y apto para URL, que identifica de forma única a un establecimiento gastronómico dentro de la plataforma (por ejemplo, `la-parrilla-del-sur`).

**Valida**
- Solo letras minúsculas ASCII (`a-z`), dígitos (`0-9`) y guiones medios (`-`).
- No puede comenzar ni terminar con guion.
- No puede contener guiones consecutivos.
- Longitud mínima de 3 caracteres y máxima de 100 caracteres.

**Tiene identidad**
No.

**Es inmutable**
Sí.

**Invariantes que protege**
- No puede estar vacío.
- Solo admite el conjunto de caracteres definido (URL-safe).
- La unicidad es responsabilidad de la capa de persistencia, no del value object en sí.

**Módulos donde puede usarse**
- Establishments

**Debe implementarse en MVP**
Sí.

---

### 5. `Address`

**Representa**
La ubicación física de una sucursal gastronómica. Contiene la información necesaria para localizar el establecimiento en el mundo real y para mostrársela a los usuarios.

**Componentes**
- `Street`: calle y número (requerido).
- `Neighborhood`: barrio o colonia (opcional).
- `City`: ciudad (requerido).
- `Province`: provincia, estado o departamento (opcional).
- `Country`: referencia a `CountryCode` (requerido para publicación).

**Valida**
- Presencia de los campos requeridos para que la dirección pueda considerarse publicable.
- Longitud máxima por campo (por ejemplo, 200 caracteres para `Street`).

**Tiene identidad**
No.

**Es inmutable**
Sí.

**Invariantes que protege**
- `Street` y `City` son obligatorios como mínimo.
- Ningún campo obligatorio puede estar vacío ni en blanco.

**Módulos donde puede usarse**
- Establishments

**Debe implementarse en MVP**
Sí.

---

### 6. `GeoLocation`

**Representa**
Un par de coordenadas geográficas (latitud y longitud) que ubican con precisión una sucursal en el mapa. Se utiliza para funcionalidades de búsqueda por proximidad y visualización en mapa.

**Componentes**
- `Latitude` (`decimal`): latitud geográfica.
- `Longitude` (`decimal`): longitud geográfica.

**Valida**
- Latitud dentro del rango [-90.0, 90.0].
- Longitud dentro del rango [-180.0, 180.0].

**Tiene identidad**
No.

**Es inmutable**
Sí.

**Invariantes que protege**
- Ambos valores son obligatorios; no puede existir un `GeoLocation` con solo latitud o solo longitud.
- Los valores deben estar dentro de los rangos geográficos válidos.

**Módulos donde puede usarse**
- Establishments

**Debe implementarse en MVP**
Sí.

---

### 7. `Rating`

**Representa**
El puntaje numérico que un usuario otorga a un establecimiento o sucursal al escribir una reseña. Refleja la satisfacción general del usuario con la experiencia gastronómica.

**Valida**
- Valor entero.
- Dentro del rango [1, 5] (ambos extremos inclusive).

**Tiene identidad**
No.

**Es inmutable**
Sí.

**Invariantes que protege**
- El puntaje no puede ser menor que 1 ni mayor que 5.
- No puede ser un valor de punto flotante; solo valores enteros son válidos.

**Módulos donde puede usarse**
- Reviews

**Debe implementarse en MVP**
Sí.

---

### 8. `OpeningPeriod`

**Representa**
Un bloque de horario de atención correspondiente a un día de la semana. Una sucursal puede tener múltiples `OpeningPeriod` para representar su grilla horaria semanal completa.

**Componentes**
- `DayOfWeek`: día de la semana (enumeración estándar de .NET).
- `OpenTime` (`TimeOnly`): hora de apertura.
- `CloseTime` (`TimeOnly`): hora de cierre.

**Valida**
- El día de la semana debe ser un valor válido del enumerado.
- `OpenTime` y `CloseTime` deben ser tiempos válidos.
- Se permite que `CloseTime` sea menor que `OpenTime` para representar turnos que cruzan la medianoche (por ejemplo, 20:00 a 03:00 del día siguiente).

**Tiene identidad**
No.

**Es inmutable**
Sí.

**Invariantes que protege**
- El cruce de medianoche es un caso válido: si `CloseTime <= OpenTime`, se interpreta como que el turno termina al día siguiente.
- No está permitido un período con `OpenTime` igual a `CloseTime` (duración cero).

**Módulos donde puede usarse**
- Establishments

**Debe implementarse en MVP**
Sí.

---

### 9. `RoleScope`

**Representa**
El alcance dentro del cual un rol asignado a un usuario tiene efecto. Determina si un permiso aplica a toda la plataforma, a un establecimiento específico o a una sucursal específica.

**Componentes**
- `ScopeType`: enumeración con valores `Global`, `Establishment`, `Branch`.
- `EstablishmentId` (`Guid?`): identificador del establecimiento; solo presente cuando aplica.
- `BranchId` (`Guid?`): identificador de la sucursal; solo presente cuando aplica.

**Valida**
- Consistencia entre `ScopeType` y los IDs presentes:
  - `Global`: `EstablishmentId` y `BranchId` deben ser nulos.
  - `Establishment`: `EstablishmentId` requerido, `BranchId` debe ser nulo.
  - `Branch`: `BranchId` requerido; `EstablishmentId` puede acompañarlo para contexto pero no es obligatorio en la validación del value object.

**Tiene identidad**
No.

**Es inmutable**
Sí.

**Invariantes que protege**
- El tipo de alcance siempre debe ser coherente con la presencia o ausencia de los identificadores.
- No puede existir un `RoleScope` con `ScopeType = Global` y un `EstablishmentId` o `BranchId` definido.
- No puede existir un `RoleScope` con `ScopeType = Branch` sin un `BranchId`.

**Módulos donde puede usarse**
- Identity & Access

**Debe implementarse en MVP**
Sí.

---

### 10. `DietarySuitability`

**Representa**
Una evaluación compuesta que expresa en qué medida una sucursal puede atender una necesidad dietaria específica (por ejemplo, vegano, sin gluten, kosher). Combina el nivel de aptitud declarado, la fuente de la información y el estado de verificación.

**Componentes**
- `SuitabilityLevel`: enumeración que indica el grado de aptitud (por ejemplo, `FullyAdapted`, `PartiallyAdapted`, `NotAdapted`).
- `InformationSource`: enumeración que indica quién proveyó la información (por ejemplo, `SelfReported`, `CommunityReported`).
- `VerificationStatus`: enumeración que indica si la información fue verificada (por ejemplo, `Unverified`, `CommunityVerified`, `OfficiallyVerified`).

**Valida**
- Que las combinaciones entre los tres componentes sean semánticamente coherentes.
- Por ejemplo, `VerificationStatus = OfficiallyVerified` no es compatible con `InformationSource = CommunityReported`.

**Tiene identidad**
No.

**Es inmutable**
Sí.

**Invariantes que protege**
- Las transiciones de estado deben ser consistentes: el estado de verificación no puede ser más fuerte que lo que la fuente de información permite.
- No pueden coexistir valores contradictorios entre los tres componentes.

**Módulos donde puede usarse**
- Establishments
- Dietary Catalog

**Debe implementarse en MVP**
Secundario. Una versión simplificada (solo `SuitabilityLevel`) puede incluirse en el MVP; el modelo completo con `InformationSource` y `VerificationStatus` se implementa en una iteración posterior.

---

### 11. `CountryCode`

**Representa**
El código de país según el estándar internacional ISO 3166-1 alpha-2 (por ejemplo, `AR` para Argentina, `US` para Estados Unidos). Se usa dentro de `Address` para identificar el país de una sucursal.

**Valida**
- Exactamente 2 caracteres.
- Solo letras del alfabeto inglés.
- Normalizadas a mayúsculas.

**Tiene identidad**
No.

**Es inmutable**
Sí.

**Invariantes que protege**
- Siempre tiene exactamente 2 letras mayúsculas.
- No puede estar vacío ni contener espacios, dígitos o símbolos.
- La validación de que el código corresponda a un país real puede implementarse mediante una lista de códigos ISO válidos; como mínimo se valida la forma estructural.

**Módulos donde puede usarse**
- Establishments (como componente de `Address`)

**Debe implementarse en MVP**
Sí.

---

### 12. `TimeZoneId`

**Representa**
El identificador de zona horaria de una sucursal según la base de datos de zonas horarias IANA (también conocida como tz database o Olson database), por ejemplo `America/Argentina/Buenos_Aires` o `America/New_York`. Se usa para interpretar correctamente los horarios de apertura y para mostrar información local al usuario.

**Valida**
- Que el identificador no esté vacío.
- Que corresponda a un identificador reconocido por la base de datos IANA disponible en el entorno de ejecución (a través de `TimeZoneInfo.FindSystemTimeZoneById` en .NET).

**Tiene identidad**
No.

**Es inmutable**
Sí.

**Invariantes que protege**
- No puede ser un identificador vacío o en blanco.
- Debe ser un identificador de zona horaria reconocido; un identificador con formato válido pero desconocido para el sistema no es aceptable.

**Módulos donde puede usarse**
- Establishments (como atributo de `Branch`)

**Debe implementarse en MVP**
Sí.
