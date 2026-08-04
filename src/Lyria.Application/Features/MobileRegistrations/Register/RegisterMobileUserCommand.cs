using Lyria.Application.Abstractions.Messaging;

namespace Lyria.Application.Features.MobileRegistrations.Register;

/// <summary>
/// Registro de un usuario desde la aplicación móvil.
/// </summary>
/// <remarks>
/// El comando no admite RoleId, PasswordHash, Status, IsEmailVerified ni ImportanceLevel:
/// esos valores los decide el backend. El móvil solo aporta datos personales,
/// contraseña y las restricciones alimenticias seleccionadas.
/// </remarks>
/// <param name="Name">Nombre del usuario.</param>
/// <param name="LastName">Apellido del usuario.</param>
/// <param name="Email">Correo electrónico del usuario.</param>
/// <param name="Password">Contraseña en claro. Se transforma en hash antes de persistirse.</param>
/// <param name="Phone">Teléfono de contacto del usuario.</param>
/// <param name="BirthDate">Fecha de nacimiento del usuario.</param>
/// <param name="PhotoUrl">URL de la foto de perfil del usuario.</param>
/// <param name="RestrictionIds">
/// Restricciones alimenticias seleccionadas. Puede estar vacía.
/// </param>
public sealed record RegisterMobileUserCommand(
    string Name,
    string LastName,
    string Email,
    string Password,
    string? Phone,
    DateOnly? BirthDate,
    string? PhotoUrl,
    IReadOnlyList<Guid> RestrictionIds) : ICommand<MobileRegistrationResponse>;
