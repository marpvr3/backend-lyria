using FluentValidation;

namespace Lyria.Application.Features.PublicCatalog.GetEstablishmentBySlug;

public sealed class GetPublicEstablishmentBySlugQueryValidator
    : AbstractValidator<GetPublicEstablishmentBySlugQuery>
{
    public GetPublicEstablishmentBySlugQueryValidator()
    {
        RuleFor(x => x.Slug)
            .NotEmpty()
            .WithMessage("El slug del establecimiento es obligatorio.");
    }
}
