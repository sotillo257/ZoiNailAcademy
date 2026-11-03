using Microsoft.Extensions.Configuration;

namespace ZoiNailAcademy.Infrastructure.Email;

/// <summary>
/// Which <c>IEmailSender</c> the API uses. Resend needs <c>Resend:ApiKey</c> and <c>Email:From</c>; without them the
/// API still starts and every email is written to the log instead of sent, so no action fails while the Resend
/// account does not exist yet. <see cref="Aviso"/> explains the fallback and is logged at startup.
/// </summary>
public sealed record EmailProveedorSeleccion(string Proveedor, string? Aviso)
{
    public static EmailProveedorSeleccion Desde(IConfiguration configuration)
    {
        var email = configuration.GetSection(EmailOptions.SectionName).Get<EmailOptions>() ?? new EmailOptions();
        var resend = configuration.GetSection(ResendOptions.SectionName).Get<ResendOptions>() ?? new ResendOptions();

        if (string.Equals(email.Provider, EmailProviders.Log, StringComparison.OrdinalIgnoreCase))
        {
            return new(EmailProviders.Log, null);
        }

        if (!string.Equals(email.Provider, EmailProviders.Resend, StringComparison.OrdinalIgnoreCase))
        {
            return new(EmailProviders.Log,
                $"Email:Provider '{email.Provider}' no es válido (Resend o Log): los emails se escriben en el log y no se envían.");
        }

        var faltan = new List<string>();
        if (string.IsNullOrWhiteSpace(resend.ApiKey))
        {
            faltan.Add("Resend__ApiKey");
        }

        if (string.IsNullOrWhiteSpace(email.From))
        {
            faltan.Add("Email__From");
        }

        return faltan.Count == 0
            ? new(EmailProviders.Resend, null)
            : new(EmailProviders.Log,
                $"Resend no está configurado (falta {string.Join(" y ", faltan)}): los emails se escriben en el log y no se envían.");
    }
}
