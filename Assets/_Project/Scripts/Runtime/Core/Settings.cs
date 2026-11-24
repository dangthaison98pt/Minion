using UnityEngine;

namespace HordeLab
{
    /// <summary>
    /// Cài đặt của người chơi, lưu trong PlayerPrefs. Toàn màn hình thì Unity tự nhớ (Screen.fullScreenMode),
    /// ở đây chỉ đọc / đổi trực tiếp.
    /// </summary>
    public static class Settings
    {
        public static bool VSync = true;            // khoá khung hình theo màn hình
        public static bool ShowFps;                 // dòng FPS / số lính / số đạn ở góc dưới phải
        public static bool BattleShopOpen;          // vào trận cửa hàng mở sẵn hay thu gọn
        public static int BattleSpeed = 1;          // tốc độ khi bắt đầu mỗi đợt: 1 / 2 / 4 (nhớ theo lần bấm cuối trong trận, không hiện trong Settings)

        public static bool Fullscreen => Screen.fullScreenMode != FullScreenMode.Windowed;

        // ---- Cỡ màn cố định (16:9), người chơi chọn; cửa sổ không kéo giãn được ----
        public static readonly Vector2Int[] Sizes =
        {
            new Vector2Int(1280, 720), new Vector2Int(1600, 900), new Vector2Int(1920, 1080), new Vector2Int(2560, 1440), new Vector2Int(3840, 2160),
        };
        public static int ResIndex;                 // vị trí trong Sizes

        /// <summary>
        /// Cỡ lớn nhất còn vừa màn hình của máy (chỉ cho chọn tới đó). Chế độ cửa sổ phải chừa chỗ cho thanh tiêu đề + taskbar,
        /// không thì Windows tự bóp cửa sổ lại thành cỡ lẻ.
        /// </summary>
        public static int MaxResIndexFor(bool fullscreen)
        {
            var n = Native;
            int w = n.x, h = n.y - (fullscreen ? 0 : 100), best = 0;
            for (int i = 0; i < Sizes.Length; i++) if (Sizes[i].x <= w && Sizes[i].y <= h) best = i;
            return best;
        }
        public static int MaxResIndex => MaxResIndexFor(false);

        /// <summary>Cỡ gốc của màn hình đang chứa cửa sổ game (máy nhiều màn hình thì không nhất thiết là màn chính).</summary>
        public static Vector2Int Native
        {
            get
            {
                var d = Screen.mainWindowDisplayInfo;
                return d.width > 0 ? new Vector2Int(d.width, d.height) : new Vector2Int(Display.main.systemWidth, Display.main.systemHeight);
            }
        }

        /// <summary>
        /// Cỡ đang dùng. Toàn màn hình luôn chạy đúng cỡ gốc của màn hình — chạy cỡ nhỏ hơn thì Windows kéo giãn ảnh ra làm chữ mờ.
        /// Cỡ chọn trong Settings chỉ dùng cho chế độ cửa sổ.
        /// </summary>
        public static Vector2Int Size => Fullscreen ? Native : Sizes[ResIndex];

        public static void ApplyResolution() => Apply(Fullscreen);

        static void Apply(bool full)
        {
            var s = full ? Native : Sizes[ResIndex];
            Screen.SetResolution(s.x, s.y, full ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed);
        }

        /// <summary>Đổi sang cỡ cửa sổ kế tiếp (vòng lại cỡ nhỏ nhất). Toàn màn hình thì không đổi được.</summary>
        public static void CycleResolution()
        {
            if (Fullscreen) return;
            ResIndex = ResIndex >= MaxResIndex ? 0 : ResIndex + 1;
            ApplyResolution();
        }

        /// <summary>Chạy công cụ kiểm tra / đặt cỡ bằng dòng lệnh thì không tự đổi cỡ màn.</summary>
        static bool CommandLineOwnsScreen()
        {
            if (Application.isBatchMode || Application.isEditor) return true;
            foreach (var a in System.Environment.GetCommandLineArgs())
                if (a == "-autoshot" || a == "-bench" || a == "-screen-width" || a == "-screen-height") return true;
            return false;
        }

        public static void Load()
        {
            VSync = PlayerPrefs.GetInt("set.vsync", 1) != 0;
            ShowFps = PlayerPrefs.GetInt("set.fps", 0) != 0;
            BattleShopOpen = PlayerPrefs.GetInt("set.battleShop", 0) != 0;
            BattleSpeed = Mathf.Clamp(PlayerPrefs.GetInt("set.speed", 1), 1, 4);
            ResIndex = Mathf.Clamp(PlayerPrefs.GetInt("set.res", MaxResIndex), 0, MaxResIndex);   // lần đầu: cỡ lớn nhất vừa màn
            ApplyFrameRate();
            if (!CommandLineOwnsScreen() && (Screen.width != Size.x || Screen.height != Size.y)) ApplyResolution();
        }

        public static void Save()
        {
            PlayerPrefs.SetInt("set.vsync", VSync ? 1 : 0);
            PlayerPrefs.SetInt("set.fps", ShowFps ? 1 : 0);
            PlayerPrefs.SetInt("set.battleShop", BattleShopOpen ? 1 : 0);
            PlayerPrefs.SetInt("set.speed", BattleSpeed);
            PlayerPrefs.SetInt("set.res", ResIndex);
            PlayerPrefs.Save();
        }

        public static void ApplyFrameRate()
        {
            QualitySettings.vSyncCount = VSync ? 1 : 0;
            Application.targetFrameRate = VSync ? -1 : 60;
        }

        public static void ToggleFullscreen()
        {
            // toàn màn hình: cỡ gốc; ra cửa sổ: cỡ đã chọn (hạ xuống nếu không vừa)
            ResIndex = Mathf.Min(ResIndex, MaxResIndex);
            Apply(!Fullscreen);
        }
    }
}
