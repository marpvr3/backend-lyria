using Lyria.Application.Abstractions.Messaging;

namespace Lyria.Application.Features.EmailVerifications.Confirm;

/// <summary>
/// Confirma el correo electrónico de una cuenta con el código recibido.
/// </summary>
/// <param name="Email">Correo electrónico de la cuenta.</param>
/// <param name="Code">
/// Código de seis dígitos recibido por correo. Solo se usa para compararlo con el hash
/// almacenado; nunca se persiste, se registra en logs ni se devuelve en la respuesta.
/// </param>
public sealed record ConfirmEmailVerificationCommand(
    string Email,
    string Code) : ICommand;
