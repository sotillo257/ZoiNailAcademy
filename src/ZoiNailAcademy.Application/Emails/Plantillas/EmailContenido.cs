namespace ZoiNailAcademy.Application.Emails.Plantillas;

/// <summary>A rendered email: subject, HTML and plain-text versions.</summary>
public sealed record EmailContenido(string Asunto, string Html, string Texto);

/// <summary>Data of the academy shared by every template (layout, contact, links).</summary>
/// <param name="BaseUrl">Public website, without trailing slash.</param>
/// <param name="ContactoEmail">The client's email shown to buyers (<c>Email:ReplyTo</c>).</param>
/// <param name="WhatsAppTelefono">International format without <c>+</c>.</param>
public sealed record EmailMarca(
    string NombreAcademia, string? Direccion, string BaseUrl, TimeZoneInfo Zona, string? ContactoEmail,
    string? WhatsAppTelefono)
{
    public string Url(string ruta) => $"{BaseUrl}{ruta}";
}
