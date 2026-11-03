using MediatR;
using Microsoft.Extensions.Logging;
using ZoiNailAcademy.Application.Common;
using ZoiNailAcademy.Application.Common.Contacto;
using ZoiNailAcademy.Application.Common.Interfaces;
using ZoiNailAcademy.Application.Emails;
using ZoiNailAcademy.Application.Emails.Plantillas;

namespace ZoiNailAcademy.Application.Contacto.EnviarMensajeContacto;

/// <summary>Enqueues the message for the client with Reply-To = the sender (and the optional acknowledgement).</summary>
public class EnviarMensajeContactoHandler(
    BandejaSalida bandeja, IEmailSettings emailSettings, ILogger<EnviarMensajeContactoHandler> logger)
    : IRequestHandler<EnviarMensajeContactoCommand, Result>
{
    public async Task<Result> Handle(EnviarMensajeContactoCommand request, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrEmpty(request.Website))
        {
            logger.LogWarning("Honeypot relleno en el formulario de contacto; se ignora la petición.");
            return Result.Success();
        }

        var email = TelefonoNormalizer.NormalizarEmail(request.Email);
        var telefono = TelefonoNormalizer.TryNormalizar(request.Telefono, out var e164) ? e164 : null;
        var modelo = new ContactoEmailModelo(request.Nombre.Trim(), email, telefono, request.Mensaje.Trim());
        var marca = bandeja.Marca();

        await bandeja.EncolarParaClientaAsync(
            TiposEmail.ContactoClienta, ContactoClientaEmail.Crear(modelo, marca), null, cancellationToken, replyTo: email);

        if (emailSettings.AcuseContacto)
        {
            await bandeja.EncolarParaClienteAsync(
                TiposEmail.ContactoAcuse, email, ContactoAcuseEmail.Crear(modelo, marca), null, cancellationToken);
        }

        return Result.Success();
    }
}
