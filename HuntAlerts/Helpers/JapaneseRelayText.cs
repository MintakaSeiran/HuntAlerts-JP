using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace HuntAlerts.Helpers;

// ゲームサービスと分離し、日本語・欠損値・文字数制限を単体検証する。
internal static partial class JapaneseRelayText
{
    internal static string Build(HuntTrainMessage entry, bool useFlag, string creature, string zone, string aetheryte, string datacenter)
    {
        var parts = new List<string>();
        var isTrain = entry.huntType == "new_hunt";
        var title = isTrain ? $"[モブハントツアー] {Expansion(entry.huntKind)}" : $"[S] {Clean(creature)}";
        parts.Add(title.TrimEnd());
        Add(parts, zone);
        var nearest = Clean(aetheryte);
        if (nearest.Length > 0) parts.Add($"最寄り: {nearest}");
        var dc = Clean(datacenter);
        if (dc.Length > 0) parts.Add($"DC: {dc}");
        var world = Clean(entry.huntWorld);
        if (world.Length > 0) parts.Add($"サーバー: {world}");
        if (entry.instance > 1) parts.Add($"インスタンス{entry.instance}");
        var coords = Coordinates(entry);
        if (coords.Length > 0) parts.Add($"POS: {coords}");
        if (useFlag) parts.Add("<flag>");
        return string.Join(" / ", parts);
    }

    internal static bool HasMapPosition(HuntTrainMessage entry) =>
        Valid(entry.mapLocationX, entry.mapLocationY);

    internal static string Coordinates(HuntTrainMessage entry)
    {
        var x = entry.mapLocationX;
        var y = entry.mapLocationY;
        if (!Valid(x, y))
        {
            var match = CoordinatesPattern().Match(Clean(entry.locationCoords));
            if (!match.Success ||
                !float.TryParse(match.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out x) ||
                !float.TryParse(match.Groups[2].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out y) ||
                !Valid(x, y)) return "";
        }
        return FormattableString.Invariant($"({x:0.0}, {y:0.0})");
    }

    private static bool Valid(float x, float y) => float.IsFinite(x) && float.IsFinite(y) && x > 0 && y > 0;

    [GeneratedRegex(@"^\s*\(?\s*(?:[Xx]:\s*)?(\d+(?:\.\d+)?)\s*(?:,|\s)\s*(?:[Yy]:\s*)?(\d+(?:\.\d+)?)\s*\)?\s*$")]
    private static partial Regex CoordinatesPattern();

    internal static bool FitsChat(string channel, string text) =>
        Encoding.UTF8.GetByteCount($"{channel} {text}") <= 500;

    internal static string Clean(string? text)
    {
        var value = text?.Trim() ?? "";
        return value.Equals("unknown", StringComparison.OrdinalIgnoreCase)
            || value.Equals("invalid", StringComparison.OrdinalIgnoreCase) ? "" : value;
    }

    private static void Add(List<string> parts, string value)
    {
        var clean = Clean(value);
        if (clean.Length > 0) parts.Add(clean);
    }

    internal static string Expansion(string kind) => Clean(kind).ToLowerInvariant() switch
    {
        "arr" or "a realm reborn" => "新生",
        "hw" or "heavensward" => "蒼天",
        "sb" or "stormblood" => "紅蓮",
        "shb" or "shadowbringers" => "漆黒",
        "ew" or "endwalker" => "暁月",
        "dt" or "dawntrail" => "黄金",
        "centurio" => "新生・蒼天・紅蓮",
        _ => Clean(kind),
    };
}
