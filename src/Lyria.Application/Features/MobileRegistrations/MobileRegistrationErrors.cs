using Lyria.Application.Common.Errors;

namespace Lyria.Application.Features.MobileRegistrations;

/// <summary>
/// Errores del registro de usuarios desde la aplicación móvil.
/// </summary>
/// <remarks>
/// Las descripciones no exponen el identificador del rol configurado ni ningún
/// detalle interno de la transacción. Los identificadores de restricción sí se
/// devuelven porque los envió el propio cliente.
/// </remarks>
public static class MobileRegistrationErrors
{
    public static Error EmailAlreadyExists() =>
        Error.Conflict(
            "MobileRegistrations.EmailAlreadyExists",
            "Ya existe un usuario con ese correo electrónico.");

    /// <summary>
    /// Error único para cualquier problema con el rol base configurado:
    /// identificador ausente, mal formado, <see cref="Guid.Empty"/>, inexistente
    /// o asociado a un rol inactivo.
    /// </summary>
    /// <remarks>
    /// El cliente móvil no envía ni controla el rol, por lo que todos estos casos son
    /// errores de configuración interna y se responden de forma indistinguible.
    /// La descripción no revela cuál de ellos ocurrió ni el identificador configurado:
    /// distinguirlos hacia afuera expondría detalles internos del servidor.
    /// </remarks>
    public static Error RoleConfigurationError() =>
        Error.Failure(
            "MobileRegistrations.RoleConfigurationError",
            "El registro móvil no está disponible en este momento. " +
            "Intente nuevamente más tarde.");

    public static Error InvalidDefaultImportanceLevel() =>
        Error.Failure(
            "MobileRegistrations.InvalidDefaultImportanceLevel",
            "El registro móvil no está disponible en este momento. " +
            "El nivel de importancia predeterminado configurado no es válido.");

    public static Error RestrictionNotFound(Guid restrictionId) =>
        Error.NotFound(
            "MobileRegistrations.RestrictionNotFound",
            $"No se encontró la restricción con ID '{restrictionId}'.");

    public static Error RestrictionNotAvailable(Guid restrictionId) =>
        Error.NotFound(
            "MobileRegistrations.RestrictionNotAvailable",
            $"La restricción con ID '{restrictionId}' no está disponible en el catálogo.");
}
