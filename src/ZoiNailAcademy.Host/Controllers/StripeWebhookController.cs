using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using ZoiNailAcademy.Application.Reservas.ProcesarWebhookStripe;

namespace ZoiNailAcademy.Host.Controllers;

/// <summary>
/// Stripe webhook. Anonymous and without rate limit: authenticity comes from the signature, which is
/// computed over the raw body, so the body is read as text and never model-bound.
/// </summary>
[ApiController]
[Route("stripe")]
[AllowAnonymous]
[DisableRateLimiting]
public class StripeWebhookController(ISender sender) : ControllerBase
{
    [HttpPost("webhook")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Webhook(CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(Request.Body);
        var json = await reader.ReadToEndAsync(cancellationToken);

        await sender.Send(new ProcesarWebhookStripeCommand(json, Request.Headers["Stripe-Signature"]), cancellationToken);
        return Ok();
    }
}
