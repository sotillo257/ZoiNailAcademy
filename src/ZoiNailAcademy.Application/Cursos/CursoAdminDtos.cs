using ZoiNailAcademy.Domain.Entities;

namespace ZoiNailAcademy.Application.Cursos;

/// <summary>Editable fields of a course, shared by the create and update commands.</summary>
public interface ICursoCampos
{
    string Nombre { get; }
    string Slug { get; }
    string DescripcionCorta { get; }
    string Descripcion { get; }
    decimal Precio { get; }
    string Imagen { get; }
    string? Ubicacion { get; }
    string? DuracionTexto { get; }
    bool Activo { get; }
    int Orden { get; }
}

public sealed record CursoAdminDto(
    Guid Id, string Nombre, string Slug, string DescripcionCorta, string Descripcion, decimal Precio, string Imagen,
    string? Ubicacion, string? DuracionTexto, bool Activo, int Orden)
{
    public static CursoAdminDto From(Curso c) => new(
        c.Id, c.Nombre, c.Slug, c.DescripcionCorta, c.Descripcion, c.Precio, c.Imagen, c.Ubicacion, c.DuracionTexto,
        c.Activo, c.Orden);
}

public sealed record CursoOpcionDto(Guid Id, string Nombre, string Slug, bool Activo);
