using FluentValidation;
using ZoiNailAcademy.Domain.Entities;

namespace ZoiNailAcademy.Application.ListasEspera.MarcarContactado;

public class MarcarContactadoValidator : AbstractValidator<MarcarContactadoCommand>
{
    public MarcarContactadoValidator()
    {
        RuleFor(x => x.Notas.Value)
            .MaximumLength(ListaEspera.NotasMaxLength)
            .OverridePropertyName("Notas")
            .WithMessage($"Las notas no pueden superar {ListaEspera.NotasMaxLength} caracteres.");
    }
}
