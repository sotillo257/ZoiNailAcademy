using FluentValidation;
using ZoiNailAcademy.Application.Common.Contacto;
using ZoiNailAcademy.Domain.Entities;

namespace ZoiNailAcademy.Application.Reservas;

/// <summary>Fields of the public booking form, shared by card (F03) and manual (F04) bookings.</summary>
public interface IReservaFormulario
{
    Guid EdicionId { get; }
    string Nombre { get; }
    string Email { get; }
    string Telefono { get; }
    bool AceptaCondiciones { get; }

    /// <summary>Honeypot: hidden in the form, only bots fill it.</summary>
    string? Website { get; }
}

public abstract class ReservaFormularioValidator<T> : AbstractValidator<T> where T : IReservaFormulario
{
    protected ReservaFormularioValidator()
    {
        RuleFor(x => x.EdicionId).NotEmpty().WithMessage("Elige una fecha.");

        RuleFor(x => x.Nombre)
            .NotEmpty().WithMessage("El nombre es obligatorio.")
            .MaximumLength(Reserva.NombreMaxLength).WithMessage($"El nombre no puede superar {Reserva.NombreMaxLength} caracteres.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("El email es obligatorio.")
            .MaximumLength(Reserva.EmailMaxLength).WithMessage($"El email no puede superar {Reserva.EmailMaxLength} caracteres.")
            .EmailAddress().WithMessage("El email no es válido.");

        RuleFor(x => x.Telefono)
            .Must(TelefonoNormalizer.EsValido).WithMessage("El teléfono no es válido.");

        RuleFor(x => x.AceptaCondiciones)
            .Equal(true).WithMessage("Debes aceptar las condiciones de compra y la política de privacidad.");
    }
}
