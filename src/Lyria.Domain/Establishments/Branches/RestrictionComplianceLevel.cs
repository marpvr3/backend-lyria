namespace Lyria.Domain.Establishments.Branches;

/// <summary>
/// Nivel de cumplimiento de una restricción alimentaria en una sede.
/// </summary>
public enum RestrictionComplianceLevel : byte
{
    /// <summary>
    /// Garantizado: la sede aplica procedimientos específicos para cumplir la restricción.
    /// </summary>
    Guaranteed = 1,

    /// <summary>
    /// Parcial: la sede ofrece opciones compatibles, pero no garantiza completamente el cumplimiento.
    /// </summary>
    Partial = 2,

    /// <summary>
    /// Bajo solicitud: la sede puede atender la restricción únicamente cuando el cliente lo solicita.
    /// </summary>
    OnRequest = 3
}
