using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ZoiNailAcademy.Application.Common.Interfaces;

namespace ZoiNailAcademy.Infrastructure.Email;

/// <summary>
/// Writes emails to the log instead of sending them: the development provider, and the fallback while Resend is
/// not configured. With <c>Email:DirectorioLog</c> it also saves each one as <c>{fecha}-{tipo}.html</c>.
/// </summary>
public partial class LogEmailSender(IOptions<EmailOptions> options, TimeProvider timeProvider, ILogger<LogEmailSender> logger)
    : IEmailSender
{
    public const string ProveedorId = "log";

    [GeneratedRegex("[^A-Za-z0-9.-]")]
    private static partial Regex CaracteresNoSeguros();

    public async Task<string?> EnviarAsync(EmailMensaje mensaje, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Email {Tipo} NO enviado (proveedor Log) para {Para}: {Asunto}\n{Texto}",
            mensaje.Tipo, mensaje.Para, mensaje.Asunto, mensaje.Texto);

        var directorio = options.Value.DirectorioLog;
        if (!string.IsNullOrWhiteSpace(directorio))
        {
            Directory.CreateDirectory(directorio);
            var nombre = $"{timeProvider.GetUtcNow():yyyyMMdd-HHmmss}-{CaracteresNoSeguros().Replace(mensaje.Tipo, "_")}-{mensaje.Id:N}.html";
            await File.WriteAllTextAsync(Path.Combine(directorio, nombre), mensaje.Html, cancellationToken);
        }

        return ProveedorId;
    }
}
