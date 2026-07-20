using Lyria.Application.Abstractions.Messaging;

namespace Lyria.Application.Features.EstablishmentBranches.UpdateStatus;

public sealed record UpdateEstablishmentBranchStatusCommand(
    Guid Id,
    bool IsActive) : ICommand;
