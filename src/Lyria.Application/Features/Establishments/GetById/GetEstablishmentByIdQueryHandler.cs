using Lyria.Application.Abstractions.Messaging;
using Lyria.Application.Abstractions.Persistence;
using Lyria.Application.Common.Results;
using Lyria.Domain.Establishments;

namespace Lyria.Application.Features.Establishments.GetById;

public sealed class GetEstablishmentByIdQueryHandler(
    IEstablishmentReadService readService)
    : IQueryHandler<GetEstablishmentByIdQuery, Result<EstablishmentResponse>>
{
    public async ValueTask<Result<EstablishmentResponse>> Handle(
        GetEstablishmentByIdQuery query,
        CancellationToken cancellationToken)
    {
        var id = new EstablishmentId(query.Id);

        EstablishmentResponse? response = await readService.GetByIdAsync(
            id, cancellationToken);

        if (response is null)
        {
            return Result.Failure<EstablishmentResponse>(
                EstablishmentErrors.NotFound(query.Id));
        }

        return Result.Success(response);
    }
}
