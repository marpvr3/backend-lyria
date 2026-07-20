using Lyria.Application.Abstractions.Messaging;

namespace Lyria.Application.Features.EstablishmentBranchRestrictions.UpdateStatus;

public sealed record UpdateBranchRestrictionStatusCommand(
    Guid BranchId,
    Guid RestrictionId,
    bool IsActive) : ICommand;
