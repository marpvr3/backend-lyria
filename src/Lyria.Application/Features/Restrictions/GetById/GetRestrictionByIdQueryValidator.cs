using FluentValidation;

namespace Lyria.Application.Features.Restrictions.GetById;

public sealed class GetRestrictionByIdQueryValidator
    : AbstractValidator<GetRestrictionByIdQuery>
{
    public GetRestrictionByIdQueryValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("El identificador es obligatorio.");
    }
}
