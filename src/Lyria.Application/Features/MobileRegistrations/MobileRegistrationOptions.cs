namespace Lyria.Application.Features.MobileRegistrations;

/// <summary>
/// Opciones del registro de usuarios desde la aplicación móvil.
/// Se configura en appsettings.json bajo la sección "MobileRegistration".
/// Esta clase solo se usa en el composition root para binding.
/// Los handlers dependen de IMobileRegistrationDefaults.
/// </summary>
/// <remarks>
/// El binding no aplica validación de arranque de forma deliberada: un ambiente que
/// no expone el registro móvil no debe impedir el inicio de toda la API. La validación
/// se realiza al invocar el caso de uso y produce un error controlado.
/// Por ese motivo <see cref="DefaultRoleId"/> se modela como texto: permite recibir un
/// valor vacío o mal formado desde configuración y validarlo explícitamente.
/// </remarks>
public sealed class MobileRegistrationOptions
{
    /// <summary>
    /// Nombre de la sección en appsettings.json.
    /// </summary>
    public const string SectionName = "MobileRegistration";

    /// <summary>
    /// Identificador del rol base que el backend asigna a todo usuario móvil.
    /// Debe ser un GUID válido y distinto de <see cref="System.Guid.Empty"/>.
    /// No es un secreto, pero tampoco puede quedar fijo en el código.
    /// </summary>
    public string DefaultRoleId { get; set; } = string.Empty;

    /// <summary>
    /// Nivel de importancia inicial de las restricciones alimenticias del usuario móvil.
    /// Debe ser uno de los valores autorizados por UserRestrictionImportanceLevels.
    /// </summary>
    public string DefaultRestrictionImportanceLevel { get; set; } = string.Empty;
}
