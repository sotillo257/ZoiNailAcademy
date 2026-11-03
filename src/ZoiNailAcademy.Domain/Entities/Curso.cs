using System.Text.RegularExpressions;
using ZoiNailAcademy.Domain.Common;
using ZoiNailAcademy.Domain.Common.Exceptions;

namespace ZoiNailAcademy.Domain.Entities;

/// <summary>Course of the catalogue, maintained by the developer (SuperAdmin).</summary>
public partial class Curso : BaseAuditableEntity
{
    public const int NombreMaxLength = 120;
    public const int SlugMaxLength = 120;
    public const int DescripcionCortaMaxLength = 300;
    public const int ImagenMaxLength = 300;
    public const int UbicacionMaxLength = 300;
    public const int DuracionTextoMaxLength = 100;
    public const string SlugPattern = "^[a-z0-9]+(-[a-z0-9]+)*$";

    public string Nombre { get; private set; } = null!;
    public string Slug { get; private set; } = null!;
    public string DescripcionCorta { get; private set; } = null!;
    public string Descripcion { get; private set; } = null!;
    public decimal Precio { get; private set; }
    public string Imagen { get; private set; } = null!;
    public string? Ubicacion { get; private set; }
    public string? DuracionTexto { get; private set; }
    public bool Activo { get; private set; }
    public int Orden { get; private set; }

    [GeneratedRegex(SlugPattern)]
    private static partial Regex SlugRegex();

    private Curso()
    {
    }

    public static Curso Create(
        string nombre, string slug, string descripcionCorta, string descripcion, decimal precio, string imagen,
        string? ubicacion, string? duracionTexto, bool activo, int orden)
    {
        var curso = new Curso { Id = Guid.NewGuid() };
        curso.Apply(nombre, slug, descripcionCorta, descripcion, precio, imagen, ubicacion, duracionTexto, orden);
        curso.Activo = activo;
        return curso;
    }

    public static bool EsSlugValido(string? slug) => slug is not null && SlugRegex().IsMatch(slug);

    /// <param name="tieneEdicionesFuturasProgramadas">Whether the course has future editions in <c>Programada</c>.</param>
    /// <returns>True when the slug changed (old URLs stop working).</returns>
    public bool Actualizar(
        string nombre, string slug, string descripcionCorta, string descripcion, decimal precio, string imagen,
        string? ubicacion, string? duracionTexto, bool activo, int orden, bool tieneEdicionesFuturasProgramadas)
    {
        var slugAnterior = Slug;
        if (Activo && !activo)
        {
            EnsurePuedeDesactivarse(tieneEdicionesFuturasProgramadas);
        }

        Apply(nombre, slug, descripcionCorta, descripcion, precio, imagen, ubicacion, duracionTexto, orden);
        Activo = activo;
        return !string.Equals(slugAnterior, Slug, StringComparison.Ordinal);
    }

    /// <summary>Courses are never deleted: they are deactivated and disappear from the web.</summary>
    public void Desactivar(bool tieneEdicionesFuturasProgramadas)
    {
        if (!Activo)
        {
            return;
        }

        EnsurePuedeDesactivarse(tieneEdicionesFuturasProgramadas);
        Activo = false;
    }

    private static void EnsurePuedeDesactivarse(bool tieneEdicionesFuturasProgramadas)
    {
        if (tieneEdicionesFuturasProgramadas)
        {
            throw new BusinessRuleException(
                ErrorCodes.CursoConEdiciones,
                "El curso tiene ediciones futuras programadas: cancélalas u ocúltalas antes de desactivarlo.");
        }
    }

    private void Apply(
        string nombre, string slug, string descripcionCorta, string descripcion, decimal precio, string imagen,
        string? ubicacion, string? duracionTexto, int orden)
    {
        Nombre = Required(nombre, "El nombre del curso es obligatorio.");
        DescripcionCorta = Required(descripcionCorta, "La descripción corta del curso es obligatoria.");
        Imagen = Required(imagen, "La imagen del curso es obligatoria.");

        var slugNormalizado = slug?.Trim() ?? string.Empty;
        if (!EsSlugValido(slugNormalizado))
        {
            throw new DomainException("El slug solo admite minúsculas, números y guiones (p. ej. 'acrilico-avanzado').");
        }

        if (precio <= 0)
        {
            throw new DomainException("El precio del curso debe ser mayor que cero.");
        }

        Slug = slugNormalizado;
        Descripcion = descripcion?.Trim() ?? string.Empty;
        Precio = precio;
        Ubicacion = Optional(ubicacion);
        DuracionTexto = Optional(duracionTexto);
        Orden = orden;
    }

    private static string Required(string? value, string message) =>
        string.IsNullOrWhiteSpace(value) ? throw new DomainException(message) : value.Trim();

    private static string? Optional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
