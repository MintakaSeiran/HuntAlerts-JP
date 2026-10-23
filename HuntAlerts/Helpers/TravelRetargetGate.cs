using System;

namespace HuntAlerts.Helpers;

internal enum RetargetResult { Waiting, Ready, Failed }

// No game dependencies: a closed window alone must never authorize another trip.
internal sealed class TravelRetargetGate(long startedAt)
{
    private bool acknowledged;
    private long? stableSince;

    internal void Acknowledge() => acknowledged = true;

    internal RetargetResult Evaluate(long now, bool sourceWorld, bool loading,
        bool queuePresent, bool queueCondition, bool oldRoutineRunning, bool lifestreamBusy)
    {
        if (!sourceWorld || loading || now - startedAt >= 15000) return RetargetResult.Failed;
        if (!acknowledged || queuePresent || queueCondition || oldRoutineRunning || lifestreamBusy)
        {
            stableSince = null;
            return RetargetResult.Waiting;
        }
        stableSince ??= now;
        return now - stableSince.Value >= 500 ? RetargetResult.Ready : RetargetResult.Waiting;
    }
}
