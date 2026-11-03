using Microsoft.Extensions.Options;
using ZoiNailAcademy.Application.Common.Interfaces;

namespace ZoiNailAcademy.Infrastructure.Settings;

public class PagoSettings(IOptions<PagoOptions> options) : IPagoSettings
{
    public string Titular => options.Value.Titular;
    public string Iban => options.Value.Iban;
    public string BizumTelefono => options.Value.BizumTelefono;
    public string WhatsAppTelefono => options.Value.WhatsAppTelefono.TrimStart('+');
}
