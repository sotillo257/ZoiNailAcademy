using System.Text;
using System.Text.Encodings.Web;
using System.Text.Unicode;

namespace ZoiNailAcademy.Application.Emails.Plantillas;

/// <summary>
/// Builds the body of an email in HTML and plain text at the same time, so both versions always say the same.
/// Every value is HTML-encoded here: templates never write raw HTML with user data.
/// </summary>
public sealed class EmailCuerpo
{
    /// <summary>Keeps accents readable in the source while escaping <c>&lt; &gt; &amp; " '</c>.</summary>
    private static readonly HtmlEncoder Encoder = HtmlEncoder.Create(UnicodeRanges.All);

    private readonly StringBuilder _html = new();
    private readonly StringBuilder _texto = new();

    public static string Encode(string? valor) => Encoder.Encode(valor ?? string.Empty);

    /// <summary>Encodes each line and joins them with <c>&lt;br&gt;</c> (the encoder would turn line breaks into entities).</summary>
    public static string EncodeLineas(string? valor) =>
        string.Join("<br>", (valor ?? string.Empty).ReplaceLineEndings("\n").Split('\n').Select(Encoder.Encode));

    public EmailCuerpo Titulo(string texto)
    {
        _html.Append($"<h1 style=\"margin:0 0 16px;font-size:22px;line-height:1.3;color:{EmailLayout.ColorTexto};\">{Encode(texto)}</h1>");
        _texto.Append(texto).Append("\n\n");
        return this;
    }

    public EmailCuerpo Parrafo(string texto)
    {
        var html = EncodeLineas(texto);
        _html.Append($"<p style=\"margin:0 0 16px;font-size:15px;line-height:1.5;\">{html}</p>");
        _texto.Append(texto).Append("\n\n");
        return this;
    }

    /// <summary>The booking code, large and centered (it is also in the subject).</summary>
    public EmailCuerpo Codigo(string etiqueta, string codigo)
    {
        _html.Append(
            "<table role=\"presentation\" width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" style=\"margin:0 0 20px;\"><tr>" +
            $"<td align=\"center\" style=\"background:{EmailLayout.ColorFondoSuave};border-radius:6px;padding:16px;\">" +
            $"<div style=\"font-size:13px;color:{EmailLayout.ColorSecundario};\">{Encode(etiqueta)}</div>" +
            $"<div style=\"font-size:26px;font-weight:bold;letter-spacing:2px;color:{EmailLayout.ColorMarca};\">{Encode(codigo)}</div>" +
            "</td></tr></table>");
        _texto.Append($"{etiqueta}: {codigo}\n\n");
        return this;
    }

    /// <summary>A two-column table of label/value; rows with an empty value are left out.</summary>
    public EmailCuerpo Datos(params (string Etiqueta, string? Valor)[] filas)
    {
        _html.Append("<table role=\"presentation\" width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" style=\"margin:0 0 20px;border-collapse:collapse;\">");
        foreach (var (etiqueta, valor) in filas.Where(f => !string.IsNullOrWhiteSpace(f.Valor)))
        {
            var html = EncodeLineas(valor);
            _html.Append(
                "<tr>" +
                $"<td valign=\"top\" style=\"padding:6px 12px 6px 0;font-size:14px;color:{EmailLayout.ColorSecundario};white-space:nowrap;border-bottom:1px solid #eeeeee;\">{Encode(etiqueta)}</td>" +
                $"<td valign=\"top\" style=\"padding:6px 0;font-size:14px;font-weight:bold;border-bottom:1px solid #eeeeee;\">{html}</td>" +
                "</tr>");
            _texto.Append($"{etiqueta}: {valor}\n");
        }

        _html.Append("</table>");
        _texto.Append('\n');
        return this;
    }

    public EmailCuerpo Boton(string texto, string url)
    {
        _html.Append(
            "<table role=\"presentation\" cellpadding=\"0\" cellspacing=\"0\" style=\"margin:0 0 20px;\"><tr>" +
            $"<td style=\"background:{EmailLayout.ColorMarca};border-radius:6px;\">" +
            $"<a href=\"{Encode(url)}\" style=\"display:inline-block;padding:12px 24px;font-size:15px;font-weight:bold;color:#ffffff;text-decoration:none;\">{Encode(texto)}</a>" +
            "</td></tr></table>");
        _texto.Append($"{texto}: {url}\n\n");
        return this;
    }

    /// <summary>A highlighted warning box.</summary>
    public EmailCuerpo Aviso(string texto)
    {
        _html.Append(
            "<table role=\"presentation\" width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" style=\"margin:0 0 20px;\"><tr>" +
            $"<td style=\"background:#fff4e5;border-left:4px solid #f0a020;padding:12px 16px;font-size:14px;line-height:1.5;\">{EncodeLineas(texto)}</td>" +
            "</tr></table>");
        _texto.Append($"IMPORTANTE: {texto}\n\n");
        return this;
    }

    public string Html => _html.ToString();

    public string Texto => _texto.ToString().TrimEnd() + "\n";
}
