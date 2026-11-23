using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace HordeLab
{
    /// <summary>
    /// Toàn bộ hình ảnh được "vẽ" bằng code lúc khởi động: lính 10x10 pixel, xác, máu, mặt đất...
    /// Không cần file ảnh nào. Muốn thay bằng sprite của bạn thì chỉ cần thay các Texture2D trả về ở đây.
    /// </summary>
    public static class PixelArt
    {
        public const int UnitCell = 10, UnitCols = 4, UnitRows = 8;     // 4 khung hình x (4 loại lính x 2 trạng thái)
        public const int StampPx = 20, StampCols = 8, StampRows = 4;

        static Color32 Rgb(byte r, byte g, byte b, byte a = 255) => new Color32(r, g, b, a);

        static readonly Color32 Outline = Rgb(26, 20, 30);
        static readonly Color32 Skin = Rgb(236, 186, 140), SkinShade = Rgb(196, 138, 100);
        static readonly Color32 Steel = Rgb(184, 194, 208), SteelDark = Rgb(112, 122, 142);
        static readonly Color32 Tunic = Rgb(56, 100, 200), TunicDark = Rgb(36, 64, 140), TunicLight = Rgb(90, 140, 226);
        static readonly Color32 Gold = Rgb(236, 196, 70), Leather = Rgb(116, 76, 44), Wood = Rgb(156, 104, 54);
        static readonly Color32 Pants = Rgb(64, 54, 74), White = Rgb(226, 226, 234), StringC = Rgb(210, 206, 190);
        static readonly Color32 Hood = Rgb(98, 70, 44), HoodDark = Rgb(74, 52, 34);
        static readonly Color32 Zomb = Rgb(124, 170, 96), ZombDark = Rgb(86, 124, 70), Rags = Rgb(100, 64, 114), RagsDark = Rgb(68, 44, 82), EyeRed = Rgb(230, 44, 40);
        static readonly Color32 Bone = Rgb(228, 222, 198), BoneDark = Rgb(166, 158, 138), EyeGreen = Rgb(120, 255, 120), BowDark = Rgb(84, 62, 52);

        /// <summary>Khung vẽ với toạ độ y hướng xuống (giống phần mềm vẽ pixel).</summary>
        public class Canvas
        {
            public readonly int W, H;
            public readonly Color32[] Px;
            public Canvas(int w, int h) { W = w; H = h; Px = new Color32[w * h]; }
            public void Set(int x, int y, Color32 c) { if (x >= 0 && y >= 0 && x < W && y < H) Px[(H - 1 - y) * W + x] = c; }
            public Color32 Get(int x, int y) => (x >= 0 && y >= 0 && x < W && y < H) ? Px[(H - 1 - y) * W + x] : default;

            public void Blit(Canvas src, int ox, int oy)
            {
                for (int y = 0; y < src.H; y++)
                    for (int x = 0; x < src.W; x++)
                    {
                        var c = src.Get(x, y);
                        if (c.a > 0) Set(ox + x, oy + y, c);
                    }
            }

            public Texture2D ToTexture(string name)
            {
                var t = new Texture2D(W, H, TextureFormat.RGBA32, false)
                {
                    name = name,
                    filterMode = FilterMode.Point,
                    wrapMode = TextureWrapMode.Clamp
                };
                t.SetPixels32(Px);
                t.Apply(false, true);
                return t;
            }
        }

        // ------------------------------------------------------------------
        // Lính
        // ------------------------------------------------------------------
        public static Texture2D BuildUnitAtlas()
        {
            var atlas = new Canvas(UnitCell * UnitCols, UnitCell * UnitRows);
            for (int type = 0; type < 4; type++)
                for (int state = 0; state < 2; state++)
                    for (int f = 0; f < 4; f++)
                        atlas.Blit(DrawUnit(type, state, f), f * UnitCell, (type * 2 + state) * UnitCell);
            return atlas.ToTexture("UnitAtlas");
        }

        /// <summary>Vẽ 1 khung hình 10x10 của 1 loại lính, quay mặt sang phải.</summary>
        public static Canvas DrawUnit(int type, int state, int f)
        {
            var s = new Canvas(UnitCell, UnitCell);
            bool walk = state == 0;
            int bob = walk && (f & 1) == 1 ? 1 : 0;

            // chân
            Color32 leg = type <= UnitType.Archer ? Pants : type == UnitType.Zombie ? RagsDark : BoneDark;
            if (walk && (f & 1) == 1) { s.Set(2, 8, leg); s.Set(3, 7, leg); s.Set(5, 7, leg); s.Set(6, 8, leg); }
            else { s.Set(3, 7, leg); s.Set(3, 8, leg); s.Set(5, 7, leg); s.Set(5, 8, leg); }

            // thân
            for (int y = 4; y <= 6; y++)
                for (int x = 3; x <= 5; x++)
                    s.Set(x, y + bob, BodyColor(type, x, y));

            DrawHead(s, type, bob);
            DrawGear(s, type, walk, f, bob);
            AddOutline(s);
            return s;
        }

        static Color32 BodyColor(int type, int x, int y)
        {
            switch (type)
            {
                case UnitType.Footman: return y == 6 ? (x == 4 ? Gold : Leather) : (x == 3 ? TunicDark : Tunic);
                case UnitType.Archer: return y == 6 ? Leather : (x == 3 ? TunicDark : TunicLight);
                case UnitType.Zombie: return (x == 4 && y == 5) ? ZombDark : (x == 3 ? RagsDark : Rags);
                default:
                    if (y == 6) return BoneDark;
                    if (y == 5 && x == 4) return Outline; // khe xương sườn
                    return x == 3 ? BoneDark : Bone;
            }
        }

        static void Row(Canvas s, int x, int y, Color32 a, Color32 b, Color32 c)
        {
            s.Set(x, y, a); s.Set(x + 1, y, b); s.Set(x + 2, y, c);
        }

        static void DrawHead(Canvas s, int type, int bob)
        {
            int y1 = 1 + bob, y2 = 2 + bob, y3 = 3 + bob;
            switch (type)
            {
                case UnitType.Footman:
                    Row(s, 3, y1, Steel, Steel, Steel);
                    Row(s, 3, y2, SteelDark, Steel, Skin);
                    Row(s, 3, y3, SteelDark, Skin, Skin);
                    break;
                case UnitType.Archer:
                    Row(s, 3, y1, HoodDark, Hood, Hood);
                    Row(s, 3, y2, HoodDark, Hood, Skin);
                    Row(s, 3, y3, HoodDark, Skin, SkinShade);
                    break;
                case UnitType.Zombie: // cúi người về phía trước
                    Row(s, 4, y1, ZombDark, Zomb, Zomb);
                    Row(s, 4, y2, ZombDark, Zomb, EyeRed);
                    Row(s, 4, y3, ZombDark, Zomb, ZombDark);
                    break;
                default:
                    Row(s, 3, y1, BoneDark, Bone, Bone);
                    Row(s, 3, y2, BoneDark, Bone, EyeGreen);
                    Row(s, 3, y3, BoneDark, Bone, BoneDark);
                    break;
            }
        }

        static void DrawGear(Canvas s, int type, bool walk, int f, int bob)
        {
            switch (type)
            {
                case UnitType.Footman:
                    s.Set(2, 4 + bob, White); s.Set(2, 5 + bob, Tunic); s.Set(2, 6 + bob, White); // khiên
                    if (walk) { s.Set(6, 5 + bob, Skin); s.Set(6, 4 + bob, Gold); s.Set(6, 3 + bob, Steel); s.Set(6, 2 + bob, Steel); }
                    else if (f == 0) { s.Set(6, 4, Skin); s.Set(6, 3, Gold); s.Set(6, 2, Steel); s.Set(6, 1, Steel); }
                    else if (f == 1) { s.Set(6, 4, Skin); s.Set(7, 4, Gold); s.Set(8, 3, Steel); s.Set(8, 2, Steel); }
                    else if (f == 2) { s.Set(6, 5, Skin); s.Set(7, 5, Gold); s.Set(8, 5, Steel); s.Set(8, 4, Steel); }
                    else { s.Set(6, 6, Skin); s.Set(7, 6, Gold); s.Set(8, 7, Steel); s.Set(8, 8, Steel); }
                    break;

                case UnitType.Archer:
                case UnitType.Skeleton:
                {
                    bool skel = type == UnitType.Skeleton;
                    Color32 bow = skel ? BowDark : Wood, str = skel ? BoneDark : StringC, hand = skel ? Bone : Skin;
                    int b = walk ? bob : 0;
                    s.Set(6, 2 + b, bow); s.Set(7, 3 + b, bow); s.Set(7, 4 + b, bow); s.Set(7, 5 + b, bow); s.Set(6, 6 + b, bow);
                    if (!walk && f <= 1)
                    {
                        // kéo dây, tên đã lắp
                        s.Set(4, 4, Wood); s.Set(5, 4, Wood); s.Set(6, 4, Wood); s.Set(8, 4, skel ? EyeGreen : Steel);
                        s.Set(6, 3, str); s.Set(6, 5, str);
                        s.Set(f == 0 ? 5 : 4, 4, hand);
                    }
                    else
                    {
                        s.Set(6, 3 + b, str); s.Set(6, 5 + b, str);
                        s.Set(6, 4 + b, hand);
                    }
                    break;
                }

                case UnitType.Zombie:
                    if (walk) { s.Set(6, 4 + bob, ZombDark); s.Set(7, 4 + bob, Zomb); }
                    else if (f == 0) { s.Set(6, 4, ZombDark); s.Set(7, 3, Zomb); s.Set(8, 2, Zomb); }
                    else if (f == 1) { s.Set(6, 4, ZombDark); s.Set(7, 4, Zomb); s.Set(8, 4, Zomb); }
                    else if (f == 2) { s.Set(6, 5, ZombDark); s.Set(7, 6, Zomb); }
                    else { s.Set(6, 4, ZombDark); s.Set(7, 5, Zomb); }
                    break;
            }
        }

        static void AddOutline(Canvas s)
        {
            var marks = new List<Vector2Int>();
            for (int y = 0; y < s.H; y++)
                for (int x = 0; x < s.W; x++)
                {
                    if (s.Get(x, y).a != 0) continue;
                    if (s.Get(x - 1, y).a != 0 || s.Get(x + 1, y).a != 0 || s.Get(x, y - 1).a != 0 || s.Get(x, y + 1).a != 0)
                        marks.Add(new Vector2Int(x, y));
                }
            foreach (var m in marks) s.Set(m.x, m.y, Outline);
        }

        // ------------------------------------------------------------------
        // "Con dấu" in lên mặt đất: máu, xác, mũi tên cắm, vết cháy
        // ------------------------------------------------------------------
        public static Texture2D BuildStampAtlas()
        {
            int S = StampPx;
            var a = new Canvas(S * StampCols, S * StampRows);
            var rnd = new System.Random(4242);

            for (int v = 0; v < 8; v++) Splat(a, v * S, 0, rnd, v >= 4);                // hàng 0: vũng máu đỏ / máu đen

            for (int t = 0; t < 4; t++)                                                    // hàng 1: xác (xoay 2 kiểu)
            {
                var body = DrawUnit(t, 0, 0);
                Darken(body, 0.72f);
                a.Blit(Rotate(body, true), t * S + 5, S + 5);
                a.Blit(Rotate(body, false), (4 + t) * S + 5, S + 5);
            }

            for (int v = 0; v < 4; v++) Drops(a, v * S, 2 * S, rnd);                      // hàng 2: giọt máu, tên cắm đất
            StuckArrow(a, 4 * S, 2 * S, Wood, White, false);
            StuckArrow(a, 5 * S, 2 * S, Wood, White, true);
            StuckArrow(a, 6 * S, 2 * S, Bone, Rags, false);
            StuckArrow(a, 7 * S, 2 * S, Bone, Rags, true);

            for (int v = 0; v < 8; v++) Scorch(a, v * S, 3 * S, rnd, v >= 4);              // hàng 3: vết cháy, tro

            return a.ToTexture("StampAtlas");
        }

        static void Darken(Canvas c, float k)
        {
            for (int i = 0; i < c.Px.Length; i++)
            {
                var p = c.Px[i];
                if (p.a == 0) continue;
                c.Px[i] = new Color32((byte)(p.r * k), (byte)(p.g * k), (byte)(p.b * k), p.a);
            }
        }

        static Canvas Rotate(Canvas src, bool clockwise)
        {
            var d = new Canvas(src.H, src.W);
            for (int y = 0; y < src.H; y++)
                for (int x = 0; x < src.W; x++)
                {
                    var c = src.Get(x, y);
                    if (clockwise) d.Set(src.H - 1 - y, x, c);
                    else d.Set(y, src.W - 1 - x, c);
                }
            return d;
        }

        static void Splat(Canvas a, int ox, int oy, System.Random r, bool dark)
        {
            float baseR = 3.2f + (float)r.NextDouble() * 3.3f;
            float p1 = (float)r.NextDouble() * 6.28f, p2 = (float)r.NextDouble() * 6.28f;
            Color32 main = dark ? Rgb(92, 10, 16, 215) : Rgb(150, 16, 20, 205);
            Color32 deep = dark ? Rgb(64, 6, 10, 225) : Rgb(112, 10, 14, 220);
            for (int y = 0; y < StampPx; y++)
                for (int x = 0; x < StampPx; x++)
                {
                    float dx = x - 9.5f, dy = y - 9.5f;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float ang = Mathf.Atan2(dy, dx);
                    float rr = baseR * (1f + 0.28f * Mathf.Sin(3f * ang + p1) + 0.16f * Mathf.Sin(5f * ang + p2));
                    if (d < rr) a.Set(ox + x, oy + y, d < rr * 0.45f ? deep : main);
                }
            int drops = 3 + r.Next(5);
            for (int k = 0; k < drops; k++)
            {
                float ang = (float)r.NextDouble() * 6.28f;
                float dist = baseR + 1f + (float)r.NextDouble() * 2.5f;
                int x = Mathf.Clamp(Mathf.RoundToInt(9.5f + Mathf.Cos(ang) * dist), 0, StampPx - 1);
                int y = Mathf.Clamp(Mathf.RoundToInt(9.5f + Mathf.Sin(ang) * dist), 0, StampPx - 1);
                a.Set(ox + x, oy + y, main);
            }
        }

        static void Drops(Canvas a, int ox, int oy, System.Random r)
        {
            Color32 c = Rgb(140, 14, 18, 230);
            a.Set(ox + 10, oy + 10, c);
            int extra = r.Next(0, 3);
            for (int k = 0; k < extra; k++) a.Set(ox + 10 + r.Next(-1, 2), oy + 10 + r.Next(-1, 2), c);
        }

        static void StuckArrow(Canvas a, int ox, int oy, Color32 shaft, Color32 fletch, bool deep)
        {
            a.Set(ox + 10, oy + 10, Rgb(50, 40, 34));
            a.Set(ox + 9, oy + 9, shaft);
            if (deep)
            {
                a.Set(ox + 8, oy + 8, fletch); a.Set(ox + 7, oy + 8, fletch); a.Set(ox + 8, oy + 7, fletch);
            }
            else
            {
                a.Set(ox + 8, oy + 8, shaft);
                a.Set(ox + 7, oy + 7, fletch); a.Set(ox + 6, oy + 7, fletch); a.Set(ox + 7, oy + 6, fletch);
            }
        }

        static void Scorch(Canvas a, int ox, int oy, System.Random r, bool ash)
        {
            float baseR = 6f + (float)r.NextDouble() * 2.5f;
            for (int y = 0; y < StampPx; y++)
                for (int x = 0; x < StampPx; x++)
                {
                    float dx = x - 9.5f, dy = y - 9.5f;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float edge = baseR * (0.75f + 0.35f * (float)r.NextDouble());
                    if (d >= edge) continue;
                    Color32 c = ash
                        ? (d < edge * 0.5f ? Rgb(96, 92, 88, 150) : Rgb(80, 76, 72, 110))
                        : (d < edge * 0.5f ? Rgb(30, 22, 18, 200) : Rgb(44, 32, 26, 140));
                    a.Set(ox + x, oy + y, c);
                }
        }

        // ------------------------------------------------------------------
        // Mũi tên & bảng màu hạt hiệu ứng
        // ------------------------------------------------------------------
        public static Texture2D BuildArrowTexture()
        {
            var a = new Canvas(12, 6);
            DrawArrow(a, 0, Wood, Steel, White);       // hàng trên: tên của Vương quốc
            DrawArrow(a, 3, Bone, EyeGreen, Rags);     // hàng dưới: tên xương của Xác sống
            return a.ToTexture("Arrows");
        }

        static void DrawArrow(Canvas a, int oy, Color32 shaft, Color32 head, Color32 fletch)
        {
            for (int x = 2; x <= 9; x++) a.Set(x, oy + 1, shaft);
            a.Set(0, oy, fletch); a.Set(1, oy, fletch); a.Set(1, oy + 1, fletch); a.Set(0, oy + 2, fletch); a.Set(1, oy + 2, fletch);
            a.Set(10, oy, head); a.Set(10, oy + 1, head); a.Set(10, oy + 2, head); a.Set(11, oy + 1, head);
        }

        public static Texture2D BuildPalette()
        {
            var cols = new Color32[16]
            {
                Rgb(0, 0, 0),        // 0
                Rgb(186, 22, 26),    // 1 máu
                Rgb(112, 14, 20),    // 2 máu đen
                Rgb(70, 100, 40),    // 3
                Rgb(150, 124, 86),   // 4 bụi
                Rgb(112, 90, 64),    // 5 bụi tối
                Rgb(236, 230, 206),  // 6 mảnh xương
                Rgb(255, 250, 210),  // 7 lửa trắng
                Rgb(255, 222, 80),   // 8 lửa vàng
                Rgb(255, 142, 30),   // 9 lửa cam
                Rgb(222, 62, 22),    // 10 lửa đỏ
                Rgb(118, 112, 108),  // 11 khói
                Rgb(66, 60, 62),     // 12 khói tối
                Rgb(140, 255, 120),  // 13 tia xanh
                Rgb(214, 228, 255),  // 14 tia kim loại
                Rgb(255, 96, 10),    // 15 tàn lửa
            };
            var t = new Texture2D(16, 1, TextureFormat.RGBA32, false)
            {
                name = "Palette",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            t.SetPixels32(cols);
            t.Apply(false, true);
            return t;
        }

        // ------------------------------------------------------------------
        // Mặt đất: cỏ/đất + tường thành bên trái + nghĩa địa bên phải
        // ------------------------------------------------------------------
        public static Texture2D BuildGround(uint seed)
        {
            int W = Cfg.GroundW, H = Cfg.GroundH;
            var px = new NativeArray<Color32>(W * H, Allocator.TempJob);
            new GroundJob { pixels = px, width = W, seed = seed }.Schedule(W * H, 4096).Complete();
            DrawWall(px);
            DrawGraveyard(px);

            var tex = new Texture2D(W, H, TextureFormat.RGBA32, false)
            {
                name = "Ground",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            tex.SetPixelData(px, 0);
            tex.Apply(false, false);
            px.Dispose();
            return tex;
        }

        // Ở đây toạ độ y hướng LÊN (y = 0 là mép dưới), giống texture.
        static void P(NativeArray<Color32> px, int x, int y, Color32 c)
        {
            if (x < 0 || y < 0 || x >= Cfg.GroundW || y >= Cfg.GroundH) return;
            px[y * Cfg.GroundW + x] = c;
        }

        static Color32 GetP(NativeArray<Color32> px, int x, int y) => px[y * Cfg.GroundW + x];

        static uint IHash(int a, int b)
        {
            uint h = (uint)(a * 374761393 + b * 668265263);
            h = (h ^ (h >> 13)) * 1274126177u;
            return h ^ (h >> 16);
        }

        static void DrawWall(NativeArray<Color32> px)
        {
            int H = Cfg.GroundH;
            for (int y = 0; y < H; y++)
                for (int x = 0; x < 40; x++)
                {
                    int row = y / 5;
                    int bx = (x + (row % 2) * 4) % 8;
                    bool mortar = y % 5 == 0 || bx == 0;
                    uint h = IHash((x + (row % 2) * 4) / 8, row) % 3;
                    Color32 c;
                    if (x >= 33) c = mortar ? Rgb(48, 46, 56) : Rgb(86, 84, 96);          // mặt tường hướng ra chiến trường
                    else if (x >= 11 && x < 23) c = (y % 4 == 0) ? Rgb(84, 60, 38) : Rgb(118, 84, 52); // lối đi bằng gỗ
                    else c = mortar ? Rgb(70, 68, 78) : h == 0 ? Rgb(150, 146, 156) : h == 1 ? Rgb(136, 132, 144) : Rgb(124, 120, 132);
                    P(px, x, y, c);
                }

            // lỗ châu mai
            for (int y = 0; y < H; y++)
                if ((y / 6) % 2 == 0)
                    for (int x = 27; x < 33; x++)
                        P(px, x, y, (y % 6 == 0 || x == 27) ? Rgb(96, 94, 104) : Rgb(170, 166, 178));

            // bóng tường đổ lên cỏ
            for (int y = 0; y < H; y++)
                for (int x = 40; x < 46; x++)
                {
                    var g = GetP(px, x, y);
                    float k = 0.55f + (x - 40) * 0.075f;
                    P(px, x, y, new Color32((byte)(g.r * k), (byte)(g.g * k), (byte)(g.b * k), 255));
                }

            for (int k = 0; k < 4; k++) Tower(px, 20, 135 + k * 270);
        }

        static void Tower(NativeArray<Color32> px, int cx, int cy)
        {
            const int R = 26;
            for (int y = -R - 3; y <= R + 3; y++)
                for (int x = -R - 3; x <= R + 3; x++)
                {
                    float d = Mathf.Sqrt(x * x + y * y);
                    if (d > R + 2.5f) continue;
                    Color32 c;
                    if (d > R) c = Rgb(36, 32, 40);                                                     // viền
                    else if (d > R - 5) c = ((int)(Mathf.Atan2(y, x) * 8f) & 1) == 0 ? Rgb(156, 152, 164) : Rgb(126, 122, 136); // tường tháp
                    else if (d > R - 7) c = Rgb(70, 66, 78);
                    else
                    {
                        int ring = (int)(d / 4f);                                                     // mái ngói xanh đá
                        c = (ring & 1) == 0 ? Rgb(52, 74, 120) : Rgb(64, 90, 140);
                        if (y > 0 && x < 0 && d < R - 9) c = Rgb((byte)(c.r + 18), (byte)(c.g + 18), (byte)(c.b + 18));
                    }
                    P(px, cx + x, cy + y, c);
                }
            // cờ
            for (int y = 0; y < 6; y++) P(px, cx, cy + y, Rgb(60, 44, 32));
            for (int y = 3; y < 6; y++)
                for (int x = 1; x < 5; x++)
                    P(px, cx + x, cy + y, y == 4 && x == 2 ? Gold : Tunic);
        }

        static void DrawGraveyard(NativeArray<Color32> px)
        {
            int W = Cfg.GroundW, H = Cfg.GroundH;
            for (int y = 0; y < H; y++)
                for (int x = W - 70; x < W; x++)
                {
                    float edge = W - 64 + 6f * Mathf.Sin(y * 0.05f) + (IHash(x, y) % 5);
                    if (x < edge) continue;
                    P(px, x, y, (IHash(x, y + 7) % 7 == 0) ? Rgb(58, 50, 56) : Rgb(70, 60, 66));
                }

            var r = new System.Random(77);
            for (int gy = 14; gy < H - 14; gy += 24)
                for (int gx = W - 52; gx < W - 6; gx += 18)
                {
                    int jx = gx + r.Next(-3, 4), jy = gy + r.Next(-4, 5);
                    if (r.NextDouble() < 0.6) Tombstone(px, jx, jy);
                    else Cross(px, jx, jy);
                }
        }

        static void Tombstone(NativeArray<Color32> px, int x0, int y0)
        {
            for (int i = 0; i < 8; i++) P(px, x0 + i, y0 - 1, Rgb(36, 30, 36));        // bóng
            for (int j = -1; j <= 8; j++)
                for (int i = -1; i <= 6; i++)
                    P(px, x0 + i, y0 + j, Rgb(30, 26, 34));                                 // viền
            for (int j = 0; j < 8; j++)
                for (int i = 0; i < 6; i++)
                {
                    if (j == 7 && (i == 0 || i == 5)) { P(px, x0 + i, y0 + j, Rgb(30, 26, 34)); continue; }
                    Color32 c = (i == 0 || j == 0) ? Rgb(110, 108, 122) : Rgb(152, 150, 164);
                    if (j == 5 && i >= 2 && i <= 3) c = Rgb(84, 82, 96);
                    if (j == 3 && i >= 1 && i <= 4) c = Rgb(96, 94, 108);
                    P(px, x0 + i, y0 + j, c);
                }
        }

        static void Cross(NativeArray<Color32> px, int x0, int y0)
        {
            for (int j = -1; j <= 9; j++) for (int i = 1; i <= 3; i++) P(px, x0 + i, y0 + j, Rgb(30, 24, 26));
            for (int j = 4; j <= 7; j++) for (int i = -1; i <= 5; i++) P(px, x0 + i, y0 + j, Rgb(30, 24, 26));
            for (int j = 0; j <= 8; j++) P(px, x0 + 2, y0 + j, Rgb(120, 86, 58));
            for (int i = 0; i <= 4; i++) P(px, x0 + i, y0 + 6, Rgb(138, 100, 66));
        }
    }
}
