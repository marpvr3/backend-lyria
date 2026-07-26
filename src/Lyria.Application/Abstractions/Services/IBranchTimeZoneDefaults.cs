namespace Lyria.Application.Abstractions.Services;

/// <summary>
/// Proporciona la zona horaria predeterminada para sedes.
/// La implementación se configura en el composition root.
/// </summary>
public interface IBranchTimeZoneDefaults
{
    /// <summary>
    /// Identificador IANA de la zona horaria predeterminada.
    /// Se aplica al crear sedes cuando el cliente no envía zona horaria.
    /// </summary>
    string DefaultTimeZoneId { get; }
}
