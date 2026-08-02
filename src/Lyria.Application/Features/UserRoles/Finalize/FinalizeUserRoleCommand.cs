using Lyria.Application.Abstractions.Messaging;

namespace Lyria.Application.Features.UserRoles.Finalize;

public sealed record FinalizeUserRoleCommand(
    Guid UserId,
    Guid UserRoleId) : ICommand;
