using Lyria.Application.Abstractions.Messaging;

namespace Lyria.Application.Features.EstablishmentBranchServices.UpdateStatus;

public sealed record UpdateBranchServiceStatusCommand(
    Guid BranchId,
    Guid ServiceId,
    bool IsActive) : ICommand;
