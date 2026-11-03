using System.Text;

namespace ZoiNailAcademy.Application.Common.Csv;

/// <summary>
/// CSV for Excel in Spanish: <c>;</c> separator, CRLF line endings and UTF-8 with BOM (so accents open
/// correctly). Fields with <c>;</c>, quotes or line breaks are quoted; fields that start with
/// <c>= + - @</c> (or a tab / carriage return) get a leading <c>'</c> so Excel never runs them as formulas.
/// Shared by the waiting list (F05) and the booking exports (F07).
/// </summary>
public sealed class CsvBuilder
{
    public const char Separador = ';';
    public const string ContentType = "text/csv; charset=utf-8";

    private static readonly char[] PrefijosFormula = ['=', '+', '-', '@', '\t', '\r'];
    private static readonly char[] CaracteresEspeciales = [Separador, '"', '\r', '\n'];

    private readonly StringBuilder _sb = new();

    public CsvBuilder(params string?[] cabeceras)
    {
        Fila(cabeceras);
    }

    public CsvBuilder Fila(params string?[] campos)
    {
        _sb.AppendJoin(Separador, campos.Select(Escapar));
        _sb.Append("\r\n");
        return this;
    }

    /// <summary>The CSV as UTF-8 bytes, starting with the BOM.</summary>
    public byte[] ToBytes()
    {
        var encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);
        return [.. encoding.GetPreamble(), .. encoding.GetBytes(_sb.ToString())];
    }

    public override string ToString() => _sb.ToString();

    public static string Escapar(string? campo)
    {
        if (string.IsNullOrEmpty(campo))
        {
            return string.Empty;
        }

        if (Array.IndexOf(PrefijosFormula, campo[0]) >= 0)
        {
            campo = "'" + campo;
        }

        return campo.IndexOfAny(CaracteresEspeciales) >= 0 ? $"\"{campo.Replace("\"", "\"\"")}\"" : campo;
    }
}
