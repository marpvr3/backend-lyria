using Lyria.Application.Abstractions.Messaging;
using Lyria.Application.Abstractions.Persistence;
using Lyria.Application.Abstractions.Services;
using Lyria.Application.Common.Results;
using Lyria.Domain.Establishments.Branches;

namespace Lyria.Application.Features.BranchSpecialSchedules.UpdateTimeZone;

public sealed class UpdateBranchTimeZoneCommandHandler(
    IEstablishmentBranchRepository branchRepository,
    ITimeZoneService timeZoneService)
    : ICommandHandler<UpdateBranchTimeZoneCommand>
{
    public async ValueTask<Result> Handle(
        UpdateBranchTimeZoneCommand command,
        CancellationToken cancellationToken)
    {
        var branchId = new EstablishmentBranchId(command.BranchId);

        EstablishmentBranch? branch = await branchRepository.GetByIdAsync(branchId, cancellationToken);

        if (branch is null)
        {
            return Result.Failure(
                BranchSpecialScheduleErrors.BranchNotFound(command.BranchId));
        }

        if (!timeZoneService.IsValid(command.TimeZoneId))
        {
            return Result.Failure(
                BranchSpecialScheduleErrors.InvalidTimeZone(command.TimeZoneId));
        }

        branch.UpdateTimeZoneId(command.TimeZoneId);

        await branchRepository.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
