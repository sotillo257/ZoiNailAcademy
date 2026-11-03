namespace ZoiNailAcademy.Application.Cursos.EditarCurso;

/// <summary>Body of <c>PUT /api/admin/cursos/{id}</c>.</summary>
public sealed record EditarCursoRequest(
    string Nombre, string Slug, string DescripcionCorta, string Descripcion, decimal Precio, string Imagen,
    string? Ubicacion, string? DuracionTexto, bool Activo, int Orden)
{
    public EditarCursoCommand ToCommand(Guid id) => new(
        id, Nombre, Slug, DescripcionCorta, Descripcion, Precio, Imagen, Ubicacion, DuracionTexto, Activo, Orden);
}
