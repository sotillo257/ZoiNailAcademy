namespace ZoiNailAcademy.Application.Common.Interfaces;

/// <summary>Bank transfer / Bizum details shown to the buyer (<c>Pago:*</c>), implemented in Infrastructure.</summary>
public interface IPagoSettings
{
    string Titular { get; }
    string Iban { get; }
    string BizumTelefono { get; }

    /// <summary>International format without <c>+</c>, e.g. <c>34612345678</c>.</summary>
    string WhatsAppTelefono { get; }
}
