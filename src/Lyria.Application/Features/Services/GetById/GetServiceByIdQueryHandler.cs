using Lyria.Application.Abstractions.Messaging;
using Lyria.Application.Abstractions.Persistence;
using Lyria.Application.Common.Results;
using Lyria.Domain.Services;

namespace Lyria.Application.Features.Services.GetById;

public sealed class GetServiceByIdQueryHandler(
    IServiceReadService readService)
    : IQueryHandler<GetServiceByIdQuery, Result<ServiceResponse>>
{
    public async ValueTask<Result<ServiceResponse>> Handle(
        GetServiceByIdQuery query,
        CancellationToken cancellationToken)
    {
        var id = new ServiceId(query.Id);

        ServiceResponse? response = await readService.GetByIdAsync(id, cancellationToken);

        if (response is null)
        {
            return Result.Failure<ServiceResponse>(ServiceErrors.NotFound(query.Id));
        }

        return Result.Success(response);
    }
}
