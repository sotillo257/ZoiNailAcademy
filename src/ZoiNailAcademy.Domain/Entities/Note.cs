using ZoiNailAcademy.Domain.Common;
using ZoiNailAcademy.Domain.Common.Exceptions;

namespace ZoiNailAcademy.Domain.Entities;

/// <summary>
/// Entidad de ejemplo de la zoinailacademy: muestra el patrón de entidad por usuario
/// (UserId + factory Create + métodos que protegen las invariantes). Sustitúyela por las del nuevo dominio.
/// </summary>
public class Note : BaseAuditableEntity
{
    public const int TitleMaxLength = 200;
    public const int ContentMaxLength = 4000;

    public Guid UserId { get; private set; }
    public string Title { get; private set; } = null!;
    public string Content { get; private set; } = null!;

    private Note()
    {
    }

    private Note(Guid userId, string title, string content)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        Title = title;
        Content = content;
    }

    public static Note Create(Guid userId, string title, string content)
        => new(userId, NormalizeTitle(title), content);

    public void Update(string title, string content)
    {
        Title = NormalizeTitle(title);
        Content = content;
    }

    private static string NormalizeTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new DomainException("El título de la nota es obligatorio.");

        return title.Trim();
    }
}
