using Lyria.Application.Abstractions.Messaging;
using Lyria.Application.Abstractions.Persistence;
using Lyria.Application.Common.Results;
using Lyria.Domain.Establishments.Branches;

namespace Lyria.Application.Features.EstablishmentBranches.GetById;

public sealed class GetEstablishmentBranchByIdQueryHandler(
    IEstablishmentBranchReadService readService)
    : IQueryHandler<GetEstablishmentBranchByIdQuery, Result<EstablishmentBranchResponse>>
{
    public async ValueTask<Result<EstablishmentBranchResponse>> Handle(
        GetEstablishmentBranchByIdQuery query,
        CancellationToken cancellationToken)
    {
        var id = new EstablishmentBranchId(query.Id);

        EstablishmentBranchResponse? response = await readService.GetByIdAsync(
            id, cancellationToken);

        if (response is null)
        {
            return Result.Failure<EstablishmentBranchResponse>(
                EstablishmentBranchErrors.NotFound(query.Id));
        }

        return Result.Success(response);
    }
}
