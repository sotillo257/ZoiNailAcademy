using FluentValidation;

namespace ZoiNailAcademy.Application.Reservas.ListarReservasEdicion;

public class ListarReservasEdicionValidator : AbstractValidator<ListarReservasEdicionQuery>
{
    public static readonly string IncluirMessage =
        $"Valores admitidos en incluir: {string.Join(", ", IncluirEstados.Valores.Keys)}.";

    public ListarReservasEdicionValidator()
    {
        RuleFor(x => x.Incluir).Must(i => IncluirEstados.TryParse(i, out _)).WithMessage(IncluirMessage);
    }
}
