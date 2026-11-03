namespace ZoiNailAcademy.Application.Emails.Plantillas;

/// <summary>
/// Common layout: logo, brand colour and footer with the academy's name, address and legal links. Old-school
/// email HTML (tables, inline styles, 600 px max) so Gmail, Outlook and phones render it the same.
/// Colours and the logo path are provisional until the client approves the design.
/// </summary>
public static class EmailLayout
{
    public const string ColorMarca = "#b5838d";
    public const string ColorTexto = "#2d2a32";
    public const string ColorSecundario = "#6d6875";
    public const string ColorFondo = "#f6f1f1";
    public const string ColorFondoSuave = "#fbf6f6";

    /// <summary>Logo served by the front (absolute URL built from <c>App:BaseUrl</c>).</summary>
    public const string RutaLogo = "/email/logo.png";

    public static EmailContenido Componer(EmailMarca marca, string asunto, EmailCuerpo cuerpo)
    {
        var nombre = EmailCuerpo.Encode(marca.NombreAcademia);
        var direccion = string.IsNullOrWhiteSpace(marca.Direccion)
            ? string.Empty
            : $"<br>{EmailCuerpo.Encode(marca.Direccion)}";
        var legales =
            $"<a href=\"{EmailCuerpo.Encode(marca.Url("/aviso-legal"))}\" style=\"color:{ColorSecundario};\">Aviso legal</a> · " +
            $"<a href=\"{EmailCuerpo.Encode(marca.Url("/privacidad"))}\" style=\"color:{ColorSecundario};\">Privacidad</a>";

        var html =
            "<!DOCTYPE html><html lang=\"es\"><head><meta charset=\"utf-8\">" +
            "<meta name=\"viewport\" content=\"width=device-width,initial-scale=1\">" +
            $"<title>{EmailCuerpo.Encode(asunto)}</title></head>" +
            $"<body style=\"margin:0;padding:0;background:{ColorFondo};\">" +
            $"<table role=\"presentation\" width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" style=\"background:{ColorFondo};\"><tr><td align=\"center\" style=\"padding:24px 12px;\">" +
            "<table role=\"presentation\" width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" style=\"max-width:600px;background:#ffffff;border-radius:8px;\">" +
            $"<tr><td align=\"center\" style=\"padding:24px 24px 8px;border-bottom:3px solid {ColorMarca};\">" +
            $"<a href=\"{EmailCuerpo.Encode(marca.BaseUrl)}\"><img src=\"{EmailCuerpo.Encode(marca.Url(RutaLogo))}\" alt=\"{nombre}\" width=\"160\" style=\"display:block;border:0;max-width:160px;height:auto;\"></a>" +
            "</td></tr>" +
            $"<tr><td style=\"padding:24px;font-family:Arial,Helvetica,sans-serif;color:{ColorTexto};\">{cuerpo.Html}</td></tr>" +
            $"<tr><td align=\"center\" style=\"padding:16px 24px 24px;font-family:Arial,Helvetica,sans-serif;font-size:12px;line-height:1.5;color:{ColorSecundario};border-top:1px solid #eeeeee;\">" +
            $"{nombre}{direccion}<br>{legales}</td></tr>" +
            "</table></td></tr></table></body></html>";

        var pie = string.IsNullOrWhiteSpace(marca.Direccion)
            ? marca.NombreAcademia
            : $"{marca.NombreAcademia}\n{marca.Direccion}";
        var texto =
            $"{cuerpo.Texto}\n--\n{pie}\nAviso legal: {marca.Url("/aviso-legal")}\nPrivacidad: {marca.Url("/privacidad")}\n";

        return new EmailContenido(asunto, html, texto);
    }
}
