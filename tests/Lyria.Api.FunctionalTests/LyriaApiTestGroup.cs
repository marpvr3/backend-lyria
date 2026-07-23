using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Lyria.Api.FunctionalTests;

/// <summary>
/// Agrupa todas las pruebas funcionales que utilizan WebApplicationFactory en una
/// misma colección xUnit. Esto serializa la ejecución entre clases de prueba,
/// evitando que múltiples instancias de Program.cs congelen concurrentemente el
/// ReloadableLogger estático de Serilog (Log.Logger), lo cual produce la excepción
/// "The logger is already frozen".
///
/// Las pruebas unitarias, de integración y de arquitectura en otros proyectos no se
/// ven afectadas y continúan ejecutándose en paralelo.
/// </summary>
[CollectionDefinition(Name)]
public class LyriaApiTestGroup : ICollectionFixture<WebApplicationFactory<Program>>
{
    public const string Name = "Lyria API";
}
