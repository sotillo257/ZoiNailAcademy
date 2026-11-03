using System.Collections.Concurrent;
using MediatR;

namespace IntegrationTests.TestSupport;

/// <summary>Records every domain event published by the shared host (handlers registered by the factory).</summary>
public static class EventRecorder
{
    private static readonly ConcurrentQueue<INotification> Published = new();

    public static void Record(INotification notification) => Published.Enqueue(notification);

    public static int Count<TEvent>(Func<TEvent, bool> match) where TEvent : INotification =>
        Published.OfType<TEvent>().Count(match);
}

public class RecordingHandler<TEvent> : INotificationHandler<TEvent> where TEvent : INotification
{
    public Task Handle(TEvent notification, CancellationToken cancellationToken)
    {
        EventRecorder.Record(notification);
        return Task.CompletedTask;
    }
}
