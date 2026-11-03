using ZoiNailAcademy.Domain.Entities;

namespace ZoiNailAcademy.Application.Notes;

public sealed record NoteDto(Guid Id, string Title, string Content, DateTime CreatedAt, DateTime? UpdatedAt)
{
    public static NoteDto FromEntity(Note note)
        => new(note.Id, note.Title, note.Content, note.CreatedAt, note.UpdatedAt);
}
