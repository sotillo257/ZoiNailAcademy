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

        RuleFor(x => x.Precio)
            .GreaterThan(0).WithMessage("El precio debe ser mayor que cero.")
            .When(x => x.Precio is not null);

        RuleFor(x => x.PrecioReserva)
            .GreaterThan(0).WithMessage("La reserva de plaza debe ser mayor que cero.")
            .When(x => x.PrecioReserva is not null);

        RuleFor(x => x)
            .Must(x => x.Precio is null || x.PrecioReserva is null || x.PrecioReserva <= x.Precio)
            .WithMessage("La reserva de plaza no puede superar el precio.")
            .WithName("PrecioReserva");

        RuleFor(x => x.Notas)
            .MaximumLength(Edicion.NotasMaxLength)
            .WithMessage($"Las notas no pueden superar {Edicion.NotasMaxLength} caracteres.");

        RuleFor(x => x.Version)
            .Must(version => uint.TryParse(version, out _))
            .WithMessage("La versión de la edición es obligatoria: recarga la edición y vuelve a intentarlo.");
    }
}
