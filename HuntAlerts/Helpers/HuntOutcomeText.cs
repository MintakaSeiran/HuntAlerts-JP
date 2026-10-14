using System;

namespace HuntAlerts.Helpers;

internal static class HuntOutcomeText
{
    internal static string Clock(long epoch, bool full = false) => HuntOutcomeBook.ValidEpoch(epoch)
        ? DateTimeOffset.FromUnixTimeSeconds(epoch).ToLocalTime().ToString(full ? "yyyy-MM-dd HH:mm:ss" : "HH:mm") : "--:--";

    internal static string Duration(double seconds)
    {
        var rounded = (long)Math.Round(seconds);
        return $"{rounded / 60:00}:{rounded % 60:00}";
    }

    internal static string Average(HuntOutcome outcome, bool japanese) => outcome.AverageSeconds is double average
        ? (japanese ? $"平均 {Duration(average)} ({outcome.Samples}件)" : $"Avg {Duration(average)} (n={outcome.Samples})")
        : (japanese ? "平均 --:-- (0件)" : "Avg --:-- (n=0)");
}
