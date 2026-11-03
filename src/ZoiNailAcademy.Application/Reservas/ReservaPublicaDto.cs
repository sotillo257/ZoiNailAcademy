using ZoiNailAcademy.Domain.Enums;

namespace ZoiNailAcademy.Application.Reservas;

/// <summary>Public state of a booking (no personal data). Payment details only for a pending manual booking.</summary>
public sealed record ReservaPublicaDto(
    string Codigo, EstadoReserva Estado, MetodoPago Metodo, string Curso, DateTimeOffset Inicio, decimal Importe,
    DateTimeOffset? ExpiraEn, DatosPagoDto? DatosPago, string? WhatsappUrl);

public sealed record DatosPagoDto(string Titular, string Iban, string BizumTelefono, string Concepto);
