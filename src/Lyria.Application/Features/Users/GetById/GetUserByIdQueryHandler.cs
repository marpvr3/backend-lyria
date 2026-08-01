using Lyria.Application.Abstractions.Messaging;
using Lyria.Application.Abstractions.Persistence;
using Lyria.Application.Common.Results;
using Lyria.Domain.Users;

namespace Lyria.Application.Features.Users.GetById;

public sealed class GetUserByIdQueryHandler(
    IUserReadService readService)
    : IQueryHandler<GetUserByIdQuery, Result<UserResponse>>
{
    public async ValueTask<Result<UserResponse>> Handle(
        GetUserByIdQuery query,
        CancellationToken cancellationToken)
    {
        var id = new UserId(query.Id);

        UserResponse? response = await readService.GetByIdAsync(
            id, cancellationToken);

        if (response is null)
        {
            return Result.Failure<UserResponse>(
                UserErrors.NotFound(query.Id));
        }

        return Result.Success(response);
    }
}
