namespace Lyria.Application.Abstractions.Services;

/// <summary>
/// Proporciona los valores que el backend decide durante el registro móvil.
/// La implementación se configura en el composition root.
/// </summary>
/// <remarks>
/// Los valores se exponen como texto sin interpretar para que el caso de uso pueda
/// validarlos y responder con un error controlado en lugar de fallar al arrancar.
/// </remarks>
public interface IMobileRegistrationDefaults
{
    /// <summary>
    /// Identificador configurado del rol base que se asigna al usuario móvil.
    /// </summary>
    string DefaultRoleId { get; }

    /// <summary>
    /// Nivel de importancia inicial de las restricciones alimenticias del usuario móvil.
    /// </summary>
    string DefaultRestrictionImportanceLevel { get; }
}
