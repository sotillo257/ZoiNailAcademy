using ZoiNailAcademy.Application.Ediciones;

namespace ZoiNailAcademy.Application.Cursos;

public sealed record CursoResumenDto(
    Guid Id, string Slug, string Nombre, string DescripcionCorta, decimal Precio, string Imagen, int Orden,
    EdicionPublicaDto? ProximaEdicion);

public sealed record CursoDetalleDto(
    Guid Id, string Slug, string Nombre, string DescripcionCorta, decimal Precio, string Imagen, int Orden,
    EdicionPublicaDto? ProximaEdicion, string Descripcion, IReadOnlyList<EdicionPublicaDto> Ediciones);

public static class ProximaEdicion
{
    /// <summary>First visible edition with free seats; otherwise the first visible one; otherwise null.</summary>
    public static EdicionPublicaDto? De(IReadOnlyList<EdicionPublicaDto> visiblesOrdenadas) =>
        visiblesOrdenadas.FirstOrDefault(e => e.PlazasLibres > 0) ?? visiblesOrdenadas.FirstOrDefault();
}
