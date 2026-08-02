namespace Lyria.Application.Features.Users;

/// <summary>
/// Detalle completo de un usuario.
/// </summary>
/// <param name="Id">Identificador único del usuario.</param>
/// <param name="Name">Nombre del usuario.</param>
/// <param name="LastName">Apellido del usuario.</param>
/// <param name="Email">Correo electrónico del usuario.</param>
/// <param name="Phone">Teléfono de contacto del usuario.</param>
/// <param name="BirthDate">Fecha de nacimiento del usuario.</param>
/// <param name="PhotoUrl">URL de la foto de perfil del usuario.</param>
/// <param name="Status">Estado actual del usuario.</param>
/// <param name="IsEmailVerified">Indica si el correo electrónico ha sido verificado.</param>
/// <param name="LastLoginAtUtc">Fecha y hora UTC del último inicio de sesión.</param>
/// <param name="CreatedAtUtc">Fecha y hora UTC en que se creó el registro.</param>
/// <param name="UpdatedAtUtc">Fecha y hora UTC de la última modificación.</param>
public sealed record UserResponse(
    Guid Id,
    string Name,
    string LastName,
    string Email,
    string? Phone,
    DateOnly? BirthDate,
    string? PhotoUrl,
    string Status,
    bool IsEmailVerified,
    DateTime? LastLoginAtUtc,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);
