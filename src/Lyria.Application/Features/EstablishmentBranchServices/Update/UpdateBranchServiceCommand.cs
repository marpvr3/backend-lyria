using Lyria.Application.Abstractions.Messaging;

namespace Lyria.Application.Features.EstablishmentBranchServices.Update;

public sealed record UpdateBranchServiceCommand(
    Guid BranchId,
    Guid ServiceId,
    bool IsAvailable,
    string? Observation) : ICommand;
