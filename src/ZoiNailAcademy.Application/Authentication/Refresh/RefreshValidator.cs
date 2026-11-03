using FluentValidation;

namespace ZoiNailAcademy.Application.Authentication.Refresh;

public class RefreshValidator : AbstractValidator<RefreshCommand>
{
    public RefreshValidator()
    {
        RuleFor(x => x.RefreshToken).NotEmpty().WithMessage("El refresh token es obligatorio.");
    }
}
