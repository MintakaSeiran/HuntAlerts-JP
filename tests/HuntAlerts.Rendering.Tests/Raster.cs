using System.Drawing;
using System.Drawing.Imaging;
using System.Numerics;
using System.Runtime.InteropServices;
using Dalamud.Bindings.ImGui;

internal static unsafe class Raster
{
    internal static void Save(string path, int width, int height, byte* atlas, int aw, int ah)
    {
        var pixels = new byte[width * height * 4];
        for (var i = 3; i < pixels.Length; i += 4) pixels[i] = 255;
        var data = ImGui.GetDrawData();
        for (var n = 0; n < data.CmdListsCount; n++)
        {
            var list = new ImDrawListPtr(data.CmdLists[n]);
            for (var c = 0; c < list.CmdBuffer.Size; c++)
            {
                var cmd = list.CmdBuffer[c];
                for (var i = (int)cmd.IdxOffset; i < cmd.IdxOffset + cmd.ElemCount; i += 3)
                    Triangle(list.VtxBuffer[(int)cmd.VtxOffset + list.IdxBuffer[i]],
                        list.VtxBuffer[(int)cmd.VtxOffset + list.IdxBuffer[i + 1]],
                        list.VtxBuffer[(int)cmd.VtxOffset + list.IdxBuffer[i + 2]], cmd.ClipRect);
            }
        }
        using var image = new Bitmap(width, height, PixelFormat.Format32bppArgb);
        var bits = image.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
        try { for (var y = 0; y < height; y++) Marshal.Copy(pixels, y * width * 4, bits.Scan0 + y * bits.Stride, width * 4); }
        finally { image.UnlockBits(bits); }
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        image.Save(path, ImageFormat.Png);

        void Triangle(ImDrawVert a, ImDrawVert b, ImDrawVert c, Vector4 clip)
        {
            var area = Edge(a.Pos, b.Pos, c.Pos);
            if (Math.Abs(area) < 0.00001f) return;
            if (area < 0) { (b, c) = (c, b); area = -area; }
            var minX = Math.Max(0, (int)MathF.Ceiling(Math.Max(clip.X, Math.Min(a.Pos.X, Math.Min(b.Pos.X, c.Pos.X))) - 0.5f));
            var minY = Math.Max(0, (int)MathF.Ceiling(Math.Max(clip.Y, Math.Min(a.Pos.Y, Math.Min(b.Pos.Y, c.Pos.Y))) - 0.5f));
            var maxX = Math.Min(width, (int)MathF.Ceiling(Math.Min(clip.Z, Math.Max(a.Pos.X, Math.Max(b.Pos.X, c.Pos.X))) - 0.5f));
            var maxY = Math.Min(height, (int)MathF.Ceiling(Math.Min(clip.W, Math.Max(a.Pos.Y, Math.Max(b.Pos.Y, c.Pos.Y))) - 0.5f));
            for (var y = minY; y < maxY; y++)
            for (var x = minX; x < maxX; x++)
            {
                var p = new Vector2(x + 0.5f, y + 0.5f);
                var ea = Edge(b.Pos, c.Pos, p); var eb = Edge(c.Pos, a.Pos, p); var ec = Edge(a.Pos, b.Pos, p);
                if (!Inside(ea, b.Pos, c.Pos) || !Inside(eb, c.Pos, a.Pos) || !Inside(ec, a.Pos, b.Pos)) continue;
                var wa = ea / area; var wb = eb / area; var wc = ec / area;
                var uv = a.Uv * wa + b.Uv * wb + c.Uv * wc;
                var texel = (Math.Clamp((int)(uv.Y * ah), 0, ah - 1) * aw + Math.Clamp((int)(uv.X * aw), 0, aw - 1)) * 4;
                var alpha = Channel(24) / 255 * atlas[texel + 3] / 255;
                var dest = (y * width + x) * 4;
                for (var channel = 0; channel < 3; channel++)
                    pixels[dest + 2 - channel] = (byte)Math.Clamp(Channel(channel * 8) * atlas[texel + channel] / 255 * alpha
                        + pixels[dest + 2 - channel] * (1 - alpha), 0, 255);
                float Channel(int shift) => ((a.Col >> shift) & 255) * wa + ((b.Col >> shift) & 255) * wb + ((c.Col >> shift) & 255) * wc;
            }
        }
    }
    private static float Edge(Vector2 a, Vector2 b, Vector2 p) => (b.X - a.X) * (p.Y - a.Y) - (b.Y - a.Y) * (p.X - a.X);
    private static bool Inside(float edge, Vector2 a, Vector2 b) => edge > 0 || (edge == 0 && (b.Y < a.Y || (b.Y == a.Y && b.X > a.X)));
}
