using System.Security.Claims;
using Lyria.Application.Abstractions.Security;
using Lyria.Domain.Users;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Lyria.Api.Security;

/// <summary>
/// Resuelve la identidad autenticada a partir del claim <c>sub</c> del access token.
/// </summary>
/// <remarks>
/// La identidad procede exclusivamente de <see cref="HttpContext.User"/>, que solo se
/// puebla tras validar firma, emisor, audiencia y expiración del token. No se consulta
/// el cuerpo de la solicitud, la query string ni cabeceras personalizadas.
/// </remarks>
internal sealed class CurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    public UserId? UserId
    {
        get
        {
            ClaimsPrincipal? principal = httpContextAccessor.HttpContext?.User;

            if (principal?.Identity is not { IsAuthenticated: true })
            {
                return null;
            }

            string? subject = principal.FindFirstValue(JwtRegisteredClaimNames.Sub);

            if (string.IsNullOrWhiteSpace(subject))
            {
                return null;
            }

            if (!Guid.TryParse(subject, out Guid userId) || userId == Guid.Empty)
            {
                return null;
            }

            return new UserId(userId);
        }
    }
}
