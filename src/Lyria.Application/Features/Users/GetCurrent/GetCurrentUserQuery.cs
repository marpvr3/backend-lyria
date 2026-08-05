using Lyria.Application.Abstractions.Messaging;
using Lyria.Application.Common.Results;

namespace Lyria.Application.Features.Users.GetCurrent;

/// <summary>
/// Perfil del usuario autenticado.
/// </summary>
/// <remarks>
/// No lleva parámetros de forma deliberada: la identidad se resuelve exclusivamente
/// desde el access token validado, nunca desde datos enviados por el cliente.
/// </remarks>
public sealed record GetCurrentUserQuery : IQuery<Result<CurrentUserResponse>>;
