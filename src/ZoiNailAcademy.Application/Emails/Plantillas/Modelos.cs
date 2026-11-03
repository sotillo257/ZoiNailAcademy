using ZoiNailAcademy.Domain.Entities;
using ZoiNailAcademy.Domain.Enums;

namespace ZoiNailAcademy.Application.Emails.Plantillas;

/// <summary>Booking data the templates show.</summary>
public sealed record ReservaEmailModelo(
    Guid ReservaId, Guid EdicionId, string Codigo, string Curso, string CursoSlug, DateTimeOffset Inicio,
    string? Ubicacion, decimal Importe, MetodoPago Metodo, string Nombre, string Email, string Telefono,
    DateTimeOffset ExpiraEn, string? StripePaymentIntentId)
{
    /// <param name="r">Booking with <c>Edicion.Curso</c> loaded.</param>
    public static ReservaEmailModelo From(Reserva r) => new(
        r.Id, r.EdicionId, r.Codigo, r.Edicion.Curso.Nombre, r.Edicion.Curso.Slug, r.Edicion.Inicio,
        r.Edicion.Curso.Ubicacion, r.Importe, r.Metodo, r.Nombre, r.Email, r.Telefono, r.ExpiraEn, r.StripePaymentIntentId);

    public string MetodoTexto => Metodo == MetodoPago.Stripe ? "Tarjeta" : "Transferencia o Bizum";
}

public sealed record DatosPagoEmail(string Titular, string Iban, string BizumTelefono, string WhatsappUrl);

/// <param name="EdicionPreferida">Start of the preferred edition, if any.</param>
public sealed record ListaEsperaEmailModelo(
    string Curso, string Nombre, string Email, string Telefono, string? FechaPreferida, DateTimeOffset? EdicionPreferida);

public sealed record ContactoEmailModelo(string Nombre, string Email, string? Telefono, string Mensaje);

/// <summary>A date of a new-dates notice, with the free seats when the email was enqueued.</summary>
public sealed record FechaAvisoModelo(DateTimeOffset Inicio, int PlazasLibres);

/// <param name="BajaToken">The person's unsubscribe token for this course (<c>ListaEspera.BajaToken</c>).</param>
public sealed record AvisoNuevasEdicionesModelo(
    string Curso, string CursoSlug, string Nombre, IReadOnlyList<FechaAvisoModelo> Fechas, string BajaToken);

/// <param name="Interesados">People of the waiting list that were emailed.</param>
public sealed record AvisoNuevasEdicionesClientaModelo(string Curso, int Interesados, IReadOnlyList<FechaAvisoModelo> Fechas);
