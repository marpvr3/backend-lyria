using Lyria.Application.Abstractions.Messaging;

namespace Lyria.Application.Features.EstablishmentCategories.Deactivate;

public sealed record DeactivateEstablishmentCategoryCommand(Guid Id) : ICommand;
