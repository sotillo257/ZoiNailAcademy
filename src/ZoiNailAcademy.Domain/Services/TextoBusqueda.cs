using System.Globalization;
using System.Text;

namespace ZoiNailAcademy.Domain.Services;

/// <summary>
/// Text normalized for "contains" searches that ignore case and accents: <c>José Núñez</c> → <c>jose nunez</c>.
/// Entities store their searchable fields already normalized, so the query is a plain <c>LIKE</c>.
/// </summary>
public static class TextoBusqueda
{
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
