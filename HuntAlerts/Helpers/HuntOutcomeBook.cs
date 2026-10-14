using System;
using System.Collections.Generic;
using System.Linq;

namespace HuntAlerts.Helpers;

internal sealed record HuntOutcomeEvent(int Version, string World, string Creature, int Instance,
    long PostedEpoch, long DeathEpoch, string Zone = "", string Kind = "");

internal readonly record struct HuntOutcome(long DeathEpoch, long? DurationSeconds, double? AverageSeconds, int Samples);

internal sealed class HuntOutcomeBook
{
    internal const long MaxMatchSeconds = 24 * 60 * 60;
    private readonly record struct Key(string World, string Creature, int Instance);
    private sealed class Series
    {
        internal readonly SortedSet<long> Spawns = new();
        internal readonly SortedSet<long> Deaths = new();
        internal readonly Dictionary<long, long> Matches = new();

        internal void Match()
        {
            Matches.Clear();
            foreach (var death in Deaths)
            {
                // A late kill can only close the latest preceding appearance, never a newer one.
                var spawn = Spawns.GetViewBetween(1, death).Max;
                if (spawn > 0 && death - spawn <= MaxMatchSeconds) Matches.TryAdd(spawn, death);
            }
        }
    }

    private readonly Dictionary<Key, Series> series = new();
    internal int EventCount { get; private set; }

    internal static bool ValidEpoch(long epoch) => epoch > 0 && epoch <= 253402300799;

    private static string Clean(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "";
        var end = value.IndexOfAny(['[', '［']);
        var clean = (end < 0 ? value : value[..end]).Trim().ToUpperInvariant();
        return clean is "UNKNOWN" or "INVALID" ? "" : clean;
    }

    private static Key Identity(string world, string creature, int instance) => new(Clean(world), Clean(creature), Math.Max(1, instance));

    internal bool HasHistory(string world, string creature, int instance) => series.ContainsKey(Identity(world, creature, instance));

    internal bool Add(HuntOutcomeEvent entry)
    {
        var key = Identity(entry.World, entry.Creature, entry.Instance);
        if (entry.Version != 1 || key.World.Length == 0 || key.Creature.Length == 0
            || entry.DeathEpoch < 0 || !ValidEpoch(entry.DeathEpoch == 0 ? entry.PostedEpoch : entry.DeathEpoch)) return false;
        if (!series.TryGetValue(key, out var data)) series[key] = data = new Series();
        var changed = entry.DeathEpoch == 0 ? data.Spawns.Add(entry.PostedEpoch) : data.Deaths.Add(entry.DeathEpoch);
        if (!changed) return false;
        EventCount++;
        data.Match();
        return true;
    }

    internal HuntOutcome Get(HuntTrainMessage message)
    {
        if (message.huntType != "srank" || !ValidEpoch(message.PostedEpoch)
            || !series.TryGetValue(Identity(message.huntWorld, message.creatureName, message.instance), out var data)) return default;
        var death = data.Matches.GetValueOrDefault(message.PostedEpoch);
        // Only completed, earlier appearances inform the next appearance's average.
        var samples = data.Matches.Where(p => p.Value < message.PostedEpoch && p.Value > p.Key).ToArray();
        return new HuntOutcome(death, death > message.PostedEpoch ? death - message.PostedEpoch : null,
            samples.Length == 0 ? null : samples.Average(p => (double)(p.Value - p.Key)), samples.Length);
    }
}
