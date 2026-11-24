using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static HordeLab.UiKit;

namespace HordeLab
{
    /// <summary>
    /// Toàn bộ giao diện (uGUI + TextMeshPro), dựng bằng code một lần rồi mỗi frame chỉ cập nhật chữ / ẩn hiện.
    /// Bố cục: thanh trên · bảng thành bên trái (lưới + tay bài + ô bán) · dải dưới chiến trường (cửa hàng + nút bắt đầu đợt).
    /// Thao tác: kéo-thả HOẶC bấm chọn thẻ rồi bấm ô. Esc = menu tạm dừng.
    /// </summary>
    [DefaultExecutionOrder(100)]   // chạy SAU Battle.LateUpdate: lúc đó job mô phỏng đã xong, đọc số liệu an toàn
    public class GameUi : MonoBehaviour
    {
        public const float VH = 1080f, TopH = 72f, PanelW = 520f, DockH = 206f;   // đủ cao cho hình thẻ cửa hàng x3

        Game g;
        Battle battle;
        City city;
        float scale = 1f, VW = 1920f;

        /// <summary>Công cụ chụp hình dùng để giả vị trí chuột (toạ độ ảo, gốc trên-trái) cho tooltip hiện ra.</summary>
        [System.NonSerialized] public Vector2? fakeMouse;
        public bool PauseOpen => pause != null && pause.gameObject.activeSelf;

        // ---- gốc ----
        RectTransform root, hud, veilRt;
        RawImage viewImage;
        Image veil;

        // ---- thanh trên ----
        readonly Image[] hearts = new Image[Tuning.Hearts];
        TextMeshProUGUI waveText, castleText, enemiesText, crystalText, perfText;
        readonly TextMeshProUGUI[] resText = new TextMeshProUGUI[ResId.Count];
        readonly RectTransform[] resHot = new RectTransform[ResId.Count];
        RectTransform crystalHot, castleBar, castleFill;
        Button speedBtn, pauseBtn;

        // ---- bảng thành ----
        RectTransform leftPanel, gridView, gridContent, handArea, sellZone;
        TextMeshProUGUI titleText, gridHint, handLabel, hostileText, waveInfo, battleInfo, sellText, nextLabel, waveTag;
        Image titleEmblem, attackerEmblem, enemyEmblem;
        readonly Image[] hostileEmblems = new Image[Tuning.MaxHostile];
        RectTransform statsPanel; TextMeshProUGUI statsText; Button statsBtn, shopBtn; Image shopCrystalIcon;
        bool statsOpen = true, shopOpenPrep = true, shopOpenBattle = false;   // trong trận cửa hàng mặc định thu gọn
        Button landBtn;
        const float DockClosedH = 64f;
        Image sellGlow;
        readonly Dictionary<Vector2Int, CellView> cells = new Dictionary<Vector2Int, CellView>();
        readonly List<Vector2Int> wanted = new List<Vector2Int>(), highlight = new List<Vector2Int>();
        readonly CardView[] cards = new CardView[Tuning.HandLimit];
        readonly Image[] previewIcons = new Image[5];
        readonly int[] previewUnit = new int[5];
        static readonly float[] GridCells = { 48f, 64f, 96f, 128f };
        int gridZoom = 2;
        Vector2 gridPan, panFrom, panStart;
        bool panning;

        // ---- dải dưới: cửa hàng + bắt đầu đợt ----
        RectTransform dock;
        readonly ShopView[] shopSlots = new ShopView[Tuning.ShopSlots];
        Button rollBtn, startBtn;
        TextMeshProUGUI shopCrystals, startHint;

        // ---- nổi ----
        RectTransform tip, ghost, reward, end, factionScreen, menu, pause;
        TextMeshProUGUI tipTitle, tipSub, tipBody;
        Image tipLine, ghostImg;
        readonly Image[] tipStars = new Image[Tuning.MaxStars];
        CanvasGroup rewardGroup, endGroup, augGroup;
        RectTransform augScreen;
        TextMeshProUGUI augTitle, augSub, augOwned;
        readonly TextMeshProUGUI[] augCards = new TextMeshProUGUI[Tuning.AugmentChoices];   // dòng nhóm (trên cùng)
        readonly TextMeshProUGUI[] augBig = new TextMeshProUGUI[Tuning.AugmentChoices], augName = new TextMeshProUGUI[Tuning.AugmentChoices],
                                   augLine = new TextMeshProUGUI[Tuning.AugmentChoices];
        readonly Image[] augIcon = new Image[Tuning.AugmentChoices], augBadge = new Image[Tuning.AugmentChoices];
        readonly RectTransform[] augCardRt = new RectTransform[Tuning.AugmentChoices];
        readonly Image[] augBands = new Image[Tuning.AugmentChoices];
        readonly Button[] augRolls = new Button[Tuning.AugmentChoices];
        static readonly Color[] AugColor = { new Color(1f, 0.85f, 0.29f), new Color(1f, 0.52f, 0.4f), new Color(0.56f, 0.91f, 1f), new Color(0.84f, 0.6f, 1f), new Color(0.62f, 0.88f, 0.48f) };
        static readonly string[] AugHex = { "#ffd84a", "#ff8466", "#8fe8ff", "#d79bff", "#9fe07a" };
        static readonly string[] AugCatName = { "Economy", "Military", "Utility", "Legendary", "Specialist" };
        TextMeshProUGUI rewardTitle, rewardSub, rewardEvent, endTitle, endText, continueInfo, newGameNote;
        readonly RewardView[] rewardCards = new RewardView[Tuning.RewardChoices];
        Button continueBtn;

        // ---- tương tác ----
        Vector2 mouse;                    // toạ độ màn hình (pixel, gốc dưới-trái)
        int dragCard = -1, pressCard = -1, selectedCard = -1;
        bool dragFromCell, hoverCellValid;
        Vector2Int dragCell, hoverCell;
        Vector2 pressAt;
        int infoDef = -1;
        int infoStars = 1;   // số sao của thẻ đang rê chuột (thẻ trên tay có thể nhiều sao)
        Building infoBuilding;
        string tipText;
        string replaceTip;   // báo trước khi thả thẻ đè lên công trình (hiện cả lúc đang kéo)

        class CellView
        {
            public RectTransform rt, barRt, barFill;
            public Image tile, strip, starBg, shape, frame, tint, bar;
            public TextMeshProUGUI name;
            public Image[] stars = new Image[Tuning.MaxStars];
            public Image burst;           // hiệu ứng gộp lên sao: ngôi sao to bật lên giữa ô
            public float mergedAt = -99f;
        }

        class CardView { public RectTransform rt; public Image bg, tile, frame, starBg; public readonly Image[] stars = new Image[Tuning.MaxStars]; public CanvasGroup group; }

        class ShopView
        {
            public RectTransform rt;
            public Image bg, tile, lockFrame, lockIcon, catStrip, footStrip, priceIcon;
            public TextMeshProUGUI name, cat, price;
            public Button buy, lockBtn;
            public CanvasGroup look;           // làm mờ khi không đủ Tinh thể
            public GameObject content;
        }
        TextMeshProUGUI landCost, rollCost;
        Image landIcon, rollIcon, landCrystal, rollCrystal;

        class RewardView
        {
            public Image tile; public TextMeshProUGUI text, extra;
            public RectTransform rt;
            public readonly List<TipChip> chips = new List<TipChip>();
            public readonly List<Image> icons = new List<Image>();
            public readonly List<TextMeshProUGUI> texts = new List<TextMeshProUGUI>();
        }

        // ==================================================================
        // Dựng
        // ==================================================================
        /// <summary>Công cụ kiểm tra: toạ độ giao diện (gốc trên-trái) của tâm ô lưới, để rê chuột giả vào.</summary>
        public Vector2? CellUiPos(Vector2Int p)
        {
            if (!cells.TryGetValue(p, out var v)) return null;
            var c = new Vector3[4];
            v.rt.GetWorldCorners(c);
            var mid = (c[0] + c[2]) * 0.5f;
            return new Vector2(mid.x / scale, (Screen.height - mid.y) / scale);
        }

        public void Init(Game game)
        {
            g = game;
            game.onMerged += OnMerged;
            battle = game.battle;
            city = game.city;
            UiKit.Load(battle.UnitAtlas);

            if (EventSystem.current == null)
            {
                var es = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
                es.transform.SetParent(transform, false);
            }

            // Chiến trường: ảnh độ phân giải thấp phóng nguyên lần, nằm dưới mọi thứ
            var viewCanvas = NewCanvas("Battlefield Canvas", -100, false);
            viewImage = Node("View", viewCanvas.transform).gameObject.AddComponent<RawImage>();
            viewImage.raycastTarget = false;
            var vr = viewImage.rectTransform;
            vr.anchorMin = vr.anchorMax = vr.pivot = Vector2.zero;

            // lớp mờ chuyển cảnh: nằm trên mọi thứ, chỉ bật khi đang chuyển
            var fadeCanvas = NewCanvas("Fade Canvas", 100, true);
            fader = Img(fadeCanvas.transform, White, new Color(0.03f, 0.02f, 0.04f, 0f), false, true, "Fader");
            fader.rectTransform.Fill();
            fader.gameObject.SetActive(false);

            var canvas = NewCanvas("UI Canvas", 0, true);
            root = (RectTransform)canvas.transform;

            veil = Img(root, White, new Color(0.05f, 0.04f, 0.07f, 0f), false, false, "Veil");
            veilRt = veil.rectTransform;
            veil.Fill(PanelW, TopH, 0f, 0f);

            hud = Node("HUD", root);
            hud.Fill();
            BuildLeftPanel();
            BuildDock();
            BuildStats();
            BuildTopBar();
            perfText = Txt(hud, "", 13f, TextAlignmentOptions.BottomRight, false, false, Grey, "Perf");
            perfText.rectTransform.anchorMin = perfText.rectTransform.anchorMax = perfText.rectTransform.pivot = new Vector2(1f, 0f);
            perfText.rectTransform.anchoredPosition = new Vector2(-10f, 4f);
            perfText.rectTransform.sizeDelta = new Vector2(700f, 22f);

            ghostImg = Img(root, null, new Color(1f, 1f, 1f, 0.92f), false, false, "Drag ghost");
            ghost = ghostImg.rectTransform;
            Frame(ghost, Color.white).Fill();
            BuildTooltip();
            BuildReward();
            BuildAugment();
            BuildEnd();
            BuildFactionScreen();
            BuildMenu();
            BuildPause();
            BuildSettings();
        }

        // ------------------------------------------------------------------
        // Chuyển cảnh mượt giữa menu / chọn phe / màn chơi: tối dần → đổi cảnh → sáng dần
        // ------------------------------------------------------------------
        Image fader;
        bool fading;
        public bool Fading => fading;
        const float FadeOut = 0.25f, FadeIn = 0.35f;

        /// <summary>Chạy <paramref name="act"/> giữa lúc màn hình tối hẳn. Đang chuyển thì bỏ qua (không bấm 2 lần).</summary>
        public void Go(System.Action act)
        {
            if (fading) return;
            StartCoroutine(FadeRoutine(act));
        }

        System.Collections.IEnumerator FadeRoutine(System.Action act)
        {
            fading = true;
            fader.gameObject.SetActive(true);                  // chặn bấm trong lúc chuyển
            for (float t = 0f; t < FadeOut; t += Time.unscaledDeltaTime)
            {
                fader.color = new Color(0.03f, 0.02f, 0.04f, Smooth(t / FadeOut));
                yield return null;
            }
            fader.color = new Color(0.03f, 0.02f, 0.04f, 1f);
            act();
            yield return null; yield return null;                // để cảnh mới dựng xong rồi mới sáng lên
            for (float t = 0f; t < FadeIn; t += Time.unscaledDeltaTime)
            {
                fader.color = new Color(0.03f, 0.02f, 0.04f, 1f - Smooth(t / FadeIn));
                yield return null;
            }
            fader.gameObject.SetActive(false);
            fading = false;
        }

        Canvas NewCanvas(string name, int order, bool scaled)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(transform, false);
            var c = go.AddComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            c.sortingOrder = order;
            c.pixelPerfect = false;
            if (scaled)
            {
                var s = go.AddComponent<CanvasScaler>();
                s.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                s.referenceResolution = new Vector2(1920f, VH);
                s.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                s.matchWidthOrHeight = 1f;                      // mọi thứ tính theo chiều cao 1080
                go.AddComponent<GraphicRaycaster>();
            }
            return c;
        }

        void BuildTopBar()
        {
            var bar = Panel(hud, false, "Top bar");
            var rt = bar.rectTransform;
            rt.anchorMin = new Vector2(0f, 1f); rt.anchorMax = new Vector2(1f, 1f); rt.pivot = new Vector2(0.5f, 1f);
            rt.offsetMin = new Vector2(-12f, -(TopH + 2f)); rt.offsetMax = new Vector2(12f, 12f);
            const float pad = 12f, h = TopH - 14f;                 // nội dung nằm trong phần ruột, không đè viền gỗ

            float x = pad + 16f;
            for (int i = 0; i < hearts.Length; i++, x += 46f) hearts[i] = Img(rt, Heart, Color.white).At(x, pad + (h - 40f) * 0.5f, 40f, 40f);
            waveText = Txt(rt, "", 22f, TextAlignmentOptions.MidlineLeft, true).At(x + 16f, pad, 470f, h);
            enemyEmblem = Img(rt, null, Color.white);                   // phe tấn công đợt này (thay cho tên phe)
            waveTag = Txt(rt, "", 20f, TextAlignmentOptions.MidlineLeft, true).At(0f, pad, 200f, h);

            castleBar = Img(rt, White, new Color(0.25f, 0.06f, 0.06f), false, false, "Castle bar").At(640f, pad + (h - 30f) * 0.5f, 220f, 30f).rectTransform;
            castleFill = Img(castleBar, White, new Color(0.35f, 0.62f, 1f), false, false, "Fill").rectTransform;
            castleFill.anchorMin = Vector2.zero; castleFill.anchorMax = Vector2.one; castleFill.pivot = new Vector2(0f, 0.5f);
            castleFill.offsetMin = castleFill.offsetMax = Vector2.zero;
            castleText = Txt(castleBar, "", 15f, TextAlignmentOptions.Center, true);
            castleText.Fill();
            enemiesText = Txt(rt, "", 20f, TextAlignmentOptions.MidlineLeft, true).At(876f, pad, 140f, h);   // 2 dòng: nút tốc độ nằm ngay bên phải

            // bên phải: Menu, Tinh thể, 4 tài nguyên, nút tốc độ / tạm dừng
            float r = pad + 12f;
            Btn(rt, "Menu", TogglePause, 18f).AtRight(r, pad + (h - 44f) * 0.5f, 96f, 44f);
            r += 112f;
            crystalHot = Node("Crystals", rt).AtRight(r, pad, 104f, h);
            Img(crystalHot, Crystal, Color.white).At(0f, (h - 35f) * 0.5f, 35f, 35f);
            crystalText = Txt(crystalHot, "", 24f, TextAlignmentOptions.MidlineLeft, true, false, new Color(0.56f, 0.91f, 1f)).At(42f, 0f, 62f, h);
            r += 112f;
            for (int i = ResId.Count - 1; i >= 0; i--)
            {
                resHot[i] = Node(ResId.Name[i], rt).AtRight(r, pad, 126f, h);
                Img(resHot[i], ResIcons[i], Color.white).At(0f, (h - 40f) * 0.5f, 35f, 40f);
                resText[i] = Txt(resHot[i], "", 24f, TextAlignmentOptions.MidlineLeft, true, false, ResId.Tint[i]).At(42f, 0f, 84f, h);
                r += 130f;
            }
            pauseBtn = Btn(rt, "II", () => battle.paused = !battle.paused, 18f).AtRight(r + 8f, pad + (h - 44f) * 0.5f, 64f, 44f);
            speedBtn = Btn(rt, "x1", g.CycleSpeed, 18f).AtRight(r + 80f, pad + (h - 44f) * 0.5f, 64f, 44f);
        }

        void BuildLeftPanel()
        {
            var panel = Panel(hud, false, "Castle panel");
            leftPanel = panel.rectTransform;
            leftPanel.anchorMin = new Vector2(0f, 0f); leftPanel.anchorMax = new Vector2(0f, 1f); leftPanel.pivot = new Vector2(0f, 1f);
            leftPanel.offsetMin = new Vector2(-12f, -12f); leftPanel.offsetMax = new Vector2(PanelW, -TopH);
            const float ox = 12f;                                   // bảng lùi sang trái 12 để giấu viền

            titleText = Txt(leftPanel, "", 22f, TextAlignmentOptions.MidlineLeft, true).At(ox + 16f, 10f, 300f, 30f);
            titleEmblem = Img(leftPanel, null, Color.white);
            gridHint = Txt(leftPanel, "Wheel: zoom · Right-drag: move", 13f, TextAlignmentOptions.MidlineRight, false, false, Grey).At(ox + 196f, 12f, 314f, 26f);

            var view = Img(leftPanel, White, new Color(0.03f, 0.03f, 0.04f, 0.6f), false, true, "Grid view").At(ox + 10f, 44f, 500f, 500f);
            gridView = view.rectTransform;
            view.gameObject.AddComponent<RectMask2D>();
            gridContent = Node("Grid", gridView);
            gridContent.At(0f, 0f, 10f, 10f);
            Frame(leftPanel, new Color(0.5f, 0.34f, 0.2f, 0.9f)).At(ox + 7f, 41f, 506f, 506f);

            // tay bài: 2 hàng x 5
            handLabel = Txt(leftPanel, "", 15f, TextAlignmentOptions.MidlineLeft).At(ox + 12f, 552f, 496f, 24f);
            handArea = Node("Hand", leftPanel).At(ox + 10f, 578f, 500f, 198f);
            for (int i = 0; i < cards.Length; i++)
            {
                var c = new CardView();
                c.bg = Panel(handArea, true, "Card");
                c.rt = c.bg.rectTransform;
                c.rt.At(5f + (i % 5) * 98f, (i / 5) * 98f, 92f, 92f);
                c.tile = Img(c.rt, null, Color.white).AtCenter(0f, 0f, 64f, 64f);
                c.frame = Frame(c.rt, Color.white);
                c.frame.Fill();
                c.starBg = Img(c.rt, White, Dark);                                   // số sao của thẻ, góc trên trái giống ô trên lưới
                for (int s = 0; s < c.stars.Length; s++) c.stars[s] = Img(c.starBg.transform, Star, Color.white);
                c.group = c.rt.gameObject.AddComponent<CanvasGroup>();
                cards[i] = c;
            }

            // phe thù địch: biểu tượng (rê chuột xem tên)
            hostileText = Txt(leftPanel, "Hostile", 15f, TextAlignmentOptions.MidlineLeft, false, false, Grey).At(ox + 12f, 782f, 70f, 34f);
            for (int i = 0; i < hostileEmblems.Length; i++) hostileEmblems[i] = Img(leftPanel, null, Color.white).At(ox + 84f + i * 40f, 784f, 30f, 30f);
            // đợt tới: biểu tượng phe + số quân + các loại quân sẽ gặp
            nextLabel = Txt(leftPanel, "Next", 15f, TextAlignmentOptions.MidlineLeft, false, false, Grey).At(ox + 12f, 822f, 70f, 44f);
            attackerEmblem = Img(leftPanel, null, Color.white).At(ox + 82f, 826f, 36f, 36f);
            waveInfo = Txt(leftPanel, "", 17f, TextAlignmentOptions.MidlineLeft, true).At(ox + 124f, 822f, 120f, 44f);
            for (int i = 0; i < previewIcons.Length; i++)
            {
                var bg = Img(leftPanel, White, Slot, false, false, "Enemy").At(ox + 254f + i * 51f, 822f, 44f, 44f);
                previewIcons[i] = Img(bg.transform, null, Color.white).AtCenter(0f, 0f, 52f, 52f);
            }
            augOwned = Txt(leftPanel, "", 13f, TextAlignmentOptions.TopLeft, false, true).At(ox + 12f, 872f, 496f, 34f);
            battleInfo = Txt(leftPanel, "", 15f, TextAlignmentOptions.TopLeft, false, true).At(ox + 12f, 560f, 496f, 300f);

            // ô bán: luôn hiện lúc chuẩn bị
            var sell = Panel(leftPanel, true, "Sell zone");
            sellZone = sell.rectTransform;
            sellZone.anchorMin = sellZone.anchorMax = new Vector2(0f, 0f); sellZone.pivot = new Vector2(0f, 0f);
            sellZone.anchoredPosition = new Vector2(ox + 10f, 24f); sellZone.sizeDelta = new Vector2(500f, 72f);
            sellGlow = Img(sellZone, White, new Color(1f, 0.82f, 0.3f, 0.25f));
            sellGlow.Fill(8f, 8f, 8f, 8f);
            sellText = Txt(sellZone, "", 18f, TextAlignmentOptions.Center, true);
            sellText.Fill();
        }

        void ToggleShop()
        {
            if (g.phase == Phase.Battle) shopOpenBattle = !shopOpenBattle; else shopOpenPrep = !shopOpenPrep;
        }

        /// <summary>Bảng thống kê trận (góc trên chiến trường), có nút ẩn / hiện.</summary>
        void BuildStats()
        {
            var p = Panel(hud, false, "Battle stats");
            statsPanel = p.rectTransform;
            statsPanel.anchorMin = statsPanel.anchorMax = statsPanel.pivot = new Vector2(0f, 1f);
            statsPanel.anchoredPosition = new Vector2(PanelW + 20f, -(TopH + 10f));
            // bảng 300 rộng, lề trong 16 đều 4 phía: nút Stats trải hết chiều ngang, 2 dòng số căn giữa bên dưới
            const float pw = 300f, inset = 16f, bh = 40f;
            statsPanel.sizeDelta = new Vector2(pw, inset + bh + 8f + 44f + inset);
            statsText = Txt(statsPanel, "", 15f, TextAlignmentOptions.Center, false, true).At(inset, inset + bh + 8f, pw - 2f * inset, 44f);
            statsBtn = Btn(hud, "Stats", () => statsOpen = !statsOpen, 18f);
            var br = statsBtn.GetComponent<RectTransform>();
            br.anchorMin = br.anchorMax = br.pivot = new Vector2(0f, 1f);
            br.anchoredPosition = new Vector2(PanelW + 20f + inset, -(TopH + 10f + inset));
            br.sizeDelta = new Vector2(pw - 2f * inset, bh);
        }

        /// <summary>
        /// Cửa hàng kiểu TFT: cột trái 2 nút to (mua đất, đổi lại) chỉ có icon + giá; 4 thẻ hình lớn, bấm vào thẻ là mua,
        /// tên dưới trái, giá dưới phải, ổ khoá ở góc; ô đã bán để trống. Tab SHOP nằm trên cửa hàng để thu gọn / mở.
        /// </summary>
        const float DockInset = 20f;          // lùi nội dung cửa hàng vào trong viền gỗ (viền dày ~12)
        static readonly Color CardDark = new Color(0.16f, 0.13f, 0.14f), CardHover = new Color(0.24f, 0.2f, 0.2f);

        /// <summary>Nút nền tối giống thẻ cửa hàng (sáng lên khi rê chuột).</summary>
        Button DarkBtn(Transform parent, System.Action onClick, string name)
        {
            var img = Img(parent, White, Color.white, false, true, name);
            var b = img.gameObject.AddComponent<Button>();
            b.targetGraphic = img;
            b.transition = Selectable.Transition.ColorTint;
            b.colors = new ColorBlock { normalColor = CardDark, highlightedColor = CardHover, pressedColor = new Color(0.1f, 0.08f, 0.09f),
                                        selectedColor = CardDark, disabledColor = CardDark, colorMultiplier = 1f, fadeDuration = 0.05f };
            b.navigation = new Navigation { mode = Navigation.Mode.None };
            b.onClick.AddListener(() => onClick());
            img.gameObject.AddComponent<CanvasGroup>();                 // SetOn làm mờ qua CanvasGroup
            Frame(img.transform, new Color(0.5f, 0.38f, 0.26f, 0.9f)).Fill();
            return b;
        }

        void BuildDock()
        {
            var panel = Panel(hud, false, "Shop dock");
            dock = panel.rectTransform;
            dock.anchorMin = new Vector2(0f, 0f); dock.anchorMax = new Vector2(1f, 0f); dock.pivot = new Vector2(0f, 0f);
            dock.offsetMin = new Vector2(PanelW + 52f, 10f); dock.offsetMax = new Vector2(-10f, 10f + DockH);

            // tab SHOP + Tinh thể (luôn hiện, kể cả khi thu gọn)
            shopBtn = Btn(hud, "", ToggleShop, 18f, "Shop tab");
            var tab = shopBtn.GetComponent<RectTransform>();
            tab.anchorMin = tab.anchorMax = tab.pivot = new Vector2(0f, 0f);
            shopBtn.Label().At(16f, 6f, 86f, 36f);                    // tab cao 48: chữ + icon có khoảng đệm, không dính viền
            shopBtn.Label().alignment = TextAlignmentOptions.MidlineLeft;
            shopCrystalIcon = Img(tab, Crystal, Color.white).At(110f, 12f, 24f, 24f);
            shopCrystals = Txt(tab, "", 19f, TextAlignmentOptions.MidlineLeft, true, false, new Color(0.56f, 0.91f, 1f)).At(140f, 6f, 60f, 36f);

            // mua đất (như "Mua XP" của TFT): icon ô đất + giá
            const float bh = (DockH - DockInset * 2f - 8f) * 0.5f;      // 2 nút cao bằng nhau, nằm trọn trong viền
            landBtn = DarkBtn(dock, () => g.BuyLand(), "Buy land").At(DockInset, DockInset, 200f, bh);
            landIcon = Img(landBtn.transform, null, Color.white).At(10f, (bh - 48f) * 0.5f, 48f, 48f);
            landCrystal = Img(landBtn.transform, Crystal, Color.white).At(124f, (bh - 26f) * 0.5f, 26f, 26f);
            landCost = Txt(landBtn.transform, "", 24f, TextAlignmentOptions.MidlineLeft, true).At(154f, 0f, 40f, bh);
            // đổi lại: icon roll + giá
            rollBtn = DarkBtn(dock, () => g.PayRoll(), "Reroll").At(DockInset, DockInset + bh + 8f, 200f, bh);
            rollIcon = Img(rollBtn.transform, StatIcons[PixelArt.StatReroll], Color.white).At(12f, (bh - 44f) * 0.5f, 44f, 44f);
            rollCrystal = Img(rollBtn.transform, Crystal, Color.white).At(124f, (bh - 26f) * 0.5f, 26f, 26f);
            rollCost = Txt(rollBtn.transform, "", 24f, TextAlignmentOptions.MidlineLeft, true).At(154f, 0f, 40f, bh);

            for (int i = 0; i < shopSlots.Length; i++)
            {
                int slot = i;
                var v = new ShopView();
                v.bg = Img(dock, White, new Color(0.09f, 0.08f, 0.1f), false, true, "Shop card");
                v.rt = v.bg.rectTransform;
                v.buy = v.bg.gameObject.AddComponent<Button>();          // bấm vào thẻ là mua
                v.buy.targetGraphic = v.bg;
                v.buy.transition = Selectable.Transition.None;
                v.buy.navigation = new Navigation { mode = Navigation.Mode.None };
                v.buy.onClick.AddListener(() => g.Buy(slot));
                v.content = Node("Content", v.rt).gameObject;
                ((RectTransform)v.content.transform).Fill();
                v.look = v.content.AddComponent<CanvasGroup>();
                v.look.blocksRaycasts = false;
                v.tile = Img(v.content.transform, null, Color.white);
                v.tile.preserveAspect = true;
                v.footStrip = Img(v.content.transform, White, new Color(0f, 0f, 0f, 0.4f));
                v.catStrip = Img(v.content.transform, White, new Color(0f, 0f, 0f, 0.4f));
                v.cat = Txt(v.catStrip.transform, "", 13f, TextAlignmentOptions.MidlineLeft, false, false, new Color(0.85f, 0.85f, 0.85f));
                v.cat.Fill(6f, 0f, 6f, 0f);
                v.name = Txt(v.content.transform, "", 16f, TextAlignmentOptions.MidlineLeft, true, false, Gold);
                v.name.enableAutoSizing = true; v.name.fontSizeMin = 11f; v.name.fontSizeMax = 16f;   // tên dài thì thu nhỏ, không cắt
                v.price = Txt(v.content.transform, "", 18f, TextAlignmentOptions.MidlineRight, true);
                v.priceIcon = Img(v.content.transform, Crystal, Color.white);
                v.lockFrame = Frame(v.rt, new Color(1f, 0.82f, 0.3f, 0.95f));
                v.lockFrame.Fill();
                v.lockFrame.raycastTarget = false;
                v.lockIcon = Img(v.rt, StatIcons[PixelArt.StatLock], Color.white, false, true, "Lock");
                v.lockBtn = v.lockIcon.gameObject.AddComponent<Button>();
                v.lockBtn.transition = Selectable.Transition.None;
                v.lockBtn.navigation = new Navigation { mode = Navigation.Mode.None };
                v.lockBtn.onClick.AddListener(() => g.ToggleLock(slot));
                shopSlots[i] = v;
            }

            startBtn = Btn(hud, "", g.StartWave, 26f, "Wave button");
            var sb = startBtn.GetComponent<RectTransform>();
            sb.anchorMin = sb.anchorMax = sb.pivot = new Vector2(1f, 0f);
            startHint = Txt(dock, "", 14f, TextAlignmentOptions.Center, false, true, Grey);
            startHint.Show(false);
        }

        /// <summary>1 ô "icon + số" trong tooltip. br = xuống hàng trước ô này.</summary>
        struct TipChip { public Sprite icon; public string text; public bool br; public TipChip(Sprite i, string t, bool b = false) { icon = i; text = t; br = b; } }
        readonly List<TipChip> chips = new List<TipChip>();
        readonly List<Image> chipIcons = new List<Image>();
        readonly List<TextMeshProUGUI> chipTexts = new List<TextMeshProUGUI>();
        TextMeshProUGUI tipExtra;

        void BuildTooltip()
        {
            var p = Panel(root, true, "Tooltip");
            p.raycastTarget = false;
            tip = p.rectTransform;
            tip.anchorMin = tip.anchorMax = new Vector2(0f, 1f);
            tip.pivot = new Vector2(0f, 1f);
            tipTitle = Txt(tip, "", 20f, TextAlignmentOptions.TopLeft, true, false, Gold);
            tipSub = Txt(tip, "", 14f, TextAlignmentOptions.TopLeft, false, false, Grey);
            tipLine = Img(tip, White, new Color(0.85f, 0.75f, 0.5f, 0.5f));
            tipBody = Txt(tip, "", 15f, TextAlignmentOptions.TopLeft, false, true);
            tipExtra = Txt(tip, "", 14f, TextAlignmentOptions.TopLeft, false, true);
            for (int i = 0; i < tipStars.Length; i++) tipStars[i] = Img(tip, Star, Color.white);
        }

        RectTransform Modal(string name, float dim, out RectTransform panel, float w, float h)
        {
            var bg = Img(root, White, new Color(0f, 0f, 0f, dim), false, true, name);
            bg.Fill();
            panel = Panel(bg.transform).AtCenter(0f, 0f, w, h).rectTransform;
            return bg.rectTransform;
        }

        void BuildReward()
        {
            reward = Modal("Reward", 0.55f, out var p, 840f, 560f);
            rewardGroup = reward.gameObject.AddComponent<CanvasGroup>();
            rewardTitle = Txt(p, "", 26f, TextAlignmentOptions.Center, true).At(0f, 22f, 840f, 40f);
            rewardSub = Txt(p, "", 16f, TextAlignmentOptions.Center).At(0f, 64f, 840f, 28f);
            rewardEvent = Txt(p, "", 16f, TextAlignmentOptions.Center, false, false, new Color(1f, 0.85f, 0.29f)).At(0f, 506f, 840f, 30f);
            for (int i = 0; i < rewardCards.Length; i++)
            {
                int pick = i;
                var card = Panel(p, true, "Reward card").At(30f + i * 265f, 104f, 250f, 388f);
                var v = new RewardView();
                v.rt = card.rectTransform;
                v.tile = Img(card.transform, null, Color.white).At(77f, 18f, 96f, 96f);
                v.text = Txt(card.transform, "", 14f, TextAlignmentOptions.TopLeft, false, true).At(18f, 124f, 214f, 100f);
                v.extra = Txt(card.transform, "", 13f, TextAlignmentOptions.TopLeft, false, true);
                Btn(card.transform, "Pick", () => g.PickReward(pick)).At(18f, 328f, 214f, 42f);
                rewardCards[i] = v;
            }
        }

        /// <summary>Xếp các ô "icon + số" từ (x, y), rộng w, tràn thì xuống hàng. Trả về y ngay dưới hàng cuối.</summary>
        float LayoutChips(RectTransform parent, List<TipChip> list, List<Image> icons, List<TextMeshProUGUI> texts, float x0, float y0, float w)
        {
            const float chipH = 24f, icon = 20f;
            while (icons.Count < list.Count)
            {
                icons.Add(Img(parent, null, Color.white, false, false, "Chip icon"));
                texts.Add(Txt(parent, "", 15f, TextAlignmentOptions.MidlineLeft, true));
            }
            float cx = x0, cy = y0;
            for (int i = 0; i < icons.Count; i++)
            {
                bool on = i < list.Count;
                icons[i].Show(on); texts[i].Show(on);
                if (!on) continue;
                var c = list[i];
                texts[i].Set(c.text);
                float tw = texts[i].GetPreferredValues(c.text).x + 2f, cw = icon + 4f + tw;
                if ((c.br && cx > x0) || cx + cw > x0 + w) { cx = x0; cy += chipH; }
                icons[i].sprite = c.icon;
                icons[i].At(cx, cy + (chipH - icon) * 0.5f, icon, icon);
                texts[i].At(cx + icon + 4f, cy, tw, chipH);
                cx += cw + 12f;
            }
            return list.Count > 0 ? cy + chipH : y0;
        }

        void BuildAugment()
        {
            augScreen = Modal("Augment", 0.55f, out var p, 840f, 430f);
            augGroup = augScreen.gameObject.AddComponent<CanvasGroup>();
            augTitle = Txt(p, "", 26f, TextAlignmentOptions.Center, true).At(0f, 22f, 840f, 40f);
            augSub = Txt(p, "", 16f, TextAlignmentOptions.Center).At(0f, 64f, 840f, 28f);
            for (int i = 0; i < augCards.Length; i++)
            {
                int pick = i;
                var card = Panel(p, true, "Augment card").At(30f + i * 265f, 104f, 250f, 296f);
                augCardRt[i] = card.rectTransform;
                augBands[i] = Img(card.transform, White, Color.white).At(14f, 14f, 222f, 6f);
                // thẻ gọn: nhóm · icon to (+ icon phụ ở góc) · SỐ TO · tên · 1 dòng ngắn. Mô tả đầy đủ: rê chuột vào thẻ
                augCards[i] = Txt(card.transform, "", 12f, TextAlignmentOptions.Center).At(14f, 24f, 222f, 18f);
                augIcon[i] = Img(card.transform, null, Color.white).At(87f, 46f, 76f, 76f);
                augIcon[i].preserveAspect = true;
                augBadge[i] = Img(card.transform, null, Color.white).At(141f, 98f, 30f, 30f);
                augBig[i] = Txt(card.transform, "", 30f, TextAlignmentOptions.Center, true).At(10f, 128f, 230f, 38f);
                augName[i] = Txt(card.transform, "", 18f, TextAlignmentOptions.Center, true, false, new Color(1f, 0.9f, 0.62f)).At(10f, 166f, 230f, 24f);
                augLine[i] = Txt(card.transform, "", 14f, TextAlignmentOptions.Top, false, true, Grey).At(18f, 192f, 214f, 40f);
                Btn(card.transform, "Choose", () => g.PickAugment(pick)).At(18f, 238f, 138f, 42f);
                augRolls[i] = Btn(card.transform, "Reroll", () => g.RerollAugment(pick), 15f).At(162f, 238f, 70f, 42f);   // mỗi ô đổi được 1 lần
            }
        }

        /// <summary>Icon to + icon phụ ở góc cho thẻ lõi.</summary>
        void AugSprites(AugmentDef d, out Sprite main, out Sprite badge)
        {
            badge = null;
            if (d.unit >= 0)
            {
                main = UnitIcons[d.unit];
                switch (d.unit)
                {
                    case UnitType.Swordsman: case UnitType.ForestArcher: case UnitType.Archer: badge = StatIcons[PixelArt.StatRange]; break;
                    case UnitType.Monk: case UnitType.Treant: case UnitType.Shaman: case UnitType.Shade: badge = Heart; break;
                    case UnitType.Catapult: case UnitType.Cannon: case UnitType.Eagle: case UnitType.Screamer: badge = StatIcons[PixelArt.StatSplash]; break;
                    case UnitType.Druid: case UnitType.GoatRider: case UnitType.Bear: case UnitType.Eye: badge = StatIcons[PixelArt.StatTime]; break;
                    case UnitType.DeerRider: badge = StatIcons[PixelArt.StatSpeed]; break;
                    case UnitType.Aberration: case UnitType.Summoner: badge = UnitIcons[UnitType.Voidling]; break;
                    default: badge = StatIcons[PixelArt.StatDamage]; break;
                }
                return;
            }
            switch (d.id)
            {
                case Aug.Harvest: main = ResIcons[ResId.Food]; badge = StatIcons[PixelArt.StatBonus]; break;
                case Aug.Timber: main = ResIcons[ResId.Wood]; badge = StatIcons[PixelArt.StatBonus]; break;
                case Aug.GoldRush: main = ResIcons[ResId.Gold]; badge = StatIcons[PixelArt.StatBonus]; break;
                case Aug.BabyBoom: main = ResIcons[ResId.Pop]; badge = StatIcons[PixelArt.StatBonus]; break;
                case Aug.Stockpile: main = Tiles[B.Barn]; badge = ResIcons[ResId.Gold]; break;
                case Aug.Treasury: main = Crystal; break;
                case Aug.RichGetRicher: main = Crystal; badge = StatIcons[PixelArt.StatBonus]; break;
                case Aug.Sharpened: main = StatIcons[PixelArt.StatDamage]; break;
                case Aug.IronSkin: main = Heart; break;
                case Aug.Drill: main = StatIcons[PixelArt.StatTime]; badge = StatIcons[PixelArt.StatBonus]; break;
                case Aug.Supply: main = ResIcons[ResId.Pop]; badge = Crystal; break;
                case Aug.Medics: main = Heart; badge = StatIcons[PixelArt.StatBonus]; break;
                case Aug.Walls: main = Tiles[PixelArt.TileCandidate]; badge = Heart; break;
                case Aug.Scouts: main = StatIcons[PixelArt.StatRange]; break;
                case Aug.FreeRoll: main = Crystal; badge = StatIcons[PixelArt.StatTime]; break;
                case Aug.Bargain: main = Crystal; break;
                case Aug.LandGrant: main = Tiles[B.Expand]; break;
                case Aug.Recruit: main = Tiles[B.Barracks + g.faction * 5]; badge = StarFor(2); break;
                case Aug.Architect: main = Shapes[(int)Shape.Ring]; badge = StatIcons[PixelArt.StatBonus]; break;
                case Aug.Dragon: main = UnitIcons[UnitType.Dragon]; break;
                case Aug.Meteor: main = MeteorIcon; break;
                case Aug.Ballista: main = BoltIcon; break;
                case Aug.Mercenary: main = UnitIcons[UnitType.Axeman]; badge = Crystal; break;
                case Aug.LastStand: main = Heart; badge = StarFor(4); break;
                default: main = Star; break;
            }
        }

        static string AugLine(int a)
        {
            var d = GameData.Augments[a];
            return $"<color={AugHex[(int)d.cat]}><b>{d.name}</b></color>  {d.desc}";
        }

        void BuildEnd()
        {
            end = Modal("End", 0.6f, out var p, 720f, 260f);
            endGroup = end.gameObject.AddComponent<CanvasGroup>();
            endTitle = Txt(p, "", 40f, TextAlignmentOptions.Center, true).At(0f, 26f, 720f, 60f);
            endText = Txt(p, "", 17f, TextAlignmentOptions.Center).At(0f, 98f, 720f, 30f);
            Btn(p, "New run", () => Go(g.NewGame)).At(110f, 170f, 240f, 52f);
            Btn(p, "Main menu", () => Go(g.EnterMenu)).At(370f, 170f, 240f, 52f);
        }

        void BuildFactionScreen()
        {
            var bg = Img(root, White, new Color(0.03f, 0.03f, 0.05f, 0.9f), false, true, "Faction pick");
            factionScreen = bg.rectTransform;
            bg.Fill();
            var title = Txt(factionScreen, "Choose your faction", 40f, TextAlignmentOptions.Center, true);
            title.rectTransform.anchorMin = new Vector2(0f, 1f); title.rectTransform.anchorMax = new Vector2(1f, 1f); title.rectTransform.pivot = new Vector2(0.5f, 1f);
            title.rectTransform.anchoredPosition = new Vector2(0f, -50f); title.rectTransform.sizeDelta = new Vector2(0f, 60f);
            var sub = Txt(factionScreen, $"Hold the castle for {Tuning.Waves} waves. Each time it falls you lose 1 of {Tuning.Hearts} hearts. Three other factions become your enemies.",
                          16f, TextAlignmentOptions.Center, false, false, Grey);
            sub.rectTransform.anchorMin = new Vector2(0f, 1f); sub.rectTransform.anchorMax = new Vector2(1f, 1f); sub.rectTransform.pivot = new Vector2(0.5f, 1f);
            sub.rectTransform.anchoredPosition = new Vector2(0f, -116f); sub.rectTransform.sizeDelta = new Vector2(0f, 30f);

            const float w = 340f, gap = 16f, h = 690f;
            float total = w * Faction.Count + gap * (Faction.Count - 1);
            for (int f = 0; f < Faction.Count; f++)
            {
                int pick = f;
                var fd = GameData.Factions[f];
                var p = Panel(factionScreen).AtCenter(-total * 0.5f + w * 0.5f + f * (w + gap), 20f, w, h).rectTransform;
                Img(p, White, fd.color).At(14f, 14f, w - 28f, 5f);
                Img(p, Emblems[f], Color.white).At(18f, 22f, 44f, 44f);
                Txt(p, fd.name, 26f, TextAlignmentOptions.Center, true, false, fd.color).At(0f, 26f, w, 36f);
                Txt(p, $"<color=#9fe07a>Strength:</color> {fd.strength}", 15f, TextAlignmentOptions.TopLeft, false, true).At(20f, 72f, w - 40f, 66f);
                Txt(p, $"<color=#ff8f8f>Weakness:</color> {fd.weakness}", 15f, TextAlignmentOptions.TopLeft, false, true).At(20f, 142f, w - 40f, 50f);
                for (int s = 0; s < 5; s++)
                {
                    var u = GameData.Units[f * 5 + s];
                    var row = Img(p, White, Slot).At(18f, 204f + s * 74f, w - 36f, 68f);
                    Img(row.transform, UnitIcons[f * 5 + s], Color.white).At(2f, 2f, 64f, 64f);
                    Txt(row.transform, u.name, 17f, TextAlignmentOptions.TopLeft, true).At(72f, 6f, w - 116f, 24f);
                    Txt(row.transform, $"{u.role}\n<color=#888888>{u.cost.Describe()}</color>", 13f, TextAlignmentOptions.TopLeft, false, true, new Color(0.73f, 0.73f, 0.73f)).At(72f, 29f, w - 116f, 38f);
                }
                Btn(p, $"Play {fd.name}", () => Go(() => g.StartRun(pick))).At(20f, h - 76f, w - 40f, 54f);
            }
            var back = Btn(factionScreen, "Back", () => Go(g.EnterMenu), 18f);
            back.At(30f, 30f, 120f, 46f);
        }

        void BuildMenu()
        {
            menu = Node("Main menu", root);
            menu.Fill();
            var p = Panel(menu).At(120f, 250f, 520f, 560f).rectTransform;   // không làm mờ trận đánh nền
            menuPanel = p;
            Txt(p, "Horde Lab", 60f, TextAlignmentOptions.Center, true, false, Gold).At(0f, 34f, 520f, 76f);
            Txt(p, "Build the castle. Watch the horde.", 17f, TextAlignmentOptions.Center, false, false, Grey).At(0f, 112f, 520f, 26f);

            continueBtn = Btn(p, "Continue", () => Go(() => g.ContinueRun()), 24f).At(60f, 170f, 400f, 64f);
            continueInfo = Txt(p, "", 15f, TextAlignmentOptions.Center, false, false, Grey).At(0f, 238f, 520f, 24f);
            newGameBtn = Btn(p, "New Game", () => Go(g.NewGame), 24f);
            newGameNote = Txt(p, "", 14f, TextAlignmentOptions.Center, false, false, Grey);
            menuSettingsBtn = Btn(p, "Settings", OpenSettings, 24f);
            quitBtn = Btn(p, "Quit", () => Go(Application.Quit), 24f);
        }

        void BuildPause()
        {
            pause = Modal("Pause", 0.6f, out var p, 520f, 492f);
            Txt(p, "Paused", 36f, TextAlignmentOptions.Center, true).At(0f, 28f, 520f, 50f);
            Btn(p, "Resume", TogglePause, 22f).At(70f, 104f, 380f, 60f);
            Btn(p, "Settings", OpenSettings, 22f).At(70f, 176f, 380f, 60f);
            Btn(p, "Save & quit to menu", () => Go(() => { ClosePause(); g.QuitToMenu(); }), 22f).At(70f, 248f, 380f, 60f);
            Btn(p, "Abandon run", () => Go(() => { ClosePause(); g.DeleteSave(); g.EnterMenu(); }), 22f).At(70f, 320f, 380f, 60f);
            Txt(p, "Quitting during a battle returns you to the preparation\nbefore that wave when you continue.", 14f, TextAlignmentOptions.Center, false, true, Grey).At(20f, 400f, 480f, 60f);
            pause.gameObject.SetActive(false);
        }

        bool pausedBefore;
        Phase lastPhaseUi;

        // ------------------------------------------------------------------
        // Settings (mở từ menu chính và menu tạm dừng)
        // ------------------------------------------------------------------
        RectTransform settings, menuPanel;
        Button newGameBtn, menuSettingsBtn, quitBtn;
        readonly Button[] setBtns = new Button[5];
        public bool SettingsOpen => settings != null && settings.gameObject.activeSelf;

        void BuildSettings()
        {
            settings = Modal("Settings", 0.6f, out var p, 560f, 470f);
            Txt(p, "Settings", 36f, TextAlignmentOptions.Center, true).At(0f, 28f, 560f, 50f);
            string[] labels = { "Fullscreen", "VSync", "Show FPS", "Shop open in battle", "Battle speed" };
            System.Action[] acts =
            {
                Settings.ToggleFullscreen,
                () => { Settings.VSync = !Settings.VSync; Settings.ApplyFrameRate(); },
                () => Settings.ShowFps = !Settings.ShowFps,
                () => Settings.BattleShopOpen = !Settings.BattleShopOpen,
                () => Settings.BattleSpeed = Settings.BattleSpeed >= 4 ? 1 : Settings.BattleSpeed * 2,
            };
            for (int i = 0; i < labels.Length; i++)
            {
                int k = i;
                float y = 100f + i * 60f;
                Txt(p, labels[i], 20f, TextAlignmentOptions.MidlineLeft).At(50f, y, 300f, 48f);
                setBtns[i] = Btn(p, "", () => { acts[k](); Settings.Save(); }, 20f).At(360f, y, 150f, 48f);
            }
            Btn(p, "Back", CloseSettings, 22f).At(140f, 400f, 280f, 52f);
            settings.gameObject.SetActive(false);
        }

        public void OpenSettings()
        {
            settings.gameObject.SetActive(true);
            settings.SetAsLastSibling();
            pause.GetChild(0).gameObject.SetActive(false);          // mở từ menu tạm dừng: ẩn khung Paused cho khỏi lấp ló phía sau
        }
        public void CloseSettings()
        {
            settings.gameObject.SetActive(false);
            pause.GetChild(0).gameObject.SetActive(true);
        }

        void RefreshSettings()
        {
            if (!SettingsOpen) return;
            string On(bool b) => b ? "<color=#9fe07a>On</color>" : "<color=#aaaaaa>Off</color>";
            setBtns[0].Label().Set(On(Settings.Fullscreen));
            setBtns[1].Label().Set(On(Settings.VSync));
            setBtns[2].Label().Set(On(Settings.ShowFps));
            setBtns[3].Label().Set(On(Settings.BattleShopOpen));
            setBtns[4].Label().Set($"x{Settings.BattleSpeed}");
        }

        public void TogglePauseForTest() => TogglePause();

        void TogglePause()
        {
            if (PauseOpen) { ClosePause(); return; }
            if (g.phase != Phase.Prep && g.phase != Phase.Battle && g.phase != Phase.Reward && g.phase != Phase.Augment) return;
            pausedBefore = battle.paused;
            battle.paused = true;
            pause.gameObject.SetActive(true);
        }

        void ClosePause()
        {
            if (!PauseOpen) return;
            pause.gameObject.SetActive(false);
            battle.paused = pausedBefore && g.phase == Phase.Battle;
        }

        // ==================================================================
        // Mỗi frame
        // ==================================================================
        bool Over(RectTransform rt) => rt.gameObject.activeInHierarchy && RectTransformUtility.RectangleContainsScreenPoint(rt, mouse, null);
        bool InWorld => g.phase == Phase.Prep || g.phase == Phase.Battle || g.phase == Phase.Reward || g.phase == Phase.Augment || g.phase == Phase.Lost || g.phase == Phase.Won;
        int InterestExtra => g.Has(Aug.RichGetRicher) ? Aug.RichExtraCap : 0;
        static float Smooth(float t) => t * t * (3f - 2f * t);

        void LateUpdate()
        {
            if (g == null) return;
            scale = Screen.height / VH;
            VW = Screen.width / scale;
            mouse = fakeMouse.HasValue ? new Vector2(fakeMouse.Value.x * scale, Screen.height - fakeMouse.Value.y * scale) : (Vector2)Input.mousePosition;
            if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject != null) EventSystem.current.SetSelectedGameObject(null);   // Space / Enter không bấm nhầm nút vừa click

            // ảnh chiến trường
            var view = battle.View;
            view.TopInsetPx = InWorld ? TopH * Screen.height / VH : 0f;   // trong game: chiến trường nằm dưới thanh trên cùng, không chui xuống dưới
            if (view.Target != null)
            {
                viewImage.texture = view.Target;
                viewImage.rectTransform.sizeDelta = new Vector2(view.Target.width * view.Scale, view.Target.height * view.Scale);
            }

            var phase = g.phase;
            if (Input.GetKeyDown(KeyCode.Escape) && SettingsOpen) CloseSettings();
            else if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (selectedCard >= 0) selectedCard = -1;
                else TogglePause();
            }
            if (PauseOpen && !InWorld) ClosePause();

            menu.Show(phase == Phase.Menu);
            factionScreen.Show(phase == Phase.PickFaction);
            hud.Show(InWorld);
            if (phase == Phase.Menu) RefreshMenu();

            infoDef = -1; infoStars = 1; infoBuilding = null; tipText = null; replaceTip = null; hoverCellValid = false;
            bool modal = PauseOpen || phase == Phase.Reward || phase == Phase.Augment || phase == Phase.Lost || phase == Phase.Won;

            if (InWorld)
            {
                RefreshTopBar();
                RefreshLeftPanel(modal);
                RefreshDock(modal);
                if ((phase == Phase.Prep || phase == Phase.Battle) && !modal) HandleDragAndClick(phase == Phase.Battle);
                else { dragCard = pressCard = selectedCard = -1; dragFromCell = false; }
            }
            RefreshVeilAndModals();
            RefreshGhost();
            RefreshTooltip((modal && !(g.phase == Phase.Augment && !PauseOpen)) || !InWorld);   // màn chọn lõi vẫn hiện tooltip (mô tả đầy đủ)
            RefreshPerf();
            RefreshSettings();
            if (phase == Phase.Battle && lastPhaseUi != Phase.Battle) shopOpenBattle = Settings.BattleShopOpen;   // mỗi đợt: theo Settings
            lastPhaseUi = phase;

            battle.View.BlockMouse = !InWorld || modal || panning || (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject() && !fakeMouse.HasValue);
        }

        void RefreshMenu()
        {
            // không có lượt chơi đã lưu thì không có nút Continue; các nút dồn lên, bảng co lại, căn giữa màn hình
            var save = g.PeekSave();
            bool has = save != null;
            continueBtn.GetComponent<RectTransform>().Show(has);
            continueInfo.Show(has);
            newGameNote.Show(has);
            float y = 170f;
            if (has)
            {
                continueBtn.At(60f, y, 400f, 64f);
                continueInfo.At(0f, y + 68f, 520f, 24f);
                continueInfo.Set($"Wave {save.wave}/{Tuning.Waves} · {GameData.Factions[save.faction].name} · {save.hearts} heart(s) left");
                y += 112f;
            }
            newGameBtn.At(60f, y, 400f, 64f);
            if (has) { newGameNote.At(0f, y + 68f, 520f, 22f); newGameNote.Set("Starting a new run replaces the saved one."); y += 112f; }
            else y += 88f;
            menuSettingsBtn.At(60f, y, 400f, 64f); y += 88f;
            quitBtn.At(60f, y, 400f, 64f); y += 64f + 40f;
            menuPanel.At(120f, Mathf.Round((VH - y) * 0.5f), 520f, y);
        }

        void RefreshTopBar()
        {
            for (int i = 0; i < hearts.Length; i++) hearts[i].color = new Color(1f, 1f, 1f, i < g.hearts ? 1f : 0.18f);
            string tag = (g.IsBoss(g.wave) ? "  <color=#ff8f6a>Boss</color>" : "") +
                         (g.IsFullMoon(g.wave) && (g.attacker == Faction.Beast || g.faction == Faction.Beast) ? "  <color=#e8e6ff>Full moon</color>" : "");
            waveText.Set($"Wave <color=#ffffff>{g.wave}</color>/{Tuning.Waves}    Enemy");
            var wr = waveText.rectTransform;
            float ex = wr.anchoredPosition.x + waveText.GetPreferredValues(waveText.text).x + 10f;
            enemyEmblem.sprite = Emblems[g.attacker];
            enemyEmblem.At(ex, 12f + (TopH - 14f - 36f) * 0.5f, 36f, 36f);
            waveTag.Set(tag.Trim());
            waveTag.At(ex + 44f, 12f, 200f, TopH - 14f);
            if (Over(enemyEmblem.rectTransform)) tipText = FactionTip(g.attacker);

            bool fight = g.phase == Phase.Battle;
            castleBar.Show(fight); enemiesText.Show(fight); speedBtn.Show(fight); pauseBtn.Show(fight);
            if (fight)
            {
                castleFill.anchorMax = new Vector2(Mathf.Clamp01(g.castleHp / (float)g.MaxCastleHp), 1f);
                castleText.Set($"Castle {g.castleHp}/{g.MaxCastleHp}" + (g.lastStandUsed ? "  <color=#d79bff>Last Stand!</color>" : ""));
                enemiesText.Set($"<size=13><color=#aaaaaa>Enemies left</color></size>\n<color=#ff8f8f>{g.EnemiesRemaining:N0}</color>/{g.EnemiesTotal:N0}");
                speedBtn.Label().Set($"x{battle.speed}");
                pauseBtn.Label().Set(battle.paused ? ">" : "II");
            }

            crystalText.Set(g.crystals.ToString());
            if (Over(crystalHot)) tipText = "<b>Energy Crystal</b>\nBuy cards and land, and reroll the shop (also during battle)." +
                $"\n\nEach wave: +{Tuning.CrystalReward}, +{Tuning.WinBonus} more if the castle holds." +
                $"\n<b>Interest</b>: at the end of each wave you get +1 for every {Tuning.InterestStep} you hold (max +{Tuning.InterestMax + InterestExtra})." +
                $"\nYou hold {g.crystals} now: <color=#8fe8ff>+{Tuning.Interest(g.crystals, InterestExtra)}</color>";
            for (int i = 0; i < ResId.Count; i++)
            {
                resText[i].Set(Mathf.FloorToInt(city.res[i]).ToString("N0"));
                if (Over(resHot[i])) tipText = $"<b>{ResId.Name[i]}</b>\n{ResId.Hint[i]}";
            }
        }

        // ------------------------------------------------------------------
        // Bảng thành
        // ------------------------------------------------------------------
        /// <summary>Tên phe + điểm mạnh / yếu khi rê chuột vào biểu tượng phe.</summary>
        string FactionTip(int f)
        {
            var d = GameData.Factions[f];
            return $"<b>{FactionTag(f)}</b>\n<color=#9fe07a>+</color> {d.strength}\n<color=#ff8f8f>-</color> {d.weakness}";
        }

        void RefreshLeftPanel(bool modal)
        {
            bool prep = g.phase == Phase.Prep, fight = g.phase == Phase.Battle;
            bool shopPhase = prep || fight;
            titleText.Set("Castle");
            titleEmblem.sprite = Emblems[g.faction];
            titleEmblem.At(28f + titleText.GetPreferredValues("Castle").x + 10f, 9f, 32f, 32f);
            if (!modal && Over(titleEmblem.rectTransform)) tipText = FactionTip(g.faction);
            RefreshGrid(modal);

            // tay bài + ô bán: cả lúc chuẩn bị lẫn đang đánh (như TFT)
            handLabel.Show(shopPhase); handArea.Show(shopPhase); sellZone.Show(shopPhase);
            battleInfo.Show(false);

            // phe thù địch
            for (int i = 0; i < hostileEmblems.Length; i++)
            {
                bool on = i < g.hostile.Count;
                hostileEmblems[i].Show(on);
                if (!on) continue;
                hostileEmblems[i].sprite = Emblems[g.hostile[i]];
                if (!modal && Over(hostileEmblems[i].rectTransform)) tipText = FactionTip(g.hostile[i]);
            }

            // đợt tới (lúc đánh: đợt đang đánh)
            attackerEmblem.sprite = Emblems[g.attacker];
            if (!modal && Over(attackerEmblem.rectTransform)) tipText = FactionTip(g.attacker);
            waveInfo.Set($"x{g.EnemyPreviewCount():N0}" + (g.IsBoss(g.wave) ? "\n<size=13><color=#ff8f6a>Boss</color></size>" : ""));
            for (int i = 0; i < previewIcons.Length; i++) previewIcons[i].transform.parent.Show(false);
            int k = 0;
            for (int s = 0; s < 5; s++)
            {
                int t = g.attacker * 5 + s;
                if (GameData.Units[t].minWave > g.wave) continue;
                var bg = (RectTransform)previewIcons[k].transform.parent;
                bg.Show(true);
                previewIcons[k].sprite = UnitIcons[t];
                previewUnit[k] = t;
                if (!modal && Over(bg))
                {
                    var u = GameData.Units[t];
                    tipText = $"<b>{u.name}</b>\n<color=#aaaaaa>{u.role}</color>\nHP {u.stats.maxHp:0} · Damage {u.stats.damage:0.#} · Range {u.stats.range:0.#} · Speed {u.stats.speed:0.#}";
                }
                k++;
            }

            // lõi đang có
            if (g.Augments.Count == 0) augOwned.Set("<color=#aaaaaa>Augments: none yet</color>");
            else
            {
                var ab = new StringBuilder("<color=#aaaaaa>Augments:</color> ");
                for (int i = 0; i < g.Augments.Count; i++)
                {
                    var d = GameData.Augments[g.Augments[i]];
                    ab.Append(i > 0 ? " · " : "").Append($"<color={AugHex[(int)d.cat]}>{d.name}</color>");
                }
                augOwned.Set(ab.ToString());
            }
            if (!modal && Over(augOwned.rectTransform))
            {
                var tb = new StringBuilder("<b>Augments</b>");
                if (g.Augments.Count == 0) tb.Append($"\nPick 1 of {Tuning.AugmentChoices} after waves {string.Join(", ", Tuning.AugmentWaves)}.");
                foreach (int a in g.Augments) tb.Append('\n').Append(AugLine(a));
                tipText = tb.ToString();
            }

            if (shopPhase)
            {
                handLabel.Set($"Hand <color=#ffffff>{g.hand.Count}</color>/{Tuning.HandLimit}");
                if (selectedCard >= g.hand.Count) selectedCard = -1;
                for (int i = 0; i < cards.Length; i++)
                {
                    var c = cards[i];
                    bool on = i < g.hand.Count;
                    c.rt.Show(true);                                   // luôn hiện đủ ô: ô trống mờ để biết còn bao nhiêu chỗ
                    c.tile.Show(on);
                    if (!on) { c.starBg.Show(false); c.frame.Show(false); c.group.alpha = 0.3f; continue; }
                    c.tile.sprite = Tiles[g.hand[i]];
                    int cs = g.hand[i] == B.Expand ? 0 : g.HandStars(i);
                    const float st = 14f;
                    c.starBg.Show(cs > 0);
                    c.starBg.At(4f, 4f, 4f + cs * (st - 1f), st + 3f);
                    for (int q = 0; q < c.stars.Length; q++)
                    {
                        c.stars[q].Show(q < cs);
                        if (q < cs) { c.stars[q].At(2f + q * (st - 1f), 1f, st, st); c.stars[q].sprite = StarFor(cs); }
                    }
                    c.group.alpha = dragCard == i ? 0.3f : 1f;
                    bool over = !modal && Over(c.rt);
                    c.frame.Show(selectedCard == i || (over && dragCard < 0 && !dragFromCell));
                    c.frame.color = selectedCard == i ? new Color(1f, 0.85f, 0.29f) : Color.white;
                    if (over) { infoDef = g.hand[i]; infoStars = g.HandStars(i); }
                }

                // ô bán
                bool card = dragCard >= 0 || selectedCard >= 0;
                var db = dragFromCell ? city.Get(dragCell) : null;
                bool overSell = !modal && Over(sellZone);
                sellGlow.Show(overSell && (card || db != null));
                int sellStars = db != null ? db.stars : dragCard >= 0 ? g.HandStars(dragCard) : selectedCard >= 0 ? g.HandStars(selectedCard) : 1;
                sellText.Set(db != null || card ? $"Sell  <color=#8fe8ff>+{g.SellValue(sellStars)}</color>" : "<color=#888888>Sell</color>");
            }

            // bảng thống kê trận (ẩn / hiện bằng nút STATS)
            statsBtn.GetComponent<RectTransform>().Show(fight);
            statsPanel.Show(fight && statsOpen);
            statsBtn.Label().Set(statsOpen ? "Stats  -" : "Stats  +");
            if (fight && statsOpen)
            {
                int t = (int)g.waveTime;
                statsText.Set($"Troops <color=#8fb8ff>{g.AlliesAlive:N0}</color>    Enemies <color=#ff8f8f>{g.EnemiesRemaining:N0}</color>/{g.EnemiesTotal:N0}\n" +
                              $"Killed <color=#9fe07a>{battle.Counter(C.Dead1):N0}</color>    Lost <color=#ff8f8f>{battle.Counter(C.Dead0):N0}</color>    {t / 60:00}:{t % 60:00}");
            }
        }

        void RefreshGrid(bool modal)
        {
            city.Bounds(out var min, out var max);
            int cols = max.x - min.x + 3, rows = max.y - min.y + 3;       // chừa 1 vòng ngoài cho ô sắp mở
            bool inView = !modal && Over(gridView);

            // Lăn chuột trên lưới: đổi cỡ ô. Kéo chuột phải / giữa: di chuyển lưới.
            if (inView && !fakeMouse.HasValue)
            {
                float wheel = Input.mouseScrollDelta.y;
                if (wheel > 0.01f) gridZoom = Mathf.Min(gridZoom + 1, GridCells.Length - 1);
                else if (wheel < -0.01f) gridZoom = Mathf.Max(gridZoom - 1, 0);
                if (Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(2)) { panning = true; panFrom = mouse; panStart = gridPan; }
            }
            if (panning)
            {
                gridPan = panStart + new Vector2(mouse.x - panFrom.x, -(mouse.y - panFrom.y)) / scale;
                if (!Input.GetMouseButton(1) && !Input.GetMouseButton(2)) panning = false;
            }

            float cell = GridCells[gridZoom];
            float contentW = cols * cell, contentH = rows * cell, viewW = gridView.rect.width, viewH = gridView.rect.height;
            float maxPanX = Mathf.Max(0f, (contentW - viewW) * 0.5f), maxPanY = Mathf.Max(0f, (contentH - viewH) * 0.5f);
            gridPan = new Vector2(Mathf.Clamp(gridPan.x, -maxPanX, maxPanX), Mathf.Clamp(gridPan.y, -maxPanY, maxPanY));
            float ox = Mathf.Round((viewW - contentW) * 0.5f + gridPan.x), oy = Mathf.Round((viewH - contentH) * 0.5f + gridPan.y);
            gridContent.anchoredPosition = new Vector2(ox, -oy);
            gridHint.Show(maxPanX > 0f || maxPanY > 0f || gridZoom != 2);

            bool prep = g.phase == Phase.Prep;
            int dragDef = dragCard >= 0 && dragCard < g.hand.Count ? g.hand[dragCard]
                        : dragFromCell && city.Get(dragCell) != null ? city.Get(dragCell).def
                        : selectedCard >= 0 && selectedCard < g.hand.Count ? g.hand[selectedCard] : -1;

            wanted.Clear();
            for (int y = min.y - 1; y <= max.y + 1; y++)
                for (int x = min.x - 1; x <= max.x + 1; x++)
                {
                    var p = new Vector2Int(x, y);
                    bool open = city.IsOpen(p);
                    if (!open && !city.CanExpand(p)) continue;
                    wanted.Add(p);
                    var v = GetCell(p);
                    v.rt.At((x - min.x + 1) * cell, (y - min.y + 1) * cell, cell, cell);
                    bool over = inView && Over(v.rt);
                    var b = open ? city.Get(p) : null;
                    DrawCell(v, p, open, b, cell, over, dragDef);

                    if (!over) continue;
                    if (open || dragDef == B.Expand) { hoverCell = p; hoverCellValid = true; }
                    if (b != null) { infoDef = b.def; infoBuilding = b; }
                }
            foreach (var kv in cells) kv.Value.rt.Show(wanted.Contains(kv.Key));

            // Tô vùng buff: của nhà đang rê chuột vào, hoặc của thẻ / nhà đang cầm nếu thả xuống ô này
            highlight.Clear();
            if (hoverCellValid && city.IsOpen(hoverCell))
            {
                int def = dragDef >= 0 ? dragDef : infoBuilding != null ? infoBuilding.def : -1;
                if (def >= 0 && def != B.Expand && GameData.Buildings[def].shape != Shape.None)
                    city.BuffCells(hoverCell, GameData.Buildings[def].shape, highlight);
            }
            foreach (var p in highlight)
                if (cells.TryGetValue(p, out var v)) { v.frame.Show(true); v.frame.color = new Color(1f, 0.86f, 0.3f, 0.95f); }
            // Cầm thẻ trên tay kê lên công trình không gộp được → thay thế: viền cam + báo trước nhà cũ đi đâu
            // chỉ khi KÉO thẻ (bấm chọn rồi bấm vào nhà thì không đổi, xem HandleDragAndClick)
            replaceTip = prep && dragCard >= 0 && dragCard < g.hand.Count && hoverCellValid
                       ? g.ReplaceHint(g.hand[dragCard], g.HandStars(dragCard), hoverCell) : null;
            if (dragDef >= 0 && hoverCellValid && cells.TryGetValue(hoverCell, out var hv))
            {
                hv.frame.Show(true);
                hv.frame.color = replaceTip != null ? new Color(1f, 0.6f, 0.2f) : Color.white;
            }
            if (!prep) { dragFromCell = false; }
        }

        CellView GetCell(Vector2Int p)
        {
            if (cells.TryGetValue(p, out var v)) return v;
            v = new CellView();
            v.rt = Node("Cell", gridContent);
            v.tile = Img(v.rt, null, Color.white);
            v.tile.Fill();
            v.tint = Img(v.rt, White, Color.clear);
            v.tint.Fill();
            v.strip = Img(v.rt, White, Dark);
            v.name = Txt(v.strip.transform, "", 11f, TextAlignmentOptions.Center, true);
            v.name.Fill();
            v.starBg = Img(v.rt, White, Dark);
            for (int i = 0; i < v.stars.Length; i++) v.stars[i] = Img(v.starBg.transform, Star, Color.white);
            v.shape = Img(v.rt, null, Color.white);
            v.bar = Img(v.rt, White, new Color(0f, 0f, 0f, 0.7f));
            v.barRt = v.bar.rectTransform;
            v.barFill = Img(v.barRt, White, Color.white).rectTransform;
            v.barFill.anchorMin = Vector2.zero; v.barFill.anchorMax = Vector2.one; v.barFill.pivot = new Vector2(0f, 0.5f);
            v.barFill.offsetMin = new Vector2(1f, 1f); v.barFill.offsetMax = new Vector2(-1f, -1f);
            v.frame = Frame(v.rt, Color.white);
            v.frame.Fill();
            v.burst = Img(v.rt, Star, Color.white);
            v.burst.Show(false);
            cells[p] = v;
            return v;
        }

        /// <summary>Gộp lên sao: ô nháy trắng, sao to bật ra giữa ô rồi mờ dần, hàng sao nhỏ nảy lên (khoảng 0,9 giây).</summary>
        const float MergeFxTime = 0.9f;

        void OnMerged(Vector2Int p, int stars)
        {
            GetCell(p).mergedAt = Time.unscaledTime;
        }

        void DrawCell(CellView v, Vector2Int p, bool open, Building b, float cell, bool over, int dragDef)
        {
            bool labelled = cell >= 60f, large = cell >= 90f;
            v.frame.Show(false);
            v.tint.color = Color.clear;
            float mt = Time.unscaledTime - v.mergedAt;
            bool merging = b != null && mt < MergeFxTime;
            v.burst.Show(merging);
            if (b == null)
            {
                v.tile.sprite = Tiles[open ? PixelArt.TileEmpty : PixelArt.TileCandidate];
                v.tile.color = Color.white;
                v.strip.Show(false); v.starBg.Show(false); v.shape.Show(false); v.bar.Show(false);
                if (!open && dragDef == B.Expand)
                {
                    v.tint.color = new Color(0.3f, 0.9f, 0.4f, over ? 0.45f : 0.16f);
                    v.frame.Show(true); v.frame.color = new Color(0.6f, 1f, 0.65f, 0.9f);
                }
                else if (open && dragDef >= 0 && dragDef != B.Expand && over) v.tint.color = new Color(1f, 1f, 1f, 0.18f);
                return;
            }

            var d = b.Def;
            bool dragged = dragFromCell && dragCell == p;
            v.tile.sprite = Tiles[b.def];
            v.tile.color = new Color(1f, 1f, 1f, dragged ? 0.35f : 1f);

            float nameH = !labelled ? 0f : large ? 17f : 13f;
            v.strip.Show(labelled);
            if (labelled)
            {
                v.strip.At(0f, cell - nameH, cell, nameH);
                v.name.fontSize = large ? 11f : 9f;
                v.name.Set(d.name);
            }

            float st = large ? 14f : labelled ? 11f : 8f;
            v.starBg.Show(true);
            v.starBg.At(0f, 0f, 4f + b.stars * (st - 1f), st + 3f);
            float pop = merging && mt < 0.5f ? 1f + 0.7f * Mathf.Sin(mt / 0.5f * Mathf.PI) : 1f;   // hàng sao nảy lên
            for (int i = 0; i < v.stars.Length; i++)
            {
                v.stars[i].Show(i < b.stars);
                if (i < b.stars)
                {
                    float s2 = st * pop;
                    v.stars[i].At(2f + i * (st - 1f) + (st - s2) * 0.5f, 1f + (st - s2) * 0.5f, s2, s2);
                    v.stars[i].sprite = StarFor(b.stars);
                }
            }
            if (merging)
            {
                // ô nháy trắng rồi tắt dần, sao to (màu hạng mới) phồng ra rồi mờ dần
                float k = mt / MergeFxTime;
                v.tint.color = new Color(1f, 0.9f, 0.55f, 0.45f * Mathf.Clamp01(1f - mt / 0.3f));
                float e = Mathf.Min(1f, mt / 0.3f);
                float size = cell * (0.2f + 0.35f * (1f - (1f - e) * (1f - e)));               // phồng nhanh rồi đứng
                v.burst.sprite = StarFor(b.stars);
                v.burst.color = new Color(1f, 1f, 1f, 0.9f * Mathf.Clamp01((1f - k) * 1.6f));
                v.burst.At((cell - size) * 0.5f, (cell - size) * 0.5f, size, size);
                v.burst.transform.SetAsLastSibling();
            }

            bool shape = d.shape != Shape.None && labelled;
            v.shape.Show(shape);
            if (shape)
            {
                float sz = large ? 21f : 15f;
                v.shape.sprite = Shapes[(int)d.shape];
                v.shape.At(cell - sz - 2f, 2f, sz, sz);
            }

            bool fight = g.phase == Phase.Battle && d.period > 0f;
            v.bar.Show(fight);
            if (fight)
            {
                v.barRt.At(0f, cell - nameH - 5f, cell, 5f);
                v.barFill.anchorMax = new Vector2(Mathf.Clamp01(b.timer / d.period), 1f);
                v.barFill.GetComponent<Image>().color = b.waiting ? new Color(1f, 0.3f, 0.25f) : d.cat == Cat.Barracks ? new Color(0.55f, 0.8f, 1f) : new Color(1f, 0.86f, 0.35f);
                if (b.waiting) { v.frame.Show(true); v.frame.color = new Color(1f, 0.3f, 0.25f, 0.95f); }
                if (b.pulse > 0f) v.tint.color = new Color(1f, 1f, 1f, 0.35f * b.pulse);
            }
        }

        // ------------------------------------------------------------------
        // Dải dưới: cửa hàng + bắt đầu đợt
        // ------------------------------------------------------------------
        void RefreshDock(bool modal)
        {
            bool prep = g.phase == Phase.Prep;
            var tab = shopBtn.GetComponent<RectTransform>();
            var sb = startBtn.GetComponent<RectTransform>();
            tab.Show(g.ShopOpen);
            sb.Show(prep);
            if (!g.ShopOpen) { dock.Show(false); return; }
            bool open = prep ? shopOpenPrep : shopOpenBattle;
            dock.Show(open);

            // tab SHOP ngay trên mép cửa hàng (thu gọn thì nằm sát đáy)
            tab.anchoredPosition = new Vector2(PanelW + 52f, 10f + (open ? DockH + 2f : 0f));   // ngay trên viền cửa hàng
            tab.sizeDelta = new Vector2(216f, 48f);
            shopBtn.Label().Set(open ? "Shop  -" : "Shop  +");
            shopCrystals.Set(g.crystals.ToString());

            // nút Wave: góc dưới phải (trong cột phải của cửa hàng khi mở)
            float rightCol = prep && open ? 172f : 0f;   // nút Wave nhỏ lại, nhường chỗ cho 5 thẻ
            sb.anchoredPosition = new Vector2(-10f - DockInset, open ? 10f + DockInset : 14f);       // trong viền phải của cửa hàng
            sb.sizeDelta = new Vector2(160f, open ? DockH - DockInset * 2f : 56f);
            startBtn.Label().Set($"Wave {g.wave}");
            if (prep && !modal && Over(sb))
            {
                if (g.hand.Count >= Tuning.HandLimit) tipText = "<color=#ff8f8f>Hand is full</color>";
                else if (city.EmptyCount() == city.cells.Count) tipText = "<color=#ffd84a>Place some buildings first</color>";
            }
            if (!open) return;

            // mua đất + đổi lại: chỉ icon + giá
            bool landMax = g.LandLeft <= 0;
            landIcon.sprite = Tiles[B.Expand];
            landCost.Set(landMax ? "<color=#888888>-</color>" : g.Price(B.Expand).ToString());
            landCrystal.Show(!landMax);
            landBtn.SetOn(g.CanBuyLand);
            if (!modal && Over(landBtn.GetComponent<RectTransform>())) infoDef = B.Expand;
            rollCost.Set(g.RollCost == 0 ? "<color=#9fe07a>0</color>" : g.RollCost.ToString());
            rollBtn.SetOn(g.CanRoll);

            // 4 thẻ
            float w = dock.rect.width, h = DockH - DockInset * 2f;
            const float left = DockInset + 200f + 12f, gap = 10f;
            float slotW = Mathf.Floor((w - left - rightCol - DockInset - gap * (shopSlots.Length - 1)) / shopSlots.Length);
            for (int i = 0; i < shopSlots.Length; i++)
            {
                var v = shopSlots[i];
                v.rt.At(left + i * (slotW + gap), DockInset, slotW, h);
                int card = g.shop[i];
                bool has = card >= 0;
                v.content.SetActive(has);
                v.lockIcon.rectTransform.Show(has);
                v.lockFrame.Show(has && g.shopLocked[i]);
                v.bg.color = has ? new Color(0.16f, 0.13f, 0.14f) : new Color(0.07f, 0.06f, 0.08f);   // ô đã bán: để trống
                v.buy.interactable = has && g.CanBuy(i);
                if (!has) continue;

                var d = GameData.Buildings[card];
                v.tile.sprite = Tiles[card];
                // 3 vùng riêng, không đè nhau: hàng trên (loại + ổ khoá) · hình x2 căn giữa · hàng dưới (tên + giá)
                const float headH = 28f, footH = 34f;
                v.catStrip.At(0f, 0f, slotW, headH);                       // dải tối trên (ổ khoá vẽ đè lên, không bị che)
                v.footStrip.At(0f, h - footH, slotW, footH);               // dải tối dưới
                v.cat.rectTransform.Fill(8f, 0f, 40f, 0f);
                v.cat.Set(d.cat == Cat.Barracks ? GameData.Units[d.unit].name : GameData.CatName[(int)d.cat]);
                v.lockIcon.rectTransform.At(slotW - 32f, 5f, 26f, 26f);
                v.lockIcon.sprite = StatIcons[g.shopLocked[i] ? PixelArt.StatLockOn : PixelArt.StatLock];   // bạc = chưa khoá, vàng = đã khoá
                v.lockIcon.color = Color.white;
                float area = h - headH - footH;
                float img = Mathf.Min(PixelArt.TilePx * 3f, Mathf.Floor(Mathf.Min(slotW - 12f, area - 4f) / PixelArt.TilePx) * PixelArt.TilePx);   // phóng nguyên lần
                v.tile.At(Mathf.Round((slotW - img) * 0.5f), headH + Mathf.Round((area - img) * 0.5f), img, img);
                v.name.At(8f, h - footH, slotW - 60f, footH - 4f);
                v.name.Set(d.name);
                v.price.At(slotW - 52f, h - footH, 22f, footH - 4f);
                v.price.Set(g.Price(card).ToString());
                v.priceIcon.At(slotW - 28f, h - footH + 5f, 20f, 20f);
                v.look.alpha = g.CanBuy(i) ? 1f : 0.45f;

                if (modal) continue;
                if (Over(v.lockIcon.rectTransform))
                    tipText = g.shopLocked[i] ? "<b>Locked</b>\nStays through rerolls. Click to unlock." : "<b>Lock</b>\nKeep this card through rerolls.";
                else if (Over(v.rt)) infoDef = card;
            }
        }

        // ------------------------------------------------------------------
        // Kéo-thả và bấm chọn (chỉ lúc chuẩn bị)
        // ------------------------------------------------------------------
        void HandleDragAndClick(bool fighting)
        {
            if (fakeMouse.HasValue) return;
            bool down = Input.GetMouseButtonDown(0), up = Input.GetMouseButtonUp(0);
            if (Input.GetMouseButtonDown(1) && !Over(gridView)) selectedCard = -1;

            if (down)
            {
                pressAt = mouse;
                pressCard = -1;
                for (int i = 0; i < g.hand.Count; i++)
                    if (Over(cards[i].rt)) { pressCard = i; break; }

                if (pressCard < 0 && hoverCellValid && Over(gridView) && !fighting)       // đang đánh: không đụng lưới
                {
                    var target = city.Get(hoverCell);
                    // bấm chọn thẻ rồi bấm ô: chỉ xây vào ô trống / mở ô / gộp. Bấm vào nhà khác loại thì KHÔNG thay nhà đó
                    // (dễ bấm nhầm khi định kéo nhà đi) mà bỏ chọn thẻ và cầm nhà lên như bình thường. Muốn thay thì kéo thẻ.
                    bool playable = selectedCard >= 0 && selectedCard < g.hand.Count &&
                                    (target == null || (Tuning.MergeFromHand && city.CanMerge(hoverCell, g.hand[selectedCard], g.HandStars(selectedCard))));
                    if (playable)
                    {
                        if (g.PlayCard(selectedCard, hoverCell)) selectedCard = -1;
                    }
                    else if (target != null) { selectedCard = -1; dragFromCell = true; dragCell = hoverCell; }
                }
                else if (pressCard < 0 && selectedCard >= 0 && Over(sellZone))
                {
                    g.SellCard(selectedCard);
                    selectedCard = -1;
                }
            }

            // giữ chuột và kéo ra khỏi thẻ đủ xa thì thành kéo-thả
            if (pressCard >= 0 && dragCard < 0 && Input.GetMouseButton(0) && (mouse - pressAt).sqrMagnitude > 100f * scale * scale)
            {
                dragCard = pressCard;
                selectedCard = -1;
            }

            if (!up) return;
            if (dragCard >= 0)
            {
                if (Over(sellZone)) g.SellCard(dragCard);
                else if (hoverCellValid && Over(gridView) && !fighting) g.PlayCard(dragCard, hoverCell);
            }
            else if (dragFromCell)
            {
                if (Over(sellZone)) g.SellBuilding(dragCell);
                else if (hoverCellValid && Over(gridView)) g.MoveBuilding(dragCell, hoverCell);
                else if (Over(handArea)) g.TakeBack(dragCell);
            }
            else if (pressCard >= 0 && pressCard < g.hand.Count && Over(cards[pressCard].rt))
                selectedCard = selectedCard == pressCard ? -1 : pressCard;                    // bấm vào thẻ: chọn / bỏ chọn
            dragCard = pressCard = -1;
            dragFromCell = false;
        }

        void RefreshGhost()
        {
            int def = -1;
            if (g.phase == Phase.Prep)
            {
                if (dragCard >= 0 && dragCard < g.hand.Count) def = g.hand[dragCard];
                else if (dragFromCell && city.Get(dragCell) != null) def = city.Get(dragCell).def;
            }
            ghost.Show(def >= 0);
            if (def < 0) return;
            ghostImg.sprite = Tiles[def];
            ghost.At(mouse.x / scale - 32f, (Screen.height - mouse.y) / scale - 32f, 64f, 64f);
        }

        // ------------------------------------------------------------------
        // Màn che lúc dọn sân + bảng kết quả
        // ------------------------------------------------------------------
        void RefreshVeilAndModals()
        {
            float since = g.endedAt < 0f || !InWorld ? 99f : Time.unscaledTime - g.endedAt;
            float v = since < Game.ClearAt ? since / Game.ClearAt : Mathf.Clamp01(1f - (since - Game.ClearAt) / Game.ClearAt);
            veil.color = new Color(0.05f, 0.04f, 0.07f, Smooth(v));
            veilRt.Show(v > 0f);
            float pop = Smooth(Mathf.Clamp01((since - Game.ClearAt - 0.15f) / 0.35f));

            bool showReward = g.phase == Phase.Reward && pop > 0f;
            reward.Show(showReward);
            if (showReward)
            {
                rewardGroup.alpha = pop;
                rewardGroup.interactable = pop >= 1f && !PauseOpen;
                ((RectTransform)reward.GetChild(0)).anchoredPosition = new Vector2(0f, -(1f - pop) * 50f);
                rewardTitle.Set(g.lastWon ? $"<color=#9fe07a>Wave {g.lastWave}: Castle held!</color>" : $"<color=#ff8f8f>Wave {g.lastWave}: Castle fell — 1 heart lost</color>");
                var parts = new StringBuilder().Append(g.lastCrystals).Append(" reward");
                if (g.lastWinBonus > 0) parts.Append(" + ").Append(g.lastWinBonus).Append(" win");
                if (g.lastInterest > 0) parts.Append(" + ").Append(g.lastInterest).Append(" interest");
                rewardSub.Set($"You gain <color=#8fe8ff>+{g.lastCrystals + g.lastWinBonus + g.lastInterest} Energy Crystals</color>" +
                              $" <color=#aaaaaa>({parts})</color>" +
                              $". Pick 1 of {g.reward.Length} cards:");
                rewardEvent.Set(string.IsNullOrEmpty(g.eventNote) ? "" : $"Event: {g.eventNote}");
                for (int i = 0; i < rewardCards.Length; i++)
                {
                    // giống tooltip: tên + loại + 1 dòng mô tả, rồi hàng icon chỉ số
                    var rc = rewardCards[i];
                    rc.tile.sprite = Tiles[g.reward[i]];
                    rc.chips.Clear();
                    InfoParts(g.reward[i], null, out string title, out string sub, out string body, out _, 1, rc.chips, out string extra);
                    string head = $"<b><size=18><color=#ffe69e>{title}</color></size></b>\n<size=13><color=#aaaaaa>{sub}</color></size>\n{body}";
                    rc.text.Set(head);
                    float y = 124f + rc.text.GetPreferredValues(head, 214f, 0f).y + 8f;
                    y = LayoutChips(rc.rt, rc.chips, rc.icons, rc.texts, 18f, y, 214f);
                    rc.extra.Set(extra ?? "");
                    rc.extra.At(18f, y + 2f, 214f, 60f);
                }
            }

            bool showAug = g.phase == Phase.Augment && pop > 0f;
            augScreen.Show(showAug);
            if (showAug)
            {
                augGroup.alpha = pop;
                augGroup.interactable = pop >= 1f && !PauseOpen;
                ((RectTransform)augScreen.GetChild(0)).anchoredPosition = new Vector2(0f, -(1f - pop) * 50f);
                augTitle.Set(g.lastWon ? $"<color=#9fe07a>Wave {g.lastWave}: Castle held!</color>" : $"<color=#ff8f8f>Wave {g.lastWave}: Castle fell — 1 heart lost</color>");
                int left = 0;
                foreach (int w in Tuning.AugmentWaves) if (w > g.lastWave) left++;
                augSub.Set($"<color=#ffe69e>Choose an augment</color> — a permanent bonus for this run" +
                           (left > 0 ? $" <color=#aaaaaa>({left} more later)</color>" : " <color=#aaaaaa>(last one)</color>"));
                for (int i = 0; i < augCards.Length; i++)
                {
                    var d = GameData.Augments[g.augChoices[i]];
                    bool canRoll = g.CanRerollAugment(i);
                    augRolls[i].SetOn(canRoll);
                    augRolls[i].Label().Set(g.augRerolled[i] ? "<color=#888888>Used</color>" : "Reroll");
                    if (!PauseOpen && Over(augRolls[i].GetComponent<RectTransform>()))
                        tipText = g.augRerolled[i] ? "This slot was already rerolled." : "<b>Reroll this augment</b>\nFree, once per slot. The other two stay.";
                    augBands[i].color = AugColor[(int)d.cat];
                    augCards[i].Set($"<color={AugHex[(int)d.cat]}>{AugCatName[(int)d.cat]}{(d.leaderSkill ? " · Leader" : "")}</color>");
                    AugSprites(d, out var main, out var badge);
                    augIcon[i].sprite = main;
                    augBadge[i].Show(badge != null);
                    augBadge[i].sprite = badge;
                    augBig[i].Set($"<color={AugHex[(int)d.cat]}>{d.big}</color>");
                    augName[i].Set(d.name);
                    augLine[i].Set(d.line);
                    if (!PauseOpen && Over(augCardRt[i]) && !Over(augRolls[i].GetComponent<RectTransform>()))
                        tipText = $"<b>{d.name}</b>\n{d.desc}";                                  // mô tả đầy đủ khi rê chuột
                }
            }

            bool over = (g.phase == Phase.Lost || g.phase == Phase.Won) && pop > 0f;
            end.Show(over);
            if (over)
            {
                endGroup.alpha = pop;
                endGroup.interactable = pop >= 1f;
                ((RectTransform)end.GetChild(0)).anchoredPosition = new Vector2(0f, -(1f - pop) * 50f);
                bool won = g.phase == Phase.Won;
                endTitle.Set(won ? "<color=#ffd84a>Victory!</color>" : "<color=#ff8f8f>Defeat</color>");
                endText.Set(won ? $"{Game.The(g.faction)} held through all {Tuning.Waves} waves with {g.hearts} heart(s) left."
                                : $"All {Tuning.Hearts} hearts lost on wave {g.lastWave}.");
            }
            if (pause.gameObject.activeSelf) pause.SetAsLastSibling();
            if (SettingsOpen) settings.SetAsLastSibling();          // Settings (mở từ menu tạm dừng) luôn nằm trên cùng
        }

        // ------------------------------------------------------------------
        // Tooltip nổi cạnh con trỏ
        // ------------------------------------------------------------------
        void RefreshTooltip(bool blocked)
        {
            string title = null, sub = null, body = tipText, extra = null;
            int stars = 0;
            chips.Clear();
            if (replaceTip != null) body = replaceTip;
            else if (infoDef >= 0) InfoParts(infoDef, infoBuilding, out title, out sub, out body, out stars, infoStars, chips, out extra);
            bool show = !blocked && (replaceTip != null || (dragCard < 0 && !dragFromCell)) && (title != null || !string.IsNullOrEmpty(body));
            tip.Show(show);
            if (!show) return;

            const float maxW = 350f, pad = 18f, chipH = 24f, icon = 20f;
            float headH = title != null ? 54f : 0f;
            tipBody.Set(body ?? "");
            // tooltip chỉ có chữ: khung co theo độ dài chữ, không chừa khoảng trống thừa bên phải
            bool textOnly = title == null && chips.Count == 0 && string.IsNullOrEmpty(extra);
            float w = textOnly && !string.IsNullOrEmpty(body) ? Mathf.Min(maxW, Mathf.Ceil(tipBody.GetPreferredValues(body).x) + 2f * pad + 4f) : maxW;
            float bodyH = string.IsNullOrEmpty(body) ? 0f : tipBody.GetPreferredValues(body, w - 2f * pad, 0f).y;
            bool more = chips.Count > 0 || !string.IsNullOrEmpty(extra);

            // hàng "icon + số": xếp trái sang phải, tràn thì xuống hàng
            float y0 = pad + headH + bodyH + (bodyH > 0f && more ? 8f : 0f), cx = pad, cy = y0;   // khoảng cách chỉ khi còn phần dưới → đệm trên dưới bằng nhau
            while (chipIcons.Count < chips.Count)
            {
                chipIcons.Add(Img(tip, null, Color.white, false, false, "Chip icon"));
                chipTexts.Add(Txt(tip, "", 15f, TextAlignmentOptions.MidlineLeft, true));
            }
            for (int i = 0; i < chipIcons.Count; i++)
            {
                bool on = i < chips.Count;
                chipIcons[i].Show(on); chipTexts[i].Show(on);
                if (!on) continue;
                var c = chips[i];
                chipTexts[i].Set(c.text);
                float tw = chipTexts[i].GetPreferredValues(c.text).x + 2f;
                float cw = icon + 4f + tw;
                if ((c.br && cx > pad) || cx + cw > w - pad) { cx = pad; cy += chipH; }
                chipIcons[i].sprite = c.icon;
                chipIcons[i].At(cx, cy + (chipH - icon) * 0.5f, icon, icon);
                chipTexts[i].At(cx + icon + 4f, cy, tw, chipH);
                cx += cw + 12f;
            }
            float chipsH = chips.Count > 0 ? cy + chipH - y0 + (string.IsNullOrEmpty(extra) ? 0f : 6f) : 0f;
            tipExtra.Set(extra ?? "");
            float extraH = string.IsNullOrEmpty(extra) ? 0f : tipExtra.GetPreferredValues(extra, w - 2f * pad, 0f).y;
            tipExtra.Show(extraH > 0f);
            tipExtra.At(pad, y0 + chipsH, w - 2f * pad, extraH + 4f);
            float h = y0 + chipsH + extraH + pad;

            tipTitle.Show(title != null); tipSub.Show(title != null); tipLine.Show(title != null);
            for (int i = 0; i < tipStars.Length; i++) tipStars[i].Show(title != null && i < stars);
            if (title != null)
            {
                tipTitle.At(pad, pad - 2f, w - 2f * pad - stars * 16f, 26f);
                tipTitle.Set(title);
                tipSub.At(pad, pad + 24f, w - 2f * pad, 20f);
                tipSub.Set(sub);
                tipLine.At(pad, pad + 46f, w - 2f * pad, 1f);
                for (int i = 0; i < stars; i++) { tipStars[i].At(w - pad - (stars - i) * 16f, pad + 3f, 16f, 16f); tipStars[i].sprite = StarFor(stars); }
            }
            tipBody.At(pad, pad + headH, w - 2f * pad, bodyH + 4f);

            float mx = mouse.x / scale, my = (Screen.height - mouse.y) / scale;
            float x = mx + 24f, y = my + 14f;
            if (x + w > VW - 6f) x = mx - w - 14f;
            y = Mathf.Clamp(y, TopH + 4f, VH - h - 6f);
            tip.At(x, y, w, h);
            tip.SetAsLastSibling();
            if (pause.gameObject.activeSelf) pause.SetAsLastSibling();
            if (SettingsOpen) settings.SetAsLastSibling();          // Settings (mở từ menu tạm dừng) luôn nằm trên cùng
        }

        static string ShapeName(Shape s) =>
            s == Shape.Cross ? "Cross (4 tiles)" : s == Shape.Ring ? "Ring (8 tiles)" : s == Shape.Below ? "The tile below" : "The whole row";

        /// <summary>
        /// Tooltip gọn: mô tả ngắn + hàng icon chỉ số (<paramref name="chipsOut"/>) + vài dòng phụ (<paramref name="extra"/>: bonus, gợi ý).
        /// Bản không có icon (thẻ phần thưởng) vẫn dùng InfoParts dạng chữ bên dưới.
        /// </summary>
        void InfoParts(int def, Building b, out string title, out string sub, out string body, out int stars, int cardStars,
                       List<TipChip> chipsOut, out string extra)
        {
            var d = GameData.Buildings[def];
            stars = d.cat == Cat.Expand ? 0 : b != null ? b.stars : cardStars;
            title = d.name;
            sub = $"{GameData.CatName[(int)d.cat]}{(d.faction >= 0 ? " · " + GameData.Factions[d.faction].name : "")}";
            body = d.desc;
            var ex = new StringBuilder();
            string W(string s) => $"<color=#ffffff>{s}</color>";

            if (d.cat == Cat.Barracks)
            {
                var u = GameData.Units[d.unit];
                var f = GameData.Factions[d.faction];
                Res cost = b != null ? city.CallCost(b) : new Res(u.cost.pop * f.popCostMul, u.cost.food, u.cost.wood, u.cost.gold);
                float period = d.period / (b != null ? b.speed : 1f);
                float power = b != null ? city.TroopPower(b) : 1f;
                chipsOut.Add(new TipChip(UnitIcons[d.unit], W($"x{f.squadSide * f.squadSide}")));
                chipsOut.Add(new TipChip(Heart, W($"{u.stats.maxHp * power * (b != null ? b.hp : 1f):0}")));
                chipsOut.Add(new TipChip(StatIcons[PixelArt.StatDamage], W($"{u.stats.damage * power * (b != null ? b.dmg : 1f):0.#}")));
                chipsOut.Add(new TipChip(StatIcons[PixelArt.StatRange], W($"{u.stats.range:0.#}")));
                chipsOut.Add(new TipChip(StatIcons[PixelArt.StatSpeed], W($"{u.stats.speed:0.#}")));
                if (u.stats.splash > 0f) chipsOut.Add(new TipChip(StatIcons[PixelArt.StatSplash], W($"{u.stats.splash:0.#}")));
                chipsOut.Add(new TipChip(StatIcons[PixelArt.StatTime], W($"{period:0.#}s"), true));
                for (int r = 0; r < ResId.Count; r++)
                    if (cost[r] > 0.01f) chipsOut.Add(new TipChip(ResIcons[r], $"<color=#ff9f8f>-{cost[r]:0.#}</color>"));
                if (b != null)
                {
                    if (b.regen > 0f) ex.Append($"<color=#9fe07a>Regenerates {b.regen:0.#} HP/s</color>\n");
                    if (b.costMul < 0.999f) ex.Append($"<color=#9fe07a>-{(1f - b.costMul) * 100f:0}% cost</color>\n");
                }
                if (u.stats.minRange > 0f) ex.Append($"<color=#aaaaaa>Cannot hit targets closer than {u.stats.minRange:0.#}</color>\n");
            }
            else if (d.period > 0f)
            {
                float per = b != null ? d.period / b.speed : d.period;
                float y = b != null ? city.Yield(b) : d.amount;
                bool known = b != null || (def != B.Market && def != B.TownHall && def != B.HunterLodge);
                chipsOut.Add(new TipChip(StatIcons[PixelArt.StatTime], W($"{per:0.#}s")));
                if (known) chipsOut.Add(new TipChip(ResIcons[d.res], $"<color=#9fe07a>+{y:0.#}</color>"));
                if (b != null && b.def == B.Nursery) ex.Append("<color=#aaaaaa>Still growing</color>\n");
                if (b != null && b.def == B.HunterLodge) ex.Append($"<color=#aaaaaa>{b.edges} wild edges</color>\n");
            }
            if (d.shape != Shape.None) chipsOut.Add(new TipChip(Shapes[(int)d.shape], W(ShapeName(d.shape)), true));

            // giá bán bằng icon
            if (b != null && g.phase == Phase.Prep)
                chipsOut.Add(new TipChip(Crystal, $"<color=#8fe8ff>+{g.SellValue(b.stars)}</color> <color=#aaaaaa>Sell</color>", true));
            extra = ex.ToString().TrimEnd('\n');
        }

        void InfoParts(int def, Building b, out string title, out string sub, out string body, out int stars, int cardStars = 1)
        {
            var d = GameData.Buildings[def];
            var sb = new StringBuilder();
            stars = d.cat == Cat.Expand ? 0 : b != null ? b.stars : cardStars;
            title = d.name;
            sub = $"{GameData.CatName[(int)d.cat]}{(d.faction >= 0 ? " · " + GameData.Factions[d.faction].name : "")}";
            sb.Append(d.desc).Append('\n');

            if (d.cat == Cat.Barracks)
            {
                var u = GameData.Units[d.unit];
                var f = GameData.Factions[d.faction];
                Res cost = b != null ? city.CallCost(b) : new Res(u.cost.pop * f.popCostMul, u.cost.food, u.cost.wood, u.cost.gold);
                float period = d.period / (b != null ? b.speed : 1f);
                sb.Append($"\nHP {u.stats.maxHp:0} · Damage {u.stats.damage:0.#} · Range {u.stats.range:0.#} · Speed {u.stats.speed:0.#}\n");
                if (u.stats.splash > 0f) sb.Append($"Area damage radius {u.stats.splash:0.#}{(u.stats.minRange > 0f ? $" · cannot hit targets closer than {u.stats.minRange:0.#}" : "")}\n");
                sb.Append($"Every <color=#ffffff>{period:0.#}s</color>: calls <color=#ffffff>{f.squadSide * f.squadSide}</color> troops\n");
                sb.Append($"Cost per call: <color=#ffffff>{cost.Describe()}</color>\n");
                if (b != null)
                {
                    if (b.stars > 1) sb.Append($"<color=#ffd84a>{b.stars} stars: troops have +{(city.TroopPower(b) - 1f) * 100f:0}% HP and damage</color>\n");
                    if (b.dmg > 1.001f) sb.Append($"<color=#9fe07a>+{(b.dmg - 1f) * 100f:0}% damage</color>\n");
                    if (b.hp > 1.001f) sb.Append($"<color=#9fe07a>+{(b.hp - 1f) * 100f:0}% HP</color>\n");
                    if (b.regen > 0f) sb.Append($"<color=#9fe07a>Regenerates {b.regen:0.#} HP/s</color>\n");
                    if (b.costMul < 0.999f) sb.Append($"<color=#9fe07a>-{(1f - b.costMul) * 100f:0}% cost</color>\n");
                }
            }
            else if (d.period > 0f)
            {
                if (b != null)
                {
                    sb.Append($"\nEvery <color=#ffffff>{d.period / b.speed:0.#}s</color>: <color=#9fe07a>+{city.Yield(b):0.#} {ResId.Name[d.res]}</color>");
                    if (b.def == B.Nursery) sb.Append(" (still growing)");
                    if (b.def == B.HunterLodge) sb.Append($" ({b.edges} wild edges)");
                    sb.Append('\n');
                }
                else if (def != B.Market && def != B.TownHall && def != B.HunterLodge)
                    sb.Append($"\nEvery {d.period:0.#}s: <color=#9fe07a>+{d.amount:0.#} {ResId.Name[d.res]}</color>\n");
            }
            if (d.shape != Shape.None) sb.Append("\nBuff area: ").Append(ShapeName(d.shape)).Append('\n');
            if (b == null && cardStars > 1) sb.Append($"\n<color=#ffd84a>{cardStars}-star card: built with {cardStars} stars.</color>");
            if ((b != null || cardStars > 1) && d.cat != Cat.Expand && stars < Tuning.MaxStars) sb.Append($"\n<color=#aaaaaa>Merge with another {stars}-star {d.name} to reach {stars + 1} stars.</color>");
            if (b != null && g.phase == Phase.Prep)
                sb.Append($"\n<color=#aaaaaa>Drag it to Sell for {g.SellValue(b.stars)} crystal(s), or back onto your hand (keeps its stars).</color>");
            body = sb.ToString().TrimEnd('\n');
        }

        void RefreshPerf()
        {
            bool show = (g.phase == Phase.Battle && Settings.ShowFps) || g.phase == Phase.Sandbox && Settings.ShowFps;
            perfText.transform.SetParent(g.phase == Phase.Sandbox ? root : hud, false);
            perfText.Show(show);
            if (show)
                perfText.Set($"FPS {battle.Fps:0} · sim {battle.SimMs:0.00} ms · {battle.Counter(C.Units):N0} units · {battle.Counter(C.Arrows):N0} projectiles · {battle.Counter(C.Particles):N0} particles");
        }
    }
}
