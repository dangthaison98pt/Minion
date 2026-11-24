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
        public static int BattleSpeed = 1;          // tốc độ khi bắt đầu mỗi đợt: 1 / 2 / 4

        public static bool Fullscreen => Screen.fullScreenMode != FullScreenMode.Windowed;

        public static void Load()
        {
            VSync = PlayerPrefs.GetInt("set.vsync", 1) != 0;
            ShowFps = PlayerPrefs.GetInt("set.fps", 0) != 0;
            BattleShopOpen = PlayerPrefs.GetInt("set.battleShop", 0) != 0;
            BattleSpeed = Mathf.Clamp(PlayerPrefs.GetInt("set.speed", 1), 1, 4);
            ApplyFrameRate();
        }

        public static void Save()
        {
            PlayerPrefs.SetInt("set.vsync", VSync ? 1 : 0);
            PlayerPrefs.SetInt("set.fps", ShowFps ? 1 : 0);
            PlayerPrefs.SetInt("set.battleShop", BattleShopOpen ? 1 : 0);
            PlayerPrefs.SetInt("set.speed", BattleSpeed);
            PlayerPrefs.Save();
        }

        public static void ApplyFrameRate()
        {
            QualitySettings.vSyncCount = VSync ? 1 : 0;
            Application.targetFrameRate = VSync ? -1 : 60;
        }

        public static void ToggleFullscreen()
        {
            if (Fullscreen) Screen.SetResolution(1600, 900, FullScreenMode.Windowed);
            else
            {
                var r = Screen.currentResolution;
                Screen.SetResolution(r.width, r.height, FullScreenMode.FullScreenWindow);
            }
        }
    }
}
