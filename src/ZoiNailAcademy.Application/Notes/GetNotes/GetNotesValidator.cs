using FluentValidation;

namespace ZoiNailAcademy.Application.Notes.GetNotes;

public class GetNotesValidator : AbstractValidator<GetNotesQuery>
{
    public const int MaxPageSize = 100;

    public GetNotesValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(1).WithMessage("La página debe ser mayor o igual que 1.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, MaxPageSize).WithMessage($"El tamaño de página debe estar entre 1 y {MaxPageSize}.");
    }
}
