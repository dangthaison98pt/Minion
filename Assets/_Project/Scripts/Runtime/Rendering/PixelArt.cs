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
    public static partial class PixelArt
    {
        // Ô atlas 16x16: lính bộ 10x10 (12x12 tính cả viền) vẽ ở giữa ô, quân to dùng trọn 16x16. 4 khung hình x (26 loại x 2 trạng thái)
        public const int UnitCell = 16, InfantryPx = 10, UnitCols = 4, UnitRows = UnitType.Count * 2;
        public const int StampPx = 20, StampCols = 8, StampRows = 3 + (UnitType.Count * 2 + StampCols - 1) / StampCols;
        public const int ProjW = 12, ProjH = 3;   // mỗi loại đạn 1 hàng 12x3 pixel

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
        static readonly Color32 Horse = Rgb(150, 98, 58), HorseLight = Rgb(176, 122, 74), HorseDark = Rgb(104, 66, 40), Mane = Rgb(58, 38, 30), Hoof = Rgb(46, 36, 34);
        static readonly Color32 Cloth = Rgb(196, 40, 44), ClothDark = Rgb(140, 26, 32);

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
        public static Texture2D BuildUnitAtlas() => BuildUnitCanvas().ToTexture("UnitAtlas");

        /// <summary>Lưu atlas lính + dấu in ra PNG (phóng to <paramref name="zoom"/> lần) để xem sprite mà không cần mở game.</summary>
        public static void SaveAtlases(string dir, int zoom = 6)
        {
            System.IO.File.WriteAllBytes(System.IO.Path.Combine(dir, "atlas_units.png"), Zoomed(UnitSheet(), zoom));
            System.IO.File.WriteAllBytes(System.IO.Path.Combine(dir, "atlas_stamps.png"), Zoomed(BuildStampCanvas(), zoom / 2));
            System.IO.File.WriteAllBytes(System.IO.Path.Combine(dir, "atlas_tiles.png"), Zoomed(BuildTileCanvas(), zoom / 2));
        }

        /// <summary>Bản xem cho người: mỗi hàng 1 loại quân, 4 khung đi + 4 khung đánh (atlas thật thì 2 hàng / loại).</summary>
        static Canvas UnitSheet()
        {
            var sheet = new Canvas(UnitCell * 8, UnitCell * UnitType.Count);
            for (int type = 0; type < UnitType.Count; type++)
                for (int state = 0; state < 2; state++)
                    for (int f = 0; f < 4; f++)
                    {
                        var sprite = DrawUnit(type, state, f);
                        int pad = (UnitCell - sprite.W) / 2;
                        sheet.Blit(sprite, (state * 4 + f) * UnitCell + pad, type * UnitCell + pad);
                    }
            return sheet;
        }

        static byte[] Zoomed(Canvas c, int k)
        {
            var bg = new Color32(70, 92, 52, 255);   // nền cỏ cho dễ nhìn
            var t = new Texture2D(c.W * k, c.H * k, TextureFormat.RGBA32, false);
            var px = new Color32[t.width * t.height];
            for (int y = 0; y < t.height; y++)
                for (int x = 0; x < t.width; x++)
                {
                    var p = c.Px[(y / k) * c.W + x / k];
                    px[y * t.width + x] = p.a > 0 ? p : bg;
                }
            t.SetPixels32(px);
            t.Apply();
            var png = t.EncodeToPNG();
            if (Application.isPlaying) Object.Destroy(t); else Object.DestroyImmediate(t);
            return png;
        }

        static Canvas BuildUnitCanvas()
        {
            var atlas = new Canvas(UnitCell * UnitCols, UnitCell * UnitRows);
            for (int type = 0; type < UnitType.Count; type++)
                for (int state = 0; state < 2; state++)
                    for (int f = 0; f < 4; f++)
                    {
                        var sprite = DrawUnit(type, state, f);
                        int pad = (UnitCell - sprite.W) / 2;   // lính bộ 10x10 nằm giữa ô 16x16
                        atlas.Blit(sprite, f * UnitCell + pad, (type * 2 + state) * UnitCell + pad);
                    }
            return atlas;
        }

        // Vũ khí của lính bộ
        const int WNone = 0, WSword = 1, WBow = 2, WAxe = 3, WStaff = 4, WCrossbow = 5, WJavelin = 6, WClaws = 7, WTwin = 8;
        // Phụ kiện
        const int XNone = 0, XEars = 1, XHorns = 2, XWings = 3, XPoint = 4, XTail = 5, XElfEar = 6;

        /// <summary>Mô tả ngoại hình 1 lính bộ 10x10: màu từng phần + vũ khí + phụ kiện.</summary>
        class Look
        {
            public Color32 leg, body, bodyDark, belt, top, back, face, faceLow;
            public Color32 eye; public bool hasEye;
            public int weapon; public Color32 w1, w2;
            public bool shield; public Color32 sh1, sh2;
            public int extra, extra2; public Color32 ex;
            public bool lean, dwarf; public Color32 beard; public bool hasBeard;
        }

        static readonly Color32 Robe = Rgb(232, 224, 200), RobeDark = Rgb(186, 176, 150);
        static readonly Color32 ElfGreen = Rgb(60, 150, 96), ElfDark = Rgb(36, 100, 70), Blond = Rgb(240, 214, 120), BlondDark = Rgb(196, 160, 70);
        static readonly Color32 Leaf = Rgb(92, 160, 62), LeafDark = Rgb(54, 110, 48), LeafLight = Rgb(140, 198, 84), Bark = Rgb(112, 78, 50), BarkDark = Rgb(78, 52, 36);
        static readonly Color32 DwarfTunic = Rgb(170, 84, 40), DwarfDark = Rgb(118, 56, 30), Beard = Rgb(222, 120, 40), BeardGrey = Rgb(200, 196, 188);
        static readonly Color32 Fur = Rgb(128, 128, 140), FurDark = Rgb(84, 84, 98), FurBrown = Rgb(134, 90, 56), FurBrownDark = Rgb(92, 60, 40), FurBrownLight = Rgb(168, 122, 80);
        static readonly Color32 Leo = Rgb(226, 176, 70), LeoDark = Rgb(112, 76, 36), TribeSkin = Rgb(170, 110, 74), TribeDark = Rgb(124, 78, 54);
        static readonly Color32 Feather = Rgb(240, 238, 228), FeatherDark = Rgb(130, 96, 66), Beak = Rgb(244, 190, 50);
        static readonly Color32 Void1 = Rgb(110, 60, 160), Void2 = Rgb(70, 36, 112), Void3 = Rgb(40, 22, 66), VoidGlow = Rgb(240, 90, 240), VoidEye = Rgb(140, 240, 255);

        static Look LookOf(int type)
        {
            switch (type)
            {
                case UnitType.Swordsman:
                    return new Look { leg = Pants, body = Tunic, bodyDark = TunicDark, belt = Leather, top = Steel, back = SteelDark, face = Skin, faceLow = Skin,
                                      weapon = WSword, w1 = Steel, w2 = Gold, shield = true, sh1 = White, sh2 = Tunic };
                case UnitType.Archer:
                    return new Look { leg = Pants, body = TunicLight, bodyDark = TunicDark, belt = Leather, top = Hood, back = HoodDark, face = Skin, faceLow = SkinShade,
                                      weapon = WBow, w1 = Wood, w2 = StringC };
                case UnitType.Monk:
                    return new Look { leg = RobeDark, body = Robe, bodyDark = RobeDark, belt = Gold, top = Skin, back = SkinShade, face = Skin, faceLow = SkinShade,
                                      weapon = WStaff, w1 = Wood, w2 = Gold };
                case UnitType.ForestArcher:
                    return new Look { leg = ElfDark, body = ElfGreen, bodyDark = ElfDark, belt = Leather, top = LeafDark, back = ElfDark, face = Skin, faceLow = SkinShade,
                                      weapon = WBow, w1 = HorseLight, w2 = LeafLight, extra = XPoint, ex = LeafDark };
                case UnitType.BladeDancer:
                    return new Look { leg = White, body = Rgb(70, 170, 170), bodyDark = Rgb(40, 110, 120), belt = Gold, top = Blond, back = BlondDark, face = Skin, faceLow = Skin,
                                      weapon = WTwin, w1 = White, w2 = Gold, extra = XElfEar, ex = Skin };
                case UnitType.Druid:
                    return new Look { leg = LeafDark, body = Rgb(60, 100, 60), bodyDark = Rgb(40, 70, 46), belt = Bark, top = Bark, back = BarkDark, face = Skin, faceLow = SkinShade,
                                      weapon = WStaff, w1 = Bark, w2 = LeafLight, extra = XHorns, ex = Bone };
                case UnitType.ShieldWarrior:
                    return new Look { leg = Leather, body = DwarfTunic, bodyDark = DwarfDark, belt = Gold, top = Steel, back = SteelDark, face = Skin, faceLow = Skin,
                                      weapon = WSword, w1 = Steel, w2 = Leather, shield = true, sh1 = Steel, sh2 = Gold, dwarf = true, hasBeard = true, beard = Beard, extra = XHorns, ex = White };
                case UnitType.Crossbow:
                    return new Look { leg = Leather, body = Rgb(96, 110, 80), bodyDark = Rgb(66, 78, 58), belt = Leather, top = Leather, back = HoodDark, face = Skin, faceLow = Skin,
                                      weapon = WCrossbow, w1 = Wood, w2 = Steel, dwarf = true, hasBeard = true, beard = BeardGrey };
                case UnitType.Axeman:
                    return new Look { leg = Leather, body = Rgb(150, 50, 44), bodyDark = Rgb(104, 34, 34), belt = Gold, top = Beard, back = Rgb(170, 84, 30), face = Skin, faceLow = Skin,
                                      weapon = WAxe, w1 = Steel, w2 = Wood, dwarf = true, hasBeard = true, beard = Beard };
                case UnitType.Wolf:
                    return new Look { leg = FurDark, body = Fur, bodyDark = FurDark, belt = Fur, top = Fur, back = FurDark, face = Fur, faceLow = White, hasEye = true, eye = Gold,
                                      weapon = WClaws, w1 = FurDark, w2 = White, extra = XEars, ex = FurDark, lean = true };
                case UnitType.Leopard:
                    return new Look { leg = Leo, body = Leo, bodyDark = LeoDark, belt = Leo, top = Leo, back = LeoDark, face = Leo, faceLow = White, hasEye = true, eye = EyeGreen,
                                      weapon = WClaws, w1 = Leo, w2 = White, extra = XEars, extra2 = XTail, ex = LeoDark, lean = true };
                case UnitType.Shaman:
                    return new Look { leg = TribeDark, body = TribeSkin, bodyDark = TribeDark, belt = Cloth, top = Cloth, back = ClothDark, face = Bone, faceLow = BoneDark, hasEye = true, eye = Outline,
                                      weapon = WStaff, w1 = Wood, w2 = Bone, extra = XPoint, ex = Cloth };
                case UnitType.Eagle:
                    return new Look { leg = Beak, body = FeatherDark, bodyDark = FurBrownDark, belt = FeatherDark, top = Feather, back = Rgb(190, 188, 180), face = Feather, faceLow = Beak, hasEye = true, eye = Outline,
                                      weapon = WJavelin, w1 = Wood, w2 = Steel, extra = XWings, ex = FeatherDark };
                case UnitType.Summoner:
                    return new Look { leg = Void3, body = Void2, bodyDark = Void3, belt = VoidGlow, top = Void2, back = Void3, face = Void3, faceLow = Void3, hasEye = true, eye = VoidGlow,
                                      weapon = WStaff, w1 = Void3, w2 = VoidGlow, extra = XPoint, ex = Void2 };
                default: return null;
            }
        }

        /// <summary>
        /// Vẽ 1 khung hình của 1 loại quân, quay mặt sang phải, đã có viền.
        /// Lính bộ vẽ trong 10x10 rồi đặt vào khung 12x12 (chừa 1 pixel mỗi bên cho viền), quân to 16x16.
        /// </summary>
        public static Canvas DrawUnit(int type, int state, int f)
        {
            var s = DrawBody(type, state, f);
            if (s.W == InfantryPx)
            {
                var padded = new Canvas(InfantryPx + 2, InfantryPx + 2);
                padded.Blit(s, 1, 1);
                s = padded;
            }
            AddOutline(s);
            return s;
        }

        /// <summary>Lính bộ (không phải quân to 16x16)?</summary>
        public static bool IsInfantry(Canvas sprite) => sprite.W < UnitCell;

        static Canvas DrawBody(int type, int state, int f)
        {
            switch (type)
            {
                case UnitType.Cavalry: return DrawRider(0, state, f, true);
                case UnitType.DeerRider: return DrawRider(1, state, f, true);
                case UnitType.GoatRider: return DrawRider(2, state, f, true);
                case UnitType.Catapult: return DrawCatapult(state, f);
                case UnitType.Cannon: return DrawCannon(state, f);
                case UnitType.Treant: return DrawTreant(state, f);
                case UnitType.Bear: return DrawBear(state, f);
                case UnitType.Aberration: return DrawAberration(state, f);
                case UnitType.Shade: return DrawShade(state, f);
                case UnitType.Eye: return DrawEye(state, f);
                case UnitType.Screamer: return DrawScreamer(state, f);
                case UnitType.Voidling: return DrawVoidling(state, f);
                default: return DrawHumanoid(LookOf(type), state, f);
            }
        }

        static void Row(Canvas s, int x, int y, Color32 a, Color32 b, Color32 c)
        {
            s.Set(x, y, a); s.Set(x + 1, y, b); s.Set(x + 2, y, c);
        }

        static Canvas DrawHumanoid(Look k, int state, int f)
        {
            var s = new Canvas(InfantryPx, InfantryPx);
            bool walk = state == 0;
            bool step = walk && (f & 1) == 1;
            int dy = k.dwarf ? 1 : 0;                 // người lùn: thấp hơn 1 pixel, chân ngắn
            int bob = (step ? 1 : 0) * (k.dwarf ? 0 : 1) + dy;
            int hx = 3 + (k.lean ? 1 : 0);            // dáng cúi: đầu chúi về trước

            // chân
            if (k.dwarf)
            {
                if (step) { s.Set(2, 8, k.leg); s.Set(6, 8, k.leg); }
                else { s.Set(3, 8, k.leg); s.Set(5, 8, k.leg); }
            }
            else if (step) { s.Set(2, 8, k.leg); s.Set(3, 7, k.leg); s.Set(5, 7, k.leg); s.Set(6, 8, k.leg); }
            else { s.Set(3, 7, k.leg); s.Set(3, 8, k.leg); s.Set(5, 7, k.leg); s.Set(5, 8, k.leg); }

            // thân
            for (int y = 4; y <= 6; y++)
                for (int x = 3; x <= 5; x++)
                    s.Set(x, y + bob, y == 6 ? (x == 4 ? k.belt : k.bodyDark) : (x == 3 ? k.bodyDark : k.body));

            // đầu
            int y1 = 1 + bob, y2 = 2 + bob, y3 = 3 + bob;
            Row(s, hx, y1, k.back, k.top, k.top);
            Row(s, hx, y2, k.back, k.top, k.hasEye ? k.eye : k.face);
            Row(s, hx, y3, k.back, k.face, k.faceLow);
            if (k.hasBeard) { s.Set(hx + 1, y3, k.beard); s.Set(hx + 2, y3, k.beard); s.Set(hx + 2, y3 + 1, k.beard); }

            Extra(s, k, k.extra, hx, y1, bob, walk, f);
            Extra(s, k, k.extra2, hx, y1, bob, walk, f);

            if (k.shield) { s.Set(2, 4 + bob, k.sh1); s.Set(2, 5 + bob, k.sh2); s.Set(2, 6 + bob, k.sh1); }
            Weapon(s, k, walk, f, bob);
            return s;
        }

        static void Extra(Canvas s, Look k, int extra, int hx, int y1, int bob, bool walk, int f)
        {
            switch (extra)
            {
                case XEars: s.Set(hx, y1 - 1, k.ex); s.Set(hx + 2, y1 - 1, k.ex); break;
                case XHorns: s.Set(hx - 1, y1 - 1, k.ex); s.Set(hx + 3, y1 - 1, k.ex); s.Set(hx - 1, y1, k.ex); s.Set(hx + 3, y1, k.ex); break;
                case XPoint: s.Set(hx + 1, y1 - 1, k.ex); break;
                case XElfEar: s.Set(hx - 1, y1 + 1, k.ex); break;
                case XTail:
                    if (walk && (f & 1) == 1) { s.Set(2, 6 + bob, k.ex); s.Set(1, 5 + bob, k.ex); }
                    else { s.Set(2, 6 + bob, k.ex); s.Set(1, 7 + bob, k.ex); }
                    break;
                case XWings:
                    if ((f & 1) == 1) { s.Set(2, 3 + bob, k.ex); s.Set(1, 2 + bob, k.ex); s.Set(2, 4 + bob, k.ex); s.Set(0, 2 + bob, k.ex); }
                    else { s.Set(2, 4 + bob, k.ex); s.Set(1, 5 + bob, k.ex); s.Set(2, 5 + bob, k.ex); s.Set(1, 6 + bob, k.ex); }
                    break;
            }
        }

        static void Weapon(Canvas s, Look k, bool walk, int f, int bob)
        {
            Color32 hand = k.weapon == WClaws ? k.w1 : k.face;
            int b = walk ? bob : (k.dwarf ? 1 : 0);
            switch (k.weapon)
            {
                case WSword:
                case WTwin:
                    if (walk) { s.Set(6, 5 + b, hand); s.Set(6, 4 + b, k.w2); s.Set(6, 3 + b, k.w1); s.Set(6, 2 + b, k.w1); }
                    else if (f == 0) { s.Set(6, 4 + b, hand); s.Set(6, 3 + b, k.w2); s.Set(6, 2 + b, k.w1); s.Set(6, 1 + b, k.w1); }
                    else if (f == 1) { s.Set(6, 4 + b, hand); s.Set(7, 4 + b, k.w2); s.Set(8, 3 + b, k.w1); s.Set(8, 2 + b, k.w1); }
                    else if (f == 2) { s.Set(6, 5 + b, hand); s.Set(7, 5 + b, k.w2); s.Set(8, 5 + b, k.w1); s.Set(8, 4 + b, k.w1); }
                    else { s.Set(6, 6 + b, hand); s.Set(7, 6 + b, k.w2); s.Set(8, 7 + b, k.w1); s.Set(8, 8, k.w1); }
                    if (k.weapon == WTwin)
                    {
                        // thanh kiếm thứ hai ở tay sau, vung ngược nhịp
                        if (walk || f >= 2) { s.Set(2, 5 + b, k.w2); s.Set(2, 4 + b, k.w1); s.Set(2, 3 + b, k.w1); }
                        else { s.Set(2, 5 + b, k.w2); s.Set(1, 6 + b, k.w1); s.Set(1, 7 + b, k.w1); }
                    }
                    break;

                case WAxe:
                    if (walk) { s.Set(6, 5 + b, hand); s.Set(6, 4 + b, k.w2); s.Set(6, 3 + b, k.w2); s.Set(6, 2 + b, k.w1); s.Set(7, 2 + b, k.w1); s.Set(7, 3 + b, k.w1); }
                    else if (f == 0) { s.Set(6, 4 + b, hand); s.Set(6, 3 + b, k.w2); s.Set(6, 2 + b, k.w2); s.Set(6, 1 + b, k.w1); s.Set(7, 1 + b, k.w1); s.Set(7, 2 + b, k.w1); }
                    else if (f == 1) { s.Set(6, 4 + b, hand); s.Set(7, 4 + b, k.w2); s.Set(8, 3 + b, k.w2); s.Set(8, 2 + b, k.w1); s.Set(9, 2 + b, k.w1); s.Set(9, 3 + b, k.w1); }
                    else if (f == 2) { s.Set(6, 5 + b, hand); s.Set(7, 5 + b, k.w2); s.Set(8, 5 + b, k.w2); s.Set(9, 5 + b, k.w1); s.Set(9, 4 + b, k.w1); s.Set(9, 6 + b, k.w1); }
                    else { s.Set(6, 6 + b, hand); s.Set(7, 6 + b, k.w2); s.Set(8, 7 + b, k.w2); s.Set(8, 8, k.w1); s.Set(9, 8, k.w1); s.Set(9, 7 + b, k.w1); }
                    break;

                case WBow:
                    s.Set(6, 2 + b, k.w1); s.Set(7, 3 + b, k.w1); s.Set(7, 4 + b, k.w1); s.Set(7, 5 + b, k.w1); s.Set(6, 6 + b, k.w1);
                    if (!walk && f <= 1)
                    {
                        // kéo dây, tên đã lắp
                        s.Set(4, 4 + b, Wood); s.Set(5, 4 + b, Wood); s.Set(6, 4 + b, Wood); s.Set(8, 4 + b, Steel);
                        s.Set(6, 3 + b, k.w2); s.Set(6, 5 + b, k.w2);
                        s.Set(f == 0 ? 5 : 4, 4 + b, hand);
                    }
                    else
                    {
                        s.Set(6, 3 + b, k.w2); s.Set(6, 5 + b, k.w2);
                        s.Set(6, 4 + b, hand);
                    }
                    break;

                case WCrossbow:
                    s.Set(6, 5 + b, hand);
                    s.Set(6, 4 + b, k.w1); s.Set(7, 4 + b, k.w1); s.Set(8, 4 + b, k.w1);
                    s.Set(8, 3 + b, k.w2); s.Set(8, 5 + b, k.w2); s.Set(7, 2 + b, k.w2); s.Set(7, 6 + b, k.w2);
                    if (!walk && f <= 1) s.Set(9, 4 + b, White);     // tên đã lắp
                    break;

                case WStaff:
                {
                    bool cast = !walk && f <= 1;
                    int top = cast ? 1 : 2;
                    for (int y = top + 1; y <= 7; y++) s.Set(7, y + (walk ? b : 0), k.w1);
                    s.Set(7, top + (walk ? b : 0), k.w2);
                    if (cast) { s.Set(8, top, k.w2); s.Set(7, top - 1, k.w2); s.Set(6, top, k.w2); }   // quả cầu loé sáng
                    s.Set(6, 5 + b, hand);
                    break;
                }

                case WJavelin:
                    if (walk) { s.Set(6, 5 + b, hand); s.Set(6, 4 + b, k.w1); s.Set(7, 3 + b, k.w1); s.Set(8, 2 + b, k.w1); s.Set(9, 1 + b, k.w2); }
                    else if (f <= 1) { s.Set(6, 3 + b, hand); s.Set(5, 2, k.w1); s.Set(6, 2, k.w1); s.Set(7, 2, k.w1); s.Set(8, 2, k.w1); s.Set(9, 2, k.w2); }
                    else s.Set(6 + (f == 2 ? 1 : 0), 4 + b, hand);   // đã ném
                    break;

                case WClaws:
                    if (walk) { s.Set(6, 4 + b, k.w1); s.Set(7, 4 + b, k.w2); }
                    else if (f == 0) { s.Set(6, 4 + b, k.w1); s.Set(7, 3 + b, k.w1); s.Set(8, 2 + b, k.w2); }
                    else if (f == 1) { s.Set(6, 4 + b, k.w1); s.Set(7, 4 + b, k.w1); s.Set(8, 4 + b, k.w2); }
                    else if (f == 2) { s.Set(6, 5 + b, k.w1); s.Set(7, 6 + b, k.w2); }
                    else { s.Set(6, 4 + b, k.w1); s.Set(7, 5 + b, k.w2); }
                    break;
            }
        }

        // ------------------------------------------------------------------
        // Quân cưỡi thú 16x16: 0 = kỵ binh (ngựa, thương), 1 = kỵ sĩ hươu (cung), 2 = kỵ sĩ dê núi (rìu).
        // Đi = phi nước đại 4 khung; Đánh = khung 0-1 thu vũ khí, 2-3 ra đòn.
        // ------------------------------------------------------------------
        public static Canvas DrawRider(int kind, int state, int f, bool rider)
        {
            Color32 coat = Horse, coatLight = HorseLight, coatDark = HorseDark, mane = Mane, cloth = Cloth, clothDark = ClothDark;
            if (kind == 1) { coat = Rgb(186, 138, 84); coatLight = Rgb(214, 172, 116); coatDark = Rgb(226, 214, 190); mane = Rgb(150, 104, 62); cloth = ElfGreen; clothDark = ElfDark; }
            if (kind == 2) { coat = Rgb(196, 194, 186); coatLight = Rgb(226, 224, 216); coatDark = Rgb(140, 138, 134); mane = Rgb(150, 148, 144); cloth = DwarfTunic; clothDark = DwarfDark; }

            var s = new Canvas(UnitCell, UnitCell);
            bool walk = state == 0;
            int g = walk ? f : 1;                 // tư thế chân: đứng đánh thì chân thẳng
            int bob = walk && (f == 1 || f == 3) ? -1 : 0;   // nhún lên khi chân duỗi thẳng

            // --- chân (vẽ trước để thân đè lên) ---
            Color32 legC = kind == 2 ? Rgb(120, 118, 114) : kind == 1 ? Rgb(150, 104, 62) : HorseDark;
            void Leg(int x0, int y0, int dx1, int dx2)
            {
                s.Set(x0, y0, legC); s.Set(x0 + dx1, y0 + 1, legC); s.Set(x0 + dx2, y0 + 2, Hoof);
            }
            int ly = 11 + bob;
            switch (g)
            {
                case 0: Leg(11, ly, 1, 2); Leg(10, ly, 0, 1); Leg(5, ly, -1, -2); Leg(4, ly, -1, -1); break;   // duỗi dài
                case 1: Leg(11, ly, 0, 0); Leg(9, ly, 0, 0); Leg(6, ly, 0, 0); Leg(4, ly, 0, 0); break;         // chạm đất
                case 2: Leg(11, ly, -1, -2); Leg(10, ly, -1, -1); Leg(5, ly, 1, 2); Leg(4, ly, 1, 1); break;    // co lại
                default: Leg(12, ly, 0, 0); Leg(10, ly, 0, 0); Leg(5, ly, 0, 0); Leg(3, ly, 0, 0); break;
            }

            // --- thân, cổ, đầu, đuôi ---
            int by = 8 + bob;
            for (int x = 4; x <= 11; x++)
            {
                s.Set(x, by, coatLight);
                s.Set(x, by + 1, coat);
                s.Set(x, by + 2, coatDark);
            }
            s.Set(11, by - 1, coat); s.Set(12, by - 1, coat); s.Set(12, by - 2, coat);    // cổ
            if (kind == 0) { s.Set(11, by - 2, mane); s.Set(11, by - 3, mane); }          // bờm
            s.Set(12, by - 3, coatLight); s.Set(13, by - 3, coat); s.Set(14, by - 2, kind == 1 ? Outline : coatDark); s.Set(13, by - 2, coat); // đầu
            s.Set(12, by - 4, coat);                                                       // tai
            s.Set(13, by - 3, Outline);                                                    // mắt
            if (kind == 1)   // gạc hươu
            {
                s.Set(13, by - 5, Bone); s.Set(13, by - 6, Bone); s.Set(14, by - 6, Bone); s.Set(12, by - 6, Bone); s.Set(14, by - 5, BoneDark);
            }
            if (kind == 2)   // sừng dê cong ra sau + râu dê
            {
                s.Set(12, by - 5, BoneDark); s.Set(11, by - 5, Bone); s.Set(10, by - 4, Bone); s.Set(10, by - 3, BoneDark);
                s.Set(14, by - 1, White);
            }
            int tail = walk && (f & 1) == 0 ? 0 : 1;
            if (kind == 0) { s.Set(3, by, mane); s.Set(2, by + 1 - tail, mane); s.Set(2, by + 2 - tail, mane); }   // đuôi bay
            else { s.Set(3, by - tail, White); s.Set(3, by + 1 - tail, mane); }                                      // đuôi cụt
            s.Set(6, by, cloth); s.Set(7, by, cloth); s.Set(8, by, cloth); s.Set(7, by + 1, clothDark); // yên + vải phủ

            if (rider)
            {
                Color32 tunic = kind == 1 ? ElfGreen : kind == 2 ? DwarfTunic : Tunic;
                Color32 tunicDark = kind == 1 ? ElfDark : kind == 2 ? DwarfDark : TunicDark;
                int ry = by - 1;                                         // hông kỵ sĩ ngồi trên yên
                s.Set(8, ry + 2, Pants); s.Set(8, ry + 3, Leather);      // chân + ủng thả xuống sườn thú
                for (int y = ry - 2; y <= ry; y++) { s.Set(7, y, tunicDark); s.Set(8, y, tunic); }
                s.Set(8, ry, Gold);                                       // thắt lưng
                if (kind == 1)
                {
                    s.Set(7, ry - 3, BlondDark); s.Set(8, ry - 3, Skin);  // mặt
                    s.Set(7, ry - 4, Blond); s.Set(8, ry - 4, Blond);     // tóc vàng
                }
                else
                {
                    s.Set(7, ry - 3, SteelDark); s.Set(8, ry - 3, Skin);  // mặt
                    s.Set(7, ry - 4, Steel); s.Set(8, ry - 4, Steel);     // mũ sắt
                    if (kind == 0) s.Set(7, ry - 5, Cloth);               // chỏm mũ đỏ
                    else { s.Set(8, ry - 2, Beard); s.Set(9, ry - 3, Beard); }   // râu người lùn
                    s.Set(6, ry - 2, kind == 0 ? White : Steel); s.Set(6, ry - 1, kind == 0 ? Tunic : Gold); s.Set(6, ry, kind == 0 ? White : Steel);   // khiên
                }

                if (kind == 1)
                {
                    // cung: luôn giương về phía trước (vừa chạy vừa bắn)
                    s.Set(10, ry - 4, Wood); s.Set(11, ry - 3, Wood); s.Set(11, ry - 2, Wood); s.Set(11, ry - 1, Wood); s.Set(10, ry, Wood);
                    s.Set(10, ry - 2, Skin);
                    if ((f & 1) == 0) { s.Set(9, ry - 2, Wood); s.Set(12, ry - 2, LeafLight); }
                }
                else if (walk)
                {
                    s.Set(9, ry - 1, Skin);
                    if (kind == 0)
                    {
                        s.Set(10, ry - 2, Wood); s.Set(11, ry - 3, Wood); s.Set(12, ry - 4, Wood); s.Set(13, ry - 5, Steel);
                        s.Set(11, ry - 4, Cloth);                         // cờ đuôi nheo
                    }
                    else { s.Set(10, ry - 2, Wood); s.Set(10, ry - 3, Wood); s.Set(10, ry - 4, Steel); s.Set(11, ry - 4, Steel); s.Set(11, ry - 3, Steel); }   // rìu dựng
                }
                else
                {
                    int reach = f >= 2 ? 1 : 0;                           // khung 2-3: ra đòn
                    s.Set(9 + reach, ry - 2, Skin);
                    if (kind == 0)
                    {
                        for (int x = 8 + reach; x <= 12 + reach; x++) s.Set(x, ry - 3, Wood);
                        s.Set(13 + reach, ry - 3, Steel);
                    }
                    else
                    {
                        int ay = f >= 2 ? ry - 1 : ry - 4;                // bổ rìu từ trên xuống
                        s.Set(10, ry - 2, Wood); s.Set(11, f >= 2 ? ry - 2 : ry - 3, Wood);
                        s.Set(12, ay, Steel); s.Set(13, ay, Steel); s.Set(12, ay + 1, Steel);
                    }
                }
            }

            return s;
        }

        // ------------------------------------------------------------------
        // Quân to 16x16 khác
        // ------------------------------------------------------------------
        static void Box(Canvas s, int x0, int y0, int x1, int y1, Color32 c)
        {
            for (int y = y0; y <= y1; y++) for (int x = x0; x <= x1; x++) s.Set(x, y, c);
        }

        static Canvas DrawTreant(int state, int f)
        {
            var s = new Canvas(UnitCell, UnitCell);
            bool walk = state == 0;
            int sway = walk && (f == 1) ? 1 : walk && f == 3 ? -1 : 0;

            // rễ (chân)
            if (walk && (f & 1) == 1) { Box(s, 4, 12, 5, 13, BarkDark); Box(s, 10, 12, 11, 13, BarkDark); s.Set(3, 13, BarkDark); s.Set(12, 13, BarkDark); }
            else { Box(s, 5, 12, 6, 13, BarkDark); Box(s, 9, 12, 10, 13, BarkDark); s.Set(4, 13, BarkDark); s.Set(11, 13, BarkDark); }
            // thân
            Box(s, 5, 6, 10, 11, Bark);
            Box(s, 5, 6, 6, 11, BarkDark);
            s.Set(8, 10, BarkDark); s.Set(9, 8, BarkDark);
            // mắt + miệng
            s.Set(8, 7, Gold); s.Set(10, 7, Gold); s.Set(9, 9, Outline); s.Set(10, 9, Outline);
            // tán lá (từ hàng 1, chừa hàng 0 cho viền)
            for (int y = 1; y <= 6; y++)
                for (int x = 2; x <= 13; x++)
                {
                    float dx = x - 7.5f - sway, dyy = (y - 4f) * 1.6f;
                    if (dx * dx + dyy * dyy > 30f) continue;
                    s.Set(x, y, ((x * 7 + y * 13) % 5 == 0) ? LeafLight : (x + sway < 6 || y > 5) ? LeafDark : Leaf);
                }
            // tay cành (không chạm cột 15 để còn chỗ cho viền)
            Box(s, 3, 7, 4, 8, BarkDark); s.Set(2, 9, BarkDark); s.Set(2, 6, Leaf);
            if (walk) { Box(s, 11, 7, 12, 8, Bark); s.Set(13, 9, Bark); s.Set(13, 6, Leaf); }
            else if (f == 0) { Box(s, 11, 5, 12, 6, Bark); s.Set(13, 4, Bark); s.Set(14, 3, Bark); s.Set(14, 2, Leaf); }       // giơ cành
            else if (f == 1) { Box(s, 11, 6, 12, 7, Bark); s.Set(13, 6, Bark); s.Set(14, 5, Bark); s.Set(14, 4, Leaf); }
            else { Box(s, 11, 8, 12, 9, Bark); s.Set(13, 9, Bark); s.Set(14, 10, Bark); s.Set(14, 11, Leaf); }                 // quật xuống
            return s;
        }

        static Canvas DrawBear(int state, int f)
        {
            var s = new Canvas(UnitCell, UnitCell);
            bool walk = state == 0;
            int bob = walk && (f & 1) == 1 ? 1 : 0;

            // chân
            if (walk && (f & 1) == 1) { Box(s, 4, 12, 5, 13, FurBrownDark); Box(s, 10, 12, 11, 13, FurBrownDark); }
            else { Box(s, 5, 12, 6, 13, FurBrownDark); Box(s, 9, 12, 10, 13, FurBrownDark); }
            // thân to
            Box(s, 4, 6 + bob, 11, 11 + bob, FurBrown);
            Box(s, 4, 6 + bob, 5, 11 + bob, FurBrownDark);
            Box(s, 7, 8 + bob, 9, 10 + bob, FurBrownLight);                  // ngực sáng màu
            // đầu chúi về trước
            Box(s, 8, 2 + bob, 12, 5 + bob, FurBrown);
            Box(s, 8, 2 + bob, 8, 5 + bob, FurBrownDark);
            s.Set(8, 1 + bob, FurBrownDark); s.Set(12, 1 + bob, FurBrownDark);   // tai
            s.Set(13, 4 + bob, FurBrownLight); s.Set(13, 5 + bob, FurBrownLight); s.Set(14, 4 + bob, Outline);   // mõm
            s.Set(11, 3 + bob, Gold);                                         // mắt
            // tay sau
            Box(s, 3, 7 + bob, 3, 9 + bob, FurBrownDark);
            // tay trước + vuốt
            if (walk) { Box(s, 12, 7 + bob, 13, 9 + bob, FurBrown); s.Set(13, 10 + bob, White); }
            else if (f == 0) { Box(s, 12, 3, 13, 5, FurBrown); s.Set(13, 2, White); s.Set(14, 2, White); }                   // giơ vuốt
            else if (f == 1) { Box(s, 12, 5, 13, 6, FurBrown); s.Set(14, 5, White); s.Set(14, 6, White); }
            else if (f == 2) { Box(s, 12, 7, 13, 8, FurBrown); s.Set(14, 8, White); s.Set(14, 9, White); }                   // tát ngang
            else { Box(s, 12, 8, 13, 10, FurBrown); s.Set(14, 11, White); }
            return s;
        }

        static Canvas DrawCatapult(int state, int f)
        {
            var s = new Canvas(UnitCell, UnitCell);
            bool walk = state == 0;
            Color32 wood = Wood, dark = Leather;
            // khung gỗ
            Box(s, 2, 10, 13, 11, wood);
            Box(s, 2, 11, 13, 11, dark);
            Box(s, 9, 5, 10, 10, wood); s.Set(8, 9, dark); s.Set(11, 9, dark); s.Set(7, 10, dark);   // trụ đỡ
            Box(s, 8, 5, 11, 5, dark);                                                                // xà ngang
            // bánh xe (lăn: nan hoa đổi chỗ)
            void Wheel(int cx)
            {
                Box(s, cx - 1, 11, cx + 1, 13, dark);
                s.Set(cx, 12, walk && (f & 1) == 1 ? Steel : Gold);
                s.Set(cx - 1, 11, walk && (f & 1) == 1 ? dark : wood); s.Set(cx + 1, 13, walk && (f & 1) == 1 ? dark : wood);
            }
            Wheel(4); Wheel(11);
            // cần bắn: đi + khung 0-1 = kéo căng ra sau (có đá trong muỗng); khung 2-3 = đã bật lên
            if (walk || f <= 1)
            {
                s.Set(9, 6, wood); s.Set(8, 7, wood); s.Set(7, 7, wood); s.Set(6, 8, wood); s.Set(5, 8, wood); s.Set(4, 9, wood);
                Box(s, 2, 8, 3, 9, dark);                       // muỗng
                Box(s, 2, 7, 3, 7, Rgb(150, 146, 156));         // viên đá
            }
            else
            {
                s.Set(9, 5, wood); s.Set(10, 4, wood); s.Set(11, 3, wood); s.Set(11, 2, wood);
                Box(s, 12, 1, 13, 2, dark);
                if (f == 2) s.Set(14, 1, Rgb(150, 146, 156));   // đá vừa rời muỗng
            }
            return s;
        }

        static Canvas DrawCannon(int state, int f)
        {
            var s = new Canvas(UnitCell, UnitCell);
            bool walk = state == 0;
            Color32 iron = Rgb(70, 72, 86), ironLight = Rgb(112, 116, 134), brass = Gold;
            int recoil = !walk && f <= 1 ? -1 : 0;              // giật lùi khi vừa bắn
            // giá pháo
            Box(s, 3, 10, 10, 11, Wood);
            Box(s, 3, 11, 10, 11, Leather);
            s.Set(2, 12, Leather); s.Set(1, 13, Leather);        // càng kéo
            // nòng
            Box(s, 5 + recoil, 7, 13 + recoil, 9, iron);
            Box(s, 5 + recoil, 7, 13 + recoil, 7, ironLight);
            Box(s, 4 + recoil, 6, 5 + recoil, 10, iron);         // đuôi nòng
            Box(s, 8 + recoil, 7, 8 + recoil, 9, brass); Box(s, 13 + recoil, 6, 13 + recoil, 10, brass);   // đai đồng + miệng nòng
            // bánh xe
            Box(s, 6, 10, 9, 13, Leather);
            Box(s, 7, 11, 8, 12, walk && (f & 1) == 1 ? Steel : Wood);
            s.Set(6, 10, Wood); s.Set(9, 13, Wood); s.Set(9, 10, Wood); s.Set(6, 13, Wood);
            // lửa đầu nòng
            if (!walk && f == 0) { Box(s, 13, 7, 14, 9, Rgb(255, 222, 80)); s.Set(14, 6, Rgb(255, 142, 30)); s.Set(14, 10, Rgb(255, 142, 30)); s.Set(13, 8, White); }
            else if (!walk && f == 1) { s.Set(13, 7, Rgb(118, 112, 108)); s.Set(14, 6, Rgb(118, 112, 108)); s.Set(14, 8, Rgb(66, 60, 62)); }
            return s;
        }

        // ------------------------------------------------------------------
        // The Void (10x10)
        // ------------------------------------------------------------------
        static Canvas DrawAberration(int state, int f)
        {
            var s = new Canvas(InfantryPx, InfantryPx);
            bool walk = state == 0;
            int squish = walk && (f & 1) == 1 ? 1 : 0;
            // khối thịt méo mó
            for (int y = 2 + squish; y <= 7; y++)
                for (int x = 2; x <= 7; x++)
                {
                    bool corner = (y == 2 + squish || y == 7) && (x == 2 || x == 7);
                    if (corner) continue;
                    s.Set(x, y, x <= 3 ? Void2 : (x + y) % 4 == 0 ? Void2 : Void1);
                }
            s.Set(3, 1 + squish, Void2); s.Set(6, 1 + squish, Void1);                // u lồi
            s.Set(5, 3 + squish, VoidGlow); s.Set(7, 4 + squish, VoidGlow); s.Set(4, 5, VoidEye);   // nhiều mắt
            // chân xúc tu
            if (walk && (f & 1) == 1) { s.Set(2, 8, Void3); s.Set(4, 8, Void3); s.Set(7, 8, Void3); }
            else { s.Set(3, 8, Void3); s.Set(5, 8, Void3); s.Set(6, 8, Void3); }
            // xúc tu quật
            if (!walk)
            {
                if (f == 0) { s.Set(8, 3, Void1); s.Set(8, 2, Void1); s.Set(9, 1, VoidGlow); }
                else if (f == 1) { s.Set(8, 4, Void1); s.Set(9, 4, VoidGlow); }
                else if (f == 2) { s.Set(8, 5, Void1); s.Set(9, 6, VoidGlow); }
                else s.Set(8, 6, Void1);
            }
            return s;
        }

        static Canvas DrawShade(int state, int f)
        {
            var s = new Canvas(InfantryPx, InfantryPx);
            bool walk = state == 0;
            Color32 a = Rgb(52, 40, 84), b = Rgb(30, 22, 52), wisp = Rgb(96, 78, 150);
            int w = f & 1;
            // đầu + thân nhọn dần xuống thành khói
            Box(s, 3, 1, 6, 3, a); s.Set(3, 1, b); s.Set(6, 1, b);
            Box(s, 3, 4, 6, 5, a); Box(s, 3, 4, 3, 5, b);
            s.Set(4, 6, a); s.Set(5, 6, a); s.Set(3 + w, 6, b);
            s.Set(4 - w, 7, b); s.Set(5, 7, a);
            s.Set(4 + w, 8, wisp); s.Set(2 + w, 7, wisp);
            s.Set(5, 2, VoidEye); s.Set(6, 2, VoidEye);                               // mắt
            // lưỡi hái bóng tối
            if (walk) { s.Set(7, 4, b); s.Set(8, 3, wisp); s.Set(8, 2, wisp); }
            else if (f == 0) { s.Set(7, 3, b); s.Set(7, 2, wisp); s.Set(8, 1, wisp); s.Set(9, 1, VoidEye); }
            else if (f == 1) { s.Set(7, 4, b); s.Set(8, 4, wisp); s.Set(9, 3, wisp); s.Set(9, 4, VoidEye); }
            else if (f == 2) { s.Set(7, 5, b); s.Set(8, 5, wisp); s.Set(9, 6, wisp); s.Set(9, 5, VoidEye); }
            else { s.Set(7, 5, b); s.Set(8, 6, wisp); s.Set(8, 7, VoidEye); }
            return s;
        }

        static Canvas DrawEye(int state, int f)
        {
            var s = new Canvas(InfantryPx, InfantryPx);
            bool walk = state == 0;
            int bob = walk && (f & 1) == 1 ? 1 : 0;
            bool glow = !walk && f <= 1;
            // nhãn cầu
            for (int y = 0; y <= 5; y++)
                for (int x = 2; x <= 7; x++)
                {
                    float dx = x - 4.5f, dy = y - 2.5f;
                    if (dx * dx + dy * dy > 8.6f) continue;
                    s.Set(x, y + 1 + bob, x <= 3 ? Rgb(200, 186, 210) : White);
                }
            Box(s, 5, 3 + bob, 6, 4 + bob, glow ? VoidGlow : Void1);                 // tròng mắt nhìn sang phải
            s.Set(6, 3 + bob, glow ? White : Void3);
            s.Set(2, 2 + bob, Void2); s.Set(2, 5 + bob, Void2);                       // mí thịt
            // xúc tu lơ lửng
            int w = f & 1;
            s.Set(3, 7 + bob, Void2); s.Set(3 - w, 8, Void1);
            s.Set(5, 7 + bob, Void2); s.Set(5 + w, 8, Void1);
            s.Set(6, 7 + bob, Void2);
            if (glow) { s.Set(8, 3 + bob, VoidGlow); s.Set(9, 4 + bob, VoidGlow); }
            return s;
        }

        static Canvas DrawScreamer(int state, int f)
        {
            var s = new Canvas(InfantryPx, InfantryPx);
            bool walk = state == 0;
            bool scream = !walk && f <= 2;
            int bob = walk && (f & 1) == 1 ? 1 : 0;
            // chân gầy
            if (walk && (f & 1) == 1) { s.Set(3, 8, Void3); s.Set(6, 8, Void3); }
            else { s.Set(4, 8, Void3); s.Set(5, 8, Void3); }
            // thân mảnh
            Box(s, 4, 5 + bob, 5, 7, Void2); s.Set(3, 6 + bob, Void3); s.Set(6, 6 + bob, Void3);
            // cái đầu to với cái miệng khổng lồ
            Box(s, 2, 1 + bob, 7, 4 + bob, Void1);
            Box(s, 2, 1 + bob, 2, 4 + bob, Void2);
            s.Set(4, 1 + bob, VoidGlow); s.Set(6, 1 + bob, VoidGlow);                 // mắt
            if (scream) { Box(s, 4, 2 + bob, 7, 4 + bob, Outline); s.Set(5, 2 + bob, White); s.Set(7, 2 + bob, White); s.Set(6, 4 + bob, White); }
            else Box(s, 5, 3 + bob, 7, 3 + bob, Outline);
            if (scream)   // sóng âm
            {
                int r = 8 + (f % 2);
                s.Set(r, 1, VoidEye); s.Set(r + (r < 9 ? 1 : 0), 3, VoidEye); s.Set(r, 5, VoidEye);
            }
            return s;
        }

        static Canvas DrawVoidling(int state, int f)
        {
            var s = new Canvas(InfantryPx, InfantryPx);
            bool walk = state == 0;
            int hop = walk && (f & 1) == 1 ? 1 : 0;
            Box(s, 3, 5 - hop, 6, 7 - hop, Void1);
            s.Set(3, 5 - hop, Void2); s.Set(3, 7 - hop, Void2); s.Set(6, 7 - hop, Void2);
            s.Set(5, 6 - hop, VoidGlow);
            s.Set(4, 4 - hop, Void2);                                                  // sừng nhỏ
            s.Set(3, 8, Void3); s.Set(6, 8, Void3);
            if (!walk && (f == 1 || f == 2)) { s.Set(7, 6, Void1); s.Set(8, 6, VoidGlow); }   // cắn
            return s;
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
        // "Con dấu" in lên mặt đất: máu, giọt máu, tên cắm đất, vết cháy, xác của từng loại quân
        // ------------------------------------------------------------------
        public static Texture2D BuildStampAtlas() => BuildStampCanvas().ToTexture("StampAtlas");

        static Canvas BuildStampCanvas()
        {
            int S = StampPx;
            var a = new Canvas(S * StampCols, S * StampRows);
            var rnd = new System.Random(4242);

            for (int v = 0; v < 8; v++) Splat(a, v * S, 0, rnd, v >= 4);                  // hàng 0: vũng máu đỏ / máu đen

            for (int v = 0; v < 4; v++) Drops(a, v * S, S, rnd);                          // hàng 1: giọt máu, tên cắm đất
            StuckArrow(a, 4 * S, S, Wood, White, false);
            StuckArrow(a, 5 * S, S, Wood, White, true);
            StuckArrow(a, 6 * S, S, HorseLight, LeafLight, false);
            StuckArrow(a, 7 * S, S, HorseLight, LeafLight, true);

            for (int v = 0; v < 8; v++) Scorch(a, v * S, 2 * S, rnd, v >= 4);              // hàng 2: vết cháy, tro

            for (int t = 0; t < UnitType.Count; t++)                                       // từ hàng 3: xác (2 kiểu nằm / loại)
            {
                var body = DrawUnit(t, 0, 0);
                Darken(body, 0.72f);
                int pad = (S - body.W) / 2;
                for (int v = 0; v < 2; v++)
                {
                    int cell = StampCell.Corpse + t * 2 + v;
                    // quân to (xe, thú cưỡi) nằm nguyên chiều, lính bộ ngã xoay 90°
                    var lying = (t ==UnitType.Catapult || t == UnitType.Cannon) ? body : Rotate(body, v == 0);
                    a.Blit(lying, (cell % StampCols) * S + pad, (cell / StampCols) * S + pad);
                }
            }
            return a;
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
        // Đạn & bảng màu hạt hiệu ứng
        // ------------------------------------------------------------------
        public static Texture2D BuildArrowTexture()
        {
            var a = new Canvas(ProjW, ProjH * Proj.Count);
            DrawArrow(a, Proj.Arrow * ProjH, Wood, Steel, White);
            DrawArrow(a, Proj.ElfArrow * ProjH, HorseLight, LeafLight, Leaf);
            // tên nỏ: ngắn, đậm
            for (int x = 4; x <= 9; x++) a.Set(x, Proj.Bolt * ProjH + 1, Leather);
            a.Set(10, Proj.Bolt * ProjH + 1, Steel); a.Set(3, Proj.Bolt * ProjH, SteelDark); a.Set(3, Proj.Bolt * ProjH + 2, SteelDark);
            // đá
            Blob(a, Proj.Rock * ProjH, Rgb(150, 146, 156), Rgb(104, 100, 112), Rgb(186, 182, 192));
            // đạn pháo
            Blob(a, Proj.Cannonball * ProjH, Rgb(46, 46, 56), Rgb(24, 24, 30), Rgb(110, 112, 130));
            // lao
            for (int x = 1; x <= 9; x++) a.Set(x, Proj.Javelin * ProjH + 1, Wood);
            a.Set(10, Proj.Javelin * ProjH + 1, Steel); a.Set(11, Proj.Javelin * ProjH + 1, Steel); a.Set(10, Proj.Javelin * ProjH, Steel); a.Set(10, Proj.Javelin * ProjH + 2, Steel);
            // cầu phép: có vệt đuôi
            Orb(a, Proj.NatureOrb * ProjH, LeafLight, Leaf);
            Orb(a, Proj.VoidOrb * ProjH, VoidGlow, Void1);
            Orb(a, Proj.LightOrb * ProjH, Rgb(255, 250, 210), Gold);
            return a.ToTexture("Projectiles");
        }

        static void DrawArrow(Canvas a, int oy, Color32 shaft, Color32 head, Color32 fletch)
        {
            for (int x = 2; x <= 9; x++) a.Set(x, oy + 1, shaft);
            a.Set(0, oy, fletch); a.Set(1, oy, fletch); a.Set(1, oy + 1, fletch); a.Set(0, oy + 2, fletch); a.Set(1, oy + 2, fletch);
            a.Set(10, oy, head); a.Set(10, oy + 1, head); a.Set(10, oy + 2, head); a.Set(11, oy + 1, head);
        }

        static void Blob(Canvas a, int oy, Color32 main, Color32 dark, Color32 light)
        {
            for (int y = 0; y < 3; y++) for (int x = 5; x <= 7; x++) a.Set(x, oy + y, y == 2 || x == 5 ? dark : main);
            a.Set(7, oy, light);
        }

        static void Orb(Canvas a, int oy, Color32 core, Color32 glow)
        {
            for (int y = 0; y < 3; y++) for (int x = 7; x <= 9; x++) a.Set(x, oy + y, glow);
            a.Set(8, oy + 1, core); a.Set(9, oy + 1, core);
            a.Set(5, oy + 1, glow); a.Set(3, oy + 1, glow); a.Set(4, oy, glow);
        }

        public static Texture2D BuildPalette()
        {
            var cols = new Color32[16]
            {
                Rgb(186, 84, 232),   // 0 dịch hư không
                Rgb(186, 22, 26),    // 1 máu
                Rgb(112, 14, 20),    // 2 máu đen
                Rgb(96, 164, 62),    // 3 lá cây
                Rgb(150, 124, 86),   // 4 bụi
                Rgb(146, 100, 58),   // 5 dăm gỗ
                Rgb(236, 230, 206),  // 6 mảnh xương
                Rgb(255, 250, 210),  // 7 lửa trắng / ánh sáng
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
        // Biểu tượng giao diện (vẽ bằng chuỗi ký tự: '.' = trong suốt, '#' = viền, a/b/c = 3 màu)
        // ------------------------------------------------------------------
        public static Texture2D Icon(string name, string[] rows, Color32 a, Color32 b, Color32 c)
        {
            var cv = new Canvas(rows[0].Length, rows.Length);
            for (int y = 0; y < rows.Length; y++)
                for (int x = 0; x < rows[y].Length; x++)
                {
                    char ch = rows[y][x];
                    if (ch == '.') continue;
                    cv.Set(x, y, ch == '#' ? Outline : ch == 'a' ? a : ch == 'b' ? b : c);
                }
            return cv.ToTexture(name);
        }

        public static Texture2D HeartIcon() => Icon("Heart", new[]
        {
            ".##.##.",
            "#ab#aa#",
            "#aaaaa#",
            "#aaaac#",
            ".#aac#.",
            "..#c#..",
            "...#...",
        }, Rgb(226, 52, 64), Rgb(255, 170, 170), Rgb(150, 26, 44));

        /// <summary>
        /// Ngôi sao theo hạng của công trình: 1 sao = đồng, 2 = bạc, 3 = vàng, 4 = bạch kim xanh.
        /// a = màu chính, b = chấm sáng, c = bóng tối.
        /// </summary>
        public static Texture2D StarIcon(int stars)
        {
            Color32 a, b, c;
            switch (stars)
            {
                case 1: a = Rgb(214, 132, 62); b = Rgb(246, 196, 140); c = Rgb(146, 82, 38); break;     // đồng
                case 2: a = Rgb(204, 212, 224); b = Rgb(250, 252, 255); c = Rgb(128, 138, 156); break;  // bạc
                case 3: a = Rgb(255, 214, 64); b = Rgb(255, 240, 160); c = Rgb(214, 150, 30); break;    // vàng
                default: a = Rgb(124, 222, 255); b = Rgb(228, 252, 255); c = Rgb(58, 138, 214); break;  // bạch kim xanh
            }
            return Icon("Star" + stars, new[]
            {
                "...#...",
                "..#b#..",
                "###a###",
                "#abaaa#",
                ".#aca#.",
                "#ac#ca#",
                "##...##",
            }, a, b, c);
        }

        public static Texture2D CrystalIcon() => Icon("Crystal", new[]
        {
            "..###..",
            ".#bba#.",
            "#bbaaa#",
            "#baaac#",
            ".#aac#.",
            "..#c#..",
            "...#...",
        }, Rgb(90, 220, 240), Rgb(200, 250, 255), Rgb(40, 140, 190));

        /// <summary>Biểu tượng 4 tài nguyên, theo thứ tự ResId: Dân, Lương thực, Gỗ, Vàng.</summary>
        public static Texture2D[] ResourceIcons() => new[]
        {
            Icon("Pop", new[]
            {
                "..###..",
                ".#bbb#.",
                ".#bbb#.",
                "..###..",
                ".#aaa#.",
                "#aaaaa#",
                "#aa#aa#",
                ".##.##.",
            }, Rgb(86, 136, 230), Skin, White),
            Icon("Food", new[]
            {
                "..#.#..",
                ".#a#a#.",
                ".#a#a#.",
                "#a#a#a#",
                "#a#a#a#",
                ".##b##.",
                "..#b#..",
                "..#b#..",
            }, Rgb(240, 204, 84), Rgb(150, 170, 70), White),
            Icon("Wood", new[]
            {
                ".......",
                ".#####.",
                "#baaaa#",
                "#bacaa#",
                "#baaca#",
                "#baaaa#",
                ".#####.",
                ".......",
            }, Rgb(160, 106, 58), Rgb(226, 190, 130), Rgb(112, 72, 40)),
            Icon("Gold", new[]
            {
                "..###..",
                ".#aaa#.",
                "#abaaa#",
                "#abaac#",
                "#aaaac#",
                "#aaacc#",
                ".#ccc#.",
                "..###..",
            }, Rgb(250, 204, 60), Rgb(255, 244, 170), Rgb(196, 140, 30)),
        };

        // ------------------------------------------------------------------
        // Mặt đất: bên trong thành (trái) + tường thành + chiến trường cỏ/đất
        // ------------------------------------------------------------------
        public static Texture2D BuildGround(uint seed)
        {
            int W = Cfg.GroundW, H = Cfg.GroundH;
            var px = new NativeArray<Color32>(W * H, Allocator.TempJob);
            new GroundJob { pixels = px, width = W, seed = seed }.Schedule(W * H, 4096).Complete();
            DrawCastle(px);
            DrawEnemyEdge(px);

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

        static void DrawCastle(NativeArray<Color32> px)
        {
            int H = Cfg.GroundH, X0 = Cfg.CastlePx;

            // Sân trong thành: lát đá tối màu (lưới công trình của giao diện nằm đè lên vùng này)
            for (int y = 0; y < H; y++)
                for (int x = 0; x < X0; x++)
                {
                    int row = y / 12;
                    int sx = x + (row % 2) * 9;
                    bool mortar = y % 12 == 0 || sx % 18 == 0;
                    uint h = IHash(sx / 18, row) % 4;
                    Color32 c = mortar ? Rgb(40, 38, 46) : h == 0 ? Rgb(74, 72, 82) : h == 1 ? Rgb(68, 66, 76) : h == 2 ? Rgb(80, 76, 84) : Rgb(64, 62, 70);
                    if (IHash(x, y) % 53 == 0) c = Rgb(56, 70, 50);   // rêu
                    P(px, x, y, c);
                }

            // Tường thành (mỏng): lối đi gỗ ở trong, hàng châu mai, mặt đá hướng ra chiến trường
            int WP = Cfg.WallPx;
            for (int y = 0; y < H; y++)
                for (int x = 0; x < WP; x++)
                {
                    int row = y / 4;
                    bool mortar = y % 4 == 0 || (x + (row % 2) * 3) % 6 == 0;
                    Color32 c;
                    if (x >= WP - 3) c = mortar ? Rgb(48, 46, 56) : Rgb(86, 84, 96);                  // mặt tường hướng ra chiến trường
                    else if (x >= WP - 7) c = (y / 5) % 2 == 0 ? Rgb(170, 166, 178) : Rgb(104, 102, 114);   // châu mai
                    else if (x >= 2 && x < WP - 8) c = (y % 4 == 0) ? Rgb(84, 60, 38) : Rgb(118, 84, 52); // lối đi bằng gỗ
                    else c = mortar ? Rgb(70, 68, 78) : Rgb(136, 132, 144);
                    P(px, X0 + x, y, c);
                }

            // bóng tường đổ lên cỏ
            for (int y = 0; y < H; y++)
                for (int x = Cfg.WallPx; x < Cfg.WallPx + 6; x++)
                {
                    var g = GetP(px, X0 + x, y);
                    float k = 0.55f + (x - Cfg.WallPx) * 0.075f;
                    P(px, X0 + x, y, new Color32((byte)(g.r * k), (byte)(g.g * k), (byte)(g.b * k), 255));
                }

            for (int k = 0; k < 4; k++) Tower(px, X0 + Cfg.WallPx / 2, H / 8 + k * (H / 4));
        }

        static void Tower(NativeArray<Color32> px, int cx, int cy)
        {
            const int R = 11;
            for (int y = -R - 2; y <= R + 2; y++)
                for (int x = -R - 2; x <= R + 2; x++)
                {
                    float d = Mathf.Sqrt(x * x + y * y);
                    if (d > R + 1.5f) continue;
                    Color32 c;
                    if (d > R) c = Rgb(36, 32, 40);                                                     // viền
                    else if (d > R - 3) c = ((int)(Mathf.Atan2(y, x) * 5f) & 1) == 0 ? Rgb(156, 152, 164) : Rgb(126, 122, 136); // tường tháp
                    else if (d > R - 4) c = Rgb(70, 66, 78);
                    else
                    {
                        int ring = (int)(d / 2.5f);                                                   // mái ngói xanh đá
                        c = (ring & 1) == 0 ? Rgb(52, 74, 120) : Rgb(64, 90, 140);
                        if (y > 0 && x < 0) c = Rgb((byte)(c.r + 18), (byte)(c.g + 18), (byte)(c.b + 18));
                    }
                    P(px, cx + x, cy + y, c);
                }
            // cờ
            for (int y = 0; y < 5; y++) P(px, cx, cy + y, Rgb(60, 44, 32));
            for (int y = 3; y < 5; y++)
                for (int x = 1; x < 4; x++)
                    P(px, cx + x, cy + y, Tunic);
        }

        /// <summary>Mép phải, nơi quân địch kéo đến: đất bị giày xéo sẫm màu.</summary>
        static void DrawEnemyEdge(NativeArray<Color32> px)
        {
            int W = Cfg.GroundW, H = Cfg.GroundH;
            for (int y = 0; y < H; y++)
                for (int x = W - 70; x < W; x++)
                {
                    float edge = W - 58 + 8f * Mathf.Sin(y * 0.045f) + (IHash(x, y) % 7);
                    if (x < edge) continue;
                    P(px, x, y, (IHash(x, y + 7) % 7 == 0) ? Rgb(58, 50, 56) : Rgb(70, 60, 66));
                }
        }
    }
}
