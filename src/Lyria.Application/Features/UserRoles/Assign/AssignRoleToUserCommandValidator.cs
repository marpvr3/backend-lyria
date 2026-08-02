using FluentValidation;

namespace Lyria.Application.Features.UserRoles.Assign;

public sealed class AssignRoleToUserCommandValidator
    : AbstractValidator<AssignRoleToUserCommand>
{
    public AssignRoleToUserCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("El identificador del usuario es obligatorio.");

        RuleFor(x => x.RoleId)
            .NotEmpty()
            .WithMessage("El identificador del rol es obligatorio.");

        RuleFor(x => x.ScopeType)
            .NotEmpty()
            .WithMessage("El tipo de alcance es obligatorio.");
    }
}
