using System.Text;
using Lyria.Infrastructure.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Lyria.Api.Extensions;

/// <summary>
/// Registro de la autenticación Bearer basada en el access token JWT de Lyria.
/// </summary>
public static class AuthenticationExtensions
{
    /// <summary>
    /// Configura la validación del access token.
    /// </summary>
    /// <remarks>
    /// Se validan de forma obligatoria firma, emisor, audiencia y expiración.
    /// <c>ClockSkew</c> se fija en cero para que el token expire exactamente cuando
    /// indica su claim <c>exp</c>, en lugar de los 5 minutos de tolerancia
    /// predeterminados.
    ///
    /// <c>MapInboundClaims</c> se deshabilita para que el claim <c>sub</c> conserve su
    /// nombre original en lugar de reescribirse como <c>nameidentifier</c>: la identidad
    /// se lee siempre desde <c>sub</c>.
    ///
    /// No se registra ninguna política de autorización predeterminada: los endpoints
    /// públicos y el registro móvil siguen siendo anónimos, y la protección se aplica
    /// endpoint por endpoint con <c>[Authorize]</c>.
    /// </remarks>
    public static IServiceCollection AddLyriaAuthentication(this IServiceCollection services)
    {
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer();

        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtOptions>>((bearerOptions, jwtOptions) =>
            {
                JwtOptions jwt = jwtOptions.Value;

                bearerOptions.MapInboundClaims = false;

                // El token nunca debe devolverse al cliente en una cabecera de error.
                bearerOptions.IncludeErrorDetails = false;

                bearerOptions.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(jwt.SigningKey)),
                    ValidateIssuer = true,
                    ValidIssuer = jwt.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwt.Audience,
                    ValidateLifetime = true,
                    RequireExpirationTime = true,
                    RequireSignedTokens = true,
                    ClockSkew = TimeSpan.Zero,
                    NameClaimType = "sub"
                };
            });

        services.AddAuthorization();

        return services;
    }
}
