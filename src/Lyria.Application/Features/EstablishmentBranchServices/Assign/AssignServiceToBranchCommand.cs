using Lyria.Application.Abstractions.Messaging;

namespace Lyria.Application.Features.EstablishmentBranchServices.Assign;

public sealed record AssignServiceToBranchCommand(
    Guid BranchId,
    Guid ServiceId,
    bool IsAvailable,
    string? Observation) : ICommand;
