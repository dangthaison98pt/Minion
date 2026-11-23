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
        public const float VH = 1080f, TopH = 72f, PanelW = 520f, DockH = 176f;

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
        TextMeshProUGUI titleText, gridHint, handLabel, hostileText, waveInfo, battleInfo, sellText;
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
        CanvasGroup rewardGroup, endGroup;
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
        Building infoBuilding;
        string tipText;

        class CellView
        {
            public RectTransform rt, barRt, barFill;
            public Image tile, strip, starBg, shape, frame, tint, bar;
            public TextMeshProUGUI name;
            public Image[] stars = new Image[Tuning.MaxStars];
        }

        class CardView { public RectTransform rt; public Image bg, tile, frame; public CanvasGroup group; }

        class ShopView
        {
            public RectTransform rt;
            public Image tile, lockFrame;
            public TextMeshProUGUI name, cat, sold;
            public Button buy, lockBtn;
            public GameObject content;
        }

        class RewardView { public Image tile; public TextMeshProUGUI text; }

        // ==================================================================
        // Dựng
        // ==================================================================
        public void Init(Game game)
        {
            g = game;
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

            var canvas = NewCanvas("UI Canvas", 0, true);
            root = (RectTransform)canvas.transform;

            veil = Img(root, White, new Color(0.05f, 0.04f, 0.07f, 0f), false, false, "Veil");
            veilRt = veil.rectTransform;
            veil.Fill(PanelW, TopH, 0f, 0f);

            hud = Node("HUD", root);
            hud.Fill();
            BuildLeftPanel();
            BuildDock();
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
            BuildEnd();
            BuildFactionScreen();
            BuildMenu();
            BuildPause();
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

            castleBar = Img(rt, White, new Color(0.25f, 0.06f, 0.06f), false, false, "Castle bar").At(640f, pad + (h - 30f) * 0.5f, 220f, 30f).rectTransform;
            castleFill = Img(castleBar, White, new Color(0.35f, 0.62f, 1f), false, false, "Fill").rectTransform;
            castleFill.anchorMin = Vector2.zero; castleFill.anchorMax = Vector2.one; castleFill.pivot = new Vector2(0f, 0.5f);
            castleFill.offsetMin = castleFill.offsetMax = Vector2.zero;
            castleText = Txt(castleBar, "", 15f, TextAlignmentOptions.Center, true);
            castleText.Fill();
            enemiesText = Txt(rt, "", 22f, TextAlignmentOptions.MidlineLeft, true).At(876f, pad, 220f, h);

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
                c.group = c.rt.gameObject.AddComponent<CanvasGroup>();
                cards[i] = c;
            }

            hostileText = Txt(leftPanel, "", 15f, TextAlignmentOptions.TopLeft, false, true).At(ox + 12f, 784f, 496f, 24f);
            waveInfo = Txt(leftPanel, "", 15f, TextAlignmentOptions.TopLeft, false, true).At(ox + 12f, 808f, 496f, 44f);
            for (int i = 0; i < previewIcons.Length; i++)
            {
                var bg = Img(leftPanel, White, Slot, false, false, "Enemy").At(ox + 12f + i * 50f, 856f, 44f, 44f);
                previewIcons[i] = Img(bg.transform, null, Color.white).AtCenter(0f, 0f, 52f, 52f);
            }
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

        void BuildDock()
        {
            var panel = Panel(hud, false, "Shop dock");
            dock = panel.rectTransform;
            dock.anchorMin = new Vector2(0f, 0f); dock.anchorMax = new Vector2(1f, 0f); dock.pivot = new Vector2(0f, 0f);
            dock.offsetMin = new Vector2(PanelW + 52f, 10f); dock.offsetMax = new Vector2(-10f, 10f + DockH);

            Txt(dock, "SHOP", 22f, TextAlignmentOptions.MidlineLeft, true).At(20f, 14f, 90f, 30f);
            Img(dock, Crystal, Color.white).At(22f, 50f, 26f, 26f);
            shopCrystals = Txt(dock, "", 22f, TextAlignmentOptions.MidlineLeft, true, false, new Color(0.56f, 0.91f, 1f)).At(54f, 46f, 80f, 34f);
            rollBtn = Btn(dock, "", () => g.PayRoll(), 17f).At(18f, 98f, 122f, 60f);

            for (int i = 0; i < shopSlots.Length; i++)
            {
                int slot = i;
                var v = new ShopView();
                var bg = Img(dock, White, Slot, false, true, "Shop slot");
                v.rt = bg.rectTransform;
                v.content = Node("Content", v.rt).gameObject;
                ((RectTransform)v.content.transform).Fill();
                v.tile = Img(v.content.transform, null, Color.white).At(8f, 8f, 96f, 96f);
                v.name = Txt(v.content.transform, "", 17f, TextAlignmentOptions.TopLeft, true, true, Gold);
                v.cat = Txt(v.content.transform, "", 13f, TextAlignmentOptions.TopLeft, false, false, Grey);
                v.buy = Btn(v.content.transform, "", () => g.Buy(slot), 17f);
                v.buy.Label().Fill(4f, 2f, 34f, 6f);                       // chừa chỗ cho biểu tượng Tinh thể bên phải
                Img(v.buy.transform, Crystal, Color.white).AtRight(12f, 7f, 20f, 20f);
                v.lockBtn = Btn(v.content.transform, "Lock", () => g.ToggleLock(slot), 15f).At(8f, 110f, 96f, 34f);
                v.sold = Txt(v.rt, "sold", 16f, TextAlignmentOptions.Center, false, false, new Color(0.47f, 0.47f, 0.47f));
                v.sold.Fill();
                v.lockFrame = Frame(v.rt, new Color(1f, 0.82f, 0.3f, 0.95f));
                v.lockFrame.Fill();
                shopSlots[i] = v;
            }

            startBtn = Btn(dock, "", g.StartWave, 26f);
            startHint = Txt(dock, "", 14f, TextAlignmentOptions.Center, false, true, Grey);
        }

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
                v.tile = Img(card.transform, null, Color.white).At(77f, 18f, 96f, 96f);
                v.text = Txt(card.transform, "", 14f, TextAlignmentOptions.TopLeft, false, true).At(18f, 124f, 214f, 200f);
                Btn(card.transform, "Pick", () => g.PickReward(pick)).At(18f, 328f, 214f, 42f);
                rewardCards[i] = v;
            }
        }

        void BuildEnd()
        {
            end = Modal("End", 0.6f, out var p, 720f, 260f);
            endGroup = end.gameObject.AddComponent<CanvasGroup>();
            endTitle = Txt(p, "", 40f, TextAlignmentOptions.Center, true).At(0f, 26f, 720f, 60f);
            endText = Txt(p, "", 17f, TextAlignmentOptions.Center).At(0f, 98f, 720f, 30f);
            Btn(p, "New run", g.NewGame).At(110f, 170f, 240f, 52f);
            Btn(p, "Main menu", g.EnterMenu).At(370f, 170f, 240f, 52f);
        }

        void BuildFactionScreen()
        {
            var bg = Img(root, White, new Color(0.03f, 0.03f, 0.05f, 0.9f), false, true, "Faction pick");
            factionScreen = bg.rectTransform;
            bg.Fill();
            var title = Txt(factionScreen, "CHOOSE YOUR FACTION", 40f, TextAlignmentOptions.Center, true);
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
                Btn(p, $"Play {fd.name}", () => g.StartRun(pick)).At(20f, h - 76f, w - 40f, 54f);
            }
            var back = Btn(factionScreen, "Back", g.EnterMenu, 18f);
            back.At(30f, 30f, 120f, 46f);
        }

        void BuildMenu()
        {
            menu = Node("Main menu", root);
            menu.Fill();
            var shade = Img(menu, White, new Color(0.02f, 0.02f, 0.04f, 0.45f), false, true, "Shade");
            shade.Fill();
            var p = Panel(menu).At(120f, 250f, 520f, 560f).rectTransform;
            Txt(p, "HORDE LAB", 60f, TextAlignmentOptions.Center, true, false, Gold).At(0f, 34f, 520f, 76f);
            Txt(p, "Build the castle. Watch the horde.", 17f, TextAlignmentOptions.Center, false, false, Grey).At(0f, 112f, 520f, 26f);

            continueBtn = Btn(p, "Continue", () => g.ContinueRun(), 24f).At(60f, 170f, 400f, 64f);
            continueInfo = Txt(p, "", 15f, TextAlignmentOptions.Center, false, false, Grey).At(0f, 238f, 520f, 24f);
            Btn(p, "New Game", g.NewGame, 24f).At(60f, 282f, 400f, 64f);
            newGameNote = Txt(p, "", 14f, TextAlignmentOptions.Center, false, false, Grey).At(0f, 350f, 520f, 22f);
            Btn(p, "Quit", Application.Quit, 24f).At(60f, 394f, 400f, 64f);
            Txt(p, "Your run is saved automatically between waves.", 14f, TextAlignmentOptions.Center, false, false, Grey).At(0f, 494f, 520f, 22f);
        }

        void BuildPause()
        {
            pause = Modal("Pause", 0.6f, out var p, 520f, 420f);
            Txt(p, "PAUSED", 36f, TextAlignmentOptions.Center, true).At(0f, 28f, 520f, 50f);
            Btn(p, "Resume", TogglePause, 22f).At(70f, 104f, 380f, 60f);
            Btn(p, "Save & quit to menu", () => { ClosePause(); g.QuitToMenu(); }, 22f).At(70f, 176f, 380f, 60f);
            Btn(p, "Abandon run", () => { ClosePause(); g.DeleteSave(); g.EnterMenu(); }, 22f).At(70f, 248f, 380f, 60f);
            Txt(p, "Quitting during a battle returns you to the preparation\nbefore that wave when you continue.", 14f, TextAlignmentOptions.Center, false, true, Grey).At(20f, 328f, 480f, 60f);
            pause.gameObject.SetActive(false);
        }

        bool pausedBefore;

        void TogglePause()
        {
            if (PauseOpen) { ClosePause(); return; }
            if (g.phase != Phase.Prep && g.phase != Phase.Battle && g.phase != Phase.Reward) return;
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
        bool InWorld => g.phase == Phase.Prep || g.phase == Phase.Battle || g.phase == Phase.Reward || g.phase == Phase.Lost || g.phase == Phase.Won;
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
            if (view.Target != null)
            {
                viewImage.texture = view.Target;
                viewImage.rectTransform.sizeDelta = new Vector2(view.Target.width * view.Scale, view.Target.height * view.Scale);
            }

            var phase = g.phase;
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (selectedCard >= 0) selectedCard = -1;
                else TogglePause();
            }
            if (PauseOpen && !InWorld) ClosePause();

            menu.Show(phase == Phase.Menu);
            factionScreen.Show(phase == Phase.PickFaction);
            hud.Show(InWorld);
            if (phase == Phase.Menu) RefreshMenu();

            infoDef = -1; infoBuilding = null; tipText = null; hoverCellValid = false;
            bool modal = PauseOpen || phase == Phase.Reward || phase == Phase.Lost || phase == Phase.Won;

            if (InWorld)
            {
                RefreshTopBar();
                RefreshLeftPanel(modal);
                RefreshDock(modal);
                if (phase == Phase.Prep && !modal) HandleDragAndClick();
                else { dragCard = pressCard = selectedCard = -1; dragFromCell = false; }
            }
            RefreshVeilAndModals();
            RefreshGhost();
            RefreshTooltip(modal || !InWorld);
            RefreshPerf();

            battle.View.BlockMouse = !InWorld || modal || panning || (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject() && !fakeMouse.HasValue);
        }

        void RefreshMenu()
        {
            var save = g.PeekSave();
            continueBtn.SetOn(save != null);
            continueInfo.Set(save != null ? $"Wave {save.wave}/{Tuning.Waves} · {GameData.Factions[save.faction].name} · {save.hearts} heart(s) left" : "No saved run");
            newGameNote.Set(save != null ? "Starting a new run replaces the saved one." : "");
        }

        void RefreshTopBar()
        {
            for (int i = 0; i < hearts.Length; i++) hearts[i].color = new Color(1f, 1f, 1f, i < g.hearts ? 1f : 0.18f);
            string tag = (g.IsBoss(g.wave) ? "  <color=#ff8f6a>BOSS</color>" : "") +
                         (g.IsFullMoon(g.wave) && (g.attacker == Faction.Beast || g.faction == Faction.Beast) ? "  <color=#e8e6ff>Full moon</color>" : "");
            waveText.Set($"Wave <color=#ffffff>{g.wave}</color>/{Tuning.Waves}    Enemy: {FactionTag(g.attacker)}{tag}");

            bool fight = g.phase == Phase.Battle;
            castleBar.Show(fight); enemiesText.Show(fight); speedBtn.Show(fight); pauseBtn.Show(fight);
            if (fight)
            {
                castleFill.anchorMax = new Vector2(Mathf.Clamp01(g.castleHp / (float)Tuning.CastleHp), 1f);
                castleText.Set($"Castle {g.castleHp}/{Tuning.CastleHp}");
                enemiesText.Set($"Enemies <color=#ff8f8f>{g.EnemiesAlive:N0}</color>");
                speedBtn.Label().Set($"x{battle.speed}");
                pauseBtn.Label().Set(battle.paused ? ">" : "II");
            }

            crystalText.Set(g.crystals.ToString());
            if (Over(crystalHot)) tipText = "<b>Energy Crystal</b>\nSpent between waves only: buy cards and reroll the shop.";
            for (int i = 0; i < ResId.Count; i++)
            {
                resText[i].Set(Mathf.FloorToInt(city.res[i]).ToString("N0"));
                if (Over(resHot[i])) tipText = $"<b>{ResId.Name[i]}</b>\n{ResId.Hint[i]}";
            }
        }

        // ------------------------------------------------------------------
        // Bảng thành
        // ------------------------------------------------------------------
        void RefreshLeftPanel(bool modal)
        {
            bool prep = g.phase == Phase.Prep;
            titleText.Set($"CASTLE — {FactionTag(g.faction)}");
            RefreshGrid(modal);

            handLabel.Show(prep); handArea.Show(prep); sellZone.Show(prep); waveInfo.Show(prep); hostileText.Show(prep);
            battleInfo.Show(!prep);
            for (int i = 0; i < previewIcons.Length; i++) previewIcons[i].transform.parent.Show(false);

            var sb = new StringBuilder("Hostile factions: ");
            for (int i = 0; i < g.hostile.Count; i++) sb.Append(i > 0 ? " · " : "").Append(FactionTag(g.hostile[i]));
            string hostile = sb.ToString();

            if (prep)
            {
                handLabel.Set(g.hand.Count == 0 ? "Hand (0) — buy cards from the shop below"
                    : selectedCard >= 0 ? $"Hand ({g.hand.Count}/{Tuning.HandLimit}) — <color=#ffd84a>now click a tile to build</color>"
                    : $"Hand ({g.hand.Count}/{Tuning.HandLimit}) — drag a card onto the grid, or click it then click a tile");
                if (selectedCard >= g.hand.Count) selectedCard = -1;
                for (int i = 0; i < cards.Length; i++)
                {
                    var c = cards[i];
                    bool on = i < g.hand.Count;
                    c.rt.Show(on);
                    if (!on) continue;
                    c.tile.sprite = Tiles[g.hand[i]];
                    c.group.alpha = dragCard == i ? 0.3f : 1f;
                    bool over = !modal && Over(c.rt);
                    c.frame.Show(selectedCard == i || (over && dragCard < 0 && !dragFromCell));
                    c.frame.color = selectedCard == i ? new Color(1f, 0.85f, 0.29f) : Color.white;
                    if (over) infoDef = g.hand[i];
                }

                hostileText.Set(hostile);
                waveInfo.Set($"Wave {g.wave}: {FactionTag(g.attacker)} attack with about <color=#ffffff>{g.EnemyPreviewCount():N0}</color> troops." +
                             (g.IsBoss(g.wave) ? " <color=#ff8f6a>Boss wave!</color>" : ""));
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

                // ô bán
                bool card = dragCard >= 0 || selectedCard >= 0;
                var db = dragFromCell ? city.Get(dragCell) : null;
                bool overSell = !modal && Over(sellZone);
                sellGlow.Show(overSell && (card || db != null));
                sellText.Set(db != null ? $"SELL  <color=#8fe8ff>+{g.SellValue(db.stars)}</color> crystal(s)"
                           : card ? $"SELL  <color=#8fe8ff>+{g.SellValue(1)}</color> crystal(s)" + (selectedCard >= 0 && dragCard < 0 ? "  — click here" : "")
                           : "<color=#aaaaaa>Drag a card or building here to sell it</color>");
            }
            else
            {
                int t = (int)g.waveTime;
                battleInfo.Set(g.phase != Phase.Battle ? hostile :
                    $"{hostile}\n\nYour troops: <color=#8fb8ff>{g.AlliesAlive:N0}</color>    Enemies: <color=#ff8f8f>{g.EnemiesAlive:N0}</color>" +
                    (g.squadsLeft > 0 ? $"  <color=#aaaaaa>(+{g.squadsLeft} squads still coming)</color>" : "") +
                    $"\nKilled: <color=#9fe07a>{battle.Counter(C.Dead1):N0}</color>    Lost: <color=#ff8f8f>{battle.Counter(C.Dead0):N0}</color>    Time {t / 60:00}:{t % 60:00}" +
                    "\n\n<color=#aaaaaa>A red frame on a troop building means it is waiting for resources.\nEnemies that reach the wall hit it for 1 damage per attack.\n\nSpace: pause · Tab: fast forward · Esc: menu\nMouse wheel: zoom the battlefield · WASD: pan</color>");
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
            if (dragDef >= 0 && hoverCellValid && cells.TryGetValue(hoverCell, out var hv)) { hv.frame.Show(true); hv.frame.color = Color.white; }
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
            cells[p] = v;
            return v;
        }

        void DrawCell(CellView v, Vector2Int p, bool open, Building b, float cell, bool over, int dragDef)
        {
            bool labelled = cell >= 60f, large = cell >= 90f;
            v.frame.Show(false);
            v.tint.color = Color.clear;
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
            for (int i = 0; i < v.stars.Length; i++)
            {
                v.stars[i].Show(i < b.stars);
                if (i < b.stars) v.stars[i].At(2f + i * (st - 1f), 1f, st, st);
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
            dock.Show(prep);
            if (!prep) return;

            float w = dock.rect.width;
            const float left = 152f, right = 276f, gap = 10f;
            float slotW = Mathf.Floor((w - left - right - gap * (shopSlots.Length - 1)) / shopSlots.Length);
            shopCrystals.Set(g.crystals.ToString());
            rollBtn.Label().Set($"Reroll\n<size=14>{Tuning.RollCost} crystal</size>");
            rollBtn.SetOn(g.CanRoll);

            for (int i = 0; i < shopSlots.Length; i++)
            {
                var v = shopSlots[i];
                v.rt.At(left + i * (slotW + gap), 14f, slotW, DockH - 28f);
                int card = g.shop[i];
                v.content.SetActive(card >= 0);
                v.sold.Show(card < 0);
                v.lockFrame.Show(card >= 0 && g.shopLocked[i]);
                if (card < 0) continue;

                var d = GameData.Buildings[card];
                v.tile.sprite = Tiles[card];
                v.name.At(112f, 8f, slotW - 120f, 46f);
                v.name.Set(d.name);
                v.cat.At(112f, 56f, slotW - 120f, 20f);
                v.cat.Set(d.cat == Cat.Barracks ? GameData.Units[d.unit].name : GameData.CatName[(int)d.cat]);
                v.buy.At(112f, 110f, slotW - 120f, 34f);
                v.buy.Label().Set($"Buy {g.Price(card)}");
                v.buy.SetOn(g.CanBuy(i));
                v.lockBtn.Label().Set(g.shopLocked[i] ? "<color=#ffd84a>Locked</color>" : "Lock");

                if (modal) continue;
                if (Over(v.lockBtn.GetComponent<RectTransform>()))
                    tipText = g.shopLocked[i] ? "<b>Locked</b>\nThis card stays through rerolls and the free refresh after each wave. Click to unlock."
                                              : "<b>Lock this slot</b>\nKeep this card through rerolls and the free refresh after each wave.";
                else if (Over(v.rt)) infoDef = card;
            }

            startBtn.At(w - right + 14f, 14f, right - 30f, 104f);
            startBtn.Label().Set($"START WAVE {g.wave}");
            startHint.At(w - right + 14f, 122f, right - 30f, 40f);
            startHint.Set(g.hand.Count >= Tuning.HandLimit ? "<color=#ff8f8f>Hand is full</color>"
                        : city.EmptyCount() == city.cells.Count ? "<color=#ffd84a>Place some buildings first</color>" : "The shop refreshes for free after each wave.");
        }

        // ------------------------------------------------------------------
        // Kéo-thả và bấm chọn (chỉ lúc chuẩn bị)
        // ------------------------------------------------------------------
        void HandleDragAndClick()
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

                if (pressCard < 0 && hoverCellValid && Over(gridView))
                {
                    if (selectedCard >= 0)
                    {
                        if (g.PlayCard(selectedCard, hoverCell)) selectedCard = -1;      // bấm chọn thẻ rồi bấm ô
                    }
                    else if (city.Get(hoverCell) != null) { dragFromCell = true; dragCell = hoverCell; }
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
                else if (hoverCellValid && Over(gridView)) g.PlayCard(dragCard, hoverCell);
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
                rewardTitle.Set(g.lastWon ? $"<color=#9fe07a>WAVE {g.lastWave}: CASTLE HELD!</color>" : $"<color=#ff8f8f>WAVE {g.lastWave}: CASTLE FELL — 1 heart lost</color>");
                rewardSub.Set($"You gain <color=#8fe8ff>+{g.lastCrystals} Energy Crystals</color>. Pick 1 of {g.reward.Length} cards:");
                rewardEvent.Set(string.IsNullOrEmpty(g.eventNote) ? "" : $"Event: {g.eventNote}");
                for (int i = 0; i < rewardCards.Length; i++)
                {
                    rewardCards[i].tile.sprite = Tiles[g.reward[i]];
                    InfoParts(g.reward[i], null, out string title, out string sub, out string body, out _);
                    rewardCards[i].text.Set($"<b><size=18><color=#ffe69e>{title}</color></size></b>\n{sub}\n\n{body}");
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
                endTitle.Set(won ? "<color=#ffd84a>VICTORY!</color>" : "<color=#ff8f8f>DEFEAT</color>");
                endText.Set(won ? $"{Game.The(g.faction)} held through all {Tuning.Waves} waves with {g.hearts} heart(s) left."
                                : $"All {Tuning.Hearts} hearts lost on wave {g.lastWave}.");
            }
            if (pause.gameObject.activeSelf) pause.SetAsLastSibling();
        }

        // ------------------------------------------------------------------
        // Tooltip nổi cạnh con trỏ
        // ------------------------------------------------------------------
        void RefreshTooltip(bool blocked)
        {
            string title = null, sub = null, body = tipText;
            int stars = 0;
            if (infoDef >= 0) InfoParts(infoDef, infoBuilding, out title, out sub, out body, out stars);
            bool show = !blocked && dragCard < 0 && !dragFromCell && (title != null || !string.IsNullOrEmpty(body));
            tip.Show(show);
            if (!show) return;

            const float w = 350f, pad = 18f;
            float headH = title != null ? 54f : 0f;
            tipBody.Set(body ?? "");
            float bodyH = string.IsNullOrEmpty(body) ? 0f : tipBody.GetPreferredValues(body, w - 2f * pad, 0f).y;
            float h = pad + headH + bodyH + pad;

            tipTitle.Show(title != null); tipSub.Show(title != null); tipLine.Show(title != null);
            for (int i = 0; i < tipStars.Length; i++) tipStars[i].Show(title != null && i < stars);
            if (title != null)
            {
                tipTitle.At(pad, pad - 2f, w - 2f * pad - stars * 16f, 26f);
                tipTitle.Set(title);
                tipSub.At(pad, pad + 24f, w - 2f * pad, 20f);
                tipSub.Set(sub);
                tipLine.At(pad, pad + 46f, w - 2f * pad, 1f);
                for (int i = 0; i < stars; i++) tipStars[i].At(w - pad - (stars - i) * 16f, pad + 3f, 16f, 16f);
            }
            tipBody.At(pad, pad + headH - 2f, w - 2f * pad, bodyH + 4f);

            float mx = mouse.x / scale, my = (Screen.height - mouse.y) / scale;
            float x = mx + 24f, y = my + 14f;
            if (x + w > VW - 6f) x = mx - w - 14f;
            y = Mathf.Clamp(y, TopH + 4f, VH - h - 6f);
            tip.At(x, y, w, h);
            tip.SetAsLastSibling();
            if (pause.gameObject.activeSelf) pause.SetAsLastSibling();
        }

        static string ShapeName(Shape s) =>
            s == Shape.Cross ? "cross (4 tiles)" : s == Shape.Ring ? "ring (8 tiles)" : s == Shape.Below ? "the tile below" : "the whole row";

        void InfoParts(int def, Building b, out string title, out string sub, out string body, out int stars)
        {
            var d = GameData.Buildings[def];
            var sb = new StringBuilder();
            stars = d.cat == Cat.Expand ? 0 : b != null ? b.stars : 1;
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
            if (b != null && b.stars < Tuning.MaxStars) sb.Append($"\n<color=#aaaaaa>Merge with another {b.stars}-star {d.name} to reach {b.stars + 1} stars.</color>");
            if (b != null && g.phase == Phase.Prep)
                sb.Append($"\n<color=#aaaaaa>Drag it to SELL for {g.SellValue(b.stars)} crystal(s){(b.stars == 1 ? ", or back onto your hand" : "")}.</color>");
            body = sb.ToString().TrimEnd('\n');
        }

        void RefreshPerf()
        {
            bool show = g.phase == Phase.Battle || g.phase == Phase.Sandbox;
            perfText.transform.SetParent(g.phase == Phase.Sandbox ? root : hud, false);
            perfText.Show(show);
            if (show)
                perfText.Set($"FPS {battle.Fps:0} · sim {battle.SimMs:0.00} ms · {battle.Counter(C.Units):N0} units · {battle.Counter(C.Arrows):N0} projectiles · {battle.Counter(C.Particles):N0} particles");
        }
    }
}
