using System.Numerics;
using Dalamud.Bindings.ImGui;
using HuntAlerts.Helpers;
using HuntAlerts.Windows;

unsafe
{
    var context = ImGui.CreateContext();
    try
    {
        var io = ImGui.GetIO();
        io.IniFilename = null;
        io.DisplaySize = new Vector2(1400, 600);
        io.DeltaTime = 1f / 60;
        io.Fonts.TexDesiredWidth = 4096;
        io.Fonts.TexDesiredHeight = 4096;
        io.Fonts.AddFontFromFileTTF("C:/Windows/Fonts/meiryo.ttc", 18, null, io.Fonts.GetGlyphRangesJapanese());
        byte* atlas = null; int aw = 0, ah = 0;
        io.Fonts.GetTexDataAsRGBA32(0, &atlas, &aw, &ah);
        if (io.Fonts.Textures.Size != 1) throw new Exception("Preview rasterizer requires a single atlas texture");
        if (atlas == null || aw == 0 || ah == 0) throw new Exception("Missing font atlas");
        var checkedCases = 0;
        foreach (var width in new[] { 340f, 500f, 1180f })
        foreach (var scale in new[] { 1f, 1.5f, 2f })
        foreach (var japanese in new[] { false, true })
        foreach (var dead in new[] { false, true })
        foreach (var rows in new[] { 2, 3 })
        foreach (var rank in new[] { "S", "SS" })
        foreach (var badgePalette in new[] { 0, 1, 2 })
        foreach (var backgroundPalette in new[] { 0, 1, 2 })
        {
            io.FontGlobalScale = scale;
            ImGui.NewFrame();
            var list = ImGui.GetForegroundDrawList();
            var origin = new Vector2(20, 20);
            var outcome = new HuntOutcome(dead ? 1792011000 : 0, dead ? 180 : null, 180, 1234);
            HuntHistoryCard.Draw(list, origin, width, rank, new string('長', 60), "長い地名テスト", 1792010820, outcome, japanese, true, rows,
                ssBadgePalette: badgePalette, ssBackgroundPalette: backgroundPalette);
            var red = false;
            for (var i = 0; i < list.VtxBuffer.Size; i++)
            {
                var v = list.VtxBuffer[i];
                if (!float.IsFinite(v.Pos.X) || !float.IsFinite(v.Pos.Y)
                    || v.Pos.X < origin.X - 2 || v.Pos.X > origin.X + width + 2
                    || v.Pos.Y < origin.Y - 2 || v.Pos.Y > origin.Y + HuntHistoryCard.Height(rows) + 2)
                    throw new Exception($"Geometry outside card: width={width} scale={scale} JP={japanese} dead={dead} at {v.Pos}");
                red |= v.Col == 0xff4646e8;
            }
            if (red != dead) throw new Exception("Stamp must appear only on defeated entries");
            ImGui.Render(); checkedCases++;
        }
        Console.WriteLine($"Passed {checkedCases} native ImGui card geometry cases.");
        if (args.Length == 1)
        {
            io.FontGlobalScale = 1;
            io.DisplaySize = new Vector2(550, 430);
            ImGui.NewFrame();
            var list = ImGui.GetForegroundDrawList();
            list.AddRectFilled(Vector2.Zero, io.DisplaySize, 0xff202020);
            list.AddText(new Vector2(20, 10), 0xffeeeeee, "HuntAlerts History");
            var y = 40f;
            foreach (var (palette, dead) in new[] { (1, false), (2, false), (2, true) })
            {
                HuntHistoryCard.Draw(list, new Vector2(14, y), 522, "SS", "Sample SS",
                    "Sample area", 1792010820,
                    new HuntOutcome(dead ? 1792011000 : 0, dead ? 180 : null, 180, 3), true, false,
                    kind: "Shadowbringers", ssBadgePalette: palette, ssBackgroundPalette: palette);
                y += HuntHistoryCard.Height(3) + 8;
            }
            ImGui.Render();
            io.Fonts.GetTexDataAsRGBA32(0, &atlas, &aw, &ah);
            Raster.Save(args[0], 550, 430, atlas, aw, ah);
        }
    }
    finally { ImGui.DestroyContext(context); }
}
