using FluentValidation;
using ZoiNailAcademy.Application.Common.Contacto;
using ZoiNailAcademy.Domain.Entities;

namespace ZoiNailAcademy.Application.ListasEspera.AltaListaEspera;

public class AltaListaEsperaValidator : AbstractValidator<AltaListaEsperaCommand>
{
    public AltaListaEsperaValidator()
    {
        RuleFor(x => x.CursoId).NotEmpty().WithMessage("Elige un curso.");

        RuleFor(x => x.FechaPreferida)
            .MaximumLength(ListaEspera.FechaPreferidaMaxLength)
            .WithMessage($"La fecha preferida no puede superar {ListaEspera.FechaPreferidaMaxLength} caracteres.");

        RuleFor(x => x.Nombre)
            .NotEmpty().WithMessage("El nombre es obligatorio.")
            .MaximumLength(ListaEspera.NombreMaxLength)
            .WithMessage($"El nombre no puede superar {ListaEspera.NombreMaxLength} caracteres.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("El email es obligatorio.")
            .MaximumLength(ListaEspera.EmailMaxLength)
            .WithMessage($"El email no puede superar {ListaEspera.EmailMaxLength} caracteres.")
            .EmailAddress().WithMessage("El email no es válido.");

        RuleFor(x => x.Telefono)
            .Must(TelefonoNormalizer.EsValido).WithMessage("El teléfono no es válido.");

        RuleFor(x => x.AceptaPrivacidad)
            .Equal(true).WithMessage("Debes aceptar la política de privacidad.");
    }
}
