using System.Collections.Generic;
using UnityEngine;

namespace HordeLab
{
    /// <summary>
    /// Logo chữ "FANTASY / WORLD WAR" vẽ bằng pixel: bộ chữ đậm 7x9 tự vẽ, tô chuyển màu, nổi khối 3D, viền tối,
    /// đường trang trí hình thoi giữa 2 dòng và vài đốm lấp lánh. Hiện ở menu chính (phóng nguyên lần, lọc Point).
    /// </summary>
    public static partial class PixelArt
    {
        const int GlyphW = 7, GlyphH = 9;

        // Chữ đậm nét 2 pixel. '#' = có pixel
        static readonly Dictionary<char, string[]> Glyphs = new Dictionary<char, string[]>
        {
            ['F'] = new[] { "#######", "#######", "##.....", "##.....", "######.", "######.", "##.....", "##.....", "##....." },
            ['A'] = new[] { ".#####.", "#######", "##...##", "##...##", "#######", "#######", "##...##", "##...##", "##...##" },
            ['N'] = new[] { "##...##", "###..##", "####.##", "#######", "##.####", "##..###", "##...##", "##...##", "##...##" },
            ['T'] = new[] { "#######", "#######", "..###..", "..###..", "..###..", "..###..", "..###..", "..###..", "..###.." },
            ['S'] = new[] { ".######", "#######", "##.....", "######.", ".######", ".....##", ".....##", "#######", "######." },
            ['Y'] = new[] { "##...##", "##...##", "##...##", ".#####.", "..###..", "..###..", "..###..", "..###..", "..###.." },
            ['W'] = new[] { "##...##", "##...##", "##...##", "##.#.##", "##.#.##", "#######", "#######", "###.###", "##...##" },
            ['O'] = new[] { ".#####.", "#######", "##...##", "##...##", "##...##", "##...##", "##...##", "#######", ".#####." },
            ['R'] = new[] { "######.", "#######", "##...##", "##...##", "######.", "#####..", "##.###.", "##..###", "##...##" },
            ['L'] = new[] { "##.....", "##.....", "##.....", "##.....", "##.....", "##.....", "##.....", "#######", "#######" },
            ['D'] = new[] { "######.", "#######", "##...##", "##...##", "##...##", "##...##", "##...##", "#######", "######." },
        };

        static int TextWidth(string s, int gap) { int w = 0; foreach (char ch in s) w += (ch == ' ' ? 4 : GlyphW) + gap; return w - gap; }

        /// <summary>Đặt chữ vào mặt nạ (true = có pixel) tại (x0, y0).</summary>
        static void StampText(bool[,] mask, string s, int x0, int y0, int gap)
        {
            int x = x0;
            foreach (char ch in s)
            {
                if (ch == ' ') { x += 4 + gap; continue; }
                var g = Glyphs[ch];
                for (int y = 0; y < GlyphH; y++)
                    for (int c = 0; c < GlyphW; c++)
                        if (g[y][c] == '#') mask[x + c, y0 + y] = true;
                x += GlyphW + gap;
            }
        }

        public static Canvas DrawLogo()
        {
            const string top = "FANTASY", bottom = "WORLD WAR";
            const int gapTop = 2, gapBot = 1, pad = 3;
            int wTop = TextWidth(top, gapTop), wBot = TextWidth(bottom, gapBot);
            int W = Mathf.Max(wTop, wBot) + pad * 2 + 2, H = pad + GlyphH + 6 + GlyphH + 3 + pad;
            var s = new Canvas(W, H);
            int yTop = pad, yBot = pad + GlyphH + 6;
            var mTop = new bool[W, H]; var mBot = new bool[W, H];
            StampText(mTop, top, (W - wTop) / 2, yTop, gapTop);
            StampText(mBot, bottom, (W - wBot) / 2, yBot, gapBot);

            // khối nổi 3D (tối) phía dưới-phải
            Color32 extTop = Rgb(46, 56, 104), extBot = Rgb(110, 40, 22), extBot2 = Rgb(70, 24, 16);
            for (int y = H - 1; y >= 0; y--)
                for (int x = W - 1; x >= 0; x--)
                {
                    if (mTop[x, y]) { s.Set(x + 1, y + 1, extTop); }
                    if (mBot[x, y]) { s.Set(x + 1, y + 2, extBot2); s.Set(x + 1, y + 1, extBot); s.Set(x, y + 2, extBot2); }
                }
            // mặt chữ: chuyển màu theo hàng trong chữ
            Color32[] topGrad = { Rgb(246, 250, 255), Rgb(226, 236, 255), Rgb(204, 218, 248), Rgb(184, 200, 240), Rgb(168, 186, 232), Rgb(150, 170, 222), Rgb(136, 156, 212), Rgb(122, 142, 200), Rgb(110, 128, 186) };
            Color32[] botGrad = { Rgb(255, 246, 190), Rgb(255, 232, 140), Rgb(252, 214, 96), Rgb(244, 192, 68), Rgb(236, 170, 52), Rgb(228, 148, 44), Rgb(216, 124, 38), Rgb(200, 102, 32), Rgb(178, 82, 28) };
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    if (mTop[x, y]) s.Set(x, y, topGrad[Mathf.Clamp(y - yTop, 0, GlyphH - 1)]);
                    if (mBot[x, y]) s.Set(x, y, botGrad[Mathf.Clamp(y - yBot, 0, GlyphH - 1)]);
                }
            // ánh sáng: pixel trên cùng-trái của mỗi nét sáng hơn
            Color32 shine = Rgb(255, 255, 236);
            for (int y = 1; y < H; y++)
                for (int x = 1; x < W; x++)
                    if (mBot[x, y] && !mBot[x, y - 1] && !mBot[x - 1, y] && y - yBot <= 1) s.Set(x, y, shine);

            // đường trang trí giữa 2 dòng: ─── ◆ ───
            int my = yTop + GlyphH + 3, cx = W / 2;
            Color32 orn = Rgb(226, 176, 64), ornD = Rgb(150, 100, 34), gem = Rgb(220, 48, 60), gemL = Rgb(255, 130, 130);
            for (int x = cx - 24; x <= cx + 24; x++)
            {
                if (System.Math.Abs(x - cx) <= 3) continue;
                s.Set(x, my, (x & 1) == 0 ? orn : ornD);
            }
            s.Set(cx - 25, my, ornD); s.Set(cx + 25, my, ornD);
            // viên ngọc hình thoi ở giữa
            s.Set(cx, my - 2, orn); s.Set(cx - 1, my - 1, orn); s.Set(cx, my - 1, gemL); s.Set(cx + 1, my - 1, orn);
            s.Set(cx - 2, my, orn); s.Set(cx - 1, my, gem); s.Set(cx, my, gem); s.Set(cx + 1, my, gem); s.Set(cx + 2, my, orn);
            s.Set(cx - 1, my + 1, orn); s.Set(cx, my + 1, gem); s.Set(cx + 1, my + 1, orn); s.Set(cx, my + 2, orn);

            AddOutline(s);
            // đốm lấp lánh (vẽ sau viền để nổi trên nền)
            Color32 sp = Rgb(255, 250, 210);
            void Sparkle(int x, int y) { s.Set(x, y, sp); s.Set(x - 1, y, Rgb(255, 214, 120)); s.Set(x + 1, y, Rgb(255, 214, 120)); s.Set(x, y - 1, Rgb(255, 214, 120)); s.Set(x, y + 1, Rgb(255, 214, 120)); }
            Sparkle(2, yBot + 1);
            Sparkle(W - 3, yTop + 2);
            return s;
        }

        public static Texture2D LogoTexture()
        {
            var t = DrawLogo().ToTexture("Logo");
            t.filterMode = FilterMode.Point;
            return t;
        }
    }
}
