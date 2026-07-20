using Lyria.Application.Abstractions.Messaging;
using Lyria.Domain.Establishments;

namespace Lyria.Application.Features.Establishments.Create;

public sealed record CreateEstablishmentCommand(
    Guid CategoryId,
    string Name,
    string Slug,
    string? Description,
    string? Website,
    string? Instagram,
    string? LogoUrl,
    string? ContactEmail,
    string? ContactPhone) : ICommand<EstablishmentId>;
