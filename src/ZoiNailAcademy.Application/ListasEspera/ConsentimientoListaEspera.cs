namespace ZoiNailAcademy.Application.ListasEspera;

/// <summary>
/// Versioned consent texts of the waiting list form. The backend stores its own constant (never the text the
/// front sends) as the GDPR proof. It also covers the email about new dates, so F11 needs no new consent.
/// A wording change adds a new constant and points <see cref="Vigente"/> to it; old entries keep their text.
/// </summary>
public static class ConsentimientoListaEspera
{
    public const string V1 =
        "Acepto la política de privacidad y que me contactéis por teléfono, WhatsApp o email para avisarme " +
        "cuando haya nuevas fechas de este curso.";

    public const string Vigente = V1;
}
