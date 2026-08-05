using Lyria.Application.Abstractions.Messaging;
using Lyria.Application.Abstractions.Persistence;
using Lyria.Application.Abstractions.Security;
using Lyria.Application.Common.Results;
using Lyria.Application.Features.Authentication;
using Lyria.Domain.Users;

namespace Lyria.Application.Features.Users.GetCurrent;

/// <summary>
/// Devuelve el perfil mínimo del usuario autenticado.
/// </summary>
/// <remarks>
/// Si el token es válido pero el usuario ya no existe, se responde con el mismo error
/// de autenticación en lugar de un 404: la respuesta no debe permitir sondear qué
/// identificadores existen ni exponer datos de otro usuario.
/// </remarks>
public sealed class GetCurrentUserQueryHandler(
    ICurrentUser currentUser,
    IUserReadService readService)
    : IQueryHandler<GetCurrentUserQuery, Result<CurrentUserResponse>>
{
    public async ValueTask<Result<CurrentUserResponse>> Handle(
        GetCurrentUserQuery query,
        CancellationToken cancellationToken)
    {
        UserId? userId = currentUser.UserId;

        if (userId is null)
        {
            return Result.Failure<CurrentUserResponse>(
                AuthenticationErrors.NotAuthenticated());
        }

        UserResponse? user = await readService.GetByIdAsync(
            userId.Value, cancellationToken);

        if (user is null)
        {
            return Result.Failure<CurrentUserResponse>(
                AuthenticationErrors.NotAuthenticated());
        }

        return Result.Success(new CurrentUserResponse(
            user.Id,
            user.Name,
            user.LastName,
            user.Email,
            user.Phone,
            user.BirthDate,
            user.PhotoUrl,
            user.Status,
            user.IsEmailVerified));
    }
}
