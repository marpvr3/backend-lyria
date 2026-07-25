using FluentValidation;

namespace Lyria.Application.Features.PublicCatalog.GetBranchById;

public sealed class GetPublicBranchByIdQueryValidator
    : AbstractValidator<GetPublicBranchByIdQuery>
{
    public GetPublicBranchByIdQueryValidator()
    {
        RuleFor(x => x.BranchId)
            .NotEmpty()
            .WithMessage("El identificador de la sede es obligatorio.");
    }
}
