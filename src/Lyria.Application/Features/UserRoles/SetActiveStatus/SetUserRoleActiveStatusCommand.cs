using Lyria.Application.Abstractions.Messaging;

namespace Lyria.Application.Features.UserRoles.SetActiveStatus;

public sealed record SetUserRoleActiveStatusCommand(
    Guid UserId,
    Guid UserRoleId,
    bool IsActive) : ICommand;
