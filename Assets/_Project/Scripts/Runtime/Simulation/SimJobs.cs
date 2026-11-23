using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Random = Unity.Mathematics.Random;

namespace HordeLab
{
    /// <summary>Hàm phụ để thêm hạt hiệu ứng / dấu in vào mảng (dùng được cả trong Burst lẫn main thread).</summary>
    static class Fx
    {
        public static void Emit(NativeArray<Particle> ps, NativeArray<int> counters, in Particle p)
        {
            int n = counters[C.Particles];
            if (n >= ps.Length) return;
            ps[n] = p;
            counters[C.Particles] = n + 1;
        }

        public static void Stamp(NativeArray<float4> stamps, NativeArray<int> counters, float2 pos, int cell, int flags)
        {
            int n = counters[C.Stamps];
            if (n >= stamps.Length) return;
            stamps[n] = new float4(pos, cell, flags);
            counters[C.Stamps] = n + 1;
        }

        /// <summary>Bắn ra một chùm hạt văng lên rồi rơi xuống (máu, tia lửa, bụi...).</summary>
        public static void Spray(NativeArray<Particle> ps, NativeArray<int> counters, ref Random rng,
                                 float2 pos, float h, int count, int kind, float speed, float up)
        {
            for (int k = 0; k < count; k++)
            {
                Emit(ps, counters, new Particle
                {
                    pos = pos,
                    vel = rng.NextFloat2Direction() * rng.NextFloat(0.3f, 1f) * speed,
                    h = h,
                    vh = rng.NextFloat(0.4f, 1f) * up,
                    maxLife = 3f,
                    kind = kind
                });
            }
        }

        public static int BloodKind(int type) =>
            type == UnitType.Skeleton ? PKind.Bone : type == UnitType.Zombie ? PKind.DarkBlood : PKind.Blood;
    }

    // =====================================================================
    // 1) Lưới không gian: xếp lính vào ô 2x2 bằng counting sort.
    //    Sau job này, lính trong ô c nằm liền nhau ở [cellStart[c], cellStart[c+1]).
    // =====================================================================
    [BurstCompile(CompileSynchronously = true)]
    struct BuildGridJob : IJob
    {
        [ReadOnly] public NativeArray<Unit> units;
        [ReadOnly] public NativeArray<int> counters;
        public NativeArray<int> cellStart;    // Cells + 1
        public NativeArray<int> cursor;       // Cells
        public NativeArray<int> teamCount;    // Cells * 2
        public NativeArray<int> cellOf;
        public NativeArray<int> sortedIndex;
        public NativeArray<float2> sortedPos;
        public NativeArray<byte> sortedTeam;

        public void Execute()
        {
            int n = counters[C.Units];
            for (int c = 0; c <= Cfg.Cells; c++) cellStart[c] = 0;
            for (int c = 0; c < Cfg.Cells * 2; c++) teamCount[c] = 0;

            for (int i = 0; i < n; i++)
            {
                float2 p = units[i].pos;
                int c = Cfg.CellY(p.y) * Cfg.GridW + Cfg.CellX(p.x);
                cellOf[i] = c;
                cellStart[c + 1]++;
                teamCount[c * 2 + Cfg.TeamOf(units[i].type)]++;
            }

            for (int c = 0; c < Cfg.Cells; c++)
            {
                cellStart[c + 1] += cellStart[c];
                cursor[c] = cellStart[c];
            }

            for (int i = 0; i < n; i++)
            {
                int c = cellOf[i];
                int k = cursor[c];
                cursor[c] = k + 1;
                sortedIndex[k] = i;
                sortedPos[k] = units[i].pos;
                sortedTeam[k] = (byte)Cfg.TeamOf(units[i].type);
            }
        }
    }

    // =====================================================================
    // 2) Suy nghĩ + di chuyển: chạy song song trên mọi nhân CPU.
    //    Mỗi lính chỉ ghi vào chính nó, đọc vị trí người khác từ bản sao đã sắp xếp → không tranh chấp.
    // =====================================================================
    [BurstCompile(CompileSynchronously = true)]
    struct ThinkMoveJob : IJobParallelFor
    {
        const float SepRadius = 0.85f;
        const float SepStrength = 7f;

        public NativeArray<Unit> units;
        [ReadOnly] public NativeArray<UnitStats> stats;
        [ReadOnly] public NativeArray<int> counters;
        [ReadOnly] public NativeArray<int> cellStart;
        [ReadOnly] public NativeArray<int> teamCount;
        [ReadOnly] public NativeArray<int> sortedIndex;
        [ReadOnly] public NativeArray<float2> sortedPos;
        [ReadOnly] public NativeArray<byte> sortedTeam;
        public NativeArray<int> attackTarget;   // -1 = không chém ai
        public NativeArray<float3> shots;       // z > 0 = bắn tên vào xy
        public float dt;
        public uint frame;

        public void Execute(int i)
        {
            if (i >= counters[C.Units]) return;

            Unit u = units[i];
            UnitStats s = stats[u.type];
            int team = Cfg.TeamOf(u.type);
            int cx = Cfg.CellX(u.pos.x), cy = Cfg.CellY(u.pos.y);

            // --- Quét 3x3 ô quanh mình: đẩy nhau ra cho khỏi chồng + tìm địch sát bên ---
            float2 sep = 0f;
            float bestD2 = float.MaxValue;
            int bestIdx = -1;
            float2 bestPos = 0f;
            int visited = 0;
            for (int y = math.max(cy - 1, 0); y <= math.min(cy + 1, Cfg.GridH - 1); y++)
            {
                for (int x = math.max(cx - 1, 0); x <= math.min(cx + 1, Cfg.GridW - 1); x++)
                {
                    int c = y * Cfg.GridW + x;
                    int end = cellStart[c + 1];
                    for (int k = cellStart[c]; k < end && visited < 96; k++)
                    {
                        int j = sortedIndex[k];
                        if (j == i) continue;
                        visited++;
                        float2 d = u.pos - sortedPos[k];
                        float d2 = math.lengthsq(d);
                        if (d2 < SepRadius * SepRadius)
                        {
                            if (d2 < 1e-6f)
                            {
                                d = new float2(math.cos((float)i), math.sin((float)i)) * 0.01f;
                                d2 = 1e-4f;
                            }
                            float dist = math.sqrt(d2);
                            sep += d / dist * (SepRadius - dist);
                        }
                        if (sortedTeam[k] != team && d2 < bestD2)
                        {
                            bestD2 = d2;
                            bestIdx = j;
                            bestPos = sortedPos[k];
                        }
                    }
                }
            }

            // --- Quét xa để tìm mục tiêu: mỗi lính chỉ làm 1 lần / 8 frame (chia việc theo thời gian) ---
            bool rescan = (((uint)i + frame) & 7u) == 0u;
            if (rescan && (s.ranged != 0 || bestIdx < 0))
            {
                int radius = s.ranged != 0 ? (int)math.ceil(s.range / Cfg.CellSize) + 1 : 8;
                u.hasGoal = FindNearestEnemy(cx, cy, 1 - team, radius, u.pos, out float2 goal) ? 1 : 0;
                u.goal = goal;
            }

            float2 desired = team == 0 ? new float2(1f, 0f) : new float2(-1f, 0f);
            bool attacking = false;
            int atk = -1;
            float3 shot = 0f;

            if (s.ranged != 0)
            {
                if (bestIdx >= 0) { u.goal = bestPos; u.hasGoal = 1; }
                if (u.hasGoal != 0)
                {
                    float2 to = u.goal - u.pos;
                    float dist = math.length(to);
                    if (dist <= s.range)
                    {
                        attacking = true;
                        desired = 0f;
                        u.faceLeft = to.x < 0f ? 1 : 0;
                        if (u.cooldown <= 0f)
                        {
                            shot = new float3(u.goal, 1f);
                            u.cooldown = s.cooldown * (0.8f + 0.4f * Hash01((uint)i, frame));
                            u.animT = 0f;
                        }
                    }
                    else desired = to / math.max(dist, 1e-4f);
                }
            }
            else
            {
                if (bestIdx >= 0)
                {
                    float dist = math.sqrt(bestD2);
                    float2 to = bestPos - u.pos;
                    float2 dir = to / math.max(dist, 1e-4f);
                    if (dist <= s.range)
                    {
                        attacking = true;
                        desired = dir * 0.15f;
                        u.faceLeft = to.x < 0f ? 1 : 0;
                        if (u.cooldown <= 0f)
                        {
                            atk = bestIdx;
                            u.cooldown = s.cooldown * (0.8f + 0.4f * Hash01((uint)i, frame));
                            u.animT = 0f;
                        }
                    }
                    else desired = dir;
                }
                else if (u.hasGoal != 0)
                {
                    float2 to = u.goal - u.pos;
                    float dist = math.length(to);
                    if (dist > 1f) desired = to / dist;
                    else u.hasGoal = 0;
                }
            }

            // --- Di chuyển ---
            float2 vel = desired * s.speed + sep * SepStrength;
            float maxV = s.speed * 1.6f;
            float vl = math.length(vel);
            if (vl > maxV) vel *= maxV / vl;
            u.pos += vel * dt;
            u.pos.x = math.clamp(u.pos.x, -Cfg.HalfW + 0.5f, Cfg.HalfW - 0.5f);
            u.pos.y = math.clamp(u.pos.y, -Cfg.HalfH + 0.6f, Cfg.HalfH - 0.6f);
            if (!attacking && math.abs(vel.x) > 0.2f) u.faceLeft = vel.x < 0f ? 1 : 0;

            u.state = attacking ? 1 : 0;
            u.animT += dt;
            u.cooldown -= dt;
            u.flash = math.max(0f, u.flash - dt * 8f);

            units[i] = u;
            attackTarget[i] = atk;
            shots[i] = shot;
        }

        /// <summary>Tìm địch gần nhất bằng cách quét các "vòng" ô lan dần ra ngoài; ô nào không có địch thì bỏ qua ngay.</summary>
        bool FindNearestEnemy(int cx, int cy, int enemyTeam, int radius, float2 from, out float2 found)
        {
            found = from;
            float best = float.MaxValue;
            bool any = false;
            for (int r = 0; r <= radius; r++)
            {
                int y0 = cy - r, y1 = cy + r;
                for (int y = y0; y <= y1; y++)
                {
                    if (y < 0 || y >= Cfg.GridH) continue;
                    int step = (y == y0 || y == y1) ? 1 : 2 * r;
                    for (int x = cx - r; x <= cx + r; x += step)
                    {
                        if (x < 0 || x >= Cfg.GridW) continue;
                        int c = y * Cfg.GridW + x;
                        if (teamCount[c * 2 + enemyTeam] == 0) continue;
                        int end = cellStart[c + 1];
                        for (int k = cellStart[c]; k < end; k++)
                        {
                            if (sortedTeam[k] != enemyTeam) continue;
                            float d2 = math.lengthsq(sortedPos[k] - from);
                            if (d2 < best) { best = d2; found = sortedPos[k]; any = true; }
                        }
                    }
                }
                if (any) return true;
            }
            return false;
        }

        static float Hash01(uint a, uint b) => (math.hash(new uint2(a, b)) & 0xFFFFFFu) / 16777216f;
    }

    // =====================================================================
    // 3) Áp dụng đòn chém và tạo mũi tên. Chạy 1 luồng (vài micro giây) để khỏi tranh chấp khi
    //    nhiều lính cùng chém 1 mục tiêu.
    // =====================================================================
    [BurstCompile(CompileSynchronously = true)]
    struct ResolveJob : IJob
    {
        const float ArrowSpeed = 22f;

        public NativeArray<Unit> units;
        [ReadOnly] public NativeArray<UnitStats> stats;
        [ReadOnly] public NativeArray<int> attackTarget;
        [ReadOnly] public NativeArray<float3> shots;
        public NativeArray<Arrow> arrows;
        public NativeArray<Particle> particles;
        public NativeArray<int> counters;
        public uint seed;

        public void Execute()
        {
            var rng = new Random(seed);
            int n = counters[C.Units];
            for (int i = 0; i < n; i++)
            {
                int t = attackTarget[i];
                if (t >= 0)
                {
                    Unit a = units[i];
                    Unit v = units[t];
                    v.hp -= stats[a.type].damage * rng.NextFloat(0.75f, 1.25f);
                    v.flash = 1f;
                    units[t] = v;
                    float2 hit = math.lerp(a.pos, v.pos, 0.6f);
                    Fx.Spray(particles, counters, ref rng, hit, 0.5f, 2, Fx.BloodKind(v.type), 2.5f, 5f);
                    Fx.Spray(particles, counters, ref rng, hit, 0.6f, 1, PKind.Spark, 3f, 3f);
                }

                float3 sh = shots[i];
                if (sh.z > 0f)
                {
                    int count = counters[C.Arrows];
                    if (count >= arrows.Length) continue;
                    Unit a = units[i];
                    float2 from = a.pos + new float2(a.faceLeft != 0 ? -0.3f : 0.3f, 0.2f);
                    float2 to = sh.xy + rng.NextFloat2Direction() * rng.NextFloat(0f, 1.8f);
                    float dist = math.max(math.distance(from, to), 1f);
                    arrows[count] = new Arrow
                    {
                        from = from,
                        to = to,
                        t = 0f,
                        invDuration = ArrowSpeed / dist,
                        arc = dist * 0.22f + 0.4f,
                        damage = stats[a.type].damage,
                        team = Cfg.TeamOf(a.type)
                    };
                    counters[C.Arrows] = count + 1;
                }
            }
        }
    }

    // =====================================================================
    // 4) Mũi tên bay: chỉ là tăng t. Vị trí tính bằng công thức parabol, không có vật lý.
    // =====================================================================
    [BurstCompile(CompileSynchronously = true)]
    struct ArrowStepJob : IJobParallelFor
    {
        public NativeArray<Arrow> arrows;
        [ReadOnly] public NativeArray<int> counters;
        public float dt;

        public void Execute(int i)
        {
            if (i >= counters[C.Arrows]) return;
            Arrow a = arrows[i];
            a.t += dt * a.invDuration;
            arrows[i] = a;
        }
    }

    // =====================================================================
    // 5) Mũi tên chạm đất: tra lưới xem trúng ai, rồi xoá mũi tên (đổi chỗ với phần tử cuối).
    // =====================================================================
    [BurstCompile(CompileSynchronously = true)]
    struct ArrowLandJob : IJob
    {
        const float HitRadius = 0.6f;

        public NativeArray<Arrow> arrows;
        public NativeArray<Unit> units;
        public NativeArray<Particle> particles;
        public NativeArray<float4> stamps;
        public NativeArray<int> counters;
        [ReadOnly] public NativeArray<int> cellStart;
        [ReadOnly] public NativeArray<int> sortedIndex;
        public uint seed;

        public void Execute()
        {
            var rng = new Random(seed);
            int n = counters[C.Arrows];
            int i = 0;
            while (i < n)
            {
                Arrow a = arrows[i];
                if (a.t < 1f) { i++; continue; }

                int victim = FindVictim(a.to, 1 - a.team);
                if (victim >= 0)
                {
                    Unit v = units[victim];
                    v.hp -= a.damage * rng.NextFloat(0.8f, 1.2f);
                    v.flash = 1f;
                    units[victim] = v;
                    Fx.Spray(particles, counters, ref rng, v.pos, 0.5f, 3, Fx.BloodKind(v.type), 2f, 6f);
                }
                else
                {
                    Fx.Spray(particles, counters, ref rng, a.to, 0f, 2, PKind.Dust, 1.5f, 3f);
                    if (rng.NextFloat() < 0.3f)
                    {
                        int cell = (a.team == 0 ? StampCell.ArrowKingdom : StampCell.ArrowUndead) + rng.NextInt(0, 2);
                        Fx.Stamp(stamps, counters, a.to, cell, rng.NextBool() ? 4 : 0);
                    }
                }

                n--;
                arrows[i] = arrows[n];
            }
            counters[C.Arrows] = n;
        }

        int FindVictim(float2 p, int enemyTeam)
        {
            int cx = Cfg.CellX(p.x), cy = Cfg.CellY(p.y);
            float best = HitRadius * HitRadius;
            int bi = -1;
            for (int y = math.max(cy - 1, 0); y <= math.min(cy + 1, Cfg.GridH - 1); y++)
            {
                for (int x = math.max(cx - 1, 0); x <= math.min(cx + 1, Cfg.GridW - 1); x++)
                {
                    int c = y * Cfg.GridW + x;
                    int end = cellStart[c + 1];
                    for (int k = cellStart[c]; k < end; k++)
                    {
                        int j = sortedIndex[k];
                        Unit u = units[j];
                        if (Cfg.TeamOf(u.type) != enemyTeam || u.hp <= 0f) continue;
                        float d2 = math.lengthsq(u.pos - p);
                        if (d2 < best) { best = d2; bi = j; }
                    }
                }
            }
            return bi;
        }
    }

    // =====================================================================
    // 6) Hạt hiệu ứng: lửa/khói bay lên, máu/bụi/tia lửa văng lên rồi rơi.
    // =====================================================================
    [BurstCompile(CompileSynchronously = true)]
    struct ParticleStepJob : IJobParallelFor
    {
        const float Gravity = 30f;

        public NativeArray<Particle> particles;
        [ReadOnly] public NativeArray<int> counters;
        public float dt;

        public void Execute(int i)
        {
            if (i >= counters[C.Particles]) return;
            Particle p = particles[i];
            p.life += dt;
            if (p.kind == PKind.Fire || p.kind == PKind.Smoke)
            {
                p.pos += p.vel * dt;
                p.h += p.vh * dt;
                p.vel *= 1f - math.min(1f, 2.5f * dt);
            }
            else
            {
                p.vh -= Gravity * dt;
                p.h += p.vh * dt;
                p.pos += p.vel * dt;
                if (p.h <= 0f) { p.h = 0f; p.landed = 1; }
            }
            particles[i] = p;
        }
    }

    /// <summary>Xoá hạt đã chết. Giọt máu chạm đất thì "in" một chấm máu lên mặt đất.</summary>
    [BurstCompile(CompileSynchronously = true)]
    struct ParticleCompactJob : IJob
    {
        public NativeArray<Particle> particles;
        public NativeArray<float4> stamps;
        public NativeArray<int> counters;
        public uint seed;

        public void Execute()
        {
            var rng = new Random(seed);
            int n = counters[C.Particles];
            int i = 0;
            while (i < n)
            {
                Particle p = particles[i];
                if (p.landed == 0 && p.life < p.maxLife) { i++; continue; }
                if (p.landed != 0 && (p.kind == PKind.Blood || p.kind == PKind.DarkBlood) && rng.NextFloat() < 0.6f)
                    Fx.Stamp(stamps, counters, p.pos, StampCell.Drop + rng.NextInt(0, 4), rng.NextInt(0, 8));
                n--;
                particles[i] = particles[n];
            }
            counters[C.Particles] = n;
        }
    }

    // =====================================================================
    // 7) Lính chết: in xác + vũng máu vào texture mặt đất rồi XOÁ khỏi mảng.
    //    Xác chết từ giờ chỉ là vài pixel trên mặt đất → không tốn gì nữa.
    // =====================================================================
    [BurstCompile(CompileSynchronously = true)]
    struct DeathJob : IJob
    {
        public NativeArray<Unit> units;
        public NativeArray<Particle> particles;
        public NativeArray<float4> stamps;
        public NativeArray<int> counters;
        public uint seed;

        public void Execute()
        {
            var rng = new Random(seed);
            int n = counters[C.Units];
            int i = 0;
            while (i < n)
            {
                Unit u = units[i];
                bool breach = u.type >= 2 && u.pos.x < Cfg.WallX;
                if (u.hp > 0f && !breach) { i++; continue; }

                if (breach)
                {
                    counters[C.Breach] = counters[C.Breach] + 1;
                    Fx.Spray(particles, counters, ref rng, u.pos, 0.5f, 6, PKind.GreenSpark, 3f, 6f);
                }
                else
                {
                    int deadIdx = u.type >= 2 ? C.DeadUndead : C.DeadKingdom;
                    counters[deadIdx] = counters[deadIdx] + 1;
                    if (u.type != UnitType.Skeleton)
                    {
                        int splat = u.type == UnitType.Zombie ? StampCell.BloodDark : StampCell.BloodRed;
                        Fx.Stamp(stamps, counters, u.pos + rng.NextFloat2(-0.3f, 0.3f), splat + rng.NextInt(0, 4), rng.NextInt(0, 8));
                    }
                    int corpse = (rng.NextBool() ? StampCell.Corpse : StampCell.CorpseAlt) + u.type;
                    Fx.Stamp(stamps, counters, u.pos, corpse, rng.NextBool() ? 4 : 0);
                    Fx.Spray(particles, counters, ref rng, u.pos, 0.6f, 7, Fx.BloodKind(u.type), 3f, 7f);
                }

                n--;
                units[i] = units[n];
            }
            counters[C.Units] = n;

            for (int t = 0; t < 4; t++) counters[C.Type0 + t] = 0;
            for (int k = 0; k < n; k++)
            {
                int idx = C.Type0 + units[k].type;
                counters[idx] = counters[idx] + 1;
            }
        }
    }

    // =====================================================================
    // 8) Đóng gói dữ liệu gọn nhẹ để gửi lên GPU (16 byte / lính).
    // =====================================================================
    [BurstCompile(CompileSynchronously = true)]
    struct PackUnitsJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<Unit> units;
        [ReadOnly] public NativeArray<int> counters;
        public NativeArray<float4> gpu;

        public void Execute(int i)
        {
            if (i >= counters[C.Units]) return;
            Unit u = units[i];
            int frame;
            if (u.state == 0)
            {
                float fps = u.type == UnitType.Zombie ? 6f : 9f;
                frame = (int)(u.animT * fps) & 3;
            }
            else frame = u.animT < 0.4f ? math.min(3, (int)(u.animT * 10f)) : 0;

            int row = u.type * 2 + u.state;
            int flags = u.faceLeft | (u.flash > 0.25f ? 2 : 0);
            gpu[i] = new float4(u.pos, row * 4 + frame, flags);
        }
    }

    [BurstCompile(CompileSynchronously = true)]
    struct PackArrowsJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<Arrow> arrows;
        [ReadOnly] public NativeArray<int> counters;
        public NativeArray<ArrowGpu> gpu;

        public void Execute(int i)
        {
            if (i >= counters[C.Arrows]) return;
            Arrow a = arrows[i];
            float t = math.saturate(a.t);
            float2 g = math.lerp(a.from, a.to, t);
            float h = a.arc * 4f * t * (1f - t);
            float2 vg = (a.to - a.from) * a.invDuration;
            float vh = a.arc * 4f * (1f - 2f * t) * a.invDuration;
            gpu[i] = new ArrowGpu
            {
                a = new float4(g, h, a.team),
                b = new float4(math.atan2(vg.y + vh, vg.x), math.atan2(vg.y, vg.x), 0f, 0f)
            };
        }
    }

    [BurstCompile(CompileSynchronously = true)]
    struct PackParticlesJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<Particle> particles;
        [ReadOnly] public NativeArray<int> counters;
        public NativeArray<float4> gpu;

        public void Execute(int i)
        {
            if (i >= counters[C.Particles]) return;
            Particle p = particles[i];
            float f = p.life / p.maxLife;
            int size = 1, color;
            switch (p.kind)
            {
                case PKind.Blood: color = 1; break;
                case PKind.DarkBlood: color = 2; break;
                case PKind.Spark: color = 14; break;
                case PKind.Dust: color = 4; break;
                case PKind.Bone: color = 6; break;
                case PKind.Fire:
                    size = f < 0.4f ? 2 : 1;
                    color = f < 0.12f ? 7 : f < 0.3f ? 8 : f < 0.55f ? 9 : f < 0.8f ? 10 : 12;
                    break;
                case PKind.Smoke:
                    size = 2;
                    color = f < 0.5f ? 11 : 12;
                    break;
                case PKind.Ember: color = f < 0.4f ? 8 : 15; break;
                default: color = 13; break;
            }
            gpu[i] = new float4(p.pos, p.h, color + 16 * (size - 1));
        }
    }

    // =====================================================================
    // Sinh texture mặt đất (cỏ/đất pixel) — chạy 1 lần lúc khởi động.
    // =====================================================================
    [BurstCompile(CompileSynchronously = true)]
    struct GroundJob : IJobParallelFor
    {
        public NativeArray<UnityEngine.Color32> pixels;
        public int width;
        public uint seed;

        public void Execute(int i)
        {
            int x = i % width, y = i / width;
            float worldX = x / (float)Cfg.PPU - Cfg.HalfW;
            // Xoay + uốn toạ độ để nhiễu không lộ lưới ô vuông
            float2 p = new float2(x, y);
            p += 40f * new float2(ValueNoise(x / 150f, y / 150f, seed + 31u) - 0.5f, ValueNoise(x / 150f, y / 150f, seed + 37u) - 0.5f);
            float2 r1 = new float2(p.x * 0.80f - p.y * 0.60f, p.x * 0.60f + p.y * 0.80f);
            float2 r2 = new float2(p.x * 0.28f + p.y * 0.96f, -p.x * 0.96f + p.y * 0.28f);
            float n = ValueNoise(r1.x / 110f, r1.y / 110f, seed) * 0.6f
                    + ValueNoise(r2.x / 28f, r2.y / 28f, seed + 7u) * 0.3f
                    + ValueNoise(p.x / 7f, p.y / 7f, seed + 13u) * 0.1f;
            n += 0.16f * math.exp(-(worldX * worldX) / (2f * 32f * 32f)); // giữa trận bị giẫm nát, nhiều đất hơn
            n += (Hash(x, y, seed + 99u) - 0.5f) * 0.08f;                 // dither cho ra chất pixel

            UnityEngine.Color32 c;
            if (n < 0.40f) c = new UnityEngine.Color32(70, 96, 50, 255);
            else if (n < 0.50f) c = new UnityEngine.Color32(84, 110, 56, 255);
            else if (n < 0.58f) c = new UnityEngine.Color32(102, 106, 62, 255);
            else if (n < 0.66f) c = new UnityEngine.Color32(116, 98, 68, 255);
            else c = new UnityEngine.Color32(128, 108, 74, 255);

            float h = Hash(x, y, seed + 5u);
            float below = Hash(x, y - 1, seed + 5u);
            if (n < 0.55f && (h < 0.006f || below < 0.006f)) c = new UnityEngine.Color32(118, 146, 70, 255); // ngọn cỏ
            else if (h > 0.9975f) c = new UnityEngine.Color32(140, 136, 126, 255);                        // sỏi
            else if (h > 0.985f) c = new UnityEngine.Color32((byte)(c.r * 0.88f), (byte)(c.g * 0.88f), (byte)(c.b * 0.88f), 255);
            pixels[i] = c;
        }

        static float Hash(int x, int y, uint s) => (math.hash(new int3(x, y, (int)s)) & 0xFFFFu) / 65535f;

        static float ValueNoise(float x, float y, uint s)
        {
            int xi = (int)math.floor(x), yi = (int)math.floor(y);
            float fx = x - xi, fy = y - yi;
            fx = fx * fx * (3f - 2f * fx);
            fy = fy * fy * (3f - 2f * fy);
            float a = Hash(xi, yi, s), b = Hash(xi + 1, yi, s), c = Hash(xi, yi + 1, s), d = Hash(xi + 1, yi + 1, s);
            return math.lerp(math.lerp(a, b, fx), math.lerp(c, d, fx), fy);
        }
    }
}
