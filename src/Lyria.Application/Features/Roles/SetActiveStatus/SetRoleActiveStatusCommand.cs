using Lyria.Application.Abstractions.Messaging;

namespace Lyria.Application.Features.Roles.SetActiveStatus;

public sealed record SetRoleActiveStatusCommand(
    Guid Id,
    bool IsActive) : ICommand;
