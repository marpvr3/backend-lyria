using Lyria.Application.Abstractions.Messaging;

namespace Lyria.Application.Features.Establishments.UpdateStatus;

public sealed record UpdateEstablishmentStatusCommand(
    Guid Id,
    bool IsActive) : ICommand;
