using FluentValidation;
using ZoiNailAcademy.Application.Common.Contacto;

namespace ZoiNailAcademy.Application.Contacto.EnviarMensajeContacto;

public class EnviarMensajeContactoValidator : AbstractValidator<EnviarMensajeContactoCommand>
{
    public const int NombreMaxLength = 120;
    public const int EmailMaxLength = 200;
    public const int MensajeMinLength = 10;
    public const int MensajeMaxLength = 3000;

    public EnviarMensajeContactoValidator()
    {
        RuleFor(x => x.Nombre)
            .NotEmpty().WithMessage("El nombre es obligatorio.")
            .MaximumLength(NombreMaxLength).WithMessage($"El nombre no puede superar {NombreMaxLength} caracteres.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("El email es obligatorio.")
            .MaximumLength(EmailMaxLength).WithMessage($"El email no puede superar {EmailMaxLength} caracteres.")
            .EmailAddress().WithMessage("El email no es válido.");

        RuleFor(x => x.Telefono)
            .Must(TelefonoNormalizer.EsValido).When(x => !string.IsNullOrWhiteSpace(x.Telefono))
            .WithMessage("El teléfono no es válido.");

        RuleFor(x => x.Mensaje)
            .Must(m => (m?.Trim().Length ?? 0) is >= MensajeMinLength and <= MensajeMaxLength)
            .WithMessage($"El mensaje debe tener entre {MensajeMinLength} y {MensajeMaxLength} caracteres.");

        RuleFor(x => x.AceptaPrivacidad)
            .Equal(true).WithMessage("Debes aceptar la política de privacidad.");
    }
}
