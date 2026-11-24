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
        // Quân khổng lồ (UFlag.Giant: Rồng, Hoàng đế vàng — nằm CUỐI bảng): 32x32, 8 hàng atlas thay vì 2 (UnitType.GiantRow)
        public const int GiantPx = 32;
        public const int UnitCell = 16, InfantryPx = 10, UnitCols = 4, UnitRows = UnitType.Count * 2 + 6 * UnitType.GiantCount;
        public const int StampPx = 20, StampCols = 8, StampRows = 3 + (UnitType.Count * 2 + StampCols - 1) / StampCols;
        public const int ProjW = 24, ProjH = 11;  // mỗi loại đạn 1 hàng 24x11 pixel (đạn thường vẽ 12x3 ở giữa, thiên thạch / nỏ to vẽ to hơn)

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
            var sheet = new Canvas(GiantPx * 8, UnitCell * UnitType.Count + (GiantPx - UnitCell) * UnitType.GiantCount);
            for (int type = 0; type < UnitType.Count; type++)
                for (int state = 0; state < 2; state++)
                    for (int f = 0; f < 4; f++)
                    {
                        var sprite = DrawUnit(type, state, f);
                        int cell = sprite.W > UnitCell ? GiantPx : UnitCell;          // quân khổng lồ: khung rộng 32
                        int pad = (cell - sprite.W) / 2;
                        int extra = type > UnitType.FirstGiant ? (type - UnitType.FirstGiant) * (GiantPx - UnitCell) : 0;   // quân khổng lồ cao 32
                        sheet.Blit(sprite, (state * 4 + f) * cell + pad, type * UnitCell + extra + pad);
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
                        if (sprite.W > UnitCell)
                        {
                            // khổng lồ 32x32: khung f nằm ở cột (f & 1) * 2, hàng GiantRow + state*4 + (f >> 1)*2 (khớp PackUnitsJob)
                            atlas.Blit(sprite, (f & 1) * GiantPx, (UnitType.GiantRow(type) + state * 4 + (f >> 1) * 2) * UnitCell);
                            continue;
                        }
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
        static readonly Color32 OrcSkin = Rgb(106, 160, 64), OrcSkinD = Rgb(70, 114, 44), OrcLeather = Rgb(112, 78, 48), OrcLeatherD = Rgb(76, 52, 32);
        static readonly Color32 OrcIron = Rgb(78, 80, 90), OrcIronL = Rgb(128, 132, 144), OrcRed = Rgb(170, 44, 38), OrcRedD = Rgb(116, 30, 28), OrcEye = Rgb(236, 64, 40);
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
                case UnitType.Grunt:
                    return new Look { leg = OrcLeatherD, body = OrcLeather, bodyDark = OrcLeatherD, belt = OrcIron, top = OrcSkinD, back = OrcSkinD, face = OrcSkin, faceLow = OrcSkinD,
                                      hasEye = true, eye = OrcEye, weapon = WAxe, w1 = OrcIronL, w2 = Wood, extra = XEars, ex = OrcSkin };
                case UnitType.SpearThrower:
                    return new Look { leg = OrcLeatherD, body = OrcRed, bodyDark = OrcRedD, belt = OrcLeather, top = OrcSkinD, back = OrcSkinD, face = OrcSkin, faceLow = OrcSkinD,
                                      hasEye = true, eye = OrcEye, weapon = WJavelin, w1 = Wood, w2 = OrcIronL, extra = XEars, ex = OrcSkin };
                case UnitType.Drummer:
                    return new Look { leg = OrcLeatherD, body = Rgb(140, 108, 74), bodyDark = Rgb(96, 72, 48), belt = Bone, top = OrcIron, back = OrcSkinD, face = OrcSkin, faceLow = OrcSkinD,
                                      hasEye = true, eye = OrcEye, weapon = WStaff, w1 = Wood, w2 = Bone, shield = true, sh1 = Rgb(196, 160, 110), sh2 = OrcRed, extra = XHorns, ex = Bone };
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
        public static Canvas DrawUnit(int type, int state, int f, bool useFiles = true)
        {
            if (useFiles)
            {
                var art = UnitFrameFromFile(type, state, f);     // có file PNG (Resources/Art/Units) thì dùng nguyên, hoạ sĩ tự vẽ viền
                if (art != null) return art;
            }
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

        /// <summary>
        /// Shader vẽ hình căn giữa quad; vị trí của lính lại là chỗ chân. Trả về số pixel phải nâng hình lên:
        /// khoảng từ tâm quad xuống hàng pixel thấp nhất (thường là viền dưới chân) của các khung đi.
        /// </summary>
        public static int FootLiftPx(int type)
        {
            int lowest = -1, cell = UnitCell, pad = 0;
            for (int f = 0; f < 4; f++)
            {
                var s = DrawUnit(type, 0, f);
                cell = s.W > UnitCell ? GiantPx : UnitCell;
                pad = (cell - s.W) / 2;
                for (int y = s.H - 1; y > lowest; y--)
                {
                    bool any = false;
                    for (int x = 0; x < s.W && !any; x++) any = s.Get(x, y).a > 0;
                    if (any) { lowest = y; break; }
                }
            }
            return lowest < 0 ? 0 : Mathf.Max(0, pad + lowest - cell / 2);
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
                case UnitType.BoarRider: return DrawRider(3, state, f, true);
                case UnitType.Troll: return DrawTroll(state, f);
                case UnitType.Warboss: return DrawWarboss(state, f);
                case UnitType.Aberration: return DrawAberration(state, f);
                case UnitType.Shade: return DrawShade(state, f);
                case UnitType.Eye: return DrawEye(state, f);
                case UnitType.Screamer: return DrawScreamer(state, f);
                case UnitType.Voidling: return DrawVoidling(state, f);
                case UnitType.Dragon: return DrawDragon(state, f);
                case UnitType.Spiderling: return DrawSpiderling(state, f);
                case UnitType.Emperor: return DrawEmperor(state, f);
                case UnitType.WorldTree: return DrawWorldTree(state, f);
                case UnitType.Thor: return DrawThor(state, f);
                case UnitType.Berserker: return DrawBerserker(state, f);
                case UnitType.Fenrir: return DrawFenrir(state, f);
                case UnitType.Broodmother: return DrawBroodmother(state, f);
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
            if (kind == 3) { coat = Rgb(112, 80, 58); coatLight = Rgb(150, 112, 80); coatDark = Rgb(72, 50, 38); mane = Rgb(46, 34, 28); cloth = OrcRed; clothDark = OrcRedD; }

            var s = new Canvas(UnitCell, UnitCell);
            bool walk = state == 0;
            int g = walk ? f : 1;                 // tư thế chân: đứng đánh thì chân thẳng
            int bob = walk && (f == 1 || f == 3) ? -1 : 0;   // nhún lên khi chân duỗi thẳng

            // --- chân (vẽ trước để thân đè lên) ---
            Color32 legC = kind == 3 ? Rgb(60, 42, 32) : kind == 2 ? Rgb(120, 118, 114) : kind == 1 ? Rgb(150, 104, 62) : HorseDark;
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
            if (kind == 3)   // lợn rừng: bờm lông dựng dọc lưng + răng nanh
            {
                s.Set(5, by - 1, mane); s.Set(7, by - 1, mane); s.Set(9, by - 1, mane); s.Set(11, by - 2, mane);
                s.Set(14, by - 1, White); s.Set(14, by - 2, White);
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
                Color32 tunic = kind == 1 ? ElfGreen : kind == 2 ? DwarfTunic : kind == 3 ? OrcLeather : Tunic;
                Color32 tunicDark = kind == 1 ? ElfDark : kind == 2 ? DwarfDark : kind == 3 ? OrcLeatherD : TunicDark;
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

            if (rider && kind == 3)
            {
                for (int y = 0; y < s.H; y++)
                    for (int x = 0; x < s.W; x++)
                    {
                        var c = s.Get(x, y);
                        if (c.Equals(Skin)) s.Set(x, y, OrcSkin);
                        else if (c.Equals(Beard)) s.Set(x, y, OrcSkinD);
                    }
                int ry2 = 8 + (walk && (f == 1 || f == 3) ? -1 : 0) - 1;
                s.Set(7, ry2 - 4, OrcIron); s.Set(8, ry2 - 4, OrcIron); s.Set(6, ry2 - 5, Bone); s.Set(9, ry2 - 5, Bone);   // mũ sắt sừng
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

        // ------------------------------------------------------------------
        // Rồng 32x32 (quân khổng lồ). Hình học nhỏ: đa giác, elip, đường thẳng dày.
        // ------------------------------------------------------------------
        static bool InPoly(float x, float y, Vector2[] p)
        {
            bool inside = false;
            for (int i = 0, j = p.Length - 1; i < p.Length; j = i++)
                if ((p[i].y > y) != (p[j].y > y) && x < (p[j].x - p[i].x) * (y - p[i].y) / (p[j].y - p[i].y) + p[i].x)
                    inside = !inside;
            return inside;
        }

        /// <summary>Tô đa giác (toạ độ pixel, y xuống), dịch xuống <paramref name="dy"/>. <paramref name="shade"/> chọn màu theo (x, y).</summary>
        static void FillPoly(Canvas s, Vector2[] pts, int dy, System.Func<int, int, Color32> shade)
        {
            float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;
            foreach (var q in pts) { minX = Mathf.Min(minX, q.x); maxX = Mathf.Max(maxX, q.x); minY = Mathf.Min(minY, q.y); maxY = Mathf.Max(maxY, q.y); }
            for (int y = Mathf.FloorToInt(minY); y <= Mathf.CeilToInt(maxY); y++)
                for (int x = Mathf.FloorToInt(minX); x <= Mathf.CeilToInt(maxX); x++)
                    if (InPoly(x + 0.5f, y + 0.5f, pts)) s.Set(x, y + dy, shade(x, y));
        }

        static void Line(Canvas s, int x0, int y0, int x1, int y1, Color32 c)
        {
            int dx = Mathf.Abs(x1 - x0), sx = x0 < x1 ? 1 : -1, dy = -Mathf.Abs(y1 - y0), sy = y0 < y1 ? 1 : -1, err = dx + dy;
            while (true)
            {
                s.Set(x0, y0, c);
                if (x0 == x1 && y0 == y1) break;
                int e2 = 2 * err;
                if (e2 >= dy) { err += dy; x0 += sx; }
                if (e2 <= dx) { err += dx; y0 += sy; }
            }
        }

        static Vector2[] V(params float[] xy)
        {
            var v = new Vector2[xy.Length / 2];
            for (int i = 0; i < v.Length; i++) v[i] = new Vector2(xy[i * 2], xy[i * 2 + 1]);
            return v;
        }

        /// <summary>
        /// Rồng đỏ 32x32 bay lơ lửng, quay mặt sang phải. Đi = đập cánh 4 khung (lên / ngang / xuống / ngang).
        /// Đánh = khung 0-1 ngửa đầu lấy hơi, khung 2-3 há miệng phun lửa. Chừa 1 pixel mép cho viền.
        /// </summary>
        static Canvas DrawDragon(int state, int f)
        {
            var s = new Canvas(GiantPx, GiantPx);
            Color32 red = Rgb(196, 50, 44), dark = Rgb(122, 26, 36), deep = Rgb(88, 18, 30), light = Rgb(236, 98, 66), shine = Rgb(255, 150, 104);
            Color32 belly = Rgb(248, 200, 112), bellyDark = Rgb(214, 150, 78);
            Color32 memb = Rgb(150, 36, 52), membLight = Rgb(196, 70, 74), membFar = Rgb(104, 24, 40);
            Color32 horn = Rgb(236, 226, 196), hornDark = Rgb(170, 156, 128), eye = Rgb(255, 228, 80), fire = Rgb(255, 214, 80), fire2 = Rgb(255, 132, 36), fire3 = Rgb(255, 246, 190);
            bool attack = state == 1;
            int wingPose = attack ? (f < 2 ? 0 : 2) : f;                     // đánh: giơ cánh lấy đà rồi đập xuống lúc phun
            int b = wingPose == 2 ? -1 : wingPose == 0 ? 1 : 0;              // cánh đập xuống thì thân bốc lên
            int hx = attack && f < 2 ? -1 : 0, hy = attack && f < 2 ? -1 : 0; // ngửa đầu lấy hơi
            bool open = attack && f >= 2;

            // ---- cánh xa (sau thân, tối) ----
            Vector2[] farWing = wingPose == 0 ? V(17, 14, 15, 3, 21, 6, 20, 13)
                              : wingPose == 2 ? V(17, 15, 21, 22, 18, 24, 16, 18)
                              : V(17, 14, 22, 9, 24, 11, 19, 16);
            FillPoly(s, farWing, b, (x, y) => membFar);

            // ---- đuôi: cong xuống rồi hất lên, chóp hình mũi giáo ----
            int tw = wingPose == 1 ? 1 : wingPose == 3 ? -1 : 0;               // đuôi đung đưa
            FillPoly(s, V(10, 17, 10, 22, 6, 23, 3, 22 + tw, 2, 20 + tw, 5, 21, 8, 19), b, (x, y) => y >= 22 ? dark : red);
            FillPoly(s, V(1, 17 + tw, 4, 19 + tw, 2, 22 + tw, 1, 20 + tw), b, (x, y) => dark);   // chóp đuôi

            // ---- chân sau + chân trước (co dưới bụng khi bay) ----
            FillPoly(s, V(10, 21, 14, 21, 13, 26, 11, 27, 9, 25), b, (x, y) => y > 24 ? dark : red);
            s.Set(10, 28 + b, horn); s.Set(12, 28 + b, horn); s.Set(11, 28 + b, hornDark);                      // vuốt sau
            FillPoly(s, V(19, 21, 22, 21, 22, 24, 20, 26, 18, 24), b, (x, y) => y > 23 ? dark : red);
            s.Set(20, 27 + b, horn); s.Set(21, 26 + b, horn);                                                   // vuốt trước

            // ---- thân: elip, sáng ở lưng, bụng vàng có vạch ----
            for (int y = 13; y <= 23; y++)
                for (int x = 7; x <= 24; x++)
                {
                    float dx = (x + 0.5f - 15.5f) / 8.5f, dy = (y + 0.5f - 18f) / 5f;
                    if (dx * dx + dy * dy > 1f) continue;
                    Color32 c = y <= 14 ? light : y >= 21 ? ((x & 1) == 0 ? bellyDark : belly) : y >= 20 ? belly : red;
                    if (y == 15 && x > 10 && x < 20) c = shine;
                    if (x <= 9 && y < 21) c = dark;                                // hông sau tối
                    s.Set(x, y + b, c);
                }

            // ---- gai lưng ----
            foreach (int gx in new[] { 9, 12, 15, 18 }) { s.Set(gx, 12 + b, horn); s.Set(gx + 1, 13 + b, hornDark); }

            // ---- cổ cong lên phải ----
            FillPoly(s, V(19, 14, 22, 12, 24, 9 + hy, 27, 9 + hy, 26, 13 + hy, 24, 17, 22, 19), b, (x, y) => x >= 25 || y >= 16 ? belly : red);

            // ---- đầu ----
            int ox = hx, oy = hy + b;
            FillPoly(s, V(23 + ox, 6, 28 + ox, 6, 30 + ox, 8, 30 + ox, 10, 27 + ox, 11, 23 + ox, 11, 22 + ox, 9), oy, (x, y) => y <= 6 ? light : red);   // sọ + mõm
            s.Set(29 + ox, 8 + oy, dark);                                                           // lỗ mũi
            s.Set(26 + ox, 8 + oy, eye); s.Set(25 + ox, 8 + oy, deep);                                // mắt
            Line(s, 23 + ox, 6 + oy, 20 + ox, 3 + oy, horn); s.Set(21 + ox, 3 + oy, hornDark);       // sừng dài
            Line(s, 25 + ox, 6 + oy, 23 + ox, 4 + oy, hornDark);                                    // sừng ngắn
            s.Set(22 + ox, 10 + oy, dark); s.Set(22 + ox, 11 + oy, dark);                            // má

            if (open)
            {
                // há miệng: hàm dưới hạ xuống, răng trắng, lửa phụt ra
                for (int x = 25; x <= 30; x++) s.Set(x, 11 + oy, deep);
                for (int x = 24; x <= 29; x++) s.Set(x, 12 + oy, x <= 25 ? red : dark);
                s.Set(27, 10 + oy, fire3); s.Set(29, 10 + oy, fire3);                               // răng
                s.Set(30, 11 + oy, fire); s.Set(30, 12 + oy, fire2);
                if (f == 3) { s.Set(30, 10 + oy, fire2); s.Set(30, 13 + oy, fire); s.Set(29, 13 + oy, fire2); }
            }
            else
            {
                for (int x = 23; x <= 29; x++) s.Set(x + ox, 11 + oy, x >= 27 ? dark : red);       // hàm khép
                s.Set(28 + ox, 11 + oy, fire3);                                                          // nanh
            }

            // ---- cánh gần (trước thân): xương cánh đỏ sẫm + màng có vân ----
            Vector2[] wing; int[] bone;
            switch (wingPose)
            {
                case 0: wing = V(16, 15, 12, 2, 8, 1, 2, 4, 5, 9, 9, 13, 13, 17); bone = new[] { 16, 15, 12, 2, 12, 2, 8, 1, 12, 2, 2, 4 }; break;      // lên
                case 2: wing = V(16, 16, 12, 22, 9, 29, 6, 29, 5, 25, 8, 20, 12, 17); bone = new[] { 16, 16, 12, 22, 12, 22, 9, 29, 12, 22, 5, 25 }; break; // xuống
                default: wing = V(16, 15, 9, 10, 1, 9, 2, 13, 6, 16, 11, 18); bone = new[] { 16, 15, 9, 10, 9, 10, 1, 9, 9, 10, 2, 13 }; break;            // ngang
            }
            FillPoly(s, wing, b, (x, y) => ((x + y) % 4 == 0) ? membLight : memb);
            for (int i = 0; i + 3 < bone.Length; i += 4) Line(s, bone[i], bone[i + 1] + b, bone[i + 2], bone[i + 3] + b, dark);
            s.Set(bone[2], bone[3] + b, horn);                                                       // móng ở khớp cánh
            return s;
        }

        /// <summary>Tô elip (cx, cy, rx, ry) bằng hàm màu theo toạ độ.</summary>
        static void Ellipse(Canvas s, float cx, float cy, float rx, float ry, System.Func<int, int, Color32> shade)
        {
            for (int y = (int)(cy - ry) - 1; y <= (int)(cy + ry) + 1; y++)
                for (int x = (int)(cx - rx) - 1; x <= (int)(cx + rx) + 1; x++)
                {
                    float dx = (x + 0.5f - cx) / rx, dy = (y + 0.5f - cy) / ry;
                    if (dx * dx + dy * dy <= 1f) s.Set(x, y, shade(x, y));
                }
        }

        /// <summary>Nét dày 2 pixel: nét chính + nét bóng ngay dưới.</summary>
        static void Thick(Canvas s, int x0, int y0, int x1, int y1, Color32 c, Color32 under)
        {
            Line(s, x0, y0 + 1, x1, y1 + 1, under);
            Line(s, x0, y0, x1, y1, c);
        }

        // =====================================================================
        // Boss khổng lồ 32x32, quay mặt sang phải (địch thì shader lật lại). Chân chạm hàng 29.
        // =====================================================================

        /// <summary>Hoàng đế vàng: giáp vàng, áo choàng xanh viền vàng, áo khoác hoàng gia, râu bạc, vương miện nạm ngọc, đại kiếm.</summary>
        static Canvas DrawEmperor(int state, int f)
        {
            var s = new Canvas(GiantPx, GiantPx);
            Color32 gold = Rgb(230, 180, 58), goldL = Rgb(255, 228, 126), goldH = Rgb(255, 248, 200), goldD = Rgb(170, 118, 34), goldX = Rgb(110, 72, 24);
            Color32 cape = Rgb(46, 72, 166), capeD = Rgb(28, 42, 106), capeL = Rgb(78, 112, 206), trim = Rgb(238, 196, 84);
            Color32 skin = Rgb(236, 186, 148), skinD = Rgb(190, 132, 102), beard = Rgb(242, 240, 232), beardD = Rgb(184, 180, 172);
            Color32 steel = Rgb(232, 238, 248), steelD = Rgb(146, 158, 182), ruby = Rgb(220, 40, 56), sapph = Rgb(70, 150, 255), eye = Rgb(40, 30, 50), arc = Rgb(255, 240, 180);
            bool attack = state == 1, stride = !attack && (f & 1) == 1;
            int b = stride ? 1 : 0, sway = attack ? (f < 2 ? -1 : 1) : (f == 1 ? 1 : f == 3 ? -1 : 0);

            // áo choàng
            FillPoly(s, V(10, 10, 20, 10, 21, 15, 19, 27, 16, 29 + 0, 6 + sway, 29, 4 + sway, 27, 8, 15), b,
                     (x, y) => y >= 28 + b ? trim : x <= 6 + sway ? capeD : x == 8 || x == 12 ? capeD : x == 9 || x == 13 ? capeL : cape);
            // chân
            int bl = stride ? 11 : 12, fl = stride ? 18 : 17;
            Box(s, bl, 23 + b, bl + 2, 28, goldD); s.Set(bl + 1, 24 + b, gold); Box(s, bl - 1, 29, bl + 2, 29, goldX);
            Box(s, fl, 23 + b, fl + 2, 28, gold); s.Set(fl, 23 + b, goldD); s.Set(fl + 1, 24 + b, goldH); Box(s, fl, 29, fl + 3, 29, goldD);
            // váy giáp
            for (int y = 20; y <= 23; y++)
                for (int x = 11 - (y - 20) / 2; x <= 20 + (y - 20) / 2; x++)
                    s.Set(x, y + b, y == 23 ? goldD : x == 13 || x == 16 || x == 19 ? goldX : x <= 12 ? goldD : gold);
            // giáp ngực
            for (int y = 12; y <= 19; y++)
                for (int x = 11; x <= 20; x++)
                    s.Set(x, y + b, x <= 12 || x >= 20 ? goldD : (x >= 14 && x <= 16 && y >= 13 && y <= 16) ? goldL : gold);
            s.Set(15, 13 + b, goldH); s.Set(14, 14 + b, goldH);
            // áo khoác hoàng gia xanh + huy hiệu
            Box(s, 14, 16 + b, 17, 23 + b, cape); Line(s, 14, 16 + b, 14, 23 + b, trim); Line(s, 17, 16 + b, 17, 23 + b, trim);
            s.Set(15, 18 + b, goldL); s.Set(16, 18 + b, goldL); s.Set(15, 19 + b, gold); s.Set(16, 19 + b, gold); s.Set(15, 17 + b, goldL); s.Set(16, 20 + b, goldD);
            // thắt lưng
            Line(s, 11, 20 + b, 20, 20 + b, goldX); s.Set(15, 20 + b, ruby); s.Set(16, 20 + b, ruby);
            // vai giáp
            Ellipse(s, 10.5f, 12.5f + b, 2.6f, 2.2f, (x, y) => y <= 11 + b ? gold : goldD);
            Ellipse(s, 20.5f, 12.5f + b, 3f, 2.4f, (x, y) => y <= 11 + b ? goldL : y >= 14 + b ? goldD : gold);
            s.Set(20, 11 + b, goldH);
            // đầu: tóc bạc sau gáy, mặt, lông mày, mũi, râu dài
            Box(s, 13, 5 + b, 13, 10 + b, beardD);
            Box(s, 14, 5 + b, 18, 10 + b, skin); Box(s, 14, 5 + b, 14, 10 + b, skinD);
            Line(s, 16, 6 + b, 18, 6 + b, beard); s.Set(17, 7 + b, eye); s.Set(19, 8 + b, skin);
            FillPoly(s, V(14, 9, 20, 9, 20, 12, 18, 15, 16, 15, 14, 12), b, (x, y) => x <= 15 || y >= 14 ? beardD : beard);
            Line(s, 17, 9 + b, 19, 9 + b, beardD);
            // vương miện
            Box(s, 13, 3 + b, 19, 4 + b, gold); Line(s, 13, 3 + b, 19, 3 + b, goldL);
            s.Set(13, 2 + b, goldL); s.Set(16, 1 + b, goldH); s.Set(16, 2 + b, goldL); s.Set(19, 2 + b, goldL); s.Set(14, 2 + b, gold); s.Set(18, 2 + b, gold);
            s.Set(16, 3 + b, ruby); s.Set(14, 4 + b, sapph); s.Set(18, 4 + b, sapph);

            // tay + đại kiếm
            if (!attack)
            {
                Thick(s, 21, 14 + b, 22, 17 + b, gold, goldD);
                Box(s, 22, 3 + b, 22, 15 + b, steel); Box(s, 23, 3 + b, 23, 15 + b, steelD); s.Set(22, 2 + b, steel);
                Line(s, 20, 16 + b, 25, 16 + b, goldL); s.Set(20, 16 + b, goldD); s.Set(25, 16 + b, goldD);
                Box(s, 22, 17 + b, 23, 18 + b, goldD); s.Set(22, 19 + b, ruby);
            }
            else if (f < 2)
            {
                int k = f;                                                       // kéo kiếm ra sau đầu lấy đà
                Thick(s, 21, 13, 20, 8 - k, gold, goldD);
                Thick(s, 19, 7 - k, 10 - k, 1, steel, steelD);
                s.Set(20, 6 - k, goldL); s.Set(21, 8 - k, goldL); s.Set(21, 7 - k, goldD);
            }
            else if (f == 2)
            {
                Thick(s, 21, 14, 24, 13, gold, goldD);
                Line(s, 25, 12, 30, 12, steel); Line(s, 25, 13, 30, 13, steelD); s.Set(31, 12, steel);
                Line(s, 25, 10, 25, 15, goldL);
                for (int y = 3; y <= 10; y++) s.Set(26 + (y - 3) / 2, y, arc);  // vệt chém
            }
            else
            {
                Thick(s, 21, 14, 24, 17, gold, goldD);
                Thick(s, 25, 18, 30, 25, steel, steelD);
                Line(s, 23, 19, 26, 16, goldL);
                for (int y = 10; y <= 24; y++) s.Set(30 - (y < 18 ? 0 : (y - 18) / 3), y, arc);
            }
            return s;
        }

        /// <summary>The World Tree: cây đại thụ khổng lồ, tán lá rộng có đốm sáng linh hồn, mặt trong thân, rễ làm chân, tay cành nắm đấm.</summary>
        static Canvas DrawWorldTree(int state, int f)
        {
            var s = new Canvas(GiantPx, GiantPx);
            Color32 bark = Rgb(118, 84, 54), barkD = Rgb(76, 52, 34), barkL = Rgb(162, 120, 76), barkX = Rgb(46, 30, 20);
            Color32 leafX = Rgb(32, 78, 44), leafD = Rgb(50, 110, 56), leaf = Rgb(78, 152, 70), leafL = Rgb(128, 196, 88), leafH = Rgb(188, 232, 132);
            Color32 spirit = Rgb(206, 255, 240), amber = Rgb(255, 204, 84), moss = Rgb(96, 146, 64);
            bool attack = state == 1, stride = !attack && (f & 1) == 1;
            int b = stride ? 1 : 0;

            // tay cành phía sau
            Thick(s, 12, 15 + b, 6, 20 + b, barkD, barkX); Box(s, 4, 20 + b, 6, 22 + b, barkD);
            // rễ chân
            int sl = stride ? -1 : 0, sr = stride ? 1 : 0;
            FillPoly(s, V(10 + sl, 23, 15 + sl, 23, 14 + sl, 28, 9 + sl, 29, 7 + sl, 28), 0, (x, y) => x <= 10 + sl ? barkX : barkD);
            FillPoly(s, V(17 + sr, 23, 22 + sr, 23, 25 + sr, 28, 23 + sr, 29, 17 + sr, 28), 0, (x, y) => x >= 22 + sr ? barkD : bark);
            Line(s, 6 + sl, 29, 9 + sl, 29, barkD); Line(s, 23 + sr, 29, 26 + sr, 29, barkD);
            // thân
            FillPoly(s, V(12, 11, 20, 11, 22, 24, 10, 24), b,
                     (x, y) => x <= 12 ? barkX : x <= 13 ? barkD : x >= 20 ? barkL : ((x == 15 || x == 18) && (y & 1) == 0) ? barkD : bark);
            Box(s, 11, 20 + b, 12, 22 + b, moss); s.Set(20, 12 + b, moss); s.Set(21, 13 + b, moss);
            // mặt
            Line(s, 14, 14 + b, 19, 14 + b, barkX);
            s.Set(15, 15 + b, amber); s.Set(18, 15 + b, amber); s.Set(14, 15 + b, barkX); s.Set(17, 15 + b, barkX);
            s.Set(17, 16 + b, barkL); s.Set(17, 17 + b, barkL);
            Box(s, 15, 19 + b, 18, 20 + b, barkX); s.Set(16, 21 + b, moss); s.Set(17, 21 + b, leafD);
            // tán lá: nhiều cụm chồng lên nhau, sáng từ trên xuống
            System.Func<int, int, Color32> canopy = (x, y) =>
            {
                int yy = y - b; bool dith = ((x * 7 + yy * 3) % 5) == 0;
                return yy <= 1 ? leafH : yy <= 4 ? (dith ? leaf : leafL) : yy <= 8 ? (dith ? leafD : leaf) : yy <= 11 ? (dith ? leafX : leafD) : leafX;
            };
            Ellipse(s, 8f, 8f + b, 7f, 4.6f, canopy);
            Ellipse(s, 24f, 8f + b, 7f, 4.6f, canopy);
            Ellipse(s, 16f, 5f + b, 9.5f, 5.2f, canopy);
            Ellipse(s, 12f, 11f + b, 5f, 2.6f, canopy);
            Ellipse(s, 21f, 11f + b, 5f, 2.6f, canopy);
            int[] hl = { 5, 3, 14, 1, 22, 2, 27, 5, 10, 5 };                              // điểm sáng trên ngọn
            for (int k = 0; k + 1 < hl.Length; k += 2) s.Set(hl[k], hl[k + 1] + b, leafH);
            // đốm sáng linh hồn lấp lánh
            int tw = f & 3;
            s.Set(tw == 0 ? 6 : 7, 7 + b, spirit); s.Set(tw == 1 ? 20 : 19, 4 + b, spirit); s.Set(26, tw == 2 ? 9 + b : 10 + b, spirit); s.Set(13, 8 + b, tw == 3 ? spirit : leafL);
            // dây leo rủ
            Line(s, 4, 11 + b, 4, 15 + b, leafD); Line(s, 27, 11 + b, 27, 14 + b, leafD); s.Set(27, 15 + b, leafL);

            // tay cành phía trước + nắm đấm gỗ
            if (!attack)
            {
                Thick(s, 20, 15 + b, 25, 20 + b, bark, barkD);
                Box(s, 25, 20 + b, 28, 23 + b, bark); Box(s, 25, 23 + b, 28, 23 + b, barkD); s.Set(27, 20 + b, barkL); s.Set(26, 19 + b, leafL);
            }
            else if (f < 2)
            {
                Thick(s, 20, 15, 22, 18, bark, barkD);                                       // co tay lấy đà
                Box(s, 20 - f, 17, 23 - f, 20, bark); s.Set(22 - f, 17, barkL); Box(s, 20 - f, 20, 23 - f, 20, barkD);
            }
            else
            {
                Box(s, 20, 14, 27, 16, bark); Line(s, 20, 16, 27, 16, barkD); Line(s, 21, 14, 27, 14, barkL);   // tay duỗi thẳng
                Box(s, 27, 12, 30, 17, bark); Line(s, 27, 17, 30, 17, barkD); s.Set(29, 12, barkL); s.Set(30, 13, barkL);
                if (f == 3) { s.Set(31, 10, leafH); s.Set(31, 15, spirit); s.Set(31, 19, leafL); s.Set(30, 9, leafL); s.Set(30, 20, leafH); }
            }
            return s;
        }

        /// <summary>Thor: thần sấm Bắc Âu, râu tóc đỏ tết bím, mũ sắt tròn, áo giáp xích, áo choàng lông thú, đai vàng, búa sấm cán ngắn toé điện.</summary>
        static Canvas DrawThor(int state, int f)
        {
            var s = new Canvas(GiantPx, GiantPx);
            Color32 skin = Rgb(232, 174, 132), skinD = Rgb(188, 128, 94), red = Rgb(204, 78, 36), redD = Rgb(140, 48, 24), redL = Rgb(240, 124, 62);
            Color32 mail = Rgb(154, 160, 176), mailD = Rgb(98, 102, 120), lea = Rgb(112, 74, 46), leaD = Rgb(74, 46, 30);
            Color32 fur = Rgb(128, 98, 66), furD = Rgb(86, 64, 44), furL = Rgb(176, 142, 102), gold = Rgb(228, 178, 60), goldL = Rgb(255, 222, 120);
            Color32 iron = Rgb(94, 98, 114), ironL = Rgb(156, 162, 182), ironD = Rgb(58, 60, 72), zap = Rgb(150, 222, 255), zapH = Rgb(236, 250, 255);
            bool attack = state == 1, stride = !attack && (f & 1) == 1;
            int b = stride ? 1 : 0;

            // áo choàng lông
            FillPoly(s, V(8, 11, 13, 11, 12, 25, 7, 27, 5, 23, 6, 15), b, (x, y) => x <= 7 ? furD : ((x * 3 + y) % 5 == 0) ? furL : fur);   // áo choàng lông vắt sau vai
            // tay sau
            Box(s, 8, 14 + b, 9, 19 + b, skinD); Box(s, 8, 17 + b, 9, 18 + b, leaD);
            // chân: quần da, ủng viền lông
            int bl = stride ? 10 : 11, fl = stride ? 18 : 17;
            Box(s, bl, 22 + b, bl + 2, 27, leaD); Box(s, bl - 1, 27, bl + 2, 29, leaD); Line(s, bl - 1, 27, bl + 2, 27, furL);
            Box(s, fl, 22 + b, fl + 3, 27, lea); Box(s, fl, 27, fl + 4, 29, lea); Line(s, fl, 27, fl + 3, 27, furL); s.Set(fl, 23 + b, leaD);
            // áo giáp xích + đai vàng
            for (int y = 13; y <= 21; y++)
                for (int x = 10; x <= 21; x++)
                    s.Set(x, y + b, x <= 11 ? mailD : (y & 1) == 0 ? mail : (x & 1) == 0 ? mailD : mail);   // vòng xích xếp hàng
            Box(s, 10, 19 + b, 21, 20 + b, gold); Line(s, 10, 19 + b, 21, 19 + b, goldL); Box(s, 15, 19 + b, 16, 20 + b, goldL);
            Line(s, 10, 21 + b, 21, 21 + b, leaD);
            // cổ áo lông dày
            Ellipse(s, 15.5f, 12f + b, 7f, 1.8f, (x, y) => y <= 11 + b ? furL : fur);
            // đầu: tóc đỏ sau gáy, mặt, mũ sắt có che mũi, mắt sáng điện
            Box(s, 12, 4 + b, 13, 12 + b, redD);
            Box(s, 14, 5 + b, 18, 10 + b, skin); Box(s, 14, 5 + b, 14, 10 + b, skinD);
            Ellipse(s, 16f, 4f + b, 3.6f, 2.2f, (x, y) => y <= 2 + b ? ironL : iron);
            Line(s, 12, 5 + b, 19, 5 + b, ironD); s.Set(18, 6 + b, iron); s.Set(18, 7 + b, iron);
            s.Set(17, 7 + b, zap); s.Set(19, 8 + b, skin);
            // râu đỏ tết 2 bím, hạt vàng
            FillPoly(s, V(15, 9, 20, 9, 20, 13, 18, 16, 16, 16, 15, 13), b, (x, y) => x <= 15 ? redD : (x == 19 && y < 13) ? redL : red);
            for (int y = 16; y <= 18; y++) { s.Set(16, y + b, (y & 1) == 0 ? red : redD); s.Set(18, y + b, (y & 1) == 0 ? red : redD); }
            s.Set(16, 19 + b, goldL); s.Set(18, 19 + b, goldL);

            // tay trước + búa sấm
            if (!attack)
            {
                Thick(s, 21, 13 + b, 23, 17 + b, skin, skinD); Box(s, 22, 16 + b, 24, 17 + b, leaD);
                Box(s, 23, 18 + b, 23, 21 + b, lea);
                Box(s, 20, 22 + b, 26, 26 + b, iron); Line(s, 20, 22 + b, 26, 22 + b, ironL); Line(s, 20, 26 + b, 26, 26 + b, ironD); Line(s, 26, 22 + b, 26, 26 + b, ironD);
                s.Set(23, 24 + b, zap);
                if ((f & 1) == 0) { s.Set(27, 21 + b, zapH); s.Set(28, 20 + b, zap); } else { s.Set(19, 25 + b, zapH); s.Set(19, 27 + b, zap); }
            }
            else if (f < 2)
            {
                Thick(s, 21, 13, 19, 7, skin, skinD);
                Line(s, 18, 6, 15, 4, lea); Line(s, 18, 7, 15, 5, leaD);
                Box(s, 8, 1, 14, 5, iron); Line(s, 8, 1, 14, 1, ironL); Line(s, 8, 5, 14, 5, ironD); s.Set(11, 3, zap);
                s.Set(7, 0 + f, zapH); s.Set(6, 3, zap); s.Set(15, 0, zap); s.Set(9 + f, 6, zapH);
            }
            else
            {
                Thick(s, 21, 13, 24, 17, skin, skinD);
                Line(s, 24, 18, 26, 21, lea); Line(s, 25, 18, 27, 21, leaD);
                Box(s, 24, 22, 30, 26, iron); Line(s, 24, 22, 30, 22, ironL); Line(s, 24, 26, 30, 26, ironD); s.Set(27, 24, zapH);
                // sét toé xuống đất
                Line(s, 23, 27, 22, 29, zap); s.Set(21, 29, zapH); Line(s, 30, 27, 31, 29, zap);
                if (f == 3) { Line(s, 26, 27, 25, 29, zapH); s.Set(20, 28, zap); s.Set(31, 21, zapH); s.Set(23, 21, zap); }
            }
            return s;
        }

        /// <summary>The Berserker: chiến binh thú khoác da sói (đầu sói trùm đầu), mình trần vẽ chàm, váy lông, rìu khổng lồ.</summary>
        static Canvas DrawBerserker(int state, int f)
        {
            var s = new Canvas(GiantPx, GiantPx);
            Color32 skin = Rgb(208, 152, 110), skinD = Rgb(160, 108, 76), skinL = Rgb(232, 182, 140), woad = Rgb(66, 108, 196);
            Color32 pelt = Rgb(132, 134, 146), peltD = Rgb(86, 88, 102), peltL = Rgb(184, 186, 196), beard = Rgb(72, 48, 34);
            Color32 lea = Rgb(112, 76, 48), leaD = Rgb(76, 50, 32), kilt = Rgb(140, 100, 62), kiltD = Rgb(98, 68, 42);
            Color32 steel = Rgb(206, 214, 228), steelD = Rgb(122, 130, 150), edge = Rgb(250, 252, 255), haft = Rgb(112, 76, 44), eye = Rgb(240, 60, 40), tooth = Rgb(240, 236, 222);
            bool attack = state == 1, stride = !attack && (f & 1) == 1;
            int b = stride ? 1 : 0;

            // tấm da sói khoác sau lưng
            FillPoly(s, V(10, 6, 15, 5, 14, 12, 11, 27, 6, 27, 7, 14), b, (x, y) => x <= 8 ? peltD : ((x + y) % 4 == 0) ? peltL : pelt);
            // chân quấn da
            int bl = stride ? 11 : 12, fl = stride ? 18 : 17;
            for (int y = 24; y <= 28; y++) { Line(s, bl, y, bl + 2, y, (y & 1) == 0 ? leaD : lea); Line(s, fl, y, fl + 2, y, (y & 1) == 0 ? lea : kilt); }
            Box(s, bl - 1, 29, bl + 2, 29, leaD); Box(s, fl, 29, fl + 3, 29, leaD);
            // váy lông, mép răng cưa
            for (int x = 10; x <= 21; x++)
            {
                int bottom = 23 + (x & 1);
                for (int y = 20; y <= bottom; y++) s.Set(x, y + b, x <= 11 || ((x + y) % 4 == 0) ? kiltD : kilt);
            }
            Line(s, 10, 20 + b, 21, 20 + b, leaD); s.Set(15, 20 + b, steelD);
            // thân trần cơ bắp + vạch chàm
            for (int y = 11; y <= 19; y++)
                for (int x = 11; x <= 20; x++)
                    s.Set(x, y + b, x <= 12 ? skinD : (y == 14 && x >= 13 && x <= 18) ? skinD : (x == 16 && y >= 15) ? skinD : (y <= 13 && x >= 17) ? skinL : skin);
            Line(s, 13, 12 + b, 14, 15 + b, woad); Line(s, 19, 12 + b, 18, 16 + b, woad); s.Set(13, 17 + b, woad); s.Set(19, 18 + b, woad);
            // vai + tay sau
            Box(s, 9, 12 + b, 10, 18 + b, skinD); Box(s, 9, 16 + b, 10, 17 + b, leaD);
            // mặt, râu đen, mắt đỏ
            Box(s, 14, 6 + b, 18, 10 + b, skin); Box(s, 14, 6 + b, 14, 10 + b, skinD);
            s.Set(17, 7 + b, eye); Line(s, 14, 8 + b, 16, 8 + b, woad); s.Set(19, 8 + b, skin);
            Box(s, 15, 9 + b, 19, 12 + b, beard); s.Set(19, 12 + b, skinD);
            // đầu sói trùm lên đầu: tai, mắt, mõm, răng
            Box(s, 13, 2 + b, 19, 5 + b, pelt); Line(s, 13, 2 + b, 19, 2 + b, peltL);
            s.Set(14, 1 + b, peltD); s.Set(14, 0 + b, peltD); s.Set(16, 1 + b, pelt); s.Set(16, 0 + b, peltD);
            Box(s, 19, 3 + b, 22, 5 + b, peltL); s.Set(22, 3 + b, beard); s.Set(18, 3 + b, beard);
            s.Set(19, 6 + b, tooth); s.Set(21, 6 + b, tooth); Line(s, 13, 5 + b, 13, 7 + b, peltD);

            // tay trước + rìu khổng lồ
            if (!attack)
            {
                Thick(s, 20, 12 + b, 22, 16 + b, skin, skinD); Box(s, 22, 15 + b, 23, 16 + b, leaD);
                Line(s, 23, 5 + b, 23, 25 + b, haft);
                FillPoly(s, V(24, 4, 29, 2, 31, 7, 29, 13, 24, 10), b, (x, y) => x >= 29 ? edge : x >= 27 ? steel : steelD);
                s.Set(23, 3 + b, steelD); s.Set(23, 4 + b, steel);
            }
            else if (f < 2)
            {
                Thick(s, 20, 12, 18, 6, skin, skinD);
                Line(s, 18, 6, 11, 2 - f, haft);
                FillPoly(s, V(11, 3 - f, 6, 0, 3, 3, 4, 8 - f, 9, 7 - f), 0, (x, y) => x <= 5 ? edge : x <= 7 ? steel : steelD);
            }
            else
            {
                Thick(s, 20, 12, 23, 15, skin, skinD);
                Line(s, 23, 15, 28, 21, haft); Line(s, 24, 15, 29, 21, haft);
                FillPoly(s, V(27, 20, 31, 19, 31, 27, 27, 28, 25, 24), 0, (x, y) => y >= 26 ? edge : x >= 29 ? steel : steelD);
                if (f == 3) { s.Set(30, 12, eye); s.Set(31, 15, eye); s.Set(29, 9, Rgb(200, 40, 40)); }   // vệt máu
            }
            return s;
        }

        /// <summary>Fenrir: sói khổng lồ lông than, bờm dựng, mắt đỏ rực, vòng xích gãy ở cổ. Đi = phi 4 khung, đánh = chồm cắn.</summary>
        static Canvas DrawFenrir(int state, int f)
        {
            var s = new Canvas(GiantPx, GiantPx);
            Color32 furD = Rgb(52, 54, 66), fur = Rgb(78, 80, 96), furM = Rgb(104, 106, 122), furL = Rgb(150, 152, 168), belly = Rgb(176, 178, 190);
            Color32 eye = Rgb(255, 70, 40), eyeH = Rgb(255, 190, 120), gum = Rgb(150, 36, 48), tooth = Rgb(246, 242, 232), chain = Rgb(176, 176, 188), chainD = Rgb(104, 104, 116), nose = Rgb(30, 26, 32);
            bool attack = state == 1;
            int b = !attack && (f & 1) == 1 ? -1 : attack && f < 2 ? 1 : 0;              // phi: nhún; đánh: hạ thấp lấy đà
            int hx = attack && f >= 2 ? 1 : 0, hy = attack ? (f < 2 ? 2 : 0) : 0;
            // vị trí chân theo khung
            int bf, ff; // chân sau / chân trước (x bàn chân)
            if (attack) { bf = f < 2 ? 6 : 4; ff = f < 2 ? 20 : 24; }
            else { bf = f == 0 ? 3 : f == 2 ? 5 : 7; ff = f == 0 ? 24 : f == 2 ? 22 : 20; }
            // đuôi xù
            FillPoly(s, V(6, 12, 1, 7, 0, 10, 2, 14, 5, 16), b, (x, y) => y <= 10 ? furM : fur);
            // chân có khớp: nét dày 2 pixel theo chiều ngang (đường gần thẳng đứng)
            void Leg(int x0, int y0, int x1, int y1, int x2, int y2, Color32 c, Color32 c2)
            {
                Line(s, x0, y0, x1, y1, c); Line(s, x0 + 1, y0, x1 + 1, y1, c2);
                Line(s, x1, y1, x2, y2, c); Line(s, x1 + 1, y1, x2 + 1, y2, c2);
                Box(s, x2 - 1, 28, x2 + 2, 29, furD);                                   // bàn chân
            }
            // chân xa (tối): sau gập ở khuỷu sau (hock), trước thẳng xuống bàn
            Leg(10, 20 + b, bf + 3, 25, bf + 4, 28, furD, furD);
            Leg(18, 20 + b, ff - 2, 25, ff - 2, 28, furD, furD);
            // thân + đùi + vai
            Ellipse(s, 14f, 16f + b, 9f, 5.2f, (x, y) => y <= 12 + b ? furL : y >= 19 + b ? fur : furM);
            Ellipse(s, 8f, 17f + b, 4.6f, 4.8f, (x, y) => y <= 14 + b ? furM : x <= 5 ? furD : fur);
            Ellipse(s, 19f, 16f + b, 4f, 4.8f, (x, y) => y <= 13 + b ? furM : fur);
            Line(s, 10, 20 + b, 17, 20 + b, belly); Line(s, 11, 21 + b, 15, 21 + b, fur);
            // chân gần
            Leg(7, 21 + b, bf + 2, 24, bf, 28, fur, furD);
            Leg(20, 20 + b, ff, 25, ff + 1, 28, furM, fur);
            // bờm dựng quanh cổ: từng chùm lông nhọn
            FillPoly(s, V(16, 10, 18, 6, 20, 9, 22, 5, 23, 9, 24, 13, 20, 18, 15, 13), b + hy / 2, (x, y) => x % 3 == 0 ? fur : x % 3 == 1 ? furL : furM);
            // vòng xích gãy
            s.Set(21, 15 + b, chain); s.Set(22, 16 + b, chainD); s.Set(23, 15 + b, chain); s.Set(20, 17 + b, chainD); s.Set(20, 18 + b, chain); s.Set(19, 19 + b, chainD);
            // đầu
            int ox = hx, oy = b + hy;
            Ellipse(s, 24.5f + ox, 11f + oy, 3.6f, 3f, (x, y) => y <= 9 + oy ? furL : furM);
            FillPoly(s, V(22, 9, 22, 5, 24, 8), oy, (x, y) => furD);
            FillPoly(s, V(24 + ox, 8, 26 + ox, 4, 27 + ox, 9), oy, (x, y) => furM);
            s.Set(26 + ox, 10 + oy, eye); s.Set(27 + ox, 10 + oy, eyeH); s.Set(25 + ox, 9 + oy, furD);
            if (attack && f >= 2)
            {
                Box(s, 27 + ox, 9 + oy, 30 + ox, 10 + oy, furM); s.Set(30 + ox, 9 + oy, nose);   // hàm trên
                Box(s, 27 + ox, 11 + oy, 30 + ox, 12 + oy, gum);                                 // miệng há
                Box(s, 26 + ox, 13 + oy, 29 + ox, 14 + oy, fur);                                  // hàm dưới
                s.Set(28 + ox, 11 + oy, tooth); s.Set(30 + ox, 11 + oy, tooth); s.Set(27 + ox, 12 + oy, tooth); s.Set(29 + ox, 12 + oy, tooth);
                if (f == 3) { s.Set(31, 13 + oy, gum); s.Set(30 + ox, 15 + oy, Rgb(200, 40, 40)); }
            }
            else
            {
                Box(s, 27, 11 + oy, 30, 13 + oy, furM); s.Set(30, 11 + oy, nose); Line(s, 27, 13 + oy, 30, 13 + oy, fur);
                s.Set(28, 14 + oy, tooth); s.Set(27, 12 + oy, furD);
            }
            return s;
        }

        /// <summary>The Broodmother: nhện hư không khổng lồ, bụng phình có vân phát sáng, cụm mắt xanh, 8 chân có khớp, nanh độc.</summary>
        static Canvas DrawBroodmother(int state, int f)
        {
            var s = new Canvas(GiantPx, GiantPx);
            Color32 car = Rgb(52, 34, 80), carM = Rgb(84, 58, 124), carL = Rgb(132, 98, 180), carH = Rgb(176, 146, 220);
            Color32 glow = Rgb(196, 112, 255), leg = Rgb(42, 28, 64), legL = Rgb(98, 72, 140), eye = Rgb(176, 255, 112), eyeH = Rgb(236, 255, 210);
            Color32 fang = Rgb(232, 222, 242), venom = Rgb(120, 232, 92);
            bool attack = state == 1;
            int rear = attack && f < 2 ? -2 : 0, lunge = attack && f >= 2 ? 1 : 0, w = !attack ? ((f & 1) == 0 ? 1 : -1) : 0;
            // chân nhện: gốc ở ngực, gối nhô cao ra ngoài, bàn chân xoè rộng
            void SpiderLeg(int ox, int oy, int fx, int fy, int kneeY, Color32 c, Color32 c2, bool thick)
            {
                int knx = ox + (fx - ox) * 3 / 5;
                if (thick) { Thick(s, ox, oy, knx, kneeY, c, c2); Thick(s, knx, kneeY, fx, fy, c, c2); s.Set(knx, kneeY, carH); }
                else { Line(s, ox, oy, knx, kneeY, c); Line(s, knx, kneeY, fx, fy, c); }
            }
            // chân xa (tối, mảnh)
            int[] farFoot = { 4, 10, 24, 29 };
            for (int k = 0; k < 4; k++)
                SpiderLeg(17 + k, 17, farFoot[k] + (k % 2 == 0 ? w : -w), 27, k == 0 || k == 3 ? 10 : 8, leg, leg, false);
            // bụng
            Ellipse(s, 8f, 16f, 6.6f, 5.6f, (x, y) => y <= 12 ? carL : y >= 20 ? car : carM);
            s.Set(6, 11, carH); s.Set(7, 11, carH); s.Set(5, 12, carH);
            // vân phát sáng hình đồng hồ cát + chấm
            Line(s, 6, 14, 10, 14, glow); Line(s, 7, 15, 9, 15, glow); s.Set(8, 16, glow); Line(s, 7, 17, 9, 17, glow); Line(s, 6, 18, 10, 18, glow);
            s.Set(3, 15, glow); s.Set(13, 15, glow); s.Set(4, 19, glow);
            // ngực + đầu
            Ellipse(s, 19f, 17f + rear, 5f, 3.8f, (x, y) => y <= 15 + rear ? carL : carM);
            Ellipse(s, 24f + lunge, 16.5f + rear, 3.2f, 3f, (x, y) => y <= 15 + rear ? carM : car);
            // cụm mắt
            int ex = 24 + lunge, ey = 15 + rear;
            s.Set(ex, ey, eye); s.Set(ex + 2, ey, eye); s.Set(ex + 1, ey + 1, eyeH); s.Set(ex + 3, ey + 1, eye); s.Set(ex + 2, ey + 2, eye); s.Set(ex - 1, ey + 1, eye);
            // nanh độc
            if (attack && f >= 2)
            {
                Line(s, ex + 2, ey + 3, ex + 4, ey + 6, fang); Line(s, ex, ey + 3, ex, ey + 6, fang);
                s.Set(ex + 4, ey + 7, venom); s.Set(ex + 3, ey + 8 + (f - 2), venom);
            }
            else { Line(s, ex + 1, ey + 3, ex + 2, ey + 5, fang); s.Set(ex, ey + 4, fang); }
            // chân gần (sáng, dày) — 2 chân sau chĩa về sau, 2 chân trước chĩa về trước; đánh: giơ 2 chân trước lên
            int[] nearFoot = { 1, 8, 26, 30 };
            for (int k = 0; k < 4; k++)
            {
                bool front = k >= 2;
                int fx = nearFoot[k] + (k % 2 == 0 ? -w : w), fy = attack && f < 2 && front ? 20 : 28;
                SpiderLeg(17 + k, 18 + (front ? rear : 0), fx, fy, (k == 0 || k == 3 ? 7 : 5) + (front ? rear : 0), legL, leg, true);
            }
            return s;
        }

        /// <summary>Nhện con 10x10 của Broodmother.</summary>
        static Canvas DrawSpiderling(int state, int f)
        {
            var s = new Canvas(InfantryPx, InfantryPx);
            Color32 car = Rgb(70, 46, 108), carL = Rgb(130, 96, 178), leg = Rgb(44, 30, 66), eye = Rgb(176, 255, 112), glow = Rgb(196, 112, 255);
            int w = (f & 1) == 0 ? 1 : 0, up = state == 1 && f >= 2 ? -1 : 0;
            // chân
            for (int k = 0; k < 3; k++)
            {
                int x0 = 3 + k * 2;
                Line(s, x0, 6, x0 - 1 + (k == 2 ? 2 : 0), 4 + ((k + w) & 1), leg);
                Line(s, x0 - 1 + (k == 2 ? 2 : 0), 4 + ((k + w) & 1), x0 - 2 + k + w, 9, leg);
            }
            Ellipse(s, 3f, 5.5f, 2.6f, 2.2f, (x, y) => y <= 4 ? carL : car);
            s.Set(3, 5, glow);
            Ellipse(s, 6.5f, 6f + up, 1.8f, 1.5f, (x, y) => car);
            s.Set(7, 5 + up, eye); s.Set(8, 6 + up, eye);
            return s;
        }


        /// <summary>Troll 16x16: khổng lồ còng lưng da xám xanh, tay dài, nanh trắng, cầm dùi cui gỗ.</summary>
        static Canvas DrawTroll(int state, int f)
        {
            var s = new Canvas(UnitCell, UnitCell);
            Color32 tr = Rgb(112, 132, 108), trD = Rgb(74, 92, 74), trL = Rgb(154, 172, 146), club = Wood, clubD = BarkDark;
            bool walk = state == 0;
            int bob = walk && (f & 1) == 1 ? 1 : 0;
            // chân
            if (walk && (f & 1) == 1) { Box(s, 4, 12, 5, 13, trD); Box(s, 9, 12, 10, 13, trD); }
            else { Box(s, 5, 12, 6, 13, trD); Box(s, 8, 12, 9, 13, trD); }
            Box(s, 4, 11, 10, 11, OrcLeatherD);                                       // khố da
            // thân còng
            Box(s, 4, 5 + bob, 10, 10 + bob, tr);
            Box(s, 4, 5 + bob, 5, 10 + bob, trD);
            Box(s, 7, 7 + bob, 9, 9 + bob, trL);                                      // bụng
            s.Set(6, 4 + bob, trD); s.Set(8, 4 + bob, trD);                             // u lưng
            // đầu chúi thấp
            Box(s, 9, 3 + bob, 12, 5 + bob, tr); s.Set(9, 2 + bob, trD);
            s.Set(11, 4 + bob, OrcEye); s.Set(13, 4 + bob, trL); s.Set(12, 6 + bob, White);   // mắt, mũi, nanh
            // tay sau
            Box(s, 3, 6 + bob, 3, 11 + bob, trD);
            // tay trước + dùi cui
            if (walk) { Box(s, 11, 6 + bob, 12, 10 + bob, tr); Box(s, 12, 11 + bob, 13, 13, club); s.Set(13, 13, clubD); }
            else if (f == 0) { Box(s, 11, 2, 12, 5, tr); Box(s, 12, 0, 14, 2, club); s.Set(14, 0, clubD); }                 // giơ dùi
            else if (f == 1) { Box(s, 11, 3, 12, 6, tr); Box(s, 13, 1, 15, 3, club); }
            else if (f == 2) { Box(s, 11, 6, 12, 8, tr); Box(s, 13, 6, 15, 8, club); s.Set(15, 8, clubD); }                 // đập ngang
            else { Box(s, 11, 8, 12, 10, tr); Box(s, 13, 10, 15, 13, club); s.Set(15, 13, clubD); s.Set(14, 14, Rgb(150, 132, 104)); }   // đập xuống đất
            return s;
        }

        /// <summary>The Warboss 32x32: thủ lĩnh Orc khổng lồ, giáp sắt đen gai nhọn, mũ sừng, nanh lớn, sơn chiến đỏ, áo choàng lông, dao phay khổng lồ.</summary>
        static Canvas DrawWarboss(int state, int f)
        {
            var s = new Canvas(GiantPx, GiantPx);
            Color32 skin = Rgb(108, 164, 66), skinD = Rgb(70, 116, 44), skinL = Rgb(146, 198, 94);
            Color32 iron = Rgb(70, 72, 82), ironD = Rgb(42, 44, 52), ironL = Rgb(128, 132, 146), spike = Rgb(206, 210, 220);
            Color32 fur = Rgb(96, 70, 48), furD = Rgb(64, 46, 32), furL = Rgb(138, 104, 72), red = Rgb(186, 40, 36), bone = Rgb(236, 226, 196), boneD = Rgb(176, 164, 136);
            Color32 blade = Rgb(196, 202, 214), bladeD = Rgb(118, 124, 140), edge = Rgb(248, 250, 255), wood = Rgb(110, 74, 44), eye = Rgb(255, 70, 40);
            bool attack = state == 1, stride = !attack && (f & 1) == 1;
            int b = stride ? 1 : 0;

            // áo choàng lông
            FillPoly(s, V(8, 11, 14, 11, 13, 26, 7, 28, 5, 24, 6, 15), b, (x, y) => x <= 7 ? furD : ((x * 3 + y) % 5 == 0) ? furL : fur);
            // chân: giáp sắt, ủng gai
            int bl = stride ? 10 : 11, fl = stride ? 18 : 17;
            Box(s, bl, 22 + b, bl + 2, 27, ironD); Box(s, bl - 1, 28, bl + 2, 29, iron); s.Set(bl - 1, 27, spike);
            Box(s, fl, 22 + b, fl + 3, 27, iron); s.Set(fl, 23 + b, ironL); Box(s, fl, 28, fl + 4, 29, ironD); s.Set(fl + 4, 27, spike);
            // thân giáp đen + thắt lưng sọ
            Box(s, 9, 13 + b, 22, 21 + b, iron);
            Box(s, 9, 13 + b, 11, 21 + b, ironD);
            Box(s, 13, 14 + b, 20, 15 + b, ironL);
            for (int x = 12; x <= 21; x += 3) s.Set(x, 17 + b, ironD);                  // đinh tán
            Box(s, 9, 20 + b, 22, 21 + b, OrcLeatherD);
            Box(s, 14, 19 + b, 17, 22 + b, bone); s.Set(15, 20 + b, Outline); s.Set(16, 20 + b, Outline); Line(s, 14, 22 + b, 17, 22 + b, boneD);   // khoá thắt lưng hình sọ
            // vai giáp gai to
            Ellipse(s, 9f, 12.5f + b, 3f, 2.5f, (x, y) => y <= 11 + b ? iron : ironD);
            Ellipse(s, 22f, 12.5f + b, 3.2f, 2.6f, (x, y) => y <= 11 + b ? ironL : iron);
            s.Set(7, 9 + b, spike); s.Set(9, 9 + b, spike); s.Set(21, 9 + b, spike); s.Set(23, 9 + b, spike); s.Set(24, 10 + b, spike);
            // tay sau
            Box(s, 7, 14 + b, 8, 19 + b, skinD); Line(s, 7, 16 + b, 8, 16 + b, red);
            // đầu: hàm bạnh, nanh lớn, mắt đỏ, sơn chiến
            Box(s, 13, 5 + b, 19, 11 + b, skin); Box(s, 13, 5 + b, 13, 11 + b, skinD); Line(s, 14, 5 + b, 18, 5 + b, skinL);
            Box(s, 14, 10 + b, 20, 12 + b, skin); Line(s, 14, 12 + b, 20, 12 + b, skinD);              // hàm dưới bạnh
            s.Set(17, 7 + b, eye); s.Set(18, 7 + b, eye); Line(s, 16, 6 + b, 19, 6 + b, Outline);       // mắt đỏ + mày cau
            Line(s, 15, 8 + b, 18, 8 + b, red); s.Set(16, 9 + b, red);                                   // sơn chiến
            s.Set(20, 8 + b, skin);                                                                       // mũi
            s.Set(16, 11 + b, bone); s.Set(16, 10 + b, bone); s.Set(19, 11 + b, bone); s.Set(19, 10 + b, bone);   // nanh
            // mũ sắt sừng
            Box(s, 12, 2 + b, 19, 5 + b, iron); Line(s, 13, 2 + b, 18, 2 + b, ironL); Line(s, 12, 5 + b, 19, 5 + b, ironD);
            Line(s, 12, 3 + b, 9, 0 + b, bone); s.Set(9, 1 + b, boneD);
            Line(s, 19, 3 + b, 22, 0 + b, bone); s.Set(22, 1 + b, boneD);
            s.Set(15, 1 + b, spike); s.Set(16, 1 + b, spike);

            // tay trước + dao phay khổng lồ
            if (!attack)
            {
                Thick(s, 22, 14 + b, 24, 18 + b, skin, skinD); Line(s, 22, 15 + b, 23, 16 + b, red);
                Line(s, 25, 6 + b, 25, 20 + b, wood);
                FillPoly(s, V(26, 2, 31, 2, 31, 14, 28, 15, 26, 12), b, (x, y) => x >= 30 ? edge : x >= 28 ? blade : bladeD);
                s.Set(29, 4 + b, Outline);                                                                // lỗ trên lưỡi dao
            }
            else if (f < 2)
            {
                Thick(s, 22, 13, 20, 7, skin, skinD);
                Line(s, 20, 6, 15, 2, wood);
                FillPoly(s, V(14, 1 - f, 4, 0, 3, 5, 6, 7, 14, 4 - f), 0, (x, y) => y <= 1 ? edge : y <= 3 ? blade : bladeD);
            }
            else
            {
                Thick(s, 22, 13, 25, 16, skin, skinD);
                Line(s, 25, 17, 28, 20, wood);
                FillPoly(s, V(26, 19, 31, 19, 31, 29, 28, 29, 26, 25), 0, (x, y) => x >= 30 ? edge : x >= 28 ? blade : bladeD);
                if (f == 3) { s.Set(24, 29, Rgb(150, 132, 104)); s.Set(22, 28, Rgb(150, 132, 104)); s.Set(31, 18, eye); }
            }
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
                if (body.W > UnitCell) body = Half(body);                                  // xác quân khổng lồ: thu nhỏ cho vừa ô dấu in
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

        /// <summary>Thu nhỏ 1/2: mỗi ô 2x2 lấy 1 pixel (ưu tiên pixel có màu, không phải viền).</summary>
        static Canvas Half(Canvas src)
        {
            var d = new Canvas(src.W / 2, src.H / 2);
            for (int y = 0; y < d.H; y++)
                for (int x = 0; x < d.W; x++)
                {
                    Color32 pick = default;
                    for (int k = 0; k < 4 && (pick.a == 0 || pick.Equals(Outline)); k++)
                    {
                        var c = src.Get(x * 2 + (k & 1), y * 2 + (k >> 1));
                        if (c.a > 0 && (pick.a == 0 || !c.Equals(Outline))) pick = c;
                    }
                    d.Set(x, y, pick);
                }
            return d;
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
            const int SmallH = 3;                                   // đạn thường vẽ trên băng 12x3 rồi đặt vào giữa ô 16x7
            var a = new Canvas(12, SmallH * Proj.Count);
            DrawArrow(a, Proj.Arrow * SmallH, Wood, Steel, White);
            DrawArrow(a, Proj.ElfArrow * SmallH, HorseLight, LeafLight, Leaf);
            // tên nỏ: ngắn, đậm
            for (int x = 4; x <= 9; x++) a.Set(x, Proj.Bolt * SmallH + 1, Leather);
            a.Set(10, Proj.Bolt * SmallH + 1, Steel); a.Set(3, Proj.Bolt * SmallH, SteelDark); a.Set(3, Proj.Bolt * SmallH + 2, SteelDark);
            // đá
            Blob(a, Proj.Rock * SmallH, Rgb(150, 146, 156), Rgb(104, 100, 112), Rgb(186, 182, 192));
            // đạn pháo
            Blob(a, Proj.Cannonball * SmallH, Rgb(46, 46, 56), Rgb(24, 24, 30), Rgb(110, 112, 130));
            // lao
            for (int x = 1; x <= 9; x++) a.Set(x, Proj.Javelin * SmallH + 1, Wood);
            a.Set(10, Proj.Javelin * SmallH + 1, Steel); a.Set(11, Proj.Javelin * SmallH + 1, Steel); a.Set(10, Proj.Javelin * SmallH, Steel); a.Set(10, Proj.Javelin * SmallH + 2, Steel);
            // cầu phép: có vệt đuôi
            Orb(a, Proj.NatureOrb * SmallH, LeafLight, Leaf);
            Orb(a, Proj.VoidOrb * SmallH, VoidGlow, Void1);
            Orb(a, Proj.LightOrb * SmallH, Rgb(255, 250, 210), Gold);

            // ghép vào ô ProjW x ProjH: hàng của loại k, đặt vào giữa
            var big = new Canvas(ProjW, ProjH * Proj.Count);
            for (int k = 0; k < Proj.Count; k++)
                for (int y = 0; y < SmallH; y++)
                    for (int x = 0; x < 12; x++)
                    {
                        var c = a.Get(x, k * SmallH + y);
                        if (c.a > 0) big.Set(x + (ProjW - 12) / 2, k * ProjH + (ProjH - SmallH) / 2 + y, c);
                    }
            DrawMeteor(big, Proj.Meteor * ProjH);
            DrawBigBolt(big, Proj.BigBolt * ProjH);
            return big.ToTexture("Projectiles");
        }

        /// <summary>Thiên thạch 24x11 bay sang phải: đầu đá cháy to (9x9) sáng rực ở mặt trước, đuôi lửa dài thon dần về sau.</summary>
        static void DrawMeteor(Canvas a, int oy)
        {
            Color32 white = Rgb(255, 248, 214), yellow = Rgb(255, 210, 72), orange = Rgb(250, 126, 32), red = Rgb(200, 52, 30), ember = Rgb(140, 30, 24);
            Color32 rock = Rgb(84, 46, 40), rockDark = Rgb(52, 28, 28);
            // đuôi lửa: thon dần từ đầu (x 15) về sau (x 0), giữa ở hàng 5
            for (int x = 0; x <= 15; x++)
            {
                int half = x >= 13 ? 3 : x >= 9 ? 2 : x >= 4 ? 1 : 0;
                for (int dy = -half; dy <= half; dy++)
                {
                    int edge = half - System.Math.Abs(dy);
                    Color32 c = x >= 12 ? (edge >= 2 ? white : yellow) : x >= 8 ? (edge >= 1 ? yellow : orange) : x >= 4 ? (edge >= 1 ? orange : red) : red;
                    if (((x * 7 + dy * 3) % 5 == 0) && x < 12) c = ember;   // lốm đốm cho có lửa
                    a.Set(x, oy + 5 + dy, c);
                }
            }
            a.Set(2, oy + 3, red); a.Set(5, oy + 8, orange); a.Set(1, oy + 6, ember);   // tàn lửa lẻ
            // đầu đá tròn 9x9 tâm (19, 5): mặt sau là đá tối có vết nứt, mặt trước nóng trắng
            for (int y = 1; y <= 9; y++)
                for (int x = 15; x <= 23; x++)
                {
                    float dx = x - 19f, dyy = y - 5f;
                    if (dx * dx + dyy * dyy > 17f) continue;
                    Color32 c = dx >= 2.5f ? white : dx >= 1f ? yellow : dx >= -0.5f ? orange : ((x + y) % 3 == 0 ? rockDark : rock);
                    a.Set(x, oy + y, c);
                }
            a.Set(17, oy + 4, orange); a.Set(16, oy + 6, orange); a.Set(18, oy + 7, red);   // vết nứt nóng chảy
        }

        /// <summary>Mũi nỏ thành 16x7: thân gỗ dày 2 pixel, đầu thép to, đuôi lông đỏ.</summary>
        static void DrawBigBolt(Canvas a, int oy)
        {
            var b = new Canvas(16, 7);
            DrawBigBolt16(b);
            for (int y = 0; y < 7; y++) for (int x = 0; x < 16; x++) { var c = b.Get(x, y); if (c.a > 0) a.Set(x + (ProjW - 16) / 2, oy + (ProjH - 7) / 2 + y, c); }
        }

        static void DrawBigBolt16(Canvas a, int oy = 0)
        {
            for (int x = 3; x <= 11; x++) { a.Set(x, oy + 3, Wood); a.Set(x, oy + 4, Leather); }
            a.Set(12, oy + 1, SteelDark); a.Set(12, oy + 2, SteelDark); a.Set(12, oy + 3, Steel); a.Set(12, oy + 4, Steel); a.Set(12, oy + 5, SteelDark);
            a.Set(13, oy + 2, Steel); a.Set(13, oy + 3, Steel); a.Set(13, oy + 4, Steel);
            a.Set(14, oy + 3, White); a.Set(14, oy + 4, Steel); a.Set(15, oy + 3, White);
            for (int x = 0; x <= 3; x++) { a.Set(x, oy + 1 + (x == 0 ? 0 : 0), x < 2 ? Cloth : ClothDark); a.Set(x, oy + 6, x < 2 ? Cloth : ClothDark); }
            a.Set(1, oy + 2, Cloth); a.Set(2, oy + 2, ClothDark); a.Set(1, oy + 5, Cloth); a.Set(2, oy + 5, ClothDark);
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
