using Lyria.Application.Abstractions.Messaging;

namespace Lyria.Application.Features.UserRestrictions.Update;

public sealed record UpdateUserRestrictionCommand(
    Guid UserId,
    Guid RestrictionId,
    string ImportanceLevel) : ICommand;
