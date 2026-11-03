using FluentValidation;
using ZoiNailAcademy.Domain.Entities;
using ZoiNailAcademy.Domain.Enums;

namespace ZoiNailAcademy.Application.Ediciones.CrearEdicion;

public class CrearEdicionValidator : AbstractValidator<CrearEdicionCommand>
{
    public CrearEdicionValidator(TimeProvider timeProvider)
    {
        RuleFor(x => x.CursoId).NotEmpty().WithMessage("El curso es obligatorio.");

        RuleFor(x => x.Inicio)
            .Must(inicio => inicio > timeProvider.GetUtcNow())
            .WithMessage("La fecha de inicio debe estar en el futuro.");

        RuleFor(x => x.Plazas)
            .InclusiveBetween(Edicion.PlazasMinimas, Edicion.PlazasMaximas)
            .WithMessage($"Las plazas deben estar entre {Edicion.PlazasMinimas} y {Edicion.PlazasMaximas}.");

        RuleFor(x => x.Estado)
            .Must(estado => estado is null or EstadoEdicion.Programada or EstadoEdicion.Oculta)
            .WithMessage("Una edición nueva solo puede estar Programada u Oculta.");

        RuleFor(x => x.Notas)
            .MaximumLength(Edicion.NotasMaxLength)
            .WithMessage($"Las notas no pueden superar {Edicion.NotasMaxLength} caracteres.");
    }
}
