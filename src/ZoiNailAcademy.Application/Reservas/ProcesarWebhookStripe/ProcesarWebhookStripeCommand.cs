using MediatR;
using ZoiNailAcademy.Application.Common;

namespace ZoiNailAcademy.Application.Reservas.ProcesarWebhookStripe;

/// <param name="Json">Raw request body, exactly as Stripe signed it.</param>
/// <param name="Firma">Value of the <c>Stripe-Signature</c> header.</param>
public sealed record ProcesarWebhookStripeCommand(string Json, string? Firma) : IRequest<Result>;
