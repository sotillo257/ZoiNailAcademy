using ZoiNailAcademy.Domain.Entities;
using ZoiNailAcademy.Domain.Enums;
using ZoiNailAcademy.Domain.Services;

namespace ZoiNailAcademy.Application.Ediciones;

public sealed record EdicionAdminDto(
    Guid Id, Guid CursoId, string Curso, DateTimeOffset Inicio, int Plazas, int Pagadas, int Pendientes, int Libres,
    EstadoEdicion Estado, bool Reservable, string? Notas, string Version)
{
    /// <param name="edicion">Edition with its <see cref="Edicion.Curso"/> loaded.</param>
    public static EdicionAdminDto From(Edicion edicion, Ocupacion ocupacion, DateTimeOffset now, TimeSpan cierre) => new(
        edicion.Id, edicion.CursoId, edicion.Curso.Nombre, edicion.Inicio, edicion.Plazas, ocupacion.Pagadas,
        ocupacion.Pendientes, PlazasCalculator.Libres(edicion.Plazas, ocupacion.Total), edicion.Estado,
        edicion.EsReservable(edicion.Curso.Activo, now, cierre), edicion.Notas,
        edicion.Version.ToString(System.Globalization.CultureInfo.InvariantCulture));
}
