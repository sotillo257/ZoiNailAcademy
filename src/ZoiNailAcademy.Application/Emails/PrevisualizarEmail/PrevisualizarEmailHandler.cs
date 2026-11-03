using MediatR;
using ZoiNailAcademy.Application.Common;
using ZoiNailAcademy.Application.Common.Exceptions;
using ZoiNailAcademy.Application.Emails.Plantillas;
using ZoiNailAcademy.Application.Reservas;
using ZoiNailAcademy.Domain.Enums;

namespace ZoiNailAcademy.Application.Emails.PrevisualizarEmail;

/// <summary>Renders a template with sample data and the real academy settings, to review texts with the client.</summary>
public class PrevisualizarEmailHandler(BandejaSalida bandeja, ReservaPublicaDtoFactory dtoFactory, TimeProvider timeProvider)
    : IRequestHandler<PrevisualizarEmailQuery, Result<EmailContenido>>
{
    public Task<Result<EmailContenido>> Handle(PrevisualizarEmailQuery request, CancellationToken cancellationToken)
    {
        var marca = bandeja.Marca();
        var now = timeProvider.GetUtcNow();
        var reserva = new ReservaEmailModelo(
            Guid.NewGuid(), Guid.NewGuid(), "CUR-7KQ4XM", "Curso de uñas acrílicas", "acrilico", new DateTimeOffset(now.AddDays(30).Date, TimeSpan.Zero).AddHours(9),
            "Calle Ejemplo 1, Madrid", 149m, MetodoPago.Stripe, "Lucía Pérez", "lucia@example.com", "+34612345678",
            now.AddHours(48), "pi_3Example123");
        var manual = reserva with { Metodo = MetodoPago.Manual };
        var datos = dtoFactory.DatosPago(manual.Codigo);
        var pago = new DatosPagoEmail(
            datos.Titular, datos.Iban, datos.BizumTelefono,
            dtoFactory.WhatsApp(manual.Codigo, manual.Curso, manual.Inicio, manual.Importe));
        var contacto = new ContactoEmailModelo(
            "Lucía Pérez", "lucia@example.com", "+34612345678", "Hola, ¿el curso incluye el material?\nGracias.");

        var fechas = new List<FechaAvisoModelo> { new(reserva.Inicio, 8), new(reserva.Inicio.AddDays(14), 2) };

        var contenido = request.Tipo switch
        {
            TiposEmail.ReservaPagadaComprador => ReservaPagadaCompradorEmail.Crear(reserva, marca),
            TiposEmail.ReservaPagadaClienta => ReservaPagadaClientaEmail.Crear(reserva, 3, marca),
            TiposEmail.ReservaManualComprador => ReservaManualCompradorEmail.Crear(manual, pago, marca),
            TiposEmail.ReservaManualClienta => ReservaManualClientaEmail.Crear(manual, marca),
            TiposEmail.ReservaExpiradaComprador => ReservaExpiradaCompradorEmail.Crear(manual, marca),
            TiposEmail.PagoSinPlazaClienta => PagoSinPlazaClientaEmail.Crear(reserva, marca),
            TiposEmail.ListaEsperaClienta => ListaEsperaClientaEmail.Crear(
                new ListaEsperaEmailModelo(reserva.Curso, "Lucía Pérez", "lucia@example.com", "+34612345678", "Sábados de marzo", reserva.Inicio),
                marca),
            TiposEmail.ContactoClienta => ContactoClientaEmail.Crear(contacto, marca),
            TiposEmail.ContactoAcuse => ContactoAcuseEmail.Crear(contacto, marca),
            TiposEmail.AvisoNuevasEdiciones => AvisoNuevasEdicionesEmail.Crear(
                new AvisoNuevasEdicionesModelo(reserva.Curso, reserva.CursoSlug, "Lucía Pérez", fechas, "token-de-ejemplo"), marca),
            TiposEmail.AvisoNuevasEdicionesClienta => AvisoNuevasEdicionesClientaEmail.Crear(
                new AvisoNuevasEdicionesClientaModelo(reserva.Curso, 12, fechas), marca),
            _ => throw new NotFoundException("Tipo de email", request.Tipo)
        };

        return Task.FromResult(Result.Success(contenido));
    }
}
