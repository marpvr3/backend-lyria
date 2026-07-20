using Lyria.Application.Abstractions.Messaging;
using Lyria.Application.Abstractions.Persistence;
using Lyria.Application.Common.Results;
using Lyria.Domain.Establishments.Branches;
using Lyria.Domain.Services;

namespace Lyria.Application.Features.EstablishmentBranchServices.GetByIds;

public sealed class GetBranchServiceByIdsQueryHandler(
    IEstablishmentBranchServiceReadService readService)
    : IQueryHandler<GetBranchServiceByIdsQuery, Result<EstablishmentBranchServiceResponse>>
{
    public async ValueTask<Result<EstablishmentBranchServiceResponse>> Handle(
        GetBranchServiceByIdsQuery query,
        CancellationToken cancellationToken)
    {
        var branchId = new EstablishmentBranchId(query.BranchId);
        var serviceId = new ServiceId(query.ServiceId);

        EstablishmentBranchServiceResponse? response =
            await readService.GetByIdsAsync(branchId, serviceId, cancellationToken);

        if (response is null)
        {
            return Result.Failure<EstablishmentBranchServiceResponse>(
                EstablishmentBranchServiceErrors.NotFound(query.BranchId, query.ServiceId));
        }

        return Result.Success(response);
    }
}
