using Lyria.Application.Abstractions.Messaging;

namespace Lyria.Application.Features.UserRestrictions.Assign;

public sealed record AssignRestrictionToUserCommand(
    Guid UserId,
    Guid RestrictionId,
    string ImportanceLevel) : ICommand;
