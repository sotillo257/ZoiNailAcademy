using System.Collections.Concurrent;
using MediatR;
using ZoiNailAcademy.Application.Ediciones;

namespace IntegrationTests.TestSupport;

/// <summary>Test handler that records every published <see cref="EdicionPublicadaEvent"/>.</summary>
public class RecordingEdicionPublicadaHandler : INotificationHandler<EdicionPublicadaEvent>
{
    private static readonly ConcurrentQueue<Guid> Published = new();

    public static int CountFor(Guid edicionId) => Published.Count(id => id == edicionId);

    public Task Handle(EdicionPublicadaEvent notification, CancellationToken cancellationToken)
    {
        Published.Enqueue(notification.EdicionId);
        return Task.CompletedTask;
    }
}
