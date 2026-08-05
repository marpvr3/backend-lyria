using Lyria.Domain.Users;

namespace Lyria.Application.Abstractions.Security;

/// <summary>
/// Identidad del usuario autenticado en la solicitud en curso.
/// </summary>
/// <remarks>
/// La identidad procede exclusivamente del access token validado. Nunca se toma del
/// cuerpo, de la query string ni de cabeceras personalizadas enviadas por el cliente.
/// </remarks>
public interface ICurrentUser
{
    /// <summary>
    /// Identificador del usuario autenticado, o <c>null</c> si la solicitud no
    /// presenta una identidad válida.
    /// </summary>
    UserId? UserId { get; }
}
