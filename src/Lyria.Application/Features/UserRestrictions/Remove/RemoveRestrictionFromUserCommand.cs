using Lyria.Application.Abstractions.Messaging;

namespace Lyria.Application.Features.UserRestrictions.Remove;

public sealed record RemoveRestrictionFromUserCommand(
    Guid UserId,
    Guid RestrictionId) : ICommand;
