using System.Globalization;
using System.Text;

namespace ZoiNailAcademy.Domain.Services;

/// <summary>
/// Text normalized for "contains" searches that ignore case and accents: <c>José Núñez</c> → <c>jose nunez</c>.
/// Entities store their searchable fields already normalized, so the query is a plain <c>LIKE</c>.
/// </summary>
public static class TextoBusqueda
{
    private static readonly char[] SeparadoresTelefono = [' ', '-', '.', '(', ')', '+'];

    /// <summary>
    /// The digits of a phone typed with spaces, dashes or prefix (<c>+34 612 34 56</c> → <c>346123456</c>), or
    /// null when the text is not a phone, so it can be matched against a stored E.164 number.
    /// </summary>
    public static string? DigitosTelefono(string? texto)
    {
        if (string.IsNullOrWhiteSpace(texto))
        {
            return null;
        }

        var digitos = string.Concat(texto.Split(SeparadoresTelefono, StringSplitOptions.RemoveEmptyEntries));
        return digitos.Length > 0 && digitos.All(char.IsAsciiDigit) ? digitos : null;
    }

    public static string Normalizar(string? texto)
    {
        if (string.IsNullOrWhiteSpace(texto))
        {
            return string.Empty;
        }

        var descompuesto = texto.Trim().Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(descompuesto.Length);
        foreach (var c in descompuesto)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
            {
                sb.Append(char.ToLowerInvariant(c));
            }
        }

        return sb.ToString().Normalize(NormalizationForm.FormC);
    }
}
