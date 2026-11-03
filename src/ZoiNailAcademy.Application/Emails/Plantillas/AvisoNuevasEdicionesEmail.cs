namespace ZoiNailAcademy.Application.Emails.Plantillas;

/// <summary>
/// New dates of a course for a person of its waiting list (F11). The footer links to the unsubscribe page and the
/// outbox sends <see cref="UrlBajaUnClic"/> as the <c>List-Unsubscribe</c> header (Gmail's native button).
/// </summary>
public static class AvisoNuevasEdicionesEmail
{
    public static EmailContenido Crear(AvisoNuevasEdicionesModelo m, EmailMarca marca)
    {
        var intro = m.Fechas.Count == 1
            ? $"Hola, {m.Nombre}. Hay una nueva fecha para {m.Curso}:"
            : $"Hola, {m.Nombre}. Hay nuevas fechas para {m.Curso}:";

        var cuerpo = new EmailCuerpo()
            .Titulo($"Nuevas fechas para {m.Curso}")
            .Parrafo(intro)
            .Datos(m.Fechas
                .OrderBy(f => f.Inicio)
                .Select(f => (EmailFormato.FechaLarga(f.Inicio, marca.Zona), (string?)Plazas(f.PlazasLibres)))
                .ToArray())
            .Parrafo("Las plazas libres son las que quedaban al enviarte este email y pueden agotarse.")
            .Boton("Reservar mi plaza", marca.Url($"/reservar/{m.CursoSlug}"))
            .Parrafo(
                $"Recibes este email porque te apuntaste a la lista de espera de {m.Curso}. " +
                "Si ya no quieres estos avisos, puedes darte de baja con el enlace del final.");

        return EmailLayout.Componer(
            marca, $"Nuevas fechas para {m.Curso}", cuerpo,
            new EnlaceBaja($"Darme de baja de los avisos de {m.Curso}", UrlPaginaBaja(marca, m.BajaToken, m.CursoSlug)));
    }

    /// <summary>
    /// Unsubscribe page of the front (<c>/baja</c>), which asks for confirmation before calling the API (a plain
    /// <c>GET</c> never unsubscribes: mail scanners open links). <c>curso</c> lets the page name the course.
    /// </summary>
    public static string UrlPaginaBaja(EmailMarca marca, string bajaToken, string cursoSlug) =>
        marca.Url($"/baja?token={Uri.EscapeDataString(bajaToken)}&curso={Uri.EscapeDataString(cursoSlug)}");

    /// <summary>
    /// One-click unsubscribe (RFC 8058): the mail client <c>POST</c>s <c>List-Unsubscribe=One-Click</c> here without
    /// JavaScript. Goes through the front's <c>/api</c> rewrite like every other API call.
    /// </summary>
    public static string UrlBajaUnClic(EmailMarca marca, string bajaToken) =>
        marca.Url($"/api/lista-espera/baja?token={Uri.EscapeDataString(bajaToken)}");

    private static string Plazas(int libres) => libres == 1 ? "Queda 1 plaza" : $"Quedan {libres} plazas";
}
