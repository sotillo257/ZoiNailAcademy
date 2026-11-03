using ZoiNailAcademy.Domain.Entities;
using ZoiNailAcademy.Domain.Enums;
using ZoiNailAcademy.Domain.Services;

namespace ZoiNailAcademy.Application.Ediciones;

public sealed record EdicionPublicaDto(
    Guid Id, DateTimeOffset Inicio, int PlazasTotales, int PlazasLibres, EstadoEdicionPublico Estado)
{
    public static EdicionPublicaDto From(Edicion edicion, Ocupacion ocupacion, int umbralUltimasPlazas)
    {
        var libres = PlazasCalculator.Libres(edicion.Plazas, ocupacion.Total);
        return new EdicionPublicaDto(
            edicion.Id, edicion.Inicio, edicion.Plazas, libres, PlazasCalculator.EstadoPublico(libres, umbralUltimasPlazas));
    }
}
