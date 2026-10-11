using System;
using System.Collections.Generic;

namespace HuntAlerts.Messaging;

// Accessed only on the framework thread. The history itself also keeps 50 entries.
internal sealed class DeferredHuntInbox<T>
{
    internal const int Capacity = 50;
    internal readonly record struct Pending(T Message, bool HistoryOnly, DateTime ReceivedAtUtc);
    private readonly Queue<Pending> pending = new();

    internal int Count => pending.Count;

    internal bool Enqueue(T message, bool historyOnly, DateTime receivedAtUtc)
    {
        var overflow = pending.Count == Capacity;
        if (overflow) pending.Dequeue();
        pending.Enqueue(new Pending(message, historyOnly, receivedAtUtc));
        return overflow;
    }

    internal bool TryTake(bool playerAvailable, out Pending message)
    {
        message = default;
        return playerAvailable && pending.TryDequeue(out message);
    }

    internal void MarkHistoryOnly()
    {
        var count = pending.Count;
        for (var i = 0; i < count; i++)
            pending.Enqueue(pending.Dequeue() with { HistoryOnly = true });
    }

    internal void Clear() => pending.Clear();
}
