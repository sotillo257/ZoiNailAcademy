using MediatR;
using Microsoft.Extensions.Logging;

namespace ZoiNailAcademy.Application.Common.Events;

public static class PublisherExtensions
{
    /// <summary>
    /// Publishes a domain event after the commit. A failing handler (e.g. an email in F06) is logged and
    /// never turns a completed operation into an error response.
    /// </summary>
    public static async Task PublicarTrasCommitAsync(
        this IPublisher publisher, INotification? notification, ILogger logger, CancellationToken cancellationToken)
    {
        if (notification is null)
        {
            return;
        }

        try
        {
            await publisher.Publish(notification, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Falló un handler del evento {Evento}", notification.GetType().Name);
        }
    }
}
