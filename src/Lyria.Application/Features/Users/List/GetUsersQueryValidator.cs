using FluentValidation;

namespace Lyria.Application.Features.Users.List;

public sealed class GetUsersQueryValidator
    : AbstractValidator<GetUsersQuery>
{
    private static readonly string[] AllowedSortByValues =
        ["name", "lastName", "email", "status", "createdAtUtc"];

    private static readonly string[] AllowedSortDirectionValues =
        ["asc", "desc"];

    public GetUsersQueryValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(1)
            .WithMessage("La página debe ser al menos 1.");

        RuleFor(x => x.PageSize)
            .GreaterThanOrEqualTo(1)
            .WithMessage("El tamaño de página debe ser al menos 1.")
            .LessThanOrEqualTo(100)
            .WithMessage("El tamaño de página no puede superar 100.");

        RuleFor(x => x.SortBy)
            .Must(value => AllowedSortByValues.Contains(value, StringComparer.OrdinalIgnoreCase))
            .WithMessage($"El campo de ordenamiento debe ser uno de: {string.Join(", ", AllowedSortByValues)}.")
            .When(x => x.SortBy is not null);

        RuleFor(x => x.SortDirection)
            .Must(value => AllowedSortDirectionValues.Contains(value, StringComparer.OrdinalIgnoreCase))
            .WithMessage("La dirección de ordenamiento debe ser 'asc' o 'desc'.")
            .When(x => x.SortDirection is not null);
    }
}
