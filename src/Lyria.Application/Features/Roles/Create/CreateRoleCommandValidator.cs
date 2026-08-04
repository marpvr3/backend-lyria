using FluentValidation;
using Lyria.Domain.Roles;

namespace Lyria.Application.Features.Roles.Create;

public sealed class CreateRoleCommandValidator
    : AbstractValidator<CreateRoleCommand>
{
    public CreateRoleCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("El nombre es obligatorio.")
            .MinimumLength(Role.NameMinLength)
            .WithMessage($"El nombre debe tener al menos {Role.NameMinLength} caracteres.")
            .MaximumLength(Role.NameMaxLength)
            .WithMessage($"El nombre no puede superar los {Role.NameMaxLength} caracteres.");

        RuleFor(x => x.Description)
            .MaximumLength(Role.DescriptionMaxLength)
            .WithMessage($"La descripción no puede superar los {Role.DescriptionMaxLength} caracteres.")
            .When(x => x.Description is not null);
    }
}
