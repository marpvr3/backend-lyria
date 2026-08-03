namespace Lyria.Infrastructure.Persistence;

/// <summary>
/// Opciones de arranque de la base de datos.
/// Se configura en appsettings.json bajo la sección "Database".
/// Esta clase solo se usa en el composition root para binding y validación.
/// </summary>
public sealed class DatabaseStartupOptions
{
    /// <summary>
    /// Nombre de la sección en appsettings.json.
    /// </summary>
    public const string SectionName = "Database";

    /// <summary>
    /// Indica si la API debe aplicar las migraciones EF Core pendientes al iniciar.
    /// El valor predeterminado es <c>false</c>: ningún entorno modifica el esquema
    /// salvo que se habilite explícitamente con la variable de entorno
    /// <c>Database__ApplyMigrationsOnStartup=true</c>.
    /// </summary>
    public bool ApplyMigrationsOnStartup { get; set; }
}
