using Lyria.Application.Abstractions.Messaging;

namespace Lyria.Application.Features.EstablishmentCategories.Reactivate;

public sealed record ReactivateEstablishmentCategoryCommand(Guid Id) : ICommand;
