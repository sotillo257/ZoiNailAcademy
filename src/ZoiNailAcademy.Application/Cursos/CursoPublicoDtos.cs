using ZoiNailAcademy.Application.Ediciones;

namespace ZoiNailAcademy.Application.Cursos;

/// <param name="PrecioReserva">Paid online when booking; the rest (<c>Precio - PrecioReserva</c>) in person.</param>
public sealed record CursoResumenDto(
    Guid Id, string Slug, string Nombre, string DescripcionCorta, decimal Precio, decimal PrecioReserva, string Imagen,
    int Orden, string? Ubicacion, string? DuracionTexto, EdicionPublicaDto? ProximaEdicion,
    IReadOnlyList<EdicionPublicaDto> Ediciones);

public sealed record CursoDetalleDto(
    Guid Id, string Slug, string Nombre, string DescripcionCorta, decimal Precio, decimal PrecioReserva, string Imagen,
    int Orden, string? Ubicacion, string? DuracionTexto, EdicionPublicaDto? ProximaEdicion, string Descripcion,
    IReadOnlyList<EdicionPublicaDto> Ediciones);

public static class ProximaEdicion
{
    /// <summary>First visible edition with free seats; otherwise the first visible one; otherwise null.</summary>
    public static EdicionPublicaDto? De(IReadOnlyList<EdicionPublicaDto> visiblesOrdenadas) =>
        visiblesOrdenadas.FirstOrDefault(e => e.PlazasLibres > 0) ?? visiblesOrdenadas.FirstOrDefault();
}
