using FluentValidation;

namespace Lyria.Application.Features.EstablishmentBranches.GetById;

public sealed class GetEstablishmentBranchByIdQueryValidator
    : AbstractValidator<GetEstablishmentBranchByIdQuery>
{
    public GetEstablishmentBranchByIdQueryValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("El identificador es obligatorio.");
    }
}
