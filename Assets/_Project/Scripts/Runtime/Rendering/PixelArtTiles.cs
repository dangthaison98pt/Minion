using UnityEngine;

namespace HordeLab
{
    /// <summary>
    /// Tranh ô công trình nhìn từ trên xuống (32x32 pixel / ô) và bộ khung giao diện kiểu pixel.
    /// Mỗi ô = 1 nền (cỏ, đất, đá, ruộng) + vài vật thể ghép lại (nhà, cây, hàng rào, thùng...).
    /// </summary>
    public static partial class PixelArt
    {
        public const int TilePx = 32, TileCols = 8;
        public const int TileEmpty = B.Count, TileCandidate = B.Count + 1, TileCount = B.Count + 2;
        public const int TileRows = (TileCount + TileCols - 1) / TileCols;

        static readonly Color32 G1 = Rgb(84, 122, 62), G2 = Rgb(70, 104, 56), G3 = Rgb(108, 148, 72);
        static readonly Color32 D1 = Rgb(150, 114, 74), D2 = Rgb(126, 92, 62), D3 = Rgb(170, 134, 90);
        static readonly Color32 S1 = Rgb(128, 126, 138), S2 = Rgb(98, 96, 110), S3 = Rgb(164, 162, 174);
        static readonly Color32 Soil = Rgb(112, 80, 52), SoilDark = Rgb(90, 62, 42), Wheat = Rgb(236, 200, 84), WheatDark = Rgb(196, 152, 56);
        static readonly Color32 Water = Rgb(70, 150, 220), WaterLight = Rgb(150, 208, 244);
        static readonly Color32 Pine = Rgb(40, 88, 60), PineLight = Rgb(62, 120, 74), Trunk = Rgb(92, 62, 40);
        static readonly Color32 RoofRed = Rgb(176, 66, 52), RoofRedD = Rgb(128, 44, 40), RoofBlue = Rgb(66, 104, 176), RoofBlueD = Rgb(44, 72, 128);
        static readonly Color32 RoofBrown = Rgb(150, 100, 58), RoofBrownD = Rgb(108, 70, 44), RoofOrange = Rgb(214, 132, 52), RoofOrangeD = Rgb(160, 92, 40);
        static readonly Color32 RoofGreen = Rgb(74, 130, 84), RoofGreenD = Rgb(48, 92, 62), RoofDark = Rgb(74, 70, 86), RoofDarkD = Rgb(50, 48, 62);
        static readonly Color32 WallC = Rgb(216, 198, 162), WallStone = Rgb(150, 148, 160), Glow = Rgb(255, 196, 70);

        public static Texture2D BuildTileAtlas() => BuildTileCanvas().ToTexture("TileAtlas");

        static Canvas BuildTileCanvas()
        {
            var atlas = new Canvas(TilePx * TileCols, TilePx * TileRows);
            for (int i = 0; i < TileCount; i++)
            {
                var t = DrawTile(i);
                EdgeShade(t);
                atlas.Blit(t, (i % TileCols) * TilePx, (i / TileCols) * TilePx);
            }
            return atlas;
        }

        // ------------------------------------------------------------------
        // Viên gạch dựng hình
        // ------------------------------------------------------------------
        static Canvas Ground(Color32 a, Color32 b, Color32 c, int seed, int density = 9)
        {
            var t = new Canvas(TilePx, TilePx);
            for (int y = 0; y < TilePx; y++)
                for (int x = 0; x < TilePx; x++)
                {
                    uint h = IHash(x + seed * 131, y + seed * 17) % 100;
                    t.Set(x, y, h < density ? b : h < density + 4 ? c : a);
                }
            return t;
        }

        static Canvas Grass(int seed) => Ground(G1, G2, G3, seed);
        static Canvas Dirt(int seed) => Ground(D1, D2, D3, seed);
        static Canvas Stone(int seed)
        {
            var t = Ground(S1, S2, S3, seed, 6);
            for (int y = 0; y < TilePx; y += 6)                       // lát đá: mạch vữa so le
                for (int x = 0; x < TilePx; x++)
                {
                    t.Set(x, y, S2);
                    if ((x + (y / 6 % 2) * 4) % 8 == 0) for (int k = 1; k < 6; k++) t.Set(x, y + k, S2);
                }
            return t;
        }

        static void EdgeShade(Canvas t)
        {
            for (int i = 0; i < TilePx; i++)
            {
                Shade(t, i, 0); Shade(t, i, TilePx - 1); Shade(t, 0, i); Shade(t, TilePx - 1, i);
            }
        }

        static void Shade(Canvas t, int x, int y, float k = 0.72f)
        {
            var p = t.Get(x, y);
            if (p.a == 0) return;
            t.Set(x, y, new Color32((byte)(p.r * k), (byte)(p.g * k), (byte)(p.b * k), 255));
        }

        static void Shadow(Canvas t, int x0, int y0, int x1, int y1)
        {
            for (int y = y0; y <= y1; y++) for (int x = x0; x <= x1; x++) Shade(t, x, y, 0.7f);
        }

        /// <summary>Nhà nhìn chếch từ trên: mái 2 dốc chiếm phần trên, tường + cửa ở dưới.</summary>
        static void House(Canvas t, int x, int y, int w, int h, Color32 roof, Color32 roofDark, Color32 wall, bool chimney = false)
        {
            Shadow(t, x + 1, y + h, x + w, y + h + 1);
            Box(t, x - 1, y - 1, x + w, y + h, Outline);
            int roofH = h - 3, ridge = y + roofH / 2 - 1;
            for (int yy = y; yy < y + roofH; yy++)
                for (int xx = x; xx < x + w; xx++)
                    t.Set(xx, yy, yy == ridge ? Lighten(roof) : yy > ridge ? ((xx + yy) % 4 == 0 ? roof : roofDark) : ((xx + yy * 2) % 5 == 0 ? roofDark : roof));
            Box(t, x, y + roofH, x + w - 1, y + h - 1, wall);
            for (int xx = x; xx < x + w; xx++) t.Set(xx, y + roofH, Darken1(wall));     // bóng mái hiên
            int dx = x + w / 2 - 1;
            Box(t, dx, y + h - 2, dx + 1, y + h - 1, Rgb(70, 46, 34));                    // cửa
            if (w >= 9) { t.Set(x + 1, y + h - 2, Glow); t.Set(x + w - 2, y + h - 2, Glow); }   // cửa sổ sáng đèn
            if (chimney) { Box(t, x + w - 3, y - 2, x + w - 2, y, S2); t.Set(x + w - 3, y - 3, S3); t.Set(x + w - 2, y - 4, S3); }
        }

        static Color32 Lighten(Color32 c) => new Color32((byte)Mathf.Min(255, c.r + 36), (byte)Mathf.Min(255, c.g + 36), (byte)Mathf.Min(255, c.b + 36), 255);
        static Color32 Darken1(Color32 c) => new Color32((byte)(c.r * 0.75f), (byte)(c.g * 0.75f), (byte)(c.b * 0.75f), 255);

        /// <summary>Cây thông nhỏ, gốc ở (x, y).</summary>
        static void Tree(Canvas t, int x, int y)
        {
            Shade(t, x + 1, y + 1, 0.7f); Shade(t, x + 2, y + 1, 0.7f);
            t.Set(x, y, Trunk); t.Set(x, y - 1, Trunk);
            int[] half = { 3, 2, 2, 1, 1, 0 };
            for (int i = 0; i < half.Length; i++)
                for (int dx = -half[i]; dx <= half[i]; dx++)
                    t.Set(x + dx, y - 2 - i, dx < 0 ? PineLight : Pine);
            t.Set(x - half[0] - 0, y - 2, Outline); t.Set(x + half[0], y - 2, Outline);
        }

        static void Bush(Canvas t, int x, int y, Color32 a, Color32 b)
        {
            Box(t, x, y, x + 2, y + 1, a); t.Set(x + 1, y - 1, a); t.Set(x, y, b); t.Set(x + 1, y - 1, b);
        }

        static void Disk(Canvas t, int cx, int cy, float r, Color32 main, Color32 dark)
        {
            for (int y = (int)(cy - r - 1); y <= cy + r + 1; y++)
                for (int x = (int)(cx - r - 1); x <= cx + r + 1; x++)
                {
                    float dx = x - cx, dy = y - cy, d = dx * dx + dy * dy;
                    if (d > (r + 0.8f) * (r + 0.8f)) continue;
                    t.Set(x, y, d > r * r ? Outline : (dx + dy > r * 0.5f ? dark : main));
                }
        }

        static void Barrel(Canvas t, int x, int y)
        {
            Box(t, x - 1, y - 1, x + 3, y + 4, Outline);
            Box(t, x, y, x + 2, y + 3, Wood); t.Set(x, y, HorseLight); t.Set(x, y + 1, Leather); t.Set(x + 1, y + 1, Leather); t.Set(x + 2, y + 1, Leather); t.Set(x + 2, y + 3, Leather);
        }

        static void Crate(Canvas t, int x, int y)
        {
            Box(t, x - 1, y - 1, x + 4, y + 4, Outline);
            Box(t, x, y, x + 3, y + 3, HorseLight); Box(t, x, y, x + 3, y, Wood); Box(t, x, y + 3, x + 3, y + 3, Leather);
            t.Set(x + 1, y + 1, Wood); t.Set(x + 2, y + 2, Wood);
        }

        static void FenceH(Canvas t, int x0, int x1, int y)
        {
            for (int x = x0; x <= x1; x++) { t.Set(x, y, Wood); if ((x - x0) % 4 == 0) { t.Set(x, y - 1, HorseLight); t.Set(x, y + 1, Leather); } }
        }

        static void FenceV(Canvas t, int x, int y0, int y1)
        {
            for (int y = y0; y <= y1; y++) { t.Set(x, y, Wood); if ((y - y0) % 4 == 0) t.Set(x, y, HorseLight); }
        }

        static void Flag(Canvas t, int x, int y, Color32 c, int len = 7)
        {
            for (int k = 0; k <= len; k++) t.Set(x, y - k, Leather);
            Box(t, x + 1, y - len, x + 4, y - len + 2, c); t.Set(x + 4, y - len + 1, Darken1(c)); t.Set(x + 5, y - len, c);
        }

        static void Logs(Canvas t, int x, int y)
        {
            Box(t, x - 1, y - 1, x + 7, y + 4, Outline);
            for (int r = 0; r < 2; r++) { Box(t, x, y + r * 2, x + 6, y + r * 2 + 1, Wood); Box(t, x, y + r * 2, x + 6, y + r * 2, HorseLight); t.Set(x + 6, y + r * 2, Rgb(226, 190, 130)); t.Set(x + 6, y + r * 2 + 1, Rgb(226, 190, 130)); }
        }

        static void Stump(Canvas t, int x, int y)
        {
            Box(t, x, y, x + 2, y + 1, Trunk); Box(t, x, y, x + 2, y, Rgb(226, 190, 130)); t.Set(x + 1, y, Wood);
        }

        static void Rock(Canvas t, int x, int y, int w, int h)
        {
            Box(t, x - 1, y, x + w, y + h, Outline); Box(t, x, y - 1, x + w - 1, y - 1, Outline);
            Box(t, x, y, x + w - 1, y + h - 1, S1); Box(t, x, y, x + w - 2, y, S3); Box(t, x + w - 2, y + 1, x + w - 1, y + h - 1, S2);
        }

        static void PathH(Canvas t, int y0, int y1)
        {
            for (int y = y0; y <= y1; y++)
                for (int x = 0; x < TilePx; x++)
                    t.Set(x, y, IHash(x, y) % 7 == 0 ? D2 : (y == y0 || y == y1) && IHash(x, y + 3) % 3 == 0 ? G2 : D1);
        }

        static void Patch(Canvas t, int cx, int cy, float rx, float ry, Color32 a, Color32 b)
        {
            for (int y = 0; y < TilePx; y++)
                for (int x = 0; x < TilePx; x++)
                {
                    float dx = (x - cx) / rx, dy = (y - cy) / ry;
                    if (dx * dx + dy * dy > 1f + (IHash(x, y) % 10) * 0.02f) continue;
                    t.Set(x, y, IHash(x + 9, y) % 6 == 0 ? b : a);
                }
        }

        // ------------------------------------------------------------------
        // Từng ô
        // ------------------------------------------------------------------
        static Canvas DrawTile(int id)
        {
            Canvas t;
            switch (id)
            {
                case B.Field:
                    t = Ground(Soil, SoilDark, Soil, 3);
                    for (int y = 3; y < 30; y += 4)
                        for (int x = 2; x < 30; x++)
                        {
                            t.Set(x, y, (x + y) % 3 == 0 ? WheatDark : Wheat);
                            if ((x * 7 + y) % 4 != 0) t.Set(x, y - 1, Wheat);
                            t.Set(x, y + 1, SoilDark);
                        }
                    t.Set(24, 9, Leather); t.Set(24, 10, Leather); t.Set(24, 11, Leather); Box(t, 22, 8, 26, 8, Leather); Box(t, 23, 6, 25, 7, Cloth); t.Set(24, 5, Wheat);   // bù nhìn
                    return t;

                case B.Barn:
                    t = Grass(5);
                    Patch(t, 20, 22, 10, 7, D1, D2);
                    FenceH(t, 9, 30, 13); FenceH(t, 9, 30, 30); FenceV(t, 9, 13, 30); FenceV(t, 30, 13, 30);
                    House(t, 2, 3, 13, 10, RoofRed, RoofRedD, WallC);
                    Cow(t, 14, 18); Cow(t, 22, 23); Cow(t, 13, 25);
                    return t;

                case B.HunterLodge:
                    t = Grass(7);
                    Tree(t, 5, 12); Tree(t, 26, 11); Tree(t, 28, 28); Tree(t, 4, 29);
                    // lều da thú
                    for (int i = 0; i < 9; i++) Box(t, 16 - i, 8 + i, 16 + i, 8 + i, i % 3 == 2 ? FurBrownDark : FurBrown);
                    for (int i = 0; i < 9; i++) { t.Set(16 - i - 1, 8 + i, Outline); t.Set(16 + i + 1, 8 + i, Outline); }
                    Box(t, 15, 13, 17, 16, Outline); t.Set(16, 6, Leather); t.Set(15, 7, Leather); t.Set(17, 7, Leather);
                    // giá phơi da + đống lửa
                    Box(t, 20, 22, 26, 22, Leather); t.Set(20, 23, Leather); t.Set(26, 23, Leather); Box(t, 22, 23, 24, 25, FurBrownLight);
                    Box(t, 9, 23, 11, 24, S2); t.Set(10, 22, Glow); t.Set(10, 21, Rgb(255, 142, 30));
                    return t;

                case B.LumberCamp:
                    t = Grass(9);
                    Tree(t, 5, 11); Tree(t, 12, 9); Tree(t, 27, 12);
                    Stump(t, 4, 18); Stump(t, 11, 22); Stump(t, 24, 17); Stump(t, 18, 14);
                    Logs(t, 4, 26); Logs(t, 14, 26);
                    House(t, 20, 19, 9, 9, RoofBrown, RoofBrownD, Wood);
                    t.Set(14, 17, Steel); t.Set(15, 18, Wood); t.Set(16, 19, Wood);   // rìu cắm gốc
                    return t;

                case B.Sawmill:
                    t = Dirt(11);
                    House(t, 3, 4, 17, 13, RoofBrown, RoofBrownD, HorseLight, true);
                    Disk(t, 25, 12, 3.2f, Steel, SteelDark); t.Set(25, 12, Outline);          // lưỡi cưa tròn
                    Box(t, 21, 16, 29, 17, Wood);
                    for (int i = 0; i < 4; i++) { Box(t, 4 + i * 6, 22 + (i % 2), 8 + i * 6, 23 + (i % 2), Rgb(226, 190, 130)); Box(t, 4 + i * 6, 24 + (i % 2), 8 + i * 6, 24 + (i % 2), Wood); }
                    Logs(t, 20, 26);
                    return t;

                case B.Nursery:
                    t = Ground(Soil, SoilDark, Soil, 13);
                    for (int y = 0; y < TilePx; y++) for (int x = 0; x < TilePx; x++) if (x < 2 || y < 2 || x > 29 || y > 29) t.Set(x, y, IHash(x, y) % 5 == 0 ? G2 : G1);
                    for (int r = 0; r < 4; r++)
                        for (int c = 0; c < 4; c++)
                        {
                            int x = 6 + c * 7, y = 8 + r * 7, size = (r + c) % 3;
                            t.Set(x, y, Trunk);
                            t.Set(x, y - 1, PineLight); if (size > 0) { t.Set(x - 1, y - 1, Pine); t.Set(x + 1, y - 1, Pine); t.Set(x, y - 2, PineLight); }
                            if (size > 1) { t.Set(x, y - 3, Pine); t.Set(x - 1, y - 2, PineLight); t.Set(x + 1, y - 2, Pine); }
                        }
                    return t;

                case B.GoldMine:
                    t = Dirt(15);
                    Patch(t, 16, 12, 14, 10, S1, S2);
                    Patch(t, 14, 9, 8, 5, S3, S1);
                    Box(t, 11, 12, 20, 21, Outline); Box(t, 12, 13, 19, 21, Rgb(30, 24, 30));  // cửa hầm
                    Box(t, 11, 11, 20, 12, Wood); Box(t, 11, 12, 11, 21, Wood); Box(t, 20, 12, 20, 21, Wood);
                    for (int y = 22; y < 31; y++) { t.Set(14, y, S2); t.Set(17, y, S2); if (y % 2 == 0) Box(t, 14, y, 17, y, Leather); }   // đường ray
                    t.Set(6, 8, Gold); t.Set(24, 7, Gold); t.Set(25, 14, Gold); t.Set(7, 16, Gold); t.Set(23, 10, Glow);
                    Box(t, 23, 23, 28, 27, Outline); Box(t, 24, 24, 27, 26, Leather); Box(t, 24, 23, 27, 23, Gold); t.Set(25, 22, Glow);       // xe goòng đầy vàng
                    return t;

                case B.Market:
                    t = Stone(17);
                    Stall(t, 3, 4, Cloth, White); Stall(t, 18, 4, Tunic, White); Stall(t, 3, 18, Rgb(214, 160, 50), White); Stall(t, 18, 18, ElfGreen, White);
                    Crate(t, 14, 14); t.Set(15, 30, Gold);
                    return t;

                case B.TownHall:
                    t = Stone(19);
                    House(t, 4, 8, 24, 17, RoofBlue, RoofBlueD, WallStone);
                    Box(t, 12, 2, 20, 12, Outline); Box(t, 13, 3, 19, 11, WallStone); Box(t, 13, 3, 19, 5, RoofBlue); Box(t, 13, 6, 19, 6, RoofBlueD);   // tháp
                    Disk(t, 16, 9, 1.6f, White, Steel);                                        // đồng hồ
                    Flag(t, 16, 2, Gold, 3);
                    Box(t, 14, 26, 18, 30, D1);
                    return t;

                case B.Cabin:
                    t = Grass(21);
                    PathH(t, 26, 28);
                    House(t, 8, 7, 15, 13, RoofBrown, RoofBrownD, Wood, true);
                    Bush(t, 3, 20, PineLight, G3); Bush(t, 26, 21, PineLight, G3); Tree(t, 28, 12);
                    t.Set(5, 12, Cloth); t.Set(6, 14, Wheat); t.Set(4, 15, White);            // hoa
                    return t;

                case B.Residential:
                    t = Grass(23);
                    PathH(t, 14, 16);
                    House(t, 2, 2, 12, 10, RoofRed, RoofRedD, WallC);
                    House(t, 18, 2, 12, 10, RoofBlue, RoofBlueD, WallC, true);
                    House(t, 2, 19, 12, 10, RoofOrange, RoofOrangeD, WallC, true);
                    House(t, 18, 19, 12, 10, RoofGreen, RoofGreenD, WallC);
                    return t;

                case B.Tavern:
                    t = Dirt(25);
                    House(t, 5, 4, 20, 15, RoofOrange, RoofOrangeD, WallC, true);
                    Barrel(t, 4, 23); Barrel(t, 9, 24); Barrel(t, 24, 22);
                    Box(t, 14, 23, 20, 24, Wood); t.Set(14, 25, Leather); t.Set(20, 25, Leather); t.Set(16, 22, Glow); t.Set(18, 22, Wheat);   // bàn + cốc bia
                    Box(t, 26, 9, 26, 15, Leather); Box(t, 27, 10, 30, 13, Wood); t.Set(28, 11, Glow); t.Set(29, 12, Glow);                     // biển hiệu
                    return t;

                case B.Windmill:
                    t = Grass(27);
                    Patch(t, 16, 26, 9, 4, Wheat, WheatDark);
                    Disk(t, 16, 16, 6f, WallC, RobeDark);
                    Disk(t, 16, 16, 3.5f, RoofRed, RoofRedD);
                    for (int k = 2; k <= 13; k++)                                             // 4 cánh quạt chéo
                    {
                        t.Set(16 + k, 16 - k, Wood); t.Set(16 - k, 16 + k, Wood); t.Set(16 - k, 16 - k, Wood); t.Set(16 + k, 16 + k, Wood);
                        if (k > 5) { t.Set(16 + k, 17 - k, White); t.Set(16 - k, 15 + k, White); t.Set(17 - k, 16 - k, White); t.Set(15 + k, 16 + k, White); }
                    }
                    t.Set(16, 16, Outline);
                    return t;

                case B.Well:
                    t = Grass(29);
                    PathH(t, 15, 17);
                    Patch(t, 16, 16, 9, 9, D1, D2);
                    Disk(t, 16, 16, 6f, S1, S2);
                    Disk(t, 16, 16, 3.4f, Water, Rgb(44, 110, 180)); t.Set(15, 15, WaterLight);
                    Box(t, 9, 8, 9, 16, Leather); Box(t, 23, 8, 23, 16, Leather); Box(t, 8, 7, 24, 8, RoofBrown); Box(t, 8, 6, 24, 6, Outline);   // mái che
                    Box(t, 4, 24, 6, 26, Wood); t.Set(5, 24, Water);                           // xô nước
                    return t;

                case B.RangerHut:
                    t = Grass(31);
                    Tree(t, 4, 10); Tree(t, 10, 8); Tree(t, 27, 9); Tree(t, 22, 7); Tree(t, 3, 22); Tree(t, 28, 21); Tree(t, 5, 30); Tree(t, 26, 30);
                    House(t, 10, 13, 12, 11, RoofGreen, RoofGreenD, Wood, true);
                    return t;

                case B.Mint:
                    t = Stone(33);
                    House(t, 4, 4, 24, 16, RoofDark, RoofDarkD, WallStone, true);
                    Disk(t, 16, 9, 3.2f, Gold, Rgb(196, 140, 30)); t.Set(15, 8, Glow);         // huy hiệu đồng tiền
                    for (int i = 0; i < 4; i++) Disk(t, 6 + i * 7, 26 + (i % 2), 2f, Gold, Rgb(196, 140, 30));
                    t.Set(26, 3, Rgb(255, 142, 30)); t.Set(25, 1, S3);
                    return t;

                case B.TradeRoad:
                    t = Grass(35);
                    PathH(t, 11, 21);
                    for (int x = 2; x < 30; x += 6) { t.Set(x, 16, D3); t.Set(x + 1, 16, D3); }
                    // xe hàng
                    Box(t, 11, 12, 21, 18, Outline); Box(t, 12, 13, 20, 17, Wood); Box(t, 12, 13, 20, 13, HorseLight);
                    Box(t, 13, 14, 15, 16, Cloth); Box(t, 17, 14, 19, 16, Gold);
                    Disk(t, 13, 19, 1.6f, Leather, Outline); Disk(t, 19, 19, 1.6f, Leather, Outline);
                    Box(t, 22, 15, 26, 15, Leather);
                    Box(t, 4, 4, 4, 9, Leather); Box(t, 5, 4, 8, 6, HorseLight);              // biển chỉ đường
                    Tree(t, 27, 9); Bush(t, 5, 26, PineLight, G3); Bush(t, 22, 27, PineLight, G3);
                    return t;

                case B.Plaza:
                    t = Stone(37);
                    Disk(t, 16, 16, 7f, S3, S1);
                    Disk(t, 16, 16, 4.6f, Water, Rgb(44, 110, 180));
                    Disk(t, 16, 16, 1.4f, S3, S2); t.Set(16, 14, WaterLight); t.Set(14, 16, WaterLight); t.Set(18, 17, WaterLight);
                    Box(t, 3, 4, 8, 5, Wood); Box(t, 23, 4, 28, 5, Wood); Box(t, 3, 27, 8, 28, Wood); Box(t, 23, 27, 28, 28, Wood);   // ghế
                    Bush(t, 3, 14, PineLight, G3); Bush(t, 26, 15, PineLight, G3);
                    return t;

                case B.Forge:
                    t = Dirt(39);
                    House(t, 3, 3, 17, 14, RoofDark, RoofDarkD, WallStone, true);
                    Box(t, 9, 14, 12, 16, Rgb(255, 142, 30)); t.Set(10, 15, Glow); t.Set(11, 14, Rgb(255, 250, 210));   // lò lửa
                    // đe
                    Box(t, 21, 20, 28, 21, Outline); Box(t, 22, 19, 27, 20, SteelDark); Box(t, 22, 19, 27, 19, Steel); Box(t, 24, 21, 25, 23, SteelDark); Box(t, 23, 24, 26, 24, Outline);
                    t.Set(28, 17, Wood); t.Set(27, 16, Wood); Box(t, 25, 14, 26, 15, Steel);                             // búa
                    Box(t, 4, 23, 10, 28, Outline); Box(t, 5, 24, 9, 27, Rgb(50, 46, 54)); t.Set(6, 25, Rgb(255, 96, 10)); t.Set(8, 26, Rgb(255, 96, 10));   // than
                    t.Set(15, 24, Steel); t.Set(15, 25, Steel); t.Set(15, 26, Steel); t.Set(15, 27, Gold); t.Set(14, 27, Gold); t.Set(16, 27, Gold);          // kiếm mới rèn
                    return t;

                case B.TrainingGround:
                    t = Ground(Rgb(196, 170, 118), Rgb(172, 146, 100), Rgb(214, 190, 138), 41);
                    FenceH(t, 1, 30, 2); FenceH(t, 1, 30, 29);
                    Dummy(t, 8, 14); Dummy(t, 22, 12); Dummy(t, 15, 23);
                    Box(t, 25, 21, 29, 21, Leather); t.Set(26, 22, Steel); t.Set(26, 23, Steel); t.Set(28, 22, Steel); t.Set(28, 23, Steel); t.Set(27, 24, Wood);   // giá vũ khí
                    Disk(t, 5, 24, 2.4f, White, Cloth); t.Set(5, 24, Cloth);                   // bia tập bắn
                    return t;

                case B.Depot:
                    t = Dirt(43);
                    House(t, 3, 3, 15, 12, RoofBrown, RoofBrownD, HorseLight);
                    Crate(t, 21, 5); Crate(t, 25, 9); Crate(t, 20, 11); Barrel(t, 4, 20); Barrel(t, 9, 21);
                    Box(t, 14, 19, 28, 28, Outline); Box(t, 15, 20, 27, 27, Rgb(196, 186, 150)); Box(t, 15, 20, 27, 21, White);   // bạt phủ hàng
                    for (int x = 16; x < 27; x += 3) Box(t, x, 22, x, 27, RobeDark);
                    return t;

                case B.Banner:
                    t = Stone(45);
                    Disk(t, 16, 22, 5f, S3, S2);
                    Box(t, 16, 3, 16, 22, Leather);
                    Box(t, 17, 4, 26, 13, Cloth); Box(t, 17, 4, 26, 4, Lighten(Cloth)); Box(t, 24, 5, 26, 13, ClothDark);
                    Box(t, 20, 7, 22, 10, Gold); t.Set(21, 6, Gold); t.Set(21, 11, Gold);
                    t.Set(17, 14, Cloth); t.Set(19, 14, Cloth); t.Set(21, 14, Cloth); t.Set(23, 14, ClothDark); t.Set(25, 14, ClothDark);
                    t.Set(16, 2, Gold);
                    Flag(t, 5, 16, Tunic, 6); Flag(t, 5, 29, Tunic, 6);
                    return t;

                case B.Infirmary:
                    t = Grass(47);
                    Patch(t, 16, 18, 13, 10, D1, D2);
                    Box(t, 4, 7, 27, 22, Outline); Box(t, 5, 8, 26, 21, White);
                    for (int x = 5; x <= 26; x++) { t.Set(x, 14, RobeDark); if (x % 7 == 5) Box(t, x, 8, x, 21, Robe); }
                    Box(t, 14, 9, 17, 13, White); Box(t, 15, 9, 16, 13, Cloth); Box(t, 13, 10, 18, 12, Cloth); Box(t, 15, 10, 16, 12, Cloth);   // chữ thập đỏ
                    Box(t, 14, 18, 17, 21, Rgb(70, 46, 34));
                    Box(t, 6, 25, 11, 27, White); t.Set(6, 26, Cloth); Box(t, 20, 25, 25, 27, White); t.Set(20, 26, Cloth);                    // cáng
                    return t;

                case B.Expand:
                    t = Grass(49);
                    Tree(t, 6, 12); Tree(t, 26, 27); Bush(t, 22, 6, PineLight, G3);
                    for (int i = 3; i < 29; i += 3) { t.Set(i, 3, White); t.Set(i, 28, White); t.Set(3, i, White); t.Set(28, i, White); }
                    Box(t, 15, 10, 16, 21, White); Box(t, 10, 15, 21, 16, White);
                    return t;

                case TileEmpty:
                    t = Grass(51);
                    Bush(t, 5, 7, G2, G3); Bush(t, 22, 23, G2, G3); t.Set(25, 8, Wheat); t.Set(9, 24, White);
                    return t;

                case TileCandidate:
                    t = Ground(Rgb(52, 76, 50), Rgb(44, 64, 44), Rgb(60, 88, 56), 53);
                    Tree(t, 8, 14); Tree(t, 22, 12); Tree(t, 14, 26); Tree(t, 26, 28); Tree(t, 4, 28);
                    for (int y = 0; y < TilePx; y++) for (int x = 0; x < TilePx; x++) Shade(t, x, y, 0.8f);
                    return t;
            }
            return DrawBarracksTile(id - B.Barracks);
        }

        static void Cow(Canvas t, int x, int y)
        {
            Box(t, x - 1, y - 1, x + 5, y + 3, Outline);
            Box(t, x, y, x + 4, y + 2, White); t.Set(x + 1, y, FurBrownDark); t.Set(x + 2, y + 1, FurBrownDark); t.Set(x + 4, y, FurBrown); t.Set(x + 5, y, FurBrown);
        }

        static void Stall(Canvas t, int x, int y, Color32 a, Color32 b)
        {
            Shadow(t, x + 1, y + 9, x + 11, y + 10);
            Box(t, x - 1, y - 1, x + 11, y + 9, Outline);
            for (int xx = 0; xx <= 10; xx++) Box(t, x + xx, y, x + xx, y + 4, (xx / 2) % 2 == 0 ? a : b);   // mái bạt sọc
            Box(t, x, y + 4, x + 10, y + 4, Darken1(a));
            Box(t, x, y + 5, x + 10, y + 8, Wood);
            t.Set(x + 2, y + 6, Cloth); t.Set(x + 4, y + 6, Wheat); t.Set(x + 6, y + 6, LeafLight); t.Set(x + 8, y + 6, Gold); t.Set(x + 5, y + 7, Rgb(226, 190, 130));
        }

        static void Dummy(Canvas t, int x, int y)
        {
            Shade(t, x + 1, y + 4, 0.7f); Shade(t, x + 2, y + 4, 0.7f);
            Box(t, x, y - 2, x, y + 3, Leather); Box(t, x - 2, y - 1, x + 2, y - 1, Leather);
            Box(t, x - 1, y - 4, x + 1, y - 3, Wheat); Box(t, x - 1, y, x + 1, y + 1, Wheat); t.Set(x, y, Cloth);
        }

        /// <summary>Nhà lính: mỗi phe 1 kiểu doanh trại, phía trước có 1 lính của loại quân đó đứng gác.</summary>
        static Canvas DrawBarracksTile(int unit)
        {
            int f = Faction.Of(unit);
            Canvas t;
            switch (f)
            {
                case Faction.Human:
                    t = Stone(60 + unit);
                    House(t, 3, 5, 18, 14, RoofBlue, RoofBlueD, WallStone);
                    for (int x = 3; x < 21; x += 4) Box(t, x, 3, x + 1, 4, WallStone);        // lỗ châu mai
                    Flag(t, 24, 12, Tunic, 9);
                    break;
                case Faction.Elf:
                    t = Grass(60 + unit);
                    Disk(t, 12, 12, 10f, Leaf, LeafDark);
                    for (int i = 0; i < 26; i++) { int x = 3 + (int)(IHash(i, 7) % 19), y = 3 + (int)(IHash(i, 11) % 19); float dx = x - 12, dy = y - 12; if (dx * dx + dy * dy < 80) t.Set(x, y, LeafLight); }
                    Disk(t, 12, 13, 4f, Wood, Leather); Box(t, 11, 13, 13, 16, Rgb(70, 46, 34));   // nhà trên cây
                    t.Set(6, 8, Glow); t.Set(18, 9, Glow); t.Set(9, 19, Glow);                    // đèn lồng
                    Tree(t, 27, 12);
                    break;
                case Faction.Dwarf:
                    t = Stone(60 + unit);
                    Patch(t, 12, 10, 13, 9, S1, S2);
                    House(t, 3, 5, 18, 14, RoofDark, RoofDarkD, WallStone, true);
                    Box(t, 9, 14, 14, 18, Outline); Box(t, 10, 15, 13, 18, Rgb(255, 142, 30)); t.Set(11, 16, Glow);   // cổng lò rực lửa
                    Flag(t, 25, 12, DwarfTunic, 9);
                    break;
                case Faction.Beast:
                    t = Dirt(60 + unit);
                    for (int i = 0; i < 12; i++) Box(t, 12 - i, 4 + i, 12 + i, 4 + i, i % 4 == 3 ? FurBrownDark : (i % 4 == 1 ? FurBrownLight : FurBrown));
                    for (int i = 0; i < 12; i++) { t.Set(12 - i - 1, 4 + i, Outline); t.Set(12 + i + 1, 4 + i, Outline); }
                    Box(t, 10, 11, 14, 15, Outline);
                    t.Set(12, 2, Bone); t.Set(11, 3, Bone); t.Set(13, 3, Bone);
                    Box(t, 26, 4, 27, 14, Wood); Box(t, 25, 5, 28, 6, Cloth); Box(t, 25, 9, 28, 10, Bone); t.Set(26, 3, Bone); t.Set(27, 3, Bone);   // cột totem
                    t.Set(4, 20, Bone); t.Set(5, 21, Bone); t.Set(6, 20, BoneDark);
                    break;
                default:
                    t = Ground(Rgb(48, 40, 62), Rgb(36, 30, 50), Rgb(62, 50, 80), 60 + unit);
                    for (int i = 0; i < 18; i++)                                               // khe nứt phát sáng
                    {
                        int x = 4 + i, y = 13 + (int)(IHash(i, 3) % 5) - 2;
                        Box(t, x, y - 1, x, y + 1, Void2); t.Set(x, y, i % 3 == 0 ? VoidEye : VoidGlow);
                    }
                    for (int k = 0; k < 3; k++)                                                // tinh thể
                    {
                        int x = 6 + k * 7, h = 6 + k % 2 * 3;
                        Box(t, x - 1, 10 - h - 1, x + 2, 10, Outline); Box(t, x, 10 - h, x + 1, 9, Void1); Box(t, x, 10 - h, x, 9, VoidGlow);
                    }
                    t.Set(25, 6, VoidEye); t.Set(27, 14, VoidGlow); t.Set(3, 22, VoidGlow);
                    break;
            }

            // khoảnh đất phía trước + lính gác
            var guard = DrawUnit(unit, 0, 0);
            bool infantry = IsInfantry(guard);                                                  // lính bộ có sẵn 1 pixel viền trống quanh hình
            int gx = TilePx - guard.W - (infantry ? 2 : 1), gy = TilePx - guard.H - 1;
            if (f != Faction.Void) Patch(t, gx + guard.W / 2, gy + guard.H - (infantry ? 4 : 3), (infantry ? InfantryPx : guard.W) * 0.55f, 3.5f, D1, D2);
            t.Blit(guard, gx, gy);
            if (infantry)                                                        // lính bộ: thêm 1 người nữa cho ra dáng tiểu đội
            {
                var second = DrawUnit(unit, 0, 1);
                t.Blit(second, gx - 9, gy + 1);
            }
            return t;
        }

        // ------------------------------------------------------------------
        // Khung giao diện kiểu pixel (vẽ nhỏ rồi phóng x3 cho ra hạt pixel to)
        // ------------------------------------------------------------------
        public const int UiScale = 3, UiBorder = 4 * UiScale;

        static Texture2D Upscaled(Canvas c, string name)
        {
            var big = new Canvas(c.W * UiScale, c.H * UiScale);
            for (int y = 0; y < big.H; y++) for (int x = 0; x < big.W; x++) big.Set(x, y, c.Get(x / UiScale, y / UiScale));
            return big.ToTexture(name);
        }

        static Canvas FrameCanvas(Color32 fill, Color32 rim, Color32 rimLight, Color32 rimDark)
        {
            var c = new Canvas(12, 12);
            Box(c, 0, 0, 11, 11, Outline);
            Box(c, 1, 1, 10, 10, rim);
            Box(c, 1, 1, 10, 1, rimLight); Box(c, 1, 1, 1, 10, rimLight);
            Box(c, 1, 10, 10, 10, rimDark); Box(c, 10, 1, 10, 10, rimDark);
            Box(c, 3, 3, 8, 8, Outline);
            Box(c, 4, 4, 7, 7, fill);
            // 4 góc bo: đinh tán
            c.Set(1, 1, rim); c.Set(10, 10, rim);
            c.Set(0, 0, default); c.Set(11, 0, default); c.Set(0, 11, default); c.Set(11, 11, default);
            return c;
        }

        /// <summary>Bảng nền: khung gỗ, ruột tối.</summary>
        public static Texture2D UiPanel() =>
            Upscaled(FrameCanvas(Rgb(34, 28, 36, 240), Rgb(128, 86, 52), Rgb(170, 122, 74), Rgb(88, 58, 40)), "UiPanel");

        /// <summary>Lá thẻ: khung vàng nhạt, ruột màu giấy sẫm.</summary>
        public static Texture2D UiCard() =>
            Upscaled(FrameCanvas(Rgb(58, 48, 50), Rgb(190, 154, 88), Rgb(236, 206, 132), Rgb(134, 100, 58)), "UiCard");

        /// <summary>Nút bấm: 0 = thường, 1 = rê chuột, 2 = đang nhấn.</summary>
        public static Texture2D UiButton(int state)
        {
            Color32 face = state == 2 ? Rgb(104, 70, 44) : state == 1 ? Rgb(176, 124, 72) : Rgb(146, 100, 60);
            var c = new Canvas(12, 12);
            Box(c, 0, 0, 11, 11, Outline);
            Box(c, 1, 1, 10, 10, face);
            if (state == 2) { Box(c, 1, 1, 10, 2, Rgb(74, 50, 34)); Box(c, 1, 1, 1, 10, Rgb(74, 50, 34)); }
            else { Box(c, 1, 1, 10, 1, Rgb(214, 164, 100)); Box(c, 1, 9, 10, 10, Rgb(96, 64, 42)); }
            c.Set(0, 0, default); c.Set(11, 0, default); c.Set(0, 11, default); c.Set(11, 11, default);
            return Upscaled(c, "UiButton" + state);
        }
    }
}
