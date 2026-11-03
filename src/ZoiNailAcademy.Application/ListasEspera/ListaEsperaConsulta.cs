using Microsoft.EntityFrameworkCore;
using ZoiNailAcademy.Domain.Entities;
using ZoiNailAcademy.Domain.Services;

namespace ZoiNailAcademy.Application.ListasEspera;

/// <summary>Filters and order shared by the backoffice listing and the CSV export.</summary>
public static class ListaEsperaConsulta
{
    private static readonly char[] SeparadoresTelefono = [' ', '-', '.', '(', ')', '+'];

    public static IQueryable<ListaEspera> Filtrar(
        IQueryable<ListaEspera> query, Guid? cursoId, bool? contactado = null, string? q = null)
    {
        query = query.AsNoTracking().Include(e => e.Curso).Include(e => e.EdicionPreferida).AsQueryable();

        if (cursoId is { } id)
        {
            query = query.Where(e => e.CursoId == id);
        }

        if (contactado is { } c)
        {
            query = query.Where(e => e.Contactado == c);
        }

        var texto = TextoBusqueda.Normalizar(q);
        if (texto.Length > 0)
        {
            // A phone may be typed with spaces or dashes ("612 34 56"): compare its digits with the stored E.164.
            var digitos = string.Concat(texto.Split(SeparadoresTelefono, StringSplitOptions.RemoveEmptyEntries));
            query = digitos.Length > 0 && digitos.All(char.IsAsciiDigit)
                ? query.Where(e => e.Busqueda.Contains(texto) || e.Telefono.Contains(digitos))
                : query.Where(e => e.Busqueda.Contains(texto));
        }

        return query;
    }

    /// <summary>Not contacted first, then the ones who have waited longest.</summary>
    public static IOrderedQueryable<ListaEspera> Ordenar(IQueryable<ListaEspera> query) =>
        query.OrderBy(e => e.Contactado).ThenBy(e => e.CreatedAt).ThenBy(e => e.Id);
}
