namespace ZoiNailAcademy.Infrastructure.Settings;

/// <summary>Section <c>Pago</c>: bank transfer and Bizum details shown to the buyer.</summary>
public class PagoOptions
{
    public const string SectionName = "Pago";

    public string Titular { get; set; } = string.Empty;
    public string Iban { get; set; } = string.Empty;
    public string BizumTelefono { get; set; } = string.Empty;

    /// <summary>International format without <c>+</c>, e.g. <c>34612345678</c>.</summary>
    public string WhatsAppTelefono { get; set; } = string.Empty;
}
