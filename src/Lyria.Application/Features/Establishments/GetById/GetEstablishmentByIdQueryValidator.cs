using FluentValidation;

namespace Lyria.Application.Features.Establishments.GetById;

public sealed class GetEstablishmentByIdQueryValidator
    : AbstractValidator<GetEstablishmentByIdQuery>
{
    public GetEstablishmentByIdQueryValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("El identificador es obligatorio.");
    }
}
