using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HordeLab
{
    /// <summary>
    /// Bộ đồ nghề dựng giao diện uGUI + TextMeshPro bằng code (không prefab, không kéo thả trong Editor).
    /// Mọi toạ độ là "ảo" trên màn hình cao 1080; gốc ở góc trên-trái của phần tử cha, y hướng xuống.
    /// </summary>
    public static class UiKit
    {
        public static readonly Color Cream = new Color(0.93f, 0.91f, 0.86f);
        public static readonly Color Gold = new Color(1f, 0.9f, 0.62f);
        public static readonly Color Grey = new Color(0.67f, 0.67f, 0.67f);
        public static readonly Color Slot = new Color(0.16f, 0.15f, 0.19f, 1f);
        public static readonly Color Dark = new Color(0.05f, 0.04f, 0.06f, 0.7f);

        public static Sprite Panel9, Card9, Btn0, Btn1, Btn2, White, Frame9;
        public static Sprite Heart, Star, Crystal;
        /// <summary>Sao theo hạng: [0] đồng (1 sao), [1] bạc, [2] vàng, [3] bạch kim xanh (4 sao).</summary>
        public static Sprite[] Stars;
        public static Sprite StarFor(int stars) => Stars[Mathf.Clamp(stars, 1, Stars.Length) - 1];
        public static Sprite[] ResIcons, Tiles, UnitIcons, Shapes;

        public static void Load(Texture2D unitAtlas)
        {
            if (Panel9 != null) return;
            int b = PixelArt.UiBorder;
            Panel9 = Nine(PixelArt.UiPanel(), b);
            Card9 = Nine(PixelArt.UiCard(), b);
            Btn0 = Nine(PixelArt.UiButton(0), 6, 9);
            Btn1 = Nine(PixelArt.UiButton(1), 6, 9);
            Btn2 = Nine(PixelArt.UiButton(2), 6, 9);
            White = Whole(Texture2D.whiteTexture);
            Frame9 = Nine(FrameTexture(), 3);
            Heart = Whole(PixelArt.HeartIcon());
            Stars = new Sprite[Tuning.MaxStars];
            for (int i = 0; i < Stars.Length; i++) Stars[i] = Whole(PixelArt.StarIcon(i + 1));
            Star = Stars[2];
            Crystal = Whole(PixelArt.CrystalIcon());

            var res = PixelArt.ResourceIcons();
            ResIcons = new Sprite[res.Length];
            for (int i = 0; i < res.Length; i++) ResIcons[i] = Whole(res[i]);

            var tiles = PixelArt.BuildTileAtlas();
            Tiles = new Sprite[PixelArt.TileCount];
            for (int i = 0; i < Tiles.Length; i++)
                Tiles[i] = Cell(tiles, i % PixelArt.TileCols, i / PixelArt.TileCols, PixelArt.TilePx, PixelArt.TilePx);

            UnitIcons = new Sprite[UnitType.Count];
            for (int t = 0; t < UnitIcons.Length; t++)                                                           // khung đi đầu tiên
                UnitIcons[t] = (GameData.Units[t].stats.flags & UFlag.Giant) != 0
                    ? Cell(unitAtlas, 0, t, PixelArt.GiantPx, PixelArt.GiantPx)        // hàng t*2 (ô 16) = hàng t (ô 32)
                    : Cell(unitAtlas, 0, t * 2, PixelArt.UnitCell, PixelArt.UnitCell);

            Shapes = new Sprite[5];
            for (int s = 1; s < Shapes.Length; s++) Shapes[s] = Whole(ShapeTexture((Shape)s));
        }

        static Sprite Whole(Texture2D t) => Sprite.Create(t, new Rect(0, 0, t.width, t.height), new Vector2(0.5f, 0.5f), 100f);

        static Sprite Nine(Texture2D t, int border, int bottom = -1) =>
            Sprite.Create(t, new Rect(0, 0, t.width, t.height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect,
                          new Vector4(border, bottom < 0 ? border : bottom, border, border));

        /// <summary>Cắt 1 ô khỏi atlas; hàng 0 là hàng TRÊN CÙNG (giống lúc vẽ).</summary>
        static Sprite Cell(Texture2D atlas, int col, int row, int w, int h) =>
            Sprite.Create(atlas, new Rect(col * w, atlas.height - (row + 1) * h, w, h), new Vector2(0.5f, 0.5f), 100f);

        static Texture2D FrameTexture()
        {
            const int n = 12;
            var t = new Texture2D(n, n, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                    px[y * n + x] = x < 3 || y < 3 || x >= n - 3 || y >= n - 3 ? new Color32(255, 255, 255, 255) : new Color32(255, 255, 255, 0);
            t.SetPixels32(px);
            t.Apply(false, true);
            return t;
        }

        /// <summary>Hình vùng buff thu nhỏ 3x3: ô giữa trắng = nhà buff, ô vàng = ô được buff.</summary>
        static Texture2D ShapeTexture(Shape shape)
        {
            var t = new Texture2D(3, 3, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[9];
            for (int y = 0; y < 3; y++)
                for (int x = 0; x < 3; x++)
                {
                    bool center = x == 1 && y == 1;
                    bool hit = shape == Shape.Ring ? !center
                             : shape == Shape.Cross ? (x == 1) != (y == 1)
                             : shape == Shape.Below ? x == 1 && y == 2
                             : shape == Shape.Row && y == 1 && !center;
                    px[(2 - y) * 3 + x] = center ? new Color32(255, 255, 255, 255) : hit ? new Color32(255, 214, 64, 255) : new Color32(20, 16, 24, 255);
                }
            t.SetPixels32(px);
            t.Apply(false, true);
            return t;
        }

        // ------------------------------------------------------------------
        // Dựng phần tử
        // ------------------------------------------------------------------
        public static RectTransform Node(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            return rt;
        }

        /// <summary>Đặt theo góc trên-trái của cha.</summary>
        public static T At<T>(this T c, float x, float y, float w, float h) where T : Component
        {
            var rt = (RectTransform)c.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(x, -y);
            rt.sizeDelta = new Vector2(w, h);
            return c;
        }

        /// <summary>Đặt theo góc trên-phải của cha: right = khoảng cách từ mép phải của cha tới mép phải phần tử.</summary>
        public static T AtRight<T>(this T c, float right, float y, float w, float h) where T : Component
        {
            var rt = (RectTransform)c.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(1f, 1f);
            rt.anchoredPosition = new Vector2(-right, -y);
            rt.sizeDelta = new Vector2(w, h);
            return c;
        }

        /// <summary>Đặt giữa cha, lệch (dx, dy) với dy hướng xuống.</summary>
        public static T AtCenter<T>(this T c, float dx, float dy, float w, float h) where T : Component
        {
            var rt = (RectTransform)c.transform;
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(dx, -dy);
            rt.sizeDelta = new Vector2(w, h);
            return c;
        }

        /// <summary>Co giãn theo cha, chừa lề.</summary>
        public static T Fill<T>(this T c, float left = 0f, float top = 0f, float right = 0f, float bottom = 0f) where T : Component
        {
            var rt = (RectTransform)c.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = new Vector2(left, bottom);
            rt.offsetMax = new Vector2(-right, -top);
            return c;
        }

        public static Image Img(Transform parent, Sprite sprite, Color color, bool sliced = false, bool blockMouse = false, string name = "Image")
        {
            var img = Node(name, parent).gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.color = color;
            img.type = sliced ? Image.Type.Sliced : Image.Type.Simple;
            img.raycastTarget = blockMouse;
            return img;
        }

        public static Image Panel(Transform parent, bool card = false, string name = "Panel") =>
            Img(parent, card ? Card9 : Panel9, Color.white, true, true, name);

        public static Image Frame(Transform parent, Color color)
        {
            var f = Img(parent, Frame9, color, true, false, "Frame");
            f.fillCenter = false;
            return f;
        }

        public static TextMeshProUGUI Txt(Transform parent, string text, float size, TextAlignmentOptions align = TextAlignmentOptions.TopLeft,
                                          bool bold = false, bool wrap = false, Color? color = null, string name = "Text")
        {
            var t = Node(name, parent).gameObject.AddComponent<TextMeshProUGUI>();
            t.text = text;
            t.fontSize = size;
            t.alignment = align;
            t.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
            t.color = color ?? Cream;
            t.richText = true;
            t.raycastTarget = false;
            t.textWrappingMode = wrap ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
            t.overflowMode = wrap ? TextOverflowModes.Overflow : TextOverflowModes.Ellipsis;
            return t;
        }

        public static Button Btn(Transform parent, string label, System.Action onClick, float fontSize = 20f, string name = "Button")
        {
            var img = Img(parent, Btn0, Color.white, true, true, name);
            var b = img.gameObject.AddComponent<Button>();
            b.targetGraphic = img;
            b.transition = Selectable.Transition.SpriteSwap;
            b.spriteState = new SpriteState { highlightedSprite = Btn1, pressedSprite = Btn2, selectedSprite = Btn0, disabledSprite = Btn0 };
            b.navigation = new Navigation { mode = Navigation.Mode.None };
            b.onClick.AddListener(() => onClick());
            var t = Txt(img.transform, label, fontSize, TextAlignmentOptions.Center, true, false, new Color(1f, 0.95f, 0.82f), "Label");
            t.Fill(6f, 2f, 6f, 6f);
            img.gameObject.AddComponent<CanvasGroup>();
            return b;
        }

        public static TextMeshProUGUI Label(this Button b) => b.GetComponentInChildren<TextMeshProUGUI>(true);

        /// <summary>Bật / tắt nút: nút tắt mờ đi và không bấm được.</summary>
        public static void SetOn(this Button b, bool on)
        {
            if (b.interactable == on) return;
            b.interactable = on;
            b.GetComponent<CanvasGroup>().alpha = on ? 1f : 0.45f;
        }

        public static void Set(this TextMeshProUGUI t, string text)
        {
            if (!ReferenceEquals(t.text, text) && t.text != text) t.text = text;
        }

        public static void Show(this Component c, bool on)
        {
            if (c.gameObject.activeSelf != on) c.gameObject.SetActive(on);
        }

        public static string Hex(Color c) => ColorUtility.ToHtmlStringRGB(c);
        public static string FactionTag(int f) => $"<color=#{Hex(GameData.Factions[f].color)}>{GameData.Factions[f].name}</color>";
    }
}
