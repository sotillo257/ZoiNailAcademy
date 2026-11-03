using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using ZoiNailAcademy.Application.Common.Interfaces;

namespace ZoiNailAcademy.Infrastructure.Email;

/// <summary>
/// Sends through the Resend API (<c>POST https://api.resend.com/emails</c>) with the outbox id as
/// <c>Idempotency-Key</c>, so a retry after a timeout never sends the email twice. Emails with
/// <see cref="EmailMensaje.BajaUrl"/> carry the one-click unsubscribe headers (RFC 8058), which Gmail shows as its
/// native "unsubscribe" button.
/// </summary>
public class ResendEmailSender(HttpClient httpClient, IOptions<EmailOptions> emailOptions, IOptions<ResendOptions> resendOptions)
    : IEmailSender
{
    public static readonly Uri BaseAddress = new("https://api.resend.com/");

    private const int MaxErrorBody = 500;

    public const string ListUnsubscribe = "List-Unsubscribe";
    public const string ListUnsubscribePost = "List-Unsubscribe-Post";
    public const string OneClick = "List-Unsubscribe=One-Click";

    public async Task<string?> EnviarAsync(EmailMensaje mensaje, CancellationToken cancellationToken)
    {
        var body = new ResendEmailRequest(
            emailOptions.Value.From, [mensaje.Para], mensaje.Asunto, mensaje.Html, mensaje.Texto,
            mensaje.ReplyTo is null ? null : [mensaje.ReplyTo],
            mensaje.BajaUrl is null
                ? null
                : new Dictionary<string, string>
                {
                    [ListUnsubscribe] = $"<{mensaje.BajaUrl}>",
                    [ListUnsubscribePost] = OneClick
                });

        using var request = new HttpRequestMessage(HttpMethod.Post, "emails") { Content = JsonContent.Create(body) };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", resendOptions.Value.ApiKey);
        request.Headers.Add("Idempotency-Key", mensaje.Id.ToString());

        using var response = await httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException(
                $"Resend respondió {(int)response.StatusCode}: {(error.Length <= MaxErrorBody ? error : error[..MaxErrorBody])}",
                null, response.StatusCode);
        }

        var resultado = await response.Content.ReadFromJsonAsync<ResendEmailResponse>(cancellationToken);
        return resultado?.Id;
    }

    private sealed record ResendEmailRequest(
        [property: JsonPropertyName("from")] string From,
        [property: JsonPropertyName("to")] string[] To,
        [property: JsonPropertyName("subject")] string Subject,
        [property: JsonPropertyName("html")] string Html,
        [property: JsonPropertyName("text")] string Text,
        [property: JsonPropertyName("reply_to"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string[]? ReplyTo,
        [property: JsonPropertyName("headers"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] Dictionary<string, string>? Headers);

    private sealed record ResendEmailResponse([property: JsonPropertyName("id")] string? Id);
}
