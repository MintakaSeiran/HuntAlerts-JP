using Dalamud.Game;
using ECommons.DalamudServices;
using ECommons.Logging;
using Lumina.Excel.Sheets;
using System;
using System.Collections.Generic;

namespace HuntAlerts.Helpers;

internal static class RelayJapaneseNames
{
    private static Dictionary<string, string>? creatures;

    internal static (string Creature, string Zone, string Aetheryte) Resolve(HuntTrainMessage entry)
    {
        // IDがない過去の通知も、照合できた項目だけ翻訳する。
        var creature = TryResolve(() => Creature(entry.creatureName), entry.creatureName);
        var zone = TryResolve(() => Zone(entry.startTerritoryTypeId, entry.startZone), entry.startZone);
        var aetheryte = TryResolve(() => AetheryteName(entry.startLocationAetheryteId, entry.startLocation), entry.startLocation);
        return (creature, zone, aetheryte);
    }

    private static string TryResolve(Func<string?> resolve, string fallback)
    {
        try
        {
            var localized = JapaneseRelayText.Clean(resolve());
            if (localized.Length > 0) return localized;
        }
        catch (Exception ex)
        {
            PluginLog.Warning($"Japanese relay name lookup failed: {ex.Message}");
        }
        return JapaneseRelayText.Clean(fallback);
    }

    private static string? Creature(string name)
    {
        var clean = JapaneseRelayText.Clean(name);
        if (clean.Length == 0) return null;
        if (creatures == null)
        {
            var names = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var japanese = Svc.Data.GetExcelSheet<BNpcName>(ClientLanguage.Japanese);
            foreach (var row in Svc.Data.GetExcelSheet<BNpcName>(ClientLanguage.English))
            {
                var english = row.Singular.ExtractText().Trim();
                if (english.Length == 0 || !japanese.TryGetRow(row.RowId, out var translated)) continue;
                var value = translated.Singular.ExtractText();
                if (!string.IsNullOrWhiteSpace(value)) names.TryAdd(english, value);
            }
            creatures = names;
        }
        return creatures.GetValueOrDefault(clean);
    }

    private static string? Zone(uint id, string fallback)
    {
        var sheet = Svc.Data.GetExcelSheet<TerritoryType>(ClientLanguage.Japanese);
        if (id != 0 && sheet.TryGetRow(id, out var zone)) return Place(zone.PlaceName.RowId);
        foreach (var row in Svc.Data.GetExcelSheet<TerritoryType>(ClientLanguage.English))
            if (JapaneseRelayText.Clean(fallback).Length > 0 &&
                string.Equals(row.PlaceName.ValueNullable?.Name.ExtractText(), fallback.Trim(), StringComparison.OrdinalIgnoreCase))
                return Place(row.PlaceName.RowId);
        return null;
    }

    private static string? AetheryteName(uint id, string fallback)
    {
        var sheet = Svc.Data.GetExcelSheet<Aetheryte>(ClientLanguage.Japanese);
        if (id != 0 && sheet.TryGetRow(id, out var aetheryte)) return Place(aetheryte.PlaceName.RowId);
        foreach (var row in Svc.Data.GetExcelSheet<Aetheryte>(ClientLanguage.English))
            if (row.IsAetheryte && JapaneseRelayText.Clean(fallback).Length > 0 &&
                string.Equals(row.PlaceName.ValueNullable?.Name.ExtractText(), fallback.Trim(), StringComparison.OrdinalIgnoreCase))
                return Place(row.PlaceName.RowId);
        return null;
    }

    private static string? Place(uint id) =>
        id != 0 && Svc.Data.GetExcelSheet<PlaceName>(ClientLanguage.Japanese).TryGetRow(id, out var place)
            ? place.Name.ExtractText() : null;
}
