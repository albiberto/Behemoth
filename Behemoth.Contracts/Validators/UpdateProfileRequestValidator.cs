using FluentValidation;

namespace Behemoth.Contracts.Validators;

public class UpdateProfileRequestValidator : AbstractValidator<Contract.Profile.Anagraphy>
{
    public UpdateProfileRequestValidator()
    {
        RuleFor(x => x.Username)
            .NotEmpty()
            .MaximumLength(50);

        RuleFor(x => x.Bio)
            .MaximumLength(500);
    }
}