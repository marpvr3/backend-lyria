using FluentValidation;

namespace Lyria.Application.Features.UserRestrictions.GetById;

public sealed class GetUserRestrictionByIdQueryValidator
    : AbstractValidator<GetUserRestrictionByIdQuery>
{
    public GetUserRestrictionByIdQueryValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("El identificador del usuario es obligatorio.");

        RuleFor(x => x.RestrictionId)
            .NotEmpty().WithMessage("El identificador de la restricción es obligatorio.");
    }
}
