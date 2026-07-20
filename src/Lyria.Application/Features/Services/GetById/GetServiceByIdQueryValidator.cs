using FluentValidation;

namespace Lyria.Application.Features.Services.GetById;

public sealed class GetServiceByIdQueryValidator
    : AbstractValidator<GetServiceByIdQuery>
{
    public GetServiceByIdQueryValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("El identificador es obligatorio.");
    }
}
