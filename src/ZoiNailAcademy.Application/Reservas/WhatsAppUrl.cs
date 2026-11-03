using System.Globalization;

namespace ZoiNailAcademy.Application.Reservas;

/// <summary>Link that opens WhatsApp with the receipt message already written.</summary>
public static class WhatsAppUrl
{
    private static readonly CultureInfo Es = CultureInfo.GetCultureInfo("es-ES");

    /// <param name="telefono">International format without <c>+</c> (<c>Pago:WhatsAppTelefono</c>).</param>
    /// <param name="inicioLocal">Start of the edition already converted to the display time zone.</param>
    public static string Construir(string telefono, string codigo, string curso, DateTimeOffset inicioLocal, decimal importe)
    {
        var mensaje =
            $"Hola, envío el comprobante de la reserva {codigo} ({curso}, {inicioLocal.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture)}). " +
            $"Importe: {importe.ToString("N2", Es)} €.";
        return $"https://wa.me/{telefono}?text={Uri.EscapeDataString(mensaje)}";
    }
}
