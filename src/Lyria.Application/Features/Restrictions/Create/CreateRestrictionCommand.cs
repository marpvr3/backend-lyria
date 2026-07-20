using Lyria.Application.Abstractions.Messaging;
using Lyria.Domain.Restrictions;

namespace Lyria.Application.Features.Restrictions.Create;

public sealed record CreateRestrictionCommand(
    string Name,
    string? Description) : ICommand<RestrictionId>;
