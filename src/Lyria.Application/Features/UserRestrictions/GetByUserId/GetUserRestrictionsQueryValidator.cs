using FluentValidation;

namespace Lyria.Application.Features.UserRestrictions.GetByUserId;

public sealed class GetUserRestrictionsQueryValidator
    : AbstractValidator<GetUserRestrictionsQuery>
{
    public GetUserRestrictionsQueryValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("El identificador del usuario es obligatorio.");
    }
}
