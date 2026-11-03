using FluentValidation;
using ZoiNailAcademy.Domain.Entities;

namespace ZoiNailAcademy.Application.Reservas.AmpliarReserva;

public class AmpliarReservaValidator : AbstractValidator<AmpliarReservaCommand>
{
    public AmpliarReservaValidator()
    {
        RuleFor(x => x.Version).Must(v => uint.TryParse(v, out _)).WithMessage(ReservaAccionValidation.VersionMessage);
        RuleFor(x => x.Horas)
            .InclusiveBetween(1, Reserva.HorasAmpliacionMaximas)
            .WithMessage($"Las horas deben estar entre 1 y {Reserva.HorasAmpliacionMaximas}.");
    }
}
