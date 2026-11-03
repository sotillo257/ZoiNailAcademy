using FluentValidation;
using ZoiNailAcademy.Domain.Entities;

namespace ZoiNailAcademy.Application.Notes.UpdateNote;

public class UpdateNoteValidator : AbstractValidator<UpdateNoteCommand>
{
    public UpdateNoteValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("El título es obligatorio.")
            .MaximumLength(Note.TitleMaxLength).WithMessage($"El título no puede superar {Note.TitleMaxLength} caracteres.");

        RuleFor(x => x.Content)
            .NotNull().WithMessage("El contenido es obligatorio.")
            .MaximumLength(Note.ContentMaxLength).WithMessage($"El contenido no puede superar {Note.ContentMaxLength} caracteres.");
    }
}
