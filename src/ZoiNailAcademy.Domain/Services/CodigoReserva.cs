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
}
