using FluentValidation;

namespace ZoiNailAcademy.Application.ListasEspera.ListarListaEspera;

public class ListarListaEsperaValidator : AbstractValidator<ListarListaEsperaQuery>
{
    public ListarListaEsperaValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(ListarListaEsperaQuery.PrimeraPagina).WithMessage("La página debe ser 1 o mayor.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, ListarListaEsperaQuery.PageSizeMaximo)
            .WithMessage($"El tamaño de página debe estar entre 1 y {ListarListaEsperaQuery.PageSizeMaximo}.");

        RuleFor(x => x.Q).MaximumLength(200).WithMessage("La búsqueda no puede superar 200 caracteres.");
    }
}
