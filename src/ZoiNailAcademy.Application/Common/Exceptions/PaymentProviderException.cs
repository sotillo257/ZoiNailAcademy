namespace ZoiNailAcademy.Application.Common.Exceptions;

/// <summary>The payment provider (Stripe) failed (HTTP 502). The message is never shown to the buyer.</summary>
public class PaymentProviderException(string message, Exception? innerException = null) : Exception(message, innerException)
{
    public const string MensajePublico = "No se pudo iniciar el pago, inténtalo de nuevo";
}
