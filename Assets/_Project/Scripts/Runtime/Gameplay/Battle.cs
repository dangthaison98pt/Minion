using System.Diagnostics;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using Debug = UnityEngine.Debug;
using Random = Unity.Mathematics.Random;

namespace HordeLab
{
    /// <summary>
    /// Chiến trường: giữ toàn bộ dữ liệu mô phỏng và vẽ nó. KHÔNG biết gì về thẻ bài, công trình hay đợt —
    /// phần đó nằm ở <see cref="Game"/>, gọi vào đây qua SpawnSquad / ResetField / onStep.
    /// Mỗi bước mô phỏng:
    ///   1. Xếp lính vào lưới            (BuildGridJob)
    ///   2. Suy nghĩ + di chuyển          (ThinkMoveJob, song song)
    ///   3. Áp đòn chém, tạo đạn, phép    (ResolveJob)
    ///   4. Đạn bay / chạm đất            (ArrowStepJob, ArrowLandJob)
    ///   5. Hạt hiệu ứng                  (ParticleStepJob, ParticleCompactJob)
    ///   6. Lính chết → in xác lên đất    (DeathJob)
    ///   7. Đóng gói dữ liệu cho GPU      (Pack*Job)
    ///   8. Upload + vài lệnh vẽ instanced
    /// </summary>
    public class Battle : MonoBehaviour
    {
        [Header("Hiệu năng")]
        [Tooltip("Giới hạn FPS bằng CPU thay cho VSync. GPU tích hợp (Intel) hạ xung khi dùng VSync nên hay tụt xuống 30-45 FPS.")]
        public int targetFps = 60;

        // ---- Điều khiển từ Game ----
        [System.NonSerialized] public bool simulate;       // false = đứng yên (đang chuẩn bị giữa các đợt)
        [System.NonSerialized] public bool paused;
        [System.NonSerialized] public int speed = 1;       // số bước mô phỏng mỗi frame (x1, x2, x4)
        /// <summary>Gọi ngay trước mỗi bước mô phỏng, lúc không có job nào đang chạy → được phép sinh quân.</summary>
        public System.Action<float> onStep;

        // ---- Đo hiệu năng (Tools/Benchmark.cs dùng) ----
        [System.NonSerialized] public uint fixedSeed;      // 0 = ngẫu nhiên theo đồng hồ
        [System.NonSerialized] public float fixedDt;       // 0 = dùng Time.deltaTime thật
        [System.NonSerialized] public bool profileJobs;    // true = chờ từng job xong để bấm giờ riêng (chậm hơn chút)
        public static readonly string[] StageNames =
            { "BuildGrid", "ThinkMove", "Resolve", "ArrowStep", "ArrowLand", "ParticleStep", "ParticleCompact", "Death", "Pack" };
        public readonly double[] stageMs = new double[9];  // cộng dồn theo từng stage
        public readonly System.Collections.Generic.List<float> frameSimMs = new System.Collections.Generic.List<float>();
        readonly Stopwatch stageSw = new Stopwatch();
        double frameAcc;

        JobHandle Mark(int stage, JobHandle h)
        {
            if (!profileJobs) return h;
            h.Complete();
            double ms = stageSw.Elapsed.TotalMilliseconds;
            stageMs[stage] += ms;
            frameAcc += ms;
            stageSw.Restart();
            return h;
        }

        // ---- Dữ liệu mô phỏng (toàn bộ là mảng struct) ----
        NativeArray<Unit> units;
        NativeArray<UnitStats> stats;
        NativeArray<Arrow> arrows;
        NativeArray<Particle> particles;
        NativeArray<float4> stamps;
        NativeArray<int> counters;
        NativeArray<int> cellStart, cursor, teamCount, cellOf, sortedIndex;
        NativeArray<float2> sortedPos;
        NativeArray<byte> sortedInfo, sortedType;
        UnitStats[] baseStats;                     // chỉ số gốc (đã đo liftPx) để bật / tắt lõi chuyên quân mỗi đợt
        NativeArray<int> attackTarget;
        NativeArray<float3> shots;
        NativeArray<float4> unitGpu, particleGpu;
        NativeArray<ArrowGpu> arrowGpu;

        // ---- Đồ hoạ ----
        GraphicsBuffer unitBuf, arrowBuf, particleBuf, stampBuf;
        Material groundMat, unitMat, arrowMat, shadowMat, particleMat, stampMat;
        Texture2D groundTex, unitAtlas;
        RenderTexture groundRT;
        CommandBuffer stampCmd;
        RenderParams rpGround, rpUnits, rpArrows, rpShadows, rpParticles;
        PixelView view;

        JobHandle simHandle;
        bool simScheduled;
        bool groundDirty = true;
        Random rng;
        uint frame;
        float elapsed;

        float simMs, fps = 60f, fpsTimer;
        int fpsFrames;
        readonly Stopwatch sw = new Stopwatch();

        public PixelView View => view;
        public float Elapsed => elapsed;
        public float SimMs => simMs;
        public float Fps => fps;
        public ref Random Rng => ref rng;
        public int Counter(int idx) => counters[idx];
        public int TypeCount(int type) => counters[C.Type0 + type];
        public RenderTexture GroundTexture => groundRT;
        public Texture2D GroundSource => groundTex;
        public Texture2D UnitAtlas => unitAtlas;

        void Awake()
        {
            Application.runInBackground = true;
            if (targetFps > 0)
            {
                QualitySettings.vSyncCount = 0;
                Application.targetFrameRate = targetFps;
            }
            foreach (var c in FindObjectsByType<Camera>(FindObjectsSortMode.None)) c.enabled = false;

            view = new PixelView();
            view.Create(transform);
            Allocate();
            CreateGraphics();
            ResetField();
        }

        // ------------------------------------------------------------------
        // Khởi tạo
        // ------------------------------------------------------------------
        void Allocate()
        {
            var p = Allocator.Persistent;
            units = new NativeArray<Unit>(Cfg.MaxUnits, p);
            stats = new NativeArray<UnitStats>(UnitType.Count, p);
            baseStats = new UnitStats[UnitType.Count];
            for (int t = 0; t < UnitType.Count; t++)
            {
                var st = GameData.Units[t].stats;
                st.liftPx = PixelArt.FootLiftPx(t);      // đo từ sprite (kể cả PNG hoạ sĩ vẽ) để chân chạm đất đúng vị trí
                stats[t] = baseStats[t] = st;
            }

            arrows = new NativeArray<Arrow>(Cfg.MaxArrows, p);
            particles = new NativeArray<Particle>(Cfg.MaxParticles, p);
            stamps = new NativeArray<float4>(Cfg.MaxStamps, p);
            counters = new NativeArray<int>(C.Length, p);

            cellStart = new NativeArray<int>(Cfg.Cells + 1, p);
            cursor = new NativeArray<int>(Cfg.Cells, p);
            teamCount = new NativeArray<int>(Cfg.Cells * 2, p);
            cellOf = new NativeArray<int>(Cfg.MaxUnits, p);
            sortedIndex = new NativeArray<int>(Cfg.MaxUnits, p);
            sortedPos = new NativeArray<float2>(Cfg.MaxUnits, p);
            sortedInfo = new NativeArray<byte>(Cfg.MaxUnits, p);
            sortedType = new NativeArray<byte>(Cfg.MaxUnits, p);
            attackTarget = new NativeArray<int>(Cfg.MaxUnits, p);
            shots = new NativeArray<float3>(Cfg.MaxUnits, p);

            unitGpu = new NativeArray<float4>(Cfg.MaxUnits, p);
            arrowGpu = new NativeArray<ArrowGpu>(Cfg.MaxArrows, p);
            particleGpu = new NativeArray<float4>(Cfg.MaxParticles, p);
        }

        static Material LoadMat(string name)
        {
            var shader = Resources.Load<Shader>("Shaders/" + name);
            if (shader == null) Debug.LogError("Không tìm thấy shader Resources/Shaders/" + name);
            return new Material(shader) { name = name };
        }

        void CreateGraphics()
        {
            unitAtlas = PixelArt.BuildUnitAtlas();
            var stampAtlas = PixelArt.BuildStampAtlas();
            var arrowTex = PixelArt.BuildArrowTexture();
            var palette = PixelArt.BuildPalette();
            groundTex = PixelArt.BuildGround(1234u);

            groundRT = new RenderTexture(Cfg.GroundW, Cfg.GroundH, 0, RenderTextureFormat.ARGB32)
            {
                name = "GroundRT",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            groundRT.Create();

            unitBuf = new GraphicsBuffer(GraphicsBuffer.Target.Structured, Cfg.MaxUnits, 16);
            arrowBuf = new GraphicsBuffer(GraphicsBuffer.Target.Structured, Cfg.MaxArrows, Marshal.SizeOf<ArrowGpu>());
            particleBuf = new GraphicsBuffer(GraphicsBuffer.Target.Structured, Cfg.MaxParticles, 16);
            stampBuf = new GraphicsBuffer(GraphicsBuffer.Target.Structured, Cfg.MaxStamps, 16);

            var field = new Vector4(-Cfg.HalfW, -Cfg.HalfH, Cfg.HalfW * 2f, Cfg.HalfH * 2f);

            groundMat = LoadMat("Ground");
            groundMat.mainTexture = groundRT;
            groundMat.SetVector("_Field", field);

            unitMat = LoadMat("Units");
            unitMat.mainTexture = unitAtlas;
            unitMat.SetBuffer("_Units", unitBuf);
            unitMat.SetVector("_Atlas", new Vector4(PixelArt.UnitCols, PixelArt.UnitRows, PixelArt.UnitCell / (float)Cfg.PPU, Cfg.PPU));

            var arrowSize = new Vector4(PixelArt.ProjW / (float)Cfg.PPU, PixelArt.ProjH / (float)Cfg.PPU, Cfg.PPU, Proj.Count);
            arrowMat = LoadMat("Arrows");
            arrowMat.mainTexture = arrowTex;
            arrowMat.SetBuffer("_Arrows", arrowBuf);
            arrowMat.SetVector("_ArrowSize", arrowSize);
            arrowMat.SetFloat("_Shadow", 0f);
            arrowMat.SetFloat("_SrcBlend", (float)BlendMode.One);
            arrowMat.SetFloat("_DstBlend", (float)BlendMode.Zero);
            arrowMat.SetFloat("_ZWrite", 1f);
            arrowMat.renderQueue = 2460;

            shadowMat = LoadMat("Arrows");
            shadowMat.mainTexture = arrowTex;
            shadowMat.SetBuffer("_Arrows", arrowBuf);
            shadowMat.SetVector("_ArrowSize", arrowSize);
            shadowMat.SetFloat("_Shadow", 1f);
            shadowMat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            shadowMat.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            shadowMat.SetFloat("_ZWrite", 0f);
            shadowMat.renderQueue = 2900;

            particleMat = LoadMat("Particles");
            particleMat.SetTexture("_Palette", palette);
            particleMat.SetBuffer("_Particles", particleBuf);
            particleMat.SetFloat("_PPU", Cfg.PPU);

            stampMat = LoadMat("Stamp");
            stampMat.mainTexture = stampAtlas;
            stampMat.SetBuffer("_Stamps", stampBuf);
            stampMat.SetVector("_Field", field);
            stampMat.SetVector("_StampAtlas", new Vector4(PixelArt.StampCols, PixelArt.StampRows, PixelArt.StampPx / (float)Cfg.PPU, Cfg.PPU));

            var bounds = new Bounds(Vector3.zero, new Vector3(1000f, 1000f, 1000f));
            RenderParams Rp(Material m) => new RenderParams(m)
            {
                camera = view.Cam,
                worldBounds = bounds,
                shadowCastingMode = ShadowCastingMode.Off,
                receiveShadows = false
            };
            rpGround = Rp(groundMat);
            rpUnits = Rp(unitMat);
            rpArrows = Rp(arrowMat);
            rpShadows = Rp(shadowMat);
            rpParticles = Rp(particleMat);

            stampCmd = new CommandBuffer { name = "Stamp corpses & blood" };
        }

        // ------------------------------------------------------------------
        // API cho Game
        // ------------------------------------------------------------------
        /// <summary>Dọn sạch chiến trường (lính, đạn, hạt, xác) để bắt đầu đợt mới.</summary>
        public void ResetField()
        {
            simHandle.Complete();
            for (int i = 0; i < C.Length; i++) counters[i] = 0;
            rng = new Random(fixedSeed != 0 ? fixedSeed : ((uint)System.Environment.TickCount | 1u));
            groundDirty = true;   // vẽ lại mặt đất sạch ở frame kế tiếp (lúc GPU sẵn sàng)
            elapsed = 0f;
            drawUnits = drawArrows = drawParticles = 0;
            simScheduled = false;   // nếu bị gọi sau khi frame này đã xếp lịch: bỏ kết quả cũ, đừng upload
        }

        public void SpawnUnit(int type, int team, float2 pos, float hpMul = 1f, float dmgMul = 1f, float regen = 0f)
        {
            int n = counters[C.Units];
            if (n >= Cfg.MaxUnits) return;
            var s = stats[type];
            float hp = s.maxHp * hpMul;
            units[n] = new Unit
            {
                pos = pos,
                goal = pos,
                hp = hp,
                maxHp = hp,
                cooldown = rng.NextFloat(0f, s.cooldown),
                animT = rng.NextFloat(0f, 1f),
                type = type,
                team = team,
                faceLeft = team,
                dmgMul = dmgMul,
                regen = regen
            };
            counters[C.Units] = n + 1;
            counters[C.Team0 + team]++;
            counters[C.Type0 + type]++;
        }

        /// <summary>Thêm 1 viên đạn của phe <paramref name="team"/> bay từ <paramref name="from"/> tới <paramref name="to"/> (lõi: thiên thạch, nỏ trên tường).</summary>
        public void FireProjectile(int team, float2 from, float2 to, float damage, float splash, int kind, float speed, float arc)
        {
            simHandle.Complete();
            int n = counters[C.Arrows];
            if (n >= arrows.Length) return;
            float dist = math.max(math.distance(from, to), 1f);
            arrows[n] = new Arrow { from = from, to = to, t = 0f, invDuration = speed / dist, arc = arc, damage = damage, splash = splash, team = team, kind = kind };
            counters[C.Arrows] = n + 1;
        }

        /// <summary>
        /// Chọn 1 lính địch của <paramref name="team"/>: <paramref name="closestToWall"/> = con gần tường thành nhất,
        /// không thì 1 con ngẫu nhiên. false nếu sân không có địch.
        /// </summary>
        public bool PickEnemy(int team, bool closestToWall, out float2 pos)
        {
            simHandle.Complete();
            pos = default;
            int n = counters[C.Units], seen = 0;
            float best = float.MaxValue;
            for (int i = 0; i < n; i++)
            {
                var u = units[i];
                if (u.team == team || u.hp <= 0f) continue;
                seen++;
                if (closestToWall) { if (u.pos.x < best) { best = u.pos.x; pos = u.pos; } }
                else if (rng.NextInt(0, seen) == 0) pos = u.pos;          // chọn ngẫu nhiên đều (reservoir)
            }
            return seen > 0;
        }

        /// <summary>
        /// Last Stand: sóng thánh dọc tường thành. Mọi quân địch cách tường dưới <paramref name="range"/> bị hất văng ra xa,
        /// choáng và mất máu (càng gần tường càng bị hất mạnh). Kèm cột sáng dọc tường.
        /// </summary>
        public void WallBlast(float range, float push, float stun, float damage)
        {
            simHandle.Complete();
            int n = counters[C.Units];
            for (int i = 0; i < n; i++)
            {
                var u = units[i];
                if (u.team != 1 || u.hp <= 0f) continue;
                float d = u.pos.x - Cfg.WallX;
                if (d > range) continue;
                u.pos.x += push * (1f - math.max(0f, d) / range) + 1f;
                u.hp -= damage;
                u.flash = 1f;
                u.cc = stun; u.ccKind = CC.Stun; u.ccImmune = 0f;
                units[i] = u;
            }
            // cột sáng + sóng dọc tường
            for (float y = -Cfg.HalfH + 1f; y < Cfg.HalfH; y += 0.7f)
                for (int k = 0; k < 3; k++)
                {
                    int m = counters[C.Particles];
                    if (m >= particles.Length) break;
                    particles[m] = new Particle
                    {
                        pos = new float2(Cfg.WallX + 0.6f, y + rng.NextFloat(-0.3f, 0.3f)),
                        vel = new float2(rng.NextFloat(8f, 22f), rng.NextFloat(-1f, 1f)),
                        h = rng.NextFloat(0.2f, 2.5f), vh = rng.NextFloat(2f, 8f), maxLife = 1.4f,
                        kind = k == 0 ? PKind.Holy : k == 1 ? PKind.Spark : PKind.Dust
                    };
                    counters[C.Particles] = m + 1;
                }
        }

        /// <summary>Công cụ kiểm tra: vị trí con đầu tiên còn sống của loại <paramref name="type"/>.</summary>
        public bool FindUnit(int type, out float2 pos)
        {
            simHandle.Complete();
            pos = default;
            int n = counters[C.Units];
            for (int i = 0; i < n; i++) if (units[i].type == type && units[i].hp > 0f) { pos = units[i].pos; return true; }
            return false;
        }

        /// <summary>Công cụ kiểm tra: khoảng cách chân gần nhất từ 1 lính loại <paramref name="type"/> tới lính bất kỳ khác; -1 nếu không có.</summary>
        public float ClosestTo(int type)
        {
            simHandle.Complete();
            int n = counters[C.Units];
            float best = float.MaxValue;
            for (int i = 0; i < n; i++)
            {
                if (units[i].type != type) continue;
                for (int j = 0; j < n; j++)
                    if (j != i) best = math.min(best, math.distance(units[i].pos, units[j].pos));
            }
            return best == float.MaxValue ? -1f : best;
        }

        /// <summary>Gọi 1 tiểu đội side x side lính xếp hàng quanh <paramref name="center"/>. Trả về số lính.</summary>
        /// <summary>
        /// Bật lõi chuyên quân cho những loại quân người chơi có lõi (đầu mỗi đợt). Chỉ quân phe người chơi mới có lõi,
        /// mà phe người chơi không bao giờ là phe địch / lính thuê, nên chỉnh chỉ số theo loại quân là an toàn.
        /// </summary>
        public void ApplyUnitAugments(System.Func<int, bool> has, bool fullMoon)
        {
            simHandle.Complete();
            for (int t = 0; t < UnitType.Count; t++)
            {
                var st = baseStats[t];
                bool on = has(t);
                st.augOn = on ? 1 : 0;
                if (on && t == UnitType.Swordsman) st.arrowResist += Aug.ShieldWallResist;            // Tường khiên
                if (on && t == UnitType.Shaman && fullMoon) st.flags |= UFlag.HealAura;              // Trăng máu
                stats[t] = st;
            }
        }

        public int SpawnSquad(int type, int team, float2 center, int side, float hpMul = 1f, float dmgMul = 1f, float regen = 0f, bool leader = false)
        {
            simHandle.Complete();
            float gap = math.max(0.9f, stats[type].radius * 1.9f);   // xếp hàng theo cỡ thân
            float half = (side - 1) * 0.5f;
            center.y = math.clamp(center.y, -Cfg.HalfH + 1f + half * gap, Cfg.HalfH - 1f - half * gap);
            for (int r = 0; r < side; r++)
                for (int c = 0; c < side; c++)
                {
                    SpawnUnit(type, team, center + new float2((c - half) * gap, (r - half) * gap) + rng.NextFloat2(-0.15f, 0.15f), hpMul, dmgMul, regen);
                    if (leader && r == side / 2 && c == side / 2 && counters[C.Units] > 0)
                    {
                        // trưởng đạo: con giữa đạo, +50% máu, viền vàng, dùng kỹ năng của lõi chuyên quân
                        int k = counters[C.Units] - 1;
                        var u = units[k];
                        u.leader = 1; u.hp *= 1.5f; u.maxHp *= 1.5f;
                        units[k] = u;
                    }
                }
            return side * side;
        }

        // ------------------------------------------------------------------
        // Vòng lặp
        // ------------------------------------------------------------------
        void Update()
        {
            view.Update();

            fpsFrames++;
            fpsTimer += Time.unscaledDeltaTime;
            if (fpsTimer >= 0.5f) { fps = fpsFrames / fpsTimer; fpsFrames = 0; fpsTimer = 0f; }
            simScheduled = false;
            if (!simulate || paused) return;

            sw.Restart();
            int steps = math.max(1, speed);
            for (int s = 0; s < steps; s++)
            {
                float dt = fixedDt > 0f ? fixedDt : math.min(Time.deltaTime, 1f / 30f);
                elapsed += dt;
                onStep?.Invoke(dt);
                ScheduleSimulation(dt);
                if (s < steps - 1) simHandle.Complete();   // tua nhanh: chạy xong bước này mới tới bước sau
            }
            simScheduled = true;
        }

        void ScheduleSimulation(float dt)
        {
            frame++;
            int nu = counters[C.Units];
            frameAcc = 0;
            stageSw.Restart();

            var h = new BuildGridJob
            {
                units = units, stats = stats, counters = counters, cellStart = cellStart, cursor = cursor, teamCount = teamCount,
                cellOf = cellOf, sortedIndex = sortedIndex, sortedPos = sortedPos, sortedInfo = sortedInfo, sortedType = sortedType
            }.Schedule();
            h = Mark(0, h);

            h = new ThinkMoveJob
            {
                units = units, stats = stats, counters = counters, cellStart = cellStart, teamCount = teamCount,
                sortedIndex = sortedIndex, sortedPos = sortedPos, sortedInfo = sortedInfo, sortedType = sortedType,
                attackTarget = attackTarget, shots = shots, dt = dt, frame = frame
            }.Schedule(nu, 64, h);
            h = Mark(1, h);

            h = new ResolveJob
            {
                units = units, stats = stats, attackTarget = attackTarget, shots = shots, cellStart = cellStart, sortedIndex = sortedIndex,
                arrows = arrows, particles = particles, counters = counters, seed = rng.NextUInt() | 1u, summonCount = GameData.SummonCount, dt = dt
            }.Schedule(h);
            h = Mark(2, h);

            h = new ArrowStepJob { arrows = arrows, counters = counters, dt = dt }.Schedule(Cfg.MaxArrows, 512, h);
            h = Mark(3, h);

            h = new ArrowLandJob
            {
                arrows = arrows, units = units, stats = stats, particles = particles, stamps = stamps, counters = counters,
                cellStart = cellStart, sortedIndex = sortedIndex, seed = rng.NextUInt() | 1u
            }.Schedule(h);
            h = Mark(4, h);

            h = new ParticleStepJob { particles = particles, counters = counters, dt = dt }.Schedule(Cfg.MaxParticles, 1024, h);
            h = Mark(5, h);
            h = new ParticleCompactJob { particles = particles, stamps = stamps, counters = counters, seed = rng.NextUInt() | 1u }.Schedule(h);
            h = Mark(6, h);
            h = new DeathJob
            {
                units = units, stats = stats, cellStart = cellStart, sortedIndex = sortedIndex,
                particles = particles, stamps = stamps, counters = counters, seed = rng.NextUInt() | 1u
            }.Schedule(h);
            h = Mark(7, h);

            var p1 = new PackUnitsJob { units = units, stats = stats, counters = counters, gpu = unitGpu }.Schedule(Cfg.MaxUnits, 256, h);
            var p2 = new PackArrowsJob { arrows = arrows, counters = counters, gpu = arrowGpu }.Schedule(Cfg.MaxArrows, 256, h);
            var p3 = new PackParticlesJob { particles = particles, counters = counters, gpu = particleGpu }.Schedule(Cfg.MaxParticles, 1024, h);
            simHandle = JobHandle.CombineDependencies(p1, p2, p3);
            if (profileJobs)
            {
                Mark(8, simHandle);
                frameSimMs.Add((float)frameAcc);
            }
            JobHandle.ScheduleBatchedJobs();
        }

        int drawUnits, drawArrows, drawParticles;   // số phần tử đã đóng gói lần gần nhất (vẫn vẽ được khi mô phỏng đứng yên)

        void LateUpdate()
        {
            if (simScheduled)
            {
                simHandle.Complete();
                sw.Stop();
                simMs = math.lerp(simMs, (float)sw.Elapsed.TotalMilliseconds, 0.1f);
                drawUnits = counters[C.Units];
                drawArrows = counters[C.Arrows];
                drawParticles = counters[C.Particles];
                unitBuf.SetData(unitGpu, 0, 0, drawUnits);
                arrowBuf.SetData(arrowGpu, 0, 0, drawArrows);
                particleBuf.SetData(particleGpu, 0, 0, drawParticles);
            }
            StampAndDraw();
        }

        void StampAndDraw()
        {
            int ns = counters[C.Stamps];

            // Mặt đất sạch (cỏ, thành). Làm lại nếu driver đánh mất nội dung RenderTexture.
            stampCmd.Clear();
            if (groundDirty || !groundRT.IsCreated())
            {
                if (!groundRT.IsCreated()) groundRT.Create();
                stampCmd.Blit(groundTex, groundRT);
                groundDirty = false;
            }

            // In xác/máu/vết cháy của frame này vào texture mặt đất: 1 lệnh vẽ cho tất cả.
            if (ns > 0)
            {
                stampBuf.SetData(stamps, 0, 0, ns);
                stampCmd.SetRenderTarget(groundRT);
                stampCmd.DrawProcedural(Matrix4x4.identity, stampMat, 0, MeshTopology.Triangles, 6, ns);
                counters[C.Stamps] = 0;
            }
            if (stampCmd.sizeInBytes > 0) Graphics.ExecuteCommandBuffer(stampCmd);

            Graphics.RenderPrimitives(rpGround, MeshTopology.Triangles, 6, 1);
            if (drawUnits > 0) Graphics.RenderPrimitives(rpUnits, MeshTopology.Triangles, 6, drawUnits);   // 1 lệnh vẽ cho mọi lính
            if (drawArrows > 0)
            {
                Graphics.RenderPrimitives(rpShadows, MeshTopology.Triangles, 6, drawArrows);
                Graphics.RenderPrimitives(rpArrows, MeshTopology.Triangles, 6, drawArrows);
            }
            if (drawParticles > 0) Graphics.RenderPrimitives(rpParticles, MeshTopology.Triangles, 6, drawParticles);
        }

        // ------------------------------------------------------------------
        void OnDestroy()
        {
            simHandle.Complete();
            void D<T>(ref NativeArray<T> a) where T : struct { if (a.IsCreated) a.Dispose(); }
            D(ref units); D(ref stats); D(ref arrows); D(ref particles); D(ref stamps); D(ref counters);
            D(ref cellStart); D(ref cursor); D(ref teamCount); D(ref cellOf); D(ref sortedIndex);
            D(ref sortedPos); D(ref sortedInfo); D(ref sortedType); D(ref attackTarget); D(ref shots);
            D(ref unitGpu); D(ref arrowGpu); D(ref particleGpu);

            unitBuf?.Release(); arrowBuf?.Release(); particleBuf?.Release(); stampBuf?.Release();
            stampCmd?.Release();
            if (groundRT != null) groundRT.Release();
            view?.Dispose();
        }
    }
}
