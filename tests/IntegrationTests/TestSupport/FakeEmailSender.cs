using System.Collections.Concurrent;
using ZoiNailAcademy.Application.Common.Interfaces;

namespace IntegrationTests.TestSupport;

/// <summary>Email provider for the outbox tests: fails a given number of times per recipient, then succeeds.</summary>
public class FakeEmailSender : IEmailSender
{
    private readonly ConcurrentDictionary<string, int> _fallosPendientes = new(StringComparer.OrdinalIgnoreCase);

    public ConcurrentQueue<EmailMensaje> Enviados { get; } = new();

    public void FallarPara(string para, int veces) => _fallosPendientes[para] = veces;

    public Task<string?> EnviarAsync(EmailMensaje mensaje, CancellationToken cancellationToken)
    {
        if (_fallosPendientes.TryGetValue(mensaje.Para, out var fallos) && fallos > 0)
        {
            _fallosPendientes[mensaje.Para] = fallos - 1;
            throw new HttpRequestException("Resend respondió 503: no disponible");
        }

        Enviados.Enqueue(mensaje);
        return Task.FromResult<string?>($"fake_{mensaje.Id:N}");
    }
}
