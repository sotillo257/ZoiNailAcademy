using ZoiNailAcademy.Domain.Entities;
using ZoiNailAcademy.Domain.Enums;

namespace ZoiNailAcademy.Application.Reservas;

/// <summary>Public state of a booking (no personal data). <c>DatosPago</c>/<c>WhatsappUrl</c> arrive in F04.</summary>
public sealed record ReservaPublicaDto(
    string Codigo, EstadoReserva Estado, MetodoPago Metodo, string Curso, DateTimeOffset Inicio, decimal Importe,
    DateTimeOffset? ExpiraEn, DatosPagoDto? DatosPago, string? WhatsappUrl)
{
    /// <param name="reserva">Booking with <c>Edicion.Curso</c> loaded.</param>
    public static ReservaPublicaDto From(Reserva reserva) => new(
        reserva.Codigo, reserva.Estado, reserva.Metodo, reserva.Edicion.Curso.Nombre, reserva.Edicion.Inicio,
        reserva.Importe, reserva.Estado == EstadoReserva.Pendiente ? reserva.ExpiraEn : null, null, null);
}

public sealed record DatosPagoDto(string Titular, string Iban, string BizumTelefono, string Concepto);
