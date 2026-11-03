using FluentValidation;
using ZoiNailAcademy.Domain.Entities;

namespace ZoiNailAcademy.Application.Reservas.CancelarReserva;

public class CancelarReservaValidator : AbstractValidator<CancelarReservaCommand>
{
    public CancelarReservaValidator()
    {
        RuleFor(x => x.Version).Must(v => uint.TryParse(v, out _)).WithMessage(ReservaAccionValidation.VersionMessage);
        RuleFor(x => x.Motivo).MaximumLength(Reserva.NotaInternaMaxLength).WithMessage(ReservaAccionValidation.NotaMessage);
    }
}
