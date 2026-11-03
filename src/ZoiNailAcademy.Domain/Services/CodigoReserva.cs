using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace ZoiNailAcademy.Domain.Services;

/// <summary>
/// Public booking code <c>CUR-XXXXXX</c>: the only identifier of a booking in public URLs, so it is
/// random (cryptographic generator), not sequential, and avoids ambiguous characters (0/O, 1/I/L).
/// </summary>
public static partial class CodigoReserva
{
    public const string Prefijo = "CUR-";
    public const string Alfabeto = "23456789ABCDEFGHJKMNPQRSTUVWXYZ";
    public const int LongitudAleatoria = 6;
    public const int Longitud = 10;

    [GeneratedRegex("^CUR-[23456789ABCDEFGHJKMNPQRSTUVWXYZ]{6}$")]
    private static partial Regex Formato();

    public static string Generar() =>
        Prefijo + new string(RandomNumberGenerator.GetItems<char>(Alfabeto, LongitudAleatoria));

    public static bool EsValido(string? codigo) => codigo is not null && Formato().IsMatch(codigo);

    /// <summary>
    /// Reads a code however the client typed it (<c>CUR-7K4Q2M</c>, <c>cur 7k4q2m</c>, <c>7K4Q2M</c>): blanks and
    /// dashes are ignored, case too, and the <c>CUR</c> prefix is optional.
    /// </summary>
    /// <param name="codigo">The code as stored (<c>CUR-XXXXXX</c>) when the text looks like one.</param>
    public static bool TryNormalizar(string? texto, out string codigo)
    {
        codigo = string.Empty;
        if (string.IsNullOrWhiteSpace(texto))
        {
            return false;
        }

        var limpio = string.Concat(texto.Where(c => !char.IsWhiteSpace(c) && c != '-')).ToUpperInvariant();
        var prefijo = Prefijo.TrimEnd('-');
        if (limpio.Length == prefijo.Length + LongitudAleatoria && limpio.StartsWith(prefijo, StringComparison.Ordinal))
        {
            limpio = limpio[prefijo.Length..];
        }

        if (limpio.Length != LongitudAleatoria || !limpio.All(c => Alfabeto.Contains(c)))
        {
            return false;
        }

        codigo = Prefijo + limpio;
        return true;
    }
}
