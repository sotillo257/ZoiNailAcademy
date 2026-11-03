using PhoneNumbers;

namespace ZoiNailAcademy.Application.Common.Contacto;

/// <summary>Normalizes phones to E.164 (<c>+34612345678</c>); numbers without prefix are Spanish.</summary>
public static class TelefonoNormalizer
{
    public const string RegionPorDefecto = "ES";

    private static readonly PhoneNumberUtil Util = PhoneNumberUtil.GetInstance();

    public static bool TryNormalizar(string? telefono, out string e164)
    {
        e164 = string.Empty;
        if (string.IsNullOrWhiteSpace(telefono))
        {
            return false;
        }

        try
        {
            var numero = Util.Parse(telefono.Trim(), RegionPorDefecto);
            if (!Util.IsValidNumber(numero))
            {
                return false;
            }

            e164 = Util.Format(numero, PhoneNumberFormat.E164);
            return true;
        }
        catch (NumberParseException)
        {
            return false;
        }
    }

    public static bool EsValido(string? telefono) => TryNormalizar(telefono, out _);

    public static string NormalizarEmail(string email) => email.Trim().ToLowerInvariant();
}
