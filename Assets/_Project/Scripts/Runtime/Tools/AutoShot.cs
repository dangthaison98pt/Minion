using System.Collections;
using System.IO;
using System.Text;
using Unity.Mathematics;
using UnityEngine;

namespace HordeLab
{
    /// <summary>
    /// Công cụ kiểm tra: chạy bản build với tham số  -autoshot "thư mục"  thì game tự dùng kỹ năng,
    /// chụp vài tấm màn hình, ghi số liệu hiệu năng rồi tự thoát. Chơi bình thường thì không làm gì cả.
    /// Thêm  -stress 20000  để dồn quân lên khoảng 20.000 lính (tường thành không bao giờ thủng để trận chạy hết thời gian đo).
    /// </summary>
    public class AutoShot : MonoBehaviour
    {
        string dir;
        int stress;
        static bool Headless => Application.isBatchMode;   // chạy ngầm: không có cửa sổ để chụp
        Battle battle;
        readonly StringBuilder log = new StringBuilder();
        float nextLog = 1f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Init()
        {
            var args = System.Environment.GetCommandLineArgs();
            if (System.Array.IndexOf(args, "-novsync") >= 0)
            {
                QualitySettings.vSyncCount = 0;
                Application.targetFrameRate = -1;
            }
            int stress = 0;
            for (int i = 0; i < args.Length - 1; i++)
                if (args[i] == "-stress") int.TryParse(args[i + 1], out stress);

            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] != "-autoshot") continue;
                var shot = new GameObject("AutoShot").AddComponent<AutoShot>();
                shot.dir = args[i + 1];
                shot.stress = stress;
                Directory.CreateDirectory(shot.dir);
            }
        }

        /// <summary>Đặt quân số ban đầu + viện binh sao cho trận giữ quanh mức <paramref name="n"/> lính.</summary>
        internal static void ApplyStress(Battle b, int n)
        {
            n = math.clamp(n, 1000, Cfg.MaxUnits - 1000);
            b.startFootmen = (int)(n * 0.28f);
            b.startArchers = (int)(n * 0.13f);
            b.startZombies = (int)(n * 0.49f);
            b.startSkeletons = (int)(n * 0.09f);
            b.kingdomCap = (int)(n * 0.45f);
            b.undeadCap = (int)(n * 0.55f);
            b.kingdomRate = b.undeadRate = n * 0.03f;   // bù quân chết thật nhanh để luôn sát mức trần
            b.undeadRateGrowth = 0f;
            b.castleMaxHp = 1000000;
            b.Restart();
        }

        IEnumerator Start()
        {
            while (battle == null) { battle = FindAnyObjectByType<Battle>(); yield return null; }
            if (Headless) log.AppendLine("Chạy ngầm (-batchmode): không chụp màn hình, không đo GPU");
            if (stress > 0)
            {
                ApplyStress(battle, stress);
                log.AppendLine($"STRESS TEST: mục tiêu {stress} lính, MaxUnits {Cfg.MaxUnits}");
            }
            log.AppendLine($"screen {Screen.width}x{Screen.height}, pixel scale {battle.View.Scale}, gfx {SystemInfo.graphicsDeviceType} ({SystemInfo.graphicsDeviceName}), " +
                           $"cpu cores {SystemInfo.processorCount}, vsync {QualitySettings.vSyncCount}, burst {Unity.Burst.BurstCompiler.IsEnabled}");

            for (int i = 0; i < 3; i++) yield return null;
            yield return new WaitForEndOfFrame();
            SaveRT(battle.GroundTexture, "00_ground_early.png");

            yield return Wait(9f);
            battle.CastVolley(new float2(20f, -10f));
            battle.CastFire(new float2(10f, 32f));      // nửa TRÊN màn hình: để kiểm tra vết cháy in đúng chỗ
            yield return Wait(9.5f);
            Shot("01_fire_and_volley.png");
            yield return Wait(16f);
            Shot("02_battle.png");
            yield return Wait(26f);
            Shot("03_battle_later.png");
            yield return new WaitForEndOfFrame();
            SaveRT(battle.View.Target, "03_lowres_target.png");
            SaveRT(battle.GroundTexture, "03_ground_texture.png");

            battle.View.Scale = 1;
            battle.View.SetScale(3, new Vector2(Screen.width * 0.5f, Screen.height * 0.5f));
            battle.View.Center = new float2(-5f, 0f);
            yield return Wait(27f);
            Shot("04_zoom_x3.png");
            yield return Wait(27.5f);
            int m = math.max(1, sumSamples);
            if (maxSim <= 0.0) log.AppendLine("CẢNH BÁO: mô phỏng không chạy (sim = 0) — số liệu dưới đây không dùng được, xem file log của player.");
            log.AppendLine($"TỔNG KẾT (từ giây thứ 5): fps tb={sumFps / m:0.0} thấp nhất={minFps:0} | sim tb={sumSim / m:0.00}ms cao nhất={maxSim:0.00}ms | " +
                           $"frame tb={totalFrame / m:0.0}ms cao nhất={worstFrame:0.0}ms | lính tb={sumUnits / m:0} cao nhất={maxUnits} | " +
                           $"mũi tên cao nhất={maxArrows} | hạt cao nhất={maxParticles}");
            File.WriteAllText(Path.Combine(dir, "stats.txt"), log.ToString());
            Application.Quit();
        }

        IEnumerator Wait(float until)
        {
            while (Time.time < until) yield return null;
        }

        readonly FrameTiming[] timings = new FrameTiming[1];
        double sumGpu, sumMain, sumRender, sumFrame, maxFrame;
        int samples;

        int sumSamples, maxUnits, maxArrows, maxParticles;
        double sumFps, sumSim, totalFrame, worstFrame, sumUnits, maxSim;
        float minFps = float.MaxValue;

        void Update()
        {
            if (battle == null) return;
            FrameTimingManager.CaptureFrameTimings();
            if (FrameTimingManager.GetLatestTimings(1, timings) > 0)
            {
                sumGpu += timings[0].gpuFrameTime;
                sumMain += timings[0].cpuMainThreadFrameTime;
                sumRender += timings[0].cpuRenderThreadFrameTime;
            }
            double ft = Time.unscaledDeltaTime * 1000.0;
            sumFrame += ft;
            if (ft > maxFrame) maxFrame = ft;
            samples++;

            // Tổng kết cả lượt chạy, bỏ 5 giây đầu (Burst/shader còn đang khởi động)
            if (Time.time >= 5f)
            {
                sumSamples++;
                sumFps += battle.Fps; minFps = math.min(minFps, battle.Fps);
                sumSim += battle.SimMs; maxSim = math.max(maxSim, battle.SimMs);
                totalFrame += ft; worstFrame = math.max(worstFrame, ft);
                int units = battle.Counter(C.Units);
                sumUnits += units; maxUnits = math.max(maxUnits, units);
                maxArrows = math.max(maxArrows, battle.Counter(C.Arrows));
                maxParticles = math.max(maxParticles, battle.Counter(C.Particles));
            }

            if (Time.time < nextLog) return;
            nextLog += 1f;
            int n = Mathf.Max(1, samples);
            log.AppendLine($"t={Time.time:0}s fps={battle.Fps:0} frame={sumFrame / n:0.0}ms(max {maxFrame:0}) gpu={sumGpu / n:0.0}ms main={sumMain / n:0.0}ms render={sumRender / n:0.0}ms sim={battle.SimMs:0.00}ms " +
                           $"units={battle.Counter(C.Units)} (K {battle.Counter(C.Type0) + battle.Counter(C.Type0 + 1)} / U {battle.Counter(C.Type0 + 2) + battle.Counter(C.Type0 + 3)}) " +
                           $"arrows={battle.Counter(C.Arrows)} particles={battle.Counter(C.Particles)} " +
                           $"deadU={battle.Counter(C.DeadUndead)} deadK={battle.Counter(C.DeadKingdom)} breach={battle.Counter(C.Breach)}");
            sumGpu = sumMain = sumRender = sumFrame = maxFrame = 0;
            samples = 0;
        }

        void Shot(string name)
        {
            if (!Headless) ScreenCapture.CaptureScreenshot(Path.Combine(dir, name));
        }

        void SaveRT(RenderTexture rt, string name)
        {
            if (Headless) return;
            var prev = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGBA32, false);
            tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            tex.Apply();
            RenderTexture.active = prev;
            File.WriteAllBytes(Path.Combine(dir, name), tex.EncodeToPNG());
            Destroy(tex);
        }
    }
}
