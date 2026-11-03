using FluentValidation;

namespace ZoiNailAcademy.Application.Authentication.ChangePassword;

public class ChangePasswordValidator : AbstractValidator<ChangePasswordCommand>
{
    public const int MinimumPasswordLength = 10;

    public ChangePasswordValidator()
    {
        RuleFor(x => x.PasswordActual)
            .NotEmpty().WithMessage("La contraseña actual es obligatoria.");

        RuleFor(x => x.PasswordNueva)
            .NotEmpty().WithMessage("La nueva contraseña es obligatoria.")
            .MinimumLength(MinimumPasswordLength)
            .WithMessage($"La nueva contraseña debe tener al menos {MinimumPasswordLength} caracteres.");
    }
}
