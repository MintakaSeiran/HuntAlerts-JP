using System;
using System.Collections.Generic;

namespace HuntAlerts.Helpers;

internal sealed class HuntRankNames
{
    // AS Mob Plate's ACT-verified BNpcName IDs; minions are intentionally excluded.
    internal static readonly uint[] SsNameIds = [0x22D3, 0x2977, 0x345E];
    private readonly HashSet<string> names = new(StringComparer.OrdinalIgnoreCase);

    internal void Add(string name)
    {
        if (!string.IsNullOrWhiteSpace(name)) names.Add(name.Trim());
    }

    internal string Rank(string? type, string? name)
    {
        if (type != "srank" || string.IsNullOrWhiteSpace(name)) return "S";
        var end = name.IndexOfAny(['[', '［']);
        var plain = (end < 0 ? name : name[..end]).Trim();
        return names.Contains(plain) ? "SS" : "S";
    }
}
