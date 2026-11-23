using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace HordeLab
{
    /// <summary>
    /// Đo chi phí mô phỏng có kiểm soát. Chạy bản build:
    ///     HordeLab.exe -batchmode -bench "ketqua.txt" [-stress 20000]
    ///  - Seed và dt cố định → mọi lượt chạy đúng một kịch bản, so sánh được giữa các lần sửa code.
    ///  - Bấm giờ riêng từng job (Battle.profileJobs).
    ///  - Chạy vài lượt, báo số TỐT NHẤT giữa các lượt để lọc nhiễu do máy đang làm việc khác.
    /// Lưu ý: số này là thời gian TÍNH TOÁN của job. Chỉ số "Mô phỏng" trên HUD đo từ lúc xếp lịch đến lúc
    /// Complete() nên gồm cả thời gian chờ luồng worker — hai số khác nhau là bình thường.
    /// </summary>
    public class Benchmark : MonoBehaviour
    {
        const int Rounds = 3;
        const int WarmupFrames = 60;     // 1 giây game: Burst/bộ nhớ ổn định
        const int MeasureFrames = 1200;  // 20 giây game (dt cố định 1/60)
        const uint Seed = 12345u;

        string outPath;
        int stress;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Init()
        {
            var args = System.Environment.GetCommandLineArgs();
            int i = System.Array.IndexOf(args, "-bench");
            if (i < 0 || i + 1 >= args.Length) return;
            var b = new GameObject("Benchmark").AddComponent<Benchmark>();
            b.outPath = args[i + 1];
            int s = System.Array.IndexOf(args, "-stress");
            if (s >= 0 && s + 1 < args.Length) int.TryParse(args[s + 1], out b.stress);
        }

        class Result
        {
            public double[] stageAvg;
            public float median, p95, max, mean;
            public int unitsStart, unitsEnd;
        }

        IEnumerator Start()
        {
            Battle battle = null;
            while (battle == null) { battle = FindAnyObjectByType<Battle>(); yield return null; }

            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = -1;   // chạy nhanh hết mức, thời gian game vẫn cố định nhờ fixedDt
            battle.fixedSeed = Seed;
            battle.fixedDt = 1f / 60f;
            battle.profileJobs = true;

            var results = new List<Result>();
            for (int r = 0; r < Rounds; r++)
            {
                if (stress > 0) AutoShot.ApplyStress(battle, stress);
                battle.castleMaxHp = 1000000;   // không bao giờ thua giữa lượt đo (thua là mô phỏng dừng)
                battle.Restart();

                for (int f = 0; f < WarmupFrames; f++) yield return null;
                int unitsStart = battle.Counter(C.Units);
                System.Array.Clear(battle.stageMs, 0, battle.stageMs.Length);
                battle.frameSimMs.Clear();
                while (battle.frameSimMs.Count < MeasureFrames) yield return null;

                int n = battle.frameSimMs.Count;
                var frames = battle.frameSimMs.OrderBy(x => x).ToArray();
                results.Add(new Result
                {
                    stageAvg = battle.stageMs.Select(x => x / n).ToArray(),
                    median = frames[n / 2],
                    p95 = frames[(int)(n * 0.95f)],
                    max = frames[n - 1],
                    mean = frames.Average(),
                    unitsStart = unitsStart,
                    unitsEnd = battle.Counter(C.Units)
                });
            }

            File.WriteAllText(outPath, Report(results));
            Application.Quit();
        }

        string Report(List<Result> results)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"BENCHMARK  stress={(stress > 0 ? stress.ToString() : "mặc định")}  seed={Seed}  dt=1/60  " +
                          $"{Rounds} lượt x {MeasureFrames} frame (sau {WarmupFrames} frame khởi động)");
            sb.AppendLine($"cpu: {SystemInfo.processorType} ({SystemInfo.processorCount} luồng), burst {Unity.Burst.BurstCompiler.IsEnabled}");
            sb.AppendLine();

            sb.AppendLine("Từng lượt (ms mô phỏng mỗi frame):");
            foreach (var r in results)
                sb.AppendLine($"  tb={r.mean:0.00}  trung vị={r.median:0.00}  p95={r.p95:0.00}  max={r.max:0.00}  lính {r.unitsStart}→{r.unitsEnd}");
            sb.AppendLine();

            sb.AppendLine($"TỐT NHẤT: trung vị {results.Min(x => x.median):0.00} ms, p95 {results.Min(x => x.p95):0.00} ms, tb {results.Min(x => x.mean):0.00} ms");
            for (int s = 0; s < Battle.StageNames.Length; s++)
                sb.AppendLine($"     {Battle.StageNames[s],-16} {results.Min(x => x.stageAvg[s]),6:0.000} ms");
            return sb.ToString();
        }
    }
}
