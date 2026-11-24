using System.Collections;
using System.IO;
using System.Text;
using Unity.Mathematics;
using UnityEngine;

namespace HordeLab
{
    /// <summary>
    /// Công cụ kiểm tra: chạy bản build với tham số  -autoshot "thư mục"  thì một con bot tự chơi (chọn phe, đặt thẻ,
    /// mua đồ, bấm bắt đầu đợt), chụp vài tấm màn hình, ghi kết quả từng đợt + số liệu hiệu năng rồi tự thoát.
    ///   -faction 0..4   phe bot chọn (mặc định 0 = Người)
    ///   -waves N        chơi tối đa N đợt (mặc định 5)
    ///   -stress 20000   bỏ qua thẻ bài, dồn khoảng 20.000 lính đánh nhau để đo hiệu năng
    ///   -batchmode      chạy ngầm, không chụp màn hình, thời gian game chạy nhanh hết mức
    /// Chơi bình thường thì công cụ này không làm gì cả.
    /// </summary>
    public class AutoShot : MonoBehaviour
    {
        string dir;
        bool saveTest;
        int stress, botFaction, maxWaves = 5;
        static bool Headless => Application.isBatchMode;   // chạy ngầm: không có cửa sổ để chụp
        Battle battle;
        Game game;
        readonly StringBuilder log = new StringBuilder();
        float nextLog = 1f;

        static int IntArg(string[] args, string name, int fallback)
        {
            for (int i = 0; i < args.Length - 1; i++)
                if (args[i] == name && int.TryParse(args[i + 1], out int v)) return v;
            return fallback;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Init()
        {
            var args = System.Environment.GetCommandLineArgs();
            if (System.Array.IndexOf(args, "-novsync") >= 0)
            {
                QualitySettings.vSyncCount = 0;
                Application.targetFrameRate = -1;
            }
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] != "-autoshot") continue;
                var shot = new GameObject("AutoShot").AddComponent<AutoShot>();
                shot.dir = args[i + 1];
                shot.stress = IntArg(args, "-stress", 0);
                shot.botFaction = math.clamp(IntArg(args, "-faction", 0), 0, Faction.Count - 1);
                shot.maxWaves = IntArg(args, "-waves", 5);
                Directory.CreateDirectory(shot.dir);
            }
        }

        IEnumerator Start()
        {
            while (battle == null || game == null)
            {
                battle = FindAnyObjectByType<Battle>();
                game = Game.I;
                yield return null;
            }
            // Bot không được ghi đè lượt chơi thật của người chơi: mặc định tắt lưu; -savetest thì lưu vào một file riêng
            saveTest = System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-savetest") >= 0;
            Game.saveFile = "run_autotest.json";
            game.savingEnabled = saveTest;
            if (Headless)
            {
                log.AppendLine("Chạy ngầm (-batchmode): không chụp màn hình, không đo GPU — xuất atlas sprite ra PNG thay thế");
                PixelArt.SaveAtlases(dir);
                QualitySettings.vSyncCount = 0;
                Application.targetFrameRate = -1;
                battle.fixedDt = 1f / 30f;
            }
            log.AppendLine($"screen {Screen.width}x{Screen.height}, pixel scale {battle.View.Scale}, gfx {SystemInfo.graphicsDeviceType} ({SystemInfo.graphicsDeviceName}), " +
                           $"cpu cores {SystemInfo.processorCount}, vsync {QualitySettings.vSyncCount}, burst {Unity.Burst.BurstCompiler.IsEnabled}");

            if (stress > 0) yield return StressRun();
            else yield return BotRun();

            int m = math.max(1, sumSamples);
            log.AppendLine($"TỔNG KẾT HIỆU NĂNG: fps tb={sumFps / m:0.0} thấp nhất={minFps:0} | sim tb={sumSim / m:0.00}ms cao nhất={maxSim:0.00}ms | " +
                           $"frame tb={totalFrame / m:0.0}ms cao nhất={worstFrame:0.0}ms | lính tb={sumUnits / m:0} cao nhất={maxUnits} | " +
                           $"đạn cao nhất={maxArrows} | hạt cao nhất={maxParticles}");
            File.WriteAllText(Path.Combine(dir, "stats.txt"), log.ToString());
            Application.Quit();
        }

        IEnumerator StressRun()
        {
            game.StartSandbox(stress);
            log.AppendLine($"STRESS TEST: mục tiêu {stress} lính, MaxUnits {Cfg.MaxUnits}");
            float t0 = Time.time;
            yield return Wait(t0 + 20f);
            Shot("01_stress.png");
            battle.View.Scale = 1;
            battle.View.SetScale(battle.View.MinScale + 2, new Vector2(Screen.width * 0.5f, Screen.height * 0.5f));
            battle.View.Center = new float2(Cfg.HoldX, 0f);
            yield return Wait(t0 + 40f);
            Shot("02_stress_zoom_x3.png");
            yield return Wait(t0 + 45f);
            if (maxSim <= 0.0) log.AppendLine("CẢNH BÁO: mô phỏng không chạy (sim = 0) — số liệu không dùng được, xem file log của player.");
        }

        /// <summary>
        /// Kiểm tra thẻ có sao (chạy cùng -savetest): kéo thẻ đè lên nhà 3 sao thì đổi, nhà cũ về tay vẫn 3 sao;
        /// đổi ngược lại; lấy nhà về tay giữ sao rồi đặt lại. Không được mất nhà nào.
        /// </summary>
        void SwapTest()
        {
            Vector2Int p = default; bool found = false;
            foreach (var kv in game.city.cells) if (kv.Value != null && kv.Value.def != B.Cabin) { p = kv.Key; found = true; break; }
            if (!found) { log.AppendLine("SWAP TEST: bỏ qua (lưới chưa có nhà)"); return; }

            var b = game.city.Get(p);
            int def = b.def, crystals0 = game.crystals, buildings0 = game.city.cells.Count - game.city.EmptyCount();
            b.stars = 3;                                                      // giả lập nhà đã gộp lên 3 sao
            game.hand.Add(B.Cabin); game.handStars.Add(1);
            int i = game.hand.Count - 1;

            bool ok1 = game.PlayCard(i, p);                                   // Cabin 1 sao đè lên nhà 3 sao
            var now = game.city.Get(p);
            bool swapped = ok1 && now.def == B.Cabin && now.stars == 1 && game.hand[i] == def && game.HandStars(i) == 3;

            bool ok2 = game.PlayCard(i, p);                                   // nhà 3 sao (giờ là thẻ) đè lại lên Cabin
            now = game.city.Get(p);
            bool back = ok2 && now.def == def && now.stars == 3 && game.hand[i] == B.Cabin && game.HandStars(i) == 1;

            bool took = game.TakeBack(p);                                     // lấy nhà 3 sao về tay rồi đặt lại
            int j = game.hand.Count - 1;
            bool kept = took && game.hand[j] == def && game.HandStars(j) == 3 && game.PlayCard(j, p) && game.city.Get(p).stars == 3;

            game.hand.RemoveAt(i); game.handStars.RemoveAt(i);                // bỏ thẻ Cabin thêm vào để thử
            game.city.Get(p).stars = 1;
            game.city.Recompute();
            bool intact = game.city.cells.Count - game.city.EmptyCount() == buildings0 && game.crystals == crystals0;
            log.AppendLine($"SWAP TEST: {(swapped && back && kept && intact ? "OK" : "FAIL")} (đổi={swapped}, đổi ngược={back}, về tay giữ sao={kept}, không mất nhà={intact})");
        }

        IEnumerator BotRun()
        {
            log.AppendLine($"BOT CHƠI THỬ: phe {GameData.Factions[botFaction].name}, tối đa {maxWaves} đợt");
            for (int i = 0; i < 30; i++) yield return null;
            Shot("00_menu.png");
            yield return Frames(3);
            game.BotAct(botFaction);              // menu → màn chọn phe
            yield return Frames(5);
            Shot("01_chon_phe.png");
            yield return Frames(3);

            game.BotAct(botFaction);              // chọn phe
            yield return Frames(5);
            game.ToggleLock(1);                    // khoá thử 1 ô cửa hàng để thấy trong ảnh
            yield return Frames(3);
            Shot("02_chuan_bi_tay_day_the.png");
            yield return Frames(3);

            int played = 0;
            bool zoomed = false;
            while (played < maxWaves && game.phase != Phase.Lost && game.phase != Phase.Won)
            {
                if (game.phase == Phase.Reward)
                {
                    if (played == 1 && !Headless)
                    {
                        yield return new WaitForSecondsRealtime(0.45f); Shot("05a_san_dang_toi.png");      // giữa lúc dọn sân
                        yield return new WaitForSecondsRealtime(1.2f); Shot("05_phan_thuong.png"); yield return Frames(3);
                    }
                    game.BotAct(botFaction);
                    yield return null;
                    continue;
                }

                // Pha chuẩn bị: đặt thẻ, mua đồ, rồi bắt đầu đợt
                int wave = game.wave;
                game.BotPlaceAndShop();
                if (played == 0)
                {
                    yield return Frames(5); Shot("03_chuan_bi_da_xay.png"); yield return Frames(3);
                    game.ui.fakeMouse = new Vector2(164f, 270f);          // rê vào ô đầu tiên của lưới để chụp tooltip
                    yield return Frames(5); Shot("03b_tooltip.png"); yield return Frames(3);
                    game.ui.fakeMouse = null;
                }
                if (played == 0 && saveTest)
                {
                    SwapTest();
                    // Cầm 1 thẻ 2 sao trên tay để thử lưu cả số sao của thẻ
                    Vector2Int held = default; bool holding = false;
                    foreach (var kv in game.city.cells)
                        if (kv.Value != null) { held = kv.Key; kv.Value.stars = 2; holding = game.TakeBack(held); break; }
                    // Kiểm tra lưu / nạp: lưu → ra menu → Continue → so lại trạng thái
                    game.SaveRun();
                    int w0 = game.wave, h0 = game.hand.Count, c0 = game.city.cells.Count, e0 = game.city.EmptyCount(), k0 = game.crystals, f0 = game.faction;
                    string s0 = string.Join(",", game.handStars);
                    float gold0 = game.city.res.gold;
                    game.QuitToMenu();
                    yield return Frames(40);
                    Shot("03c_menu_co_continue.png");
                    yield return Frames(3);
                    bool loaded = game.ContinueRun();
                    yield return Frames(5);
                    bool same = loaded && game.phase == Phase.Prep && game.wave == w0 && game.hand.Count == h0 && game.city.cells.Count == c0
                                && game.city.EmptyCount() == e0 && game.crystals == k0 && game.faction == f0 && Mathf.Approximately(game.city.res.gold, gold0)
                                && string.Join(",", game.handStars) == s0;
                    log.AppendLine($"SAVE TEST: {(same ? "OK" : "FAIL")} (loaded={loaded}, phase={game.phase}, wave {w0}->{game.wave}, hand {h0}->{game.hand.Count}, cells {c0}->{game.city.cells.Count}, empty {e0}->{game.city.EmptyCount()}, sao trên tay [{s0}]->[{string.Join(",", game.handStars)}])");
                    if (holding && game.hand.Count > 0)                                   // đặt lại nhà vừa cầm, trả về 1 sao như cũ
                    {
                        int last = game.hand.Count - 1;
                        if (game.PlayCard(last, held)) { game.city.Get(held).stars = 1; game.city.Recompute(); }
                    }
                }
                log.AppendLine($"--- Đợt {wave}: địch {GameData.Factions[game.attacker].name}, {game.EnemyPreviewCount()} quân | lưới {game.city.cells.Count} ô ({game.city.EmptyCount()} trống), tay {game.hand.Count} thẻ, tinh thể {game.crystals}");
                game.StartWave();
                if (played > 0 || Headless) battle.speed = 4;

                float shotAt = Time.time + 22f;
                bool shot = played > 0;
                while (game.phase == Phase.Battle)
                {
                    if (!shot && Time.time >= shotAt)
                    {
                        Shot("04_tran_danh.png");
                        shot = true;
                        if (!zoomed)
                        {
                            zoomed = true;
                            yield return Frames(3);
                            battle.View.SetScale(battle.View.MinScale + 2, new Vector2(Screen.width * 0.5f, Screen.height * 0.5f));
                            battle.View.Center = new float2(Cfg.HoldX, 0f);
                            yield return Frames(20);
                            Shot("04b_tran_danh_zoom_x3.png");
                            yield return Frames(3);
                            battle.View.SetScale(battle.View.MinScale, new Vector2(Screen.width * 0.5f, Screen.height * 0.5f));
                            battle.View.Center = 0f;
                            battle.speed = 4;
                        }
                    }
                    yield return null;
                }
                played++;
                var r = game.city.res;
                log.AppendLine($"    → {(game.lastWon ? "THẮNG" : "THUA")} sau {game.waveTime:0}s | thành {game.castleHp}/{Tuning.CastleHp} | diệt {battle.Counter(C.Dead1)} / mất {battle.Counter(C.Dead0)} | " +
                               $"kho: {r.pop:0} dân, {r.food:0} lương, {r.wood:0} gỗ, {r.gold:0} vàng | tim {game.hearts}" +
                               (string.IsNullOrEmpty(game.eventNote) ? "" : $" | sự kiện: {game.eventNote}"));
            }
            log.AppendLine($"KẾT THÚC: pha {game.phase}, đã chơi {played} đợt, còn {game.hearts} tim");
            if (saveTest) { game.savingEnabled = true; game.DeleteSave(); }
            yield return Frames(5);
            Shot("06_ket_thuc.png");
            yield return Frames(3);
        }

        static IEnumerator Frames(int n)
        {
            for (int i = 0; i < n; i++) yield return null;
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
            if (battle == null || game == null) return;
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

            // Tổng kết: chỉ tính lúc đang đánh, bỏ 300 frame đầu (Burst/shader còn đang khởi động)
            bool fighting = game.phase == Phase.Battle || game.phase == Phase.Sandbox;
            if (fighting && Time.frameCount > 300)
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
            nextLog += stress > 0 ? 1f : 5f;
            if (!fighting) { sumGpu = sumMain = sumRender = sumFrame = maxFrame = 0; samples = 0; return; }
            int n = Mathf.Max(1, samples);
            log.AppendLine($"    t={Time.time:0}s fps={battle.Fps:0} frame={sumFrame / n:0.0}ms(max {maxFrame:0}) gpu={sumGpu / n:0.0}ms main={sumMain / n:0.0}ms render={sumRender / n:0.0}ms sim={battle.SimMs:0.00}ms x{battle.speed} " +
                           $"lính={battle.Counter(C.Units)} (ta {battle.Counter(C.Team0)} / địch {battle.Counter(C.Team1)}) đạn={battle.Counter(C.Arrows)} hạt={battle.Counter(C.Particles)}");
            sumGpu = sumMain = sumRender = sumFrame = maxFrame = 0;
            samples = 0;
        }

        void Shot(string name)
        {
            if (!Headless) ScreenCapture.CaptureScreenshot(Path.Combine(dir, name));
        }
    }
}
