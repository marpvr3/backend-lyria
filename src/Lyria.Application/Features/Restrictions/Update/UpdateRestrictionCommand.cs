using Lyria.Application.Abstractions.Messaging;

namespace Lyria.Application.Features.Restrictions.Update;

public sealed record UpdateRestrictionCommand(
    Guid Id,
    string Name,
    string? Description) : ICommand;
