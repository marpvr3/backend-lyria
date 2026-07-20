using Lyria.Application.Abstractions.Messaging;
using Lyria.Domain.Services;

namespace Lyria.Application.Features.Services.Create;

public sealed record CreateServiceCommand(
    string Name,
    string? Description,
    string? IconUrl) : ICommand<ServiceId>;
