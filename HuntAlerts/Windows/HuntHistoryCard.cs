using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using HuntAlerts.Helpers;

namespace HuntAlerts.Windows;

internal static class HuntHistoryCard
{
    internal static float Height(int rows) => 18 + (rows == 2 ? 2 : 3) * (ImGui.GetTextLineHeight() + 6);

    internal static void Draw(ImDrawListPtr list, Vector2 origin, float width, string rank, string creature,
        string location, long posted, HuntOutcome outcome, bool japanese, bool hovered,
        int rows = 3, string kind = "Centurio", string world = "Valefor", string age = "Today")
    {
        var end = origin + new Vector2(width, Height(rows));
        var dead = outcome.DeathEpoch > 0;
        list.AddRectFilled(origin, end, dead ? 0xff151515 : hovered ? Theme.CardBgHover : Theme.CardBg, 6);
        list.AddRect(origin, end, dead ? 0xff383838 : hovered ? Theme.SRankBorder : Theme.CardBorder, 6);
        list.AddRectFilled(origin, new Vector2(origin.X + 4, end.Y), dead ? 0xff494949 : Theme.SRankBorder, 4);
        var color = dead ? 0xffaaaaaa : Theme.Text;
        var line = ImGui.GetTextLineHeight() + 6;
        var textLeft = origin + new Vector2(12, 10);
        var stampText = japanese ? "撃破済" : "DEFEATED";
        var stampWidth = ImGui.CalcTextSize(stampText).X + 24;
        var stampReserve = dead ? stampWidth + 22 : 0;
        var clock = dead ? (japanese ? "討伐 " : "Killed ") + HuntOutcomeText.Clock(outcome.DeathEpoch)
            : DateTimeOffset.FromUnixTimeSeconds(HuntOutcomeBook.ValidEpoch(posted) ? posted : 0)
                .ToLocalTime().ToString("hh:mm tt", System.Globalization.CultureInfo.GetCultureInfo("en-US"));
        var clockWidth = ImGui.CalcTextSize(clock).X;
        var leftWidth = Math.Max(0, width - 42 - clockWidth);
        var badgeWidth = Badge(rank + " RANK", textLeft, leftWidth, Theme.SRankBg, Theme.SRankBorder, Theme.SRankText);
        Text(kind, textLeft + new Vector2(badgeWidth + 8, 1), leftWidth - badgeWidth - 8, color);
        Text(clock, new Vector2(end.X - 12 - clockWidth, textLeft.Y + 1), clockWidth, color);
        var second = textLeft + new Vector2(0, line);
        var secondWidth = width - 24 - (rows == 2 && dead ? stampReserve : ImGui.CalcTextSize(age).X + 12);
        var worldWidth = Badge(world, second, Math.Min(secondWidth, width * 0.4f), Theme.WorldBg, Theme.WorldBorder, Theme.WorldText);
        Text(" · " + location, second + new Vector2(worldWidth + 6, 1), secondWidth - worldWidth - 6, dead ? color : Theme.Subtle);
        if (rows != 2 || !dead)
            Text(age, new Vector2(end.X - 12 - ImGui.CalcTextSize(age).X, second.Y + 1), ImGui.CalcTextSize(age).X, Theme.Subtle);
        if (rows != 2)
            Line(HuntOutcomeText.Average(outcome, japanese), 2, width - 24 - stampReserve, color);
        if (dead)
            Stamp(list, new Vector2(end.X - 18 - stampWidth / 2, end.Y - 12 - ImGui.GetTextLineHeight() / 2), stampText, 0.65f);

        float Badge(string text, Vector2 position, float available, uint bg, uint border, uint fg)
        {
            var label = Fit(text, Math.Max(0, available - 12));
            var size = new Vector2(Math.Min(Math.Max(0, available), ImGui.CalcTextSize(label).X + 12), ImGui.GetTextLineHeight() + 2);
            list.AddRectFilled(position, position + size, dead ? 0xff252525 : bg, 2);
            list.AddRect(position, position + size, dead ? 0xff494949 : border, 2);
            Text(label, position + new Vector2(6, 1), size.X - 12, dead ? color : fg);
            return size.X;
        }

        void Text(string text, Vector2 position, float available, uint tint)
        {
            if (available <= 0) return;
            list.PushClipRect(position, position + new Vector2(available, line), true);
            list.AddText(position, tint, Fit(text, available));
            list.PopClipRect();
        }

        void Line(string text, int row, float available, uint tint)
        {
            var position = textLeft + new Vector2(0, row * line);
            Text(text, position, available, tint);
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

    private static unsafe void Stamp(ImDrawListPtr list, Vector2 center, string text, float scale)
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
            var p = (list.VtxBuffer[i].Pos - center) * scale;
            ((ImDrawVert*)list.VtxBuffer.Data)[i].Pos = center + new Vector2(p.X * cos - p.Y * sin, p.X * sin + p.Y * cos);
        }
    }
}
