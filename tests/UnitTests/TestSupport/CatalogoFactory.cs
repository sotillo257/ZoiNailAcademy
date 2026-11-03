using ZoiNailAcademy.Domain.Entities;
using ZoiNailAcademy.Domain.Enums;

namespace UnitTests.TestSupport;

/// <summary>Builders of valid courses and editions for unit tests.</summary>
public static class CatalogoFactory
{
    public static readonly DateTimeOffset Now = new(2026, 11, 3, 10, 0, 0, TimeSpan.Zero);

    public static Curso Curso(string slug = "acrilico", bool activo = true, int orden = 1, decimal precio = 149m) =>
        ZoiNailAcademy.Domain.Entities.Curso.Create(
            "Acrílico", slug, "Curso de uñas acrílicas", "# Temario", precio, "/cursos/acrilico.jpg",
            "Madrid", "2 días", activo, orden);

    public static Edicion Edicion(
        Curso? curso = null, DateTimeOffset? inicio = null, int plazas = 10,
        EstadoEdicion estado = EstadoEdicion.Programada, string? notas = null) =>
        ZoiNailAcademy.Domain.Entities.Edicion.Create(
            curso ?? Curso(), inicio ?? Now.AddDays(30), plazas, estado, notas, Now);
}
