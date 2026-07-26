using System.ComponentModel.DataAnnotations;

namespace Lyria.Application.Features.BranchSpecialSchedules;

/// <summary>
/// Opciones de zona horaria predeterminada para sedes.
/// Se configura en appsettings.json bajo la sección "BranchTimeZone".
/// Esta clase solo se usa en el composition root para binding y validación.
/// Los handlers dependen de IBranchTimeZoneDefaults.
/// </summary>
public sealed class BranchTimeZoneOptions
{
    /// <summary>
    /// Nombre de la sección en appsettings.json.
    /// </summary>
    public const string SectionName = "BranchTimeZone";

    /// <summary>
    /// Identificador IANA de la zona horaria predeterminada.
    /// </summary>
    [Required]
    public string DefaultTimeZoneId { get; set; } = string.Empty;
}
