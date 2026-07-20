# Estrategia de pruebas — Lyria

## Framework

- **xUnit v3** como framework de pruebas.
- **Microsoft Testing Platform (MTP)** como runner.
- **ArchUnitNET** para pruebas arquitectónicas.

## Tipos de pruebas

### Pruebas unitarias de Domain (`Lyria.Domain.UnitTests`)

- Validan la lógica de negocio pura.
- Sin dependencias de infraestructura.
- Prueban entidades, value objects, eventos de dominio y excepciones.

### Pruebas unitarias de Application (`Lyria.Application.UnitTests`)

- Validan los casos de uso y la lógica de aplicación.
- Pueden usar mocks para las abstracciones de infraestructura.
- Referencia Domain para crear objetos de prueba.

### Pruebas de integración de Infrastructure (`Lyria.Infrastructure.IntegrationTests`)

- Validan la integración con sistemas externos (base de datos, servicios).
- Prueban repositorios, DbContext y configuraciones de EF Core.
- Pueden requerir una instancia de SQL Server o contenedor.

### Pruebas funcionales de API (`Lyria.Api.FunctionalTests`)

- Validan el comportamiento de los endpoints HTTP.
- Usan `WebApplicationFactory` para crear un servidor en memoria.
- Prueban el pipeline completo: routing, serialización, respuestas.

### Pruebas arquitectónicas (`Lyria.ArchitectureTests`)

- Validan las reglas de dependencia entre capas.
- Verifican convenciones de código (ubicación de controllers, etc.).
- Usan ArchUnitNET con la API fluida.
- Se ejecutan en cada build.

## Convenciones

- Los nombres de los métodos de prueba usan el formato `MetodoOCondicion_Escenario_ResultadoEsperado`.
- No se crean pruebas vacías que siempre pasen.
- Las pruebas deben ser deterministas y repetibles.

## Ejecución

```bash
# Todas las pruebas
dotnet test Lyria.slnx --configuration Release

# Un proyecto específico
dotnet test tests/Lyria.ArchitectureTests --configuration Release

# Con filtro
dotnet test Lyria.slnx --filter "FullyQualifiedName~ArchitectureTests"
```
