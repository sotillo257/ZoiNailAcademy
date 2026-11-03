using FluentValidation;
using ZoiNailAcademy.Domain.Entities;

namespace ZoiNailAcademy.Application.Cursos;

/// <summary>Field rules of <see cref="ICursoCampos"/> (data model lengths, slug format, price).</summary>
public abstract class CursoCamposValidator<T> : AbstractValidator<T> where T : ICursoCampos
{
    protected CursoCamposValidator()
    {
        RuleFor(x => x.Nombre)
            .NotEmpty().WithMessage("El nombre es obligatorio.")
            .MaximumLength(Curso.NombreMaxLength).WithMessage($"El nombre no puede superar {Curso.NombreMaxLength} caracteres.");

        RuleFor(x => x.Slug)
            .NotEmpty().WithMessage("El slug es obligatorio.")
            .MaximumLength(Curso.SlugMaxLength).WithMessage($"El slug no puede superar {Curso.SlugMaxLength} caracteres.")
            .Must(Curso.EsSlugValido)
            .WithMessage("El slug solo admite minúsculas, números y guiones (p. ej. 'acrilico-avanzado').");

        RuleFor(x => x.DescripcionCorta)
            .NotEmpty().WithMessage("La descripción corta es obligatoria.")
            .MaximumLength(Curso.DescripcionCortaMaxLength)
            .WithMessage($"La descripción corta no puede superar {Curso.DescripcionCortaMaxLength} caracteres.");

        RuleFor(x => x.Descripcion).NotNull().WithMessage("La descripción es obligatoria.");

        RuleFor(x => x.Precio)
            .GreaterThan(0).WithMessage("El precio debe ser mayor que cero.")
            .PrecisionScale(10, 2, ignoreTrailingZeros: true).WithMessage("El precio admite como máximo 2 decimales.");

        // Null means the whole price is paid when booking.
        When(x => x.PrecioReserva is not null, () =>
        {
            RuleFor(x => x.PrecioReserva)
                .GreaterThan(0).WithMessage("La reserva de plaza debe ser mayor que cero.")
                .PrecisionScale(10, 2, ignoreTrailingZeros: true)
                .WithMessage("La reserva de plaza admite como máximo 2 decimales.")
                .Must((x, reserva) => reserva <= x.Precio)
                .WithMessage("La reserva de plaza no puede superar el precio del curso.");
        });

        RuleFor(x => x.Imagen)
            .NotEmpty().WithMessage("La imagen es obligatoria.")
            .MaximumLength(Curso.ImagenMaxLength).WithMessage($"La imagen no puede superar {Curso.ImagenMaxLength} caracteres.");

        RuleFor(x => x.Ubicacion)
            .MaximumLength(Curso.UbicacionMaxLength)
            .WithMessage($"La ubicación no puede superar {Curso.UbicacionMaxLength} caracteres.");

        RuleFor(x => x.DuracionTexto)
            .MaximumLength(Curso.DuracionTextoMaxLength)
            .WithMessage($"La duración no puede superar {Curso.DuracionTextoMaxLength} caracteres.");

        RuleFor(x => x.Orden).GreaterThanOrEqualTo(0).WithMessage("El orden no puede ser negativo.");
    }
}
