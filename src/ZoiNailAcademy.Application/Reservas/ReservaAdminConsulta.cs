using System.Globalization;
using System.Linq.Expressions;
using ZoiNailAcademy.Domain.Entities;
using ZoiNailAcademy.Domain.Enums;
using ZoiNailAcademy.Domain.Services;

namespace ZoiNailAcademy.Application.Reservas;

/// <summary>
/// A booking row projected straight from SQL (one query with the edition and course joined, no N+1). It keeps
/// the <c>xmin</c> as a number and the edition seats; <see cref="ToDto"/> builds the response in memory.
/// </summary>
public sealed record ReservaAdminFila(
    Guid Id, string Codigo, Guid EdicionId, string Curso, DateTimeOffset Inicio, int Plazas, MetodoPago Metodo,
    EstadoReserva Estado, string Nombre, string Email, string Telefono, decimal Importe, decimal PrecioCurso,
    DateTimeOffset CreadaEn, DateTimeOffset? PagadaEn, DateTimeOffset ExpiraEn, DateTimeOffset? ExpiradaEn, bool RequiereRevision,
    string? NotaInterna, string? StripePaymentIntentId, uint Version)
{
    public ReservaAdminDto ToDto(bool? plazaDisponible = null) => new(
        Id, Codigo, EdicionId, Curso, Inicio, Metodo, Estado, Nombre, Email, Telefono, Importe, PrecioCurso, CreadaEn,
        PagadaEn,
        ExpiraEn, RequiereRevision, NotaInterna, StripePaymentIntentId, Version.ToString(CultureInfo.InvariantCulture),
        plazaDisponible);
}

/// <summary>Projection and search shared by the backoffice booking queries.</summary>
public static class ReservaAdminConsulta
{
    public static readonly Expression<Func<Reserva, ReservaAdminFila>> Proyeccion = r => new ReservaAdminFila(
        r.Id, r.Codigo, r.EdicionId, r.Edicion.Curso.Nombre, r.Edicion.Inicio, r.Edicion.Plazas, r.Metodo, r.Estado,
        r.Nombre, r.Email, r.Telefono, r.Importe, r.PrecioCurso, r.CreatedAt, r.PagadaEn, r.ExpiraEn, r.ExpiradaEn, r.RequiereRevision,
        r.NotaInterna, r.StripePaymentIntentId, r.Version);

    /// <summary>
    /// <paramref name="q"/> that looks like a code finds that code (however it is typed); any text is also searched
    /// in name, email and phone without case or accents, and a phone also by its digits (<c>612345678</c> finds
    /// <c>+34612345678</c>). A 6-letter name like <c>Carmen</c> also "looks like" a code, hence the OR.
    /// </summary>
    public static IQueryable<Reserva> Buscar(IQueryable<Reserva> query, string? q)
    {
        var texto = TextoBusqueda.Normalizar(q);
        if (texto.Length == 0)
        {
            return query;
        }

        var esCodigo = CodigoReserva.TryNormalizar(q, out var codigo);
        var digitos = TextoBusqueda.DigitosTelefono(texto);
        return (esCodigo, digitos) switch
        {
            (true, not null) => query.Where(r => r.Codigo == codigo || r.Busqueda.Contains(texto) || r.Telefono.Contains(digitos)),
            (true, null) => query.Where(r => r.Codigo == codigo || r.Busqueda.Contains(texto)),
            (false, not null) => query.Where(r => r.Busqueda.Contains(texto) || r.Telefono.Contains(digitos)),
            _ => query.Where(r => r.Busqueda.Contains(texto))
        };
    }
}
