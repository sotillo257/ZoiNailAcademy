using System.Globalization;
using System.Text.Json.Serialization;
using ZoiNailAcademy.Domain.Entities;
using ZoiNailAcademy.Domain.Enums;

namespace ZoiNailAcademy.Application.Reservas;

/// <summary>Booking as the backoffice sees it (shape of F07).</summary>
/// <param name="PlazaDisponible">
/// Only in the pending listing and only for expired bookings: whether their edition has a free seat now, so the
/// booking could still be confirmed or extended. Omitted from the JSON when null.
/// </param>
public sealed record ReservaAdminDto(
    Guid Id, string Codigo, Guid EdicionId, string Curso, DateTimeOffset Inicio, MetodoPago Metodo, EstadoReserva Estado,
    string Nombre, string Email, string Telefono, decimal Importe, DateTimeOffset CreadaEn, DateTimeOffset? PagadaEn,
    DateTimeOffset ExpiraEn, bool RequiereRevision, string? NotaInterna, string? StripePaymentIntentId, string Version,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] bool? PlazaDisponible = null)
{
    /// <param name="r">Booking with <c>Edicion.Curso</c> loaded.</param>
    public static ReservaAdminDto From(Reserva r) => new(
        r.Id, r.Codigo, r.EdicionId, r.Edicion.Curso.Nombre, r.Edicion.Inicio, r.Metodo, r.Estado, r.Nombre, r.Email,
        r.Telefono, r.Importe, r.CreatedAt, r.PagadaEn, r.ExpiraEn, r.RequiereRevision, r.NotaInterna,
        r.StripePaymentIntentId, r.Version.ToString(CultureInfo.InvariantCulture));
}

/// <summary>Response of the backoffice booking actions.</summary>
public sealed record ReservaAccionDto(ReservaAdminDto Reserva, IReadOnlyList<string> Avisos);
