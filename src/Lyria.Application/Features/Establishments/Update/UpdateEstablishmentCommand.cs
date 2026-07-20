using Lyria.Application.Abstractions.Messaging;

namespace Lyria.Application.Features.Establishments.Update;

public sealed record UpdateEstablishmentCommand(
    Guid Id,
    Guid CategoryId,
    string Name,
    string Slug,
    string? Description,
    string? Website,
    string? Instagram,
    string? LogoUrl,
    string? ContactEmail,
    string? ContactPhone,
    bool IsVerified) : ICommand;
