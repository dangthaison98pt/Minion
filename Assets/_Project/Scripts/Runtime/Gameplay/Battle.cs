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
    /// Bộ não của trận đánh. Mỗi frame:
    ///   1. Xếp lính vào lưới            (BuildGridJob)
    ///   2. Suy nghĩ + di chuyển          (ThinkMoveJob, song song)
    ///   3. Áp đòn chém, tạo mũi tên      (ResolveJob)
    ///   4. Mũi tên bay / chạm đất        (ArrowStepJob, ArrowLandJob)
    ///   5. Hạt hiệu ứng                  (ParticleStepJob, ParticleCompactJob)
    ///   6. Lính chết → in xác lên đất    (DeathJob)
    ///   7. Đóng gói dữ liệu cho GPU      (Pack*Job)
    ///   8. Upload + vài lệnh vẽ instanced
    /// </summary>
    public class Battle : MonoBehaviour
    {
        [Header("Quân số ban đầu")]
        public int startFootmen = 2200;
        public int startArchers = 1000;
        public int startZombies = 3800;
        public int startSkeletons = 700;

        [Header("Viện binh")]
        public int kingdomCap = 6500;
        public int undeadCap = 8500;
        public float kingdomRate = 55f;
        public float undeadRate = 60f;
        public float undeadRateGrowth = 0.6f;   // quái đến nhanh dần theo thời gian

        [Header("Tường thành")]
        public int castleMaxHp = 250;

        [Header("Hiệu năng")]
        [Tooltip("Giới hạn FPS bằng CPU thay cho VSync. GPU tích hợp (Intel) hạ xung khi dùng VSync nên hay tụt xuống 30-45 FPS.")]
        public int targetFps = 60;

        // ---- Dữ liệu mô phỏng (toàn bộ là mảng struct) ----
        NativeArray<Unit> units;
        NativeArray<UnitStats> stats;
        NativeArray<Arrow> arrows;
        NativeArray<Particle> particles;
        NativeArray<float4> stamps;
        NativeArray<int> counters;
        NativeArray<int> cellStart, cursor, teamCount, cellOf, sortedIndex;
        NativeArray<float2> sortedPos;
        NativeArray<byte> sortedTeam;
        NativeArray<int> attackTarget;
        NativeArray<float3> shots;
        NativeArray<float4> unitGpu, particleGpu;
        NativeArray<ArrowGpu> arrowGpu;

        // ---- Đồ hoạ ----
        GraphicsBuffer unitBuf, arrowBuf, particleBuf, stampBuf;
        Material groundMat, unitMat, arrowMat, shadowMat, particleMat, stampMat;
        Texture2D groundTex;
        RenderTexture groundRT;
        CommandBuffer stampCmd;
        RenderParams rpGround, rpUnits, rpArrows, rpShadows, rpParticles;
        PixelView view;

        JobHandle simHandle;
        bool simScheduled;
        bool groundDirty = true;
        Random rng;
        uint frame;
        float kingdomAcc, undeadAcc, elapsed, volleyCd, fireCd;
        bool paused, defeated;
        int castleHp;

        float simMs, fps = 60f, fpsTimer;
        int fpsFrames;
        readonly Stopwatch sw = new Stopwatch();
        GUIStyle label, small, big;
        Rect hudRect = new Rect(10, 10, 470, 214);

        public PixelView View => view;
        public float Elapsed => elapsed;
        public float SimMs => simMs;
        public float Fps => fps;
        public int Counter(int idx) => counters[idx];
        public RenderTexture GroundTexture => groundRT;
        public Texture2D GroundSource => groundTex;

        // Bấm Play ở scene trống cũng chạy được
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void AutoCreate()
        {
            if (FindAnyObjectByType<Battle>() == null) new GameObject("Battle").AddComponent<Battle>();
        }

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
            Restart();
        }

        // ------------------------------------------------------------------
        // Khởi tạo
        // ------------------------------------------------------------------
        void Allocate()
        {
            var p = Allocator.Persistent;
            units = new NativeArray<Unit>(Cfg.MaxUnits, p);
            stats = new NativeArray<UnitStats>(4, p);
            stats[UnitType.Footman] = new UnitStats { maxHp = 40f, damage = 7f, cooldown = 0.9f, range = 1.0f, speed = 3.0f };
            stats[UnitType.Archer] = new UnitStats { maxHp = 18f, damage = 6f, cooldown = 1.7f, range = 24f, speed = 2.8f, ranged = 1 };
            stats[UnitType.Zombie] = new UnitStats { maxHp = 32f, damage = 6f, cooldown = 1.1f, range = 1.0f, speed = 2.2f };
            stats[UnitType.Skeleton] = new UnitStats { maxHp = 14f, damage = 5f, cooldown = 2.2f, range = 20f, speed = 2.5f, ranged = 1 };

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
            sortedTeam = new NativeArray<byte>(Cfg.MaxUnits, p);
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
            var unitAtlas = PixelArt.BuildUnitAtlas();
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
            unitMat.SetVector("_Atlas", new Vector4(PixelArt.UnitCols, PixelArt.UnitRows, 1f, Cfg.PPU));

            arrowMat = LoadMat("Arrows");
            arrowMat.mainTexture = arrowTex;
            arrowMat.SetBuffer("_Arrows", arrowBuf);
            arrowMat.SetVector("_ArrowSize", new Vector4(1.2f, 0.3f, Cfg.PPU, 0f));
            arrowMat.SetFloat("_Shadow", 0f);
            arrowMat.SetFloat("_SrcBlend", (float)BlendMode.One);
            arrowMat.SetFloat("_DstBlend", (float)BlendMode.Zero);
            arrowMat.SetFloat("_ZWrite", 1f);
            arrowMat.renderQueue = 2460;

            shadowMat = LoadMat("Arrows");
            shadowMat.mainTexture = arrowTex;
            shadowMat.SetBuffer("_Arrows", arrowBuf);
            shadowMat.SetVector("_ArrowSize", new Vector4(1.2f, 0.3f, Cfg.PPU, 0f));
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

        public void Restart()
        {
            simHandle.Complete();
            for (int i = 0; i < C.Length; i++) counters[i] = 0;
            rng = new Random((uint)System.Environment.TickCount | 1u);
            groundDirty = true;   // vẽ lại mặt đất sạch ở frame kế tiếp (lúc GPU sẵn sàng)

            elapsed = 0f;
            kingdomAcc = undeadAcc = 0f;
            volleyCd = fireCd = 0f;
            castleHp = castleMaxHp;
            paused = defeated = false;

            SpawnBlock(UnitType.Footman, startFootmen, new float2(-58f, -50f), new float2(-30f, 50f));
            SpawnBlock(UnitType.Archer, startArchers, new float2(-72f, -50f), new float2(-62f, 50f));
            SpawnBlock(UnitType.Zombie, startZombies, new float2(28f, -51f), new float2(72f, 51f));
            SpawnBlock(UnitType.Skeleton, startSkeletons, new float2(74f, -50f), new float2(84f, 50f));
        }

        void SpawnUnit(int type, float2 pos)
        {
            int n = counters[C.Units];
            if (n >= Cfg.MaxUnits) return;
            var s = stats[type];
            units[n] = new Unit
            {
                pos = pos,
                goal = pos,
                hp = s.maxHp,
                cooldown = rng.NextFloat(0f, s.cooldown),
                animT = rng.NextFloat(0f, 1f),
                type = type,
                faceLeft = type >= 2 ? 1 : 0
            };
            counters[C.Units] = n + 1;
        }

        void SpawnBlock(int type, int count, float2 min, float2 max)
        {
            for (int i = 0; i < count; i++) SpawnUnit(type, rng.NextFloat2(min, max));
        }

        void Reinforce(float dt)
        {
            int kingdom = counters[C.Type0 + UnitType.Footman] + counters[C.Type0 + UnitType.Archer];
            int undead = counters[C.Type0 + UnitType.Zombie] + counters[C.Type0 + UnitType.Skeleton];
            kingdomAcc += kingdomRate * dt;
            undeadAcc += (undeadRate + undeadRateGrowth * elapsed) * dt;

            // Viện binh đến theo từng tiểu đội 5x5 cho giống đội hình hành quân
            while (kingdomAcc >= SquadSize)
            {
                kingdomAcc -= SquadSize;
                if (kingdom >= kingdomCap) continue;
                bool archers = rng.NextFloat() < 0.33f;
                kingdom += SpawnSquad(archers ? UnitType.Archer : UnitType.Footman, archers ? -88f : -85f);
            }
            while (undeadAcc >= SquadSize)
            {
                undeadAcc -= SquadSize;
                if (undead >= undeadCap) continue;
                bool skeletons = rng.NextFloat() < 0.18f;
                undead += SpawnSquad(skeletons ? UnitType.Skeleton : UnitType.Zombie, skeletons ? 91f : 88f);
            }
        }

        const int SquadSide = 5, SquadSize = SquadSide * SquadSide;

        int SpawnSquad(int type, float x)
        {
            float y = rng.NextFloat(-49f, 49f);
            for (int r = 0; r < SquadSide; r++)
                for (int c = 0; c < SquadSide; c++)
                    SpawnUnit(type, new float2(x + (c - 2) * 0.9f, y + (r - 2) * 0.9f) + rng.NextFloat2(-0.15f, 0.15f));
            return SquadSize;
        }

        // ------------------------------------------------------------------
        // Kỹ năng của người chơi
        // ------------------------------------------------------------------
        /// <summary>Mưa tên: 400 mũi tên bắn từ tường thành xuống chỗ con trỏ.</summary>
        public void CastVolley(float2 at)
        {
            for (int k = 0; k < 400; k++)
            {
                int n = counters[C.Arrows];
                if (n >= Cfg.MaxArrows) break;
                float2 from = new float2(rng.NextFloat(-95f, -91f), math.clamp(at.y + rng.NextFloat(-30f, 30f), -53f, 53f));
                float2 to = at + rng.NextFloat2Direction() * math.sqrt(rng.NextFloat()) * 8f;
                to = math.clamp(to, new float2(-Cfg.HalfW, -Cfg.HalfH), new float2(Cfg.HalfW, Cfg.HalfH));
                arrows[n] = new Arrow
                {
                    from = from,
                    to = to,
                    t = -rng.NextFloat(0f, 0.25f),          // bắn so le chứ không cùng lúc
                    invDuration = 1f / rng.NextFloat(1.3f, 1.9f),
                    arc = math.distance(from, to) * 0.1f + 6f,
                    damage = 14f,
                    team = 0
                };
                counters[C.Arrows] = n + 1;
            }
            volleyCd = 0.6f;
        }

        /// <summary>Hoả ngục: thiêu xác sống trong bán kính 7, để lại vết cháy trên mặt đất.</summary>
        public void CastFire(float2 at)
        {
            const float R = 7f;
            int n = counters[C.Units];
            for (int i = 0; i < n; i++)
            {
                Unit u = units[i];
                if (u.type < 2) continue;
                float d = math.distance(u.pos, at);
                if (d > R) continue;
                u.hp -= 70f * (1f - 0.5f * d / R);
                u.flash = 1f;
                units[i] = u;
            }

            for (int k = 0; k < 900; k++)
            {
                Fx.Emit(particles, counters, new Particle
                {
                    pos = at + rng.NextFloat2Direction() * math.sqrt(rng.NextFloat()) * R,
                    vel = rng.NextFloat2Direction() * rng.NextFloat(0f, 2f),
                    h = rng.NextFloat(0f, 1f),
                    vh = rng.NextFloat(2f, 9f),
                    maxLife = rng.NextFloat(0.5f, 1.6f),
                    kind = PKind.Fire
                });
            }
            for (int k = 0; k < 250; k++)
            {
                Fx.Emit(particles, counters, new Particle
                {
                    pos = at + rng.NextFloat2Direction() * math.sqrt(rng.NextFloat()) * R,
                    vel = rng.NextFloat2Direction() * rng.NextFloat(0f, 1f),
                    h = rng.NextFloat(1f, 3f),
                    vh = rng.NextFloat(2f, 4f),
                    maxLife = rng.NextFloat(1.5f, 3f),
                    kind = PKind.Smoke
                });
            }
            for (int k = 0; k < 160; k++)
            {
                Fx.Emit(particles, counters, new Particle
                {
                    pos = at + rng.NextFloat2Direction() * rng.NextFloat(0f, R * 0.5f),
                    vel = rng.NextFloat2Direction() * rng.NextFloat(4f, 10f),
                    h = 0.5f,
                    vh = rng.NextFloat(6f, 14f),
                    maxLife = 3f,
                    kind = PKind.Ember
                });
            }
            for (int k = 0; k < 45; k++)
            {
                float2 p = at + rng.NextFloat2Direction() * math.sqrt(rng.NextFloat()) * R * 0.85f;
                Fx.Stamp(stamps, counters, p, StampCell.Scorch + rng.NextInt(0, 4), rng.NextInt(0, 8));
            }
            fireCd = 2.5f;
        }

        // ------------------------------------------------------------------
        // Vòng lặp
        // ------------------------------------------------------------------
        void Update()
        {
            view.Update();
            if (Input.GetKeyDown(KeyCode.R)) Restart();
            if (Input.GetKeyDown(KeyCode.Space)) paused = !paused;

            fpsFrames++;
            fpsTimer += Time.unscaledDeltaTime;
            if (fpsTimer >= 0.5f) { fps = fpsFrames / fpsTimer; fpsFrames = 0; fpsTimer = 0f; }
            simScheduled = false;
            if (paused || defeated) return;

            float dt = math.min(Time.deltaTime, 1f / 30f);
            elapsed += dt;
            volleyCd -= dt;
            fireCd -= dt;

            if (!MouseOverHud())
            {
                float2 mouse = view.MouseWorld;
                if (Input.GetMouseButton(0) && volleyCd <= 0f) CastVolley(mouse);
                if (Input.GetMouseButton(1) && fireCd <= 0f) CastFire(mouse);
            }
            Reinforce(dt);

            sw.Restart();
            ScheduleSimulation(dt);
            simScheduled = true;
        }

        void ScheduleSimulation(float dt)
        {
            frame++;
            int nu = counters[C.Units];

            var h = new BuildGridJob
            {
                units = units, counters = counters, cellStart = cellStart, cursor = cursor, teamCount = teamCount,
                cellOf = cellOf, sortedIndex = sortedIndex, sortedPos = sortedPos, sortedTeam = sortedTeam
            }.Schedule();

            h = new ThinkMoveJob
            {
                units = units, stats = stats, counters = counters, cellStart = cellStart, teamCount = teamCount,
                sortedIndex = sortedIndex, sortedPos = sortedPos, sortedTeam = sortedTeam,
                attackTarget = attackTarget, shots = shots, dt = dt, frame = frame
            }.Schedule(nu, 64, h);

            h = new ResolveJob
            {
                units = units, stats = stats, attackTarget = attackTarget, shots = shots, arrows = arrows,
                particles = particles, counters = counters, seed = rng.NextUInt() | 1u
            }.Schedule(h);

            h = new ArrowStepJob { arrows = arrows, counters = counters, dt = dt }.Schedule(Cfg.MaxArrows, 512, h);

            h = new ArrowLandJob
            {
                arrows = arrows, units = units, particles = particles, stamps = stamps, counters = counters,
                cellStart = cellStart, sortedIndex = sortedIndex, seed = rng.NextUInt() | 1u
            }.Schedule(h);

            h = new ParticleStepJob { particles = particles, counters = counters, dt = dt }.Schedule(Cfg.MaxParticles, 1024, h);
            h = new ParticleCompactJob { particles = particles, stamps = stamps, counters = counters, seed = rng.NextUInt() | 1u }.Schedule(h);
            h = new DeathJob { units = units, particles = particles, stamps = stamps, counters = counters, seed = rng.NextUInt() | 1u }.Schedule(h);

            var p1 = new PackUnitsJob { units = units, counters = counters, gpu = unitGpu }.Schedule(Cfg.MaxUnits, 256, h);
            var p2 = new PackArrowsJob { arrows = arrows, counters = counters, gpu = arrowGpu }.Schedule(Cfg.MaxArrows, 256, h);
            var p3 = new PackParticlesJob { particles = particles, counters = counters, gpu = particleGpu }.Schedule(Cfg.MaxParticles, 1024, h);
            simHandle = JobHandle.CombineDependencies(p1, p2, p3);
            JobHandle.ScheduleBatchedJobs();
        }

        void LateUpdate()
        {
            if (simScheduled)
            {
                simHandle.Complete();
                sw.Stop();
                simMs = math.lerp(simMs, (float)sw.Elapsed.TotalMilliseconds, 0.1f);
                castleHp = math.max(0, castleMaxHp - counters[C.Breach]);
                if (castleHp == 0) defeated = true;
            }
            UploadAndDraw();
        }

        void UploadAndDraw()
        {
            int nu = counters[C.Units], na = counters[C.Arrows], np = counters[C.Particles], ns = counters[C.Stamps];

            // Mặt đất sạch (cỏ, tường, nghĩa địa). Làm lại nếu driver đánh mất nội dung RenderTexture.
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

            unitBuf.SetData(unitGpu, 0, 0, nu);
            arrowBuf.SetData(arrowGpu, 0, 0, na);
            particleBuf.SetData(particleGpu, 0, 0, np);

            Graphics.RenderPrimitives(rpGround, MeshTopology.Triangles, 6, 1);
            if (nu > 0) Graphics.RenderPrimitives(rpUnits, MeshTopology.Triangles, 6, nu);          // 1 lệnh vẽ cho mọi lính
            if (na > 0)
            {
                Graphics.RenderPrimitives(rpShadows, MeshTopology.Triangles, 6, na);
                Graphics.RenderPrimitives(rpArrows, MeshTopology.Triangles, 6, na);
            }
            if (np > 0) Graphics.RenderPrimitives(rpParticles, MeshTopology.Triangles, 6, np);
        }

        // ------------------------------------------------------------------
        // Giao diện
        // ------------------------------------------------------------------
        bool MouseOverHud()
        {
            Vector3 m = Input.mousePosition;
            return hudRect.Contains(new Vector2(m.x, Screen.height - m.y));
        }

        void EnsureStyles()
        {
            if (label != null) return;
            label = new GUIStyle(GUI.skin.label) { fontSize = 15, fontStyle = FontStyle.Bold, richText = true };
            label.normal.textColor = new Color(0.93f, 0.91f, 0.86f);
            small = new GUIStyle(label) { fontSize = 13, fontStyle = FontStyle.Normal, alignment = TextAnchor.MiddleCenter };
            big = new GUIStyle(label) { fontSize = 34, alignment = TextAnchor.MiddleCenter };
        }

        static void Fill(Rect r, Color c)
        {
            var old = GUI.color;
            GUI.color = c;
            GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.color = old;
        }

        void OnGUI()
        {
            if (Event.current.type == EventType.Repaint) view.DrawToScreen();
            if (!counters.IsCreated) return;
            EnsureStyles();

            int k0 = counters[C.Type0 + 0], k1 = counters[C.Type0 + 1];
            int u0 = counters[C.Type0 + 2], u1 = counters[C.Type0 + 3];
            int t = (int)elapsed;

            Fill(hudRect, new Color(0.05f, 0.04f, 0.07f, 0.78f));
            float x = hudRect.x + 12f, y = hudRect.y + 8f, w = hudRect.width - 24f;
            void Line(string s) { GUI.Label(new Rect(x, y, w, 22f), s, label); y += 22f; }

            Line($"<color=#8fb8ff>VƯƠNG QUỐC</color>  {k0 + k1:N0} lính   <color=#aaaaaa>({k0:N0} kiếm · {k1:N0} cung)</color>");
            Line($"<color=#9fe07a>XÁC SỐNG</color>  {u0 + u1:N0} con   <color=#aaaaaa>({u0:N0} zombie · {u1:N0} cung xương)</color>");
            Line($"Mũi tên đang bay: <color=#ffe08a>{counters[C.Arrows]:N0}</color>    Hạt hiệu ứng: <color=#ffb070>{counters[C.Particles]:N0}</color>");
            Line($"Đã diệt: <color=#9fe07a>{counters[C.DeadUndead]:N0}</color>    Tử trận: <color=#ff8f8f>{counters[C.DeadKingdom]:N0}</color>");
            Line($"Thời gian {t / 60:00}:{t % 60:00}    Quái kéo đến: {undeadRate + undeadRateGrowth * elapsed:0}/giây");
            Line($"FPS <color=#ffffff>{fps:0}</color>    Mô phỏng (Jobs + Burst): <color=#ffffff>{simMs:0.00} ms</color>");

            y += 4f;
            var bar = new Rect(x, y, w, 16f);
            Fill(bar, new Color(0.25f, 0.06f, 0.06f));
            Fill(new Rect(bar.x, bar.y, bar.width * castleHp / (float)castleMaxHp, bar.height), new Color(0.35f, 0.62f, 1f));
            GUI.Label(new Rect(bar.x, bar.y - 3f, bar.width, 22f), $"Tường thành {castleHp}/{castleMaxHp}", small);
            y += 22f;
            string volley = volleyCd <= 0f ? "<color=#9fe07a>sẵn sàng</color>" : $"{volleyCd:0.0}s";
            string fire = fireCd <= 0f ? "<color=#9fe07a>sẵn sàng</color>" : $"{fireCd:0.0}s";
            Line($"Mưa tên: {volley}    Hoả ngục: {fire}");

            var help = new Rect(0f, Screen.height - 28f, Screen.width, 28f);
            Fill(help, new Color(0.05f, 0.04f, 0.07f, 0.78f));
            GUI.Label(help, $"Chuột trái: Mưa tên    Chuột phải: Hoả ngục    Lăn chuột / + -: Phóng to (x{view.Scale})    WASD / giữ chuột giữa: Di chuyển    Space: Tạm dừng    R: Chơi lại", small);

            if (paused) GUI.Label(new Rect(0f, Screen.height * 0.5f - 30f, Screen.width, 60f), "TẠM DỪNG", big);
            if (defeated)
            {
                var r = new Rect(Screen.width * 0.5f - 330f, Screen.height * 0.5f - 60f, 660f, 120f);
                Fill(r, new Color(0.08f, 0.02f, 0.02f, 0.85f));
                GUI.Label(new Rect(r.x, r.y + 10f, r.width, 50f), "THÀNH ĐÃ THẤT THỦ!", big);
                GUI.Label(new Rect(r.x, r.y + 70f, r.width, 30f), $"Trụ được {t / 60:00}:{t % 60:00} — diệt {counters[C.DeadUndead]:N0} xác sống. Nhấn R để chơi lại.", small);
            }
        }

        // ------------------------------------------------------------------
        void OnDestroy()
        {
            simHandle.Complete();
            void D<T>(ref NativeArray<T> a) where T : struct { if (a.IsCreated) a.Dispose(); }
            D(ref units); D(ref stats); D(ref arrows); D(ref particles); D(ref stamps); D(ref counters);
            D(ref cellStart); D(ref cursor); D(ref teamCount); D(ref cellOf); D(ref sortedIndex);
            D(ref sortedPos); D(ref sortedTeam); D(ref attackTarget); D(ref shots);
            D(ref unitGpu); D(ref arrowGpu); D(ref particleGpu);

            unitBuf?.Release(); arrowBuf?.Release(); particleBuf?.Release(); stampBuf?.Release();
            stampCmd?.Release();
            if (groundRT != null) groundRT.Release();
            view?.Dispose();
        }
    }
}
