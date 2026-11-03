using FluentValidation;

namespace ZoiNailAcademy.Application.Reservas.ListarPendientes;

public class ListarPendientesValidator : AbstractValidator<ListarPendientesQuery>
{
    public ListarPendientesValidator()
    {
        RuleFor(x => x.Q).MaximumLength(ListarPendientesQuery.QMaxLength)
            .WithMessage($"La búsqueda no puede superar {ListarPendientesQuery.QMaxLength} caracteres.");
        RuleFor(x => x.Metodo).IsInEnum().WithMessage("El método de pago no es válido.");
    }
}
