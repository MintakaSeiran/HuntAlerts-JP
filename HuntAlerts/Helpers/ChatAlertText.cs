using System;

namespace HuntAlerts.Helpers;

internal static class ChatAlertText
{
    internal static string Train(bool japanese, string? kind, string world, string? datacenter = null) => Dc(datacenter) + (japanese
        ? $"{World(world)}で{Expansion(kind)}モブハントツアーが開始予定です！（クリックで詳細）"
        : $"{kind} train starting on {world}! (Click for info)");

    internal static string Spawn(bool japanese, string? kind, string creature, string world, int instance, string? datacenter = null)
    {
        if (!japanese)
            return Dc(datacenter) + (instance > 1
                ? $"{kind} S Rank {creature} (i{instance}) spawned on {world}! (Click for info)"
                : $"{kind} S Rank {creature} spawned on {world}! (Click for info)");
        var instanceText = instance > 1 ? $"（インスタンス{instance}）" : "";
        return Dc(datacenter) + $"{World(world)}に{Expansion(kind)}Sランク {Creature(creature)}{instanceText}が出現しました！（クリックで詳細）";
    }

    internal static string Kill(bool japanese, string? kind, string creature, string world, string time, string? datacenter = null) => Dc(datacenter) + (japanese
        ? $"{World(world)}の{Expansion(kind)}Sランク {Creature(creature)}は{time}に討伐されました。"
        : $"{kind} S Rank {creature} on {world} was killed at {time}.");

    private static string Dc(string? datacenter) => JapaneseRelayText.Clean(datacenter) is { Length: > 0 } dc ? $"[DC: {dc}] " : "";

    private static string Expansion(string? kind)
    {
        var groups = JapaneseRelayText.Clean(kind).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        for (var i = 0; i < groups.Length; i++) groups[i] = JapaneseRelayText.Expansion(groups[i]);
        return groups.Length == 0 ? "" : string.Join("・", groups) + " ";
    }

    private static string Creature(string name) => JapaneseRelayText.Clean(name) is { Length: > 0 } clean ? clean : "名称不明";
    private static string World(string name) => JapaneseRelayText.Clean(name) is { Length: > 0 } clean ? clean : "不明なワールド";
}
