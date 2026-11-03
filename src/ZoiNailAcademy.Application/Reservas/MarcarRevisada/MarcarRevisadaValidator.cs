using FluentValidation;
using ZoiNailAcademy.Domain.Entities;

namespace ZoiNailAcademy.Application.Reservas.MarcarRevisada;

public class MarcarRevisadaValidator : AbstractValidator<MarcarRevisadaCommand>
{
    public MarcarRevisadaValidator()
    {
        RuleFor(x => x.Version).Must(v => uint.TryParse(v, out _)).WithMessage(ReservaAccionValidation.VersionMessage);
        RuleFor(x => x.Nota).MaximumLength(Reserva.NotaInternaMaxLength).WithMessage(ReservaAccionValidation.NotaMessage);
    }
}
