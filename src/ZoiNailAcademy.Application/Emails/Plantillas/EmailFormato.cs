using System.Globalization;

namespace ZoiNailAcademy.Application.Emails.Plantillas;

/// <summary>Spanish formats of the emails: dates in the academy's time zone and amounts in euros.</summary>
public static class EmailFormato
{
    public static readonly CultureInfo Es = CultureInfo.GetCultureInfo("es-ES");

    /// <summary><c>sábado, 5 de diciembre de 2026, 10:00</c>.</summary>
    public static string FechaLarga(DateTimeOffset fecha, TimeZoneInfo zona) =>
        TimeZoneInfo.ConvertTime(fecha, zona).ToString("dddd, d 'de' MMMM 'de' yyyy, HH:mm", Es);

    /// <summary><c>05/12/2026</c>.</summary>
    public static string FechaCorta(DateTimeOffset fecha, TimeZoneInfo zona) =>
        TimeZoneInfo.ConvertTime(fecha, zona).ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);

    /// <summary><c>149,00 €</c>.</summary>
    public static string Importe(decimal importe) => $"{importe.ToString("N2", Es)} €";
}
