using ZoiNailAcademy.Application.Common.Interfaces;
using ZoiNailAcademy.Domain.Entities;
using ZoiNailAcademy.Domain.Enums;

namespace ZoiNailAcademy.Application.Reservas;

public class ReservaPublicaDtoFactory(IPagoSettings pago, IAppSettings appSettings)
{
    /// <param name="reserva">Booking with <c>Edicion.Curso</c> loaded.</param>
    public ReservaPublicaDto Crear(Reserva reserva)
    {
        var pendiente = reserva.Estado == EstadoReserva.Pendiente;
        var manualPendiente = pendiente && reserva.Metodo == MetodoPago.Manual;
        var curso = reserva.Edicion.Curso.Nombre;

        return new ReservaPublicaDto(
            reserva.Codigo, reserva.Estado, reserva.Metodo, curso, reserva.Edicion.Inicio, reserva.Importe,
            reserva.PrecioCurso, pendiente ? reserva.ExpiraEn : null,
            manualPendiente ? DatosPago(reserva.Codigo) : null,
            manualPendiente ? WhatsApp(reserva.Codigo, curso, reserva.Edicion.Inicio, reserva.Importe) : null);
    }

    public DatosPagoDto DatosPago(string codigo) => new(pago.Titular, pago.Iban, pago.BizumTelefono, codigo);

    public string WhatsApp(string codigo, string curso, DateTimeOffset inicio, decimal importe) =>
        WhatsAppUrl.Construir(pago.WhatsAppTelefono, codigo, curso, TimeZoneInfo.ConvertTime(inicio, appSettings.TimeZone), importe);
}
