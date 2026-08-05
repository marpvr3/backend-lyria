using Lyria.Application.Abstractions.Persistence;
using Lyria.Domain.Users.RefreshTokens;
using Microsoft.EntityFrameworkCore;

namespace Lyria.Infrastructure.Persistence.Repositories;

internal sealed class UserRefreshTokenRepository(LyriaDbContext dbContext)
    : IUserRefreshTokenRepository
{
    public async Task<UserRefreshToken?> GetByTokenHashAsync(
        string tokenHash,
        CancellationToken cancellationToken)
    {
        string normalizedHash = UserRefreshToken.NormalizeTokenHash(tokenHash);

        if (normalizedHash.Length == 0)
        {
            return null;
        }

        // Con seguimiento: la rotación y el cierre de sesión modifican la entidad
        // recuperada y confirman el cambio a través de IAuthenticationSessionWriter.
        return await dbContext.Set<UserRefreshToken>()
            .FirstOrDefaultAsync(rt => rt.TokenHash == normalizedHash, cancellationToken);
    }
}
