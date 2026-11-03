using FluentValidation;
using ZoiNailAcademy.Domain.Entities;

namespace ZoiNailAcademy.Application.Ediciones.EditarEdicion;

public class EditarEdicionValidator : AbstractValidator<EditarEdicionCommand>
{
    public EditarEdicionValidator()
    {
        RuleFor(x => x.Plazas)
            .InclusiveBetween(Edicion.PlazasMinimas, Edicion.PlazasMaximas)
            .WithMessage($"Las plazas deben estar entre {Edicion.PlazasMinimas} y {Edicion.PlazasMaximas}.");

        RuleFor(x => x.Estado).IsInEnum().WithMessage("El estado de la edición no es válido.");

        RuleFor(x => x.Notas)
            .MaximumLength(Edicion.NotasMaxLength)
            .WithMessage($"Las notas no pueden superar {Edicion.NotasMaxLength} caracteres.");

        RuleFor(x => x.Version)
            .Must(version => uint.TryParse(version, out _))
            .WithMessage("La versión de la edición es obligatoria: recarga la edición y vuelve a intentarlo.");
    }
}
