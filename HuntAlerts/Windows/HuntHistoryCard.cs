using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using HuntAlerts.Helpers;

namespace HuntAlerts.Windows;

internal static class HuntHistoryCard
{
    internal static float Height => 20 + 4 * (ImGui.GetTextLineHeight() + 4);

    internal static void Draw(ImDrawListPtr list, Vector2 origin, float width, string rank, string creature,
        string location, long posted, HuntOutcome outcome, bool japanese, bool hovered)
    {
        var end = origin + new Vector2(width, Height);
        var dead = outcome.DeathEpoch > 0;
        list.AddRectFilled(origin, end, dead ? 0xff151515 : hovered ? Theme.CardBgHover : Theme.CardBg, 6);
        list.AddRect(origin, end, dead ? 0xff383838 : hovered ? Theme.SRankBorder : Theme.CardBorder, 6);
        list.AddRectFilled(origin, new Vector2(origin.X + 4, end.Y), dead ? 0xff494949 : Theme.SRankBorder, 4);
        var color = dead ? 0xffaaaaaa : Theme.Text;
        var line = ImGui.GetTextLineHeight() + 4;
        var textLeft = origin + new Vector2(12, 10);
        var stampText = japanese ? "撃破済" : "DEFEATED";
        var stampWidth = ImGui.CalcTextSize(stampText).X + 24;
        var stampReserve = dead ? stampWidth + 22 : 0;
        Line($"[{rank}] {creature}", 0, width - 24, dead ? color : Theme.SRankText);
        Line(location, 1, width - 24, dead ? color : Theme.WorldText);
        var times = japanese ? $"出現 {HuntOutcomeText.Clock(posted)}" : $"Spawn {HuntOutcomeText.Clock(posted)}";
        if (dead) times += japanese ? $" / 討伐 {HuntOutcomeText.Clock(outcome.DeathEpoch)}" : $" / Killed {HuntOutcomeText.Clock(outcome.DeathEpoch)}";
        Line(times, 2, width - 24 - stampReserve, color);
        Line(HuntOutcomeText.Average(outcome, japanese), 3, width - 24 - stampReserve, color);
        if (dead)
            Stamp(list, new Vector2(end.X - 18 - stampWidth / 2, textLeft.Y + line * 3), stampText);

        void Line(string text, int row, float available, uint tint)
        {
            var position = textLeft + new Vector2(0, row * line);
            list.PushClipRect(position, position + new Vector2(Math.Max(0, available), line), true);
            list.AddText(position, tint, Fit(text, available));
            list.PopClipRect();
        }
    }

    private static string Fit(string text, float width)
    {
        if (ImGui.CalcTextSize(text).X <= width) return text;
        const string ellipsis = "...";
        while (text.Length > 0 && ImGui.CalcTextSize(text + ellipsis).X > width)
            text = text[..(text.Length >= 2 && char.IsSurrogatePair(text, text.Length - 2) ? ^2 : ^1)];
        return text + ellipsis;
    }

    private static unsafe void Stamp(ImDrawListPtr list, Vector2 center, string text)
    {
        var size = ImGui.CalcTextSize(text);
        var min = center - size / 2 - new Vector2(10, 6);
        var max = center + size / 2 + new Vector2(10, 6);
        var first = list.VtxBuffer.Size;
        const uint red = 0xff4646e8;
        list.AddRect(min, max, red, 2, ImDrawFlags.None, 2.5f);
        list.AddRect(min + new Vector2(3), max - new Vector2(3), red, 1, ImDrawFlags.None, 1);
        list.AddText(center - size / 2, red, text);
        var cos = MathF.Cos(-0.16f);
        var sin = MathF.Sin(-0.16f);
        for (var i = first; i < list.VtxBuffer.Size; i++)
        {
            var p = list.VtxBuffer[i].Pos - center;
            ((ImDrawVert*)list.VtxBuffer.Data)[i].Pos = center + new Vector2(p.X * cos - p.Y * sin, p.X * sin + p.Y * cos);
        }
    }
}
