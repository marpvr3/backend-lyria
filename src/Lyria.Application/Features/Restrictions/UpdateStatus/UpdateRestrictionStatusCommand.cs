using Lyria.Application.Abstractions.Messaging;

namespace Lyria.Application.Features.Restrictions.UpdateStatus;

public sealed record UpdateRestrictionStatusCommand(
    Guid Id,
    bool IsActive) : ICommand;
