using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Random = Unity.Mathematics.Random;

namespace HordeLab
{
    /// <summary>Hàm phụ dùng chung cho các Job (dùng được cả trong Burst lẫn main thread).</summary>
    static class Fx
    {
        public const float ChargeRate = 0.6f;      // kỵ binh phi 1/ChargeRate giây là đầy đà
        public const float AuraRadius = 2.6f;      // tầm của tu sĩ / pháp sư bộ lạc
        public const float HealRate = 4f;          // máu hồi mỗi giây khi đứng cạnh tu sĩ
        public const float PowerBonus = 0.3f;      // +30% sát thương khi đứng cạnh pháp sư bộ lạc
        public const float SlowFactor = 0.5f;

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

        /// <summary>Lửa / khói bốc lên rồi tắt.</summary>
        public static void Flames(NativeArray<Particle> ps, NativeArray<int> counters, ref Random rng,
                                  float2 pos, float radius, int count, int kind, float life)
        {
            for (int k = 0; k < count; k++)
            {
                Emit(ps, counters, new Particle
                {
                    pos = pos + rng.NextFloat2Direction() * math.sqrt(rng.NextFloat()) * radius,
                    vel = rng.NextFloat2Direction() * rng.NextFloat(0f, 1.5f),
                    h = rng.NextFloat(0f, 1f),
                    vh = rng.NextFloat(2f, 7f),
                    maxLife = rng.NextFloat(0.5f, 1f) * life,
                    kind = kind
                });
            }
        }

        /// <summary>Vòng hạt toả ra từ tâm (sóng âm, vụ nổ).</summary>
        public static void Ring(NativeArray<Particle> ps, NativeArray<int> counters, float2 pos, float radius, int count, int kind)
        {
            for (int k = 0; k < count; k++)
            {
                float ang = k * (6.2831853f / count);
                float2 dir = new float2(math.cos(ang), math.sin(ang));
                Emit(ps, counters, new Particle { pos = pos + dir * 0.3f, vel = dir * radius * 3.3f, h = 0.6f, vh = 6f, maxLife = 3f, kind = kind });
            }
        }

        /// <summary>Sát thương vùng: mọi địch trong bán kính đều dính, càng xa tâm càng nhẹ. Trả về số nạn nhân.</summary>
        public static int Splash(NativeArray<Unit> units, NativeArray<int> cellStart, NativeArray<int> sortedIndex, int unitCount,
                                 float2 p, float radius, int enemyTeam, float dmg, float slow)
        {
            int r = (int)math.ceil(radius / Cfg.CellSize);
            int cx = Cfg.CellX(p.x), cy = Cfg.CellY(p.y);
            float r2 = radius * radius;
            int hits = 0;
            for (int y = math.max(cy - r, 0); y <= math.min(cy + r, Cfg.GridH - 1); y++)
            {
                for (int x = math.max(cx - r, 0); x <= math.min(cx + r, Cfg.GridW - 1); x++)
                {
                    int c = y * Cfg.GridW + x;
                    int end = cellStart[c + 1];
                    for (int k = cellStart[c]; k < end; k++)
                    {
                        int j = sortedIndex[k];
                        if (j >= unitCount) continue;
                        Unit u = units[j];
                        if (u.team != enemyTeam || u.hp <= 0f) continue;
                        float d2 = math.lengthsq(u.pos - p);
                        if (d2 > r2) continue;
                        u.hp -= dmg * (1f - 0.5f * math.sqrt(d2) / radius);
                        u.flash = 1f;
                        if (slow > 0f) u.slow = math.max(u.slow, slow);
                        units[j] = u;
                        hits++;
                    }
                }
            }
            return hits;
        }
    }

    // =====================================================================
    // 1) Lưới không gian: xếp lính vào ô 2x2 bằng counting sort.
    //    Sau job này, lính trong ô c nằm liền nhau ở [cellStart[c], cellStart[c+1]).
    // =====================================================================
    [BurstCompile(CompileSynchronously = true)]
    struct BuildGridJob : IJob
    {
        [ReadOnly] public NativeArray<Unit> units;
        [ReadOnly] public NativeArray<UnitStats> stats;
        [ReadOnly] public NativeArray<int> counters;
        public NativeArray<int> cellStart;    // Cells + 1
        public NativeArray<int> cursor;       // Cells
        public NativeArray<int> teamCount;    // Cells * 2
        public NativeArray<int> cellOf;
        public NativeArray<int> sortedIndex;
        public NativeArray<float2> sortedPos;
        public NativeArray<byte> sortedInfo;  // bit 0 = phe, bit 1 = có vùng hồi máu, bit 2 = có vùng tăng sức mạnh, bit 3 = đi xuyên, bit 4 = còn ở ngoài màn hình

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
                teamCount[c * 2 + units[i].team]++;
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
                Unit u = units[i];
                int f = stats[u.type].flags;
                sortedIndex[k] = i;
                sortedPos[k] = u.pos;
                sortedInfo[k] = (byte)(u.team
                                       | ((f & UFlag.HealAura) != 0 ? 2 : 0)
                                       | ((f & UFlag.PowerAura) != 0 ? 4 : 0)
                                       | ((f & UFlag.Ghost) != 0 ? 8 : 0)
                                       | ((f & UFlag.Giant) != 0 ? 64 : (f & UFlag.Big) != 0 ? 32 : 0)   // cỡ thân
                                       | (u.team == 1 && u.pos.x > Cfg.HalfW ? 16 : 0));
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
        static float RadiusOfInfo(int info) => (info & 64) != 0 ? Cfg.RadiusGiant : (info & 32) != 0 ? Cfg.RadiusBig : Cfg.RadiusSmall;
        const float SepStrength = 7f;
        const int SightCells = 28;   // 56 đơn vị
        const int EnemySightCells = 3;

        public NativeArray<Unit> units;
        [ReadOnly] public NativeArray<UnitStats> stats;
        [ReadOnly] public NativeArray<int> counters;
        [ReadOnly] public NativeArray<int> cellStart;
        [ReadOnly] public NativeArray<int> teamCount;
        [ReadOnly] public NativeArray<int> sortedIndex;
        [ReadOnly] public NativeArray<float2> sortedPos;
        [ReadOnly] public NativeArray<byte> sortedInfo;
        public NativeArray<int> attackTarget;   // -1 = không chém ai, -2 = đánh vào tường thành
        public NativeArray<float3> shots;       // z = 1: bắn / làm phép vào xy, z = 2: bắn vào tường thành
        public float dt;
        public uint frame;

        public void Execute(int i)
        {
            if (i >= counters[C.Units]) return;

            Unit u = units[i];
            UnitStats s = stats[u.type];
            int team = u.team;
            float fwd = team == 0 ? 1f : -1f;
            bool ghost = (s.flags & UFlag.Ghost) != 0;
            int cx = Cfg.CellX(u.pos.x), cy = Cfg.CellY(u.pos.y);

            // --- Quét 3x3 ô quanh mình: đẩy nhau ra cho khỏi chồng + tìm địch sát bên + xem có đứng trong vùng buff không ---
            float2 sep = 0f;
            float bestD2 = float.MaxValue;
            int bestIdx = -1, bestInfo = 0;
            float2 bestPos = 0f;
            float myR = s.radius, myR2 = myR * myR;
            int visited = 0;
            int aura = 0;
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
                        int info = sortedInfo[k];
                        bool enemy = (info & 1) != team;
                        float2 d = u.pos - sortedPos[k];
                        float d2 = math.lengthsq(d);
                        float otherR = RadiusOfInfo(info);
                        float sepR = myR + otherR;
                        if (d2 < sepR * sepR && !(enemy && (ghost || (info & 8) != 0)))
                        {
                            if (d2 < 1e-6f)
                            {
                                d = new float2(math.cos((float)i), math.sin((float)i)) * 0.01f;
                                d2 = 1e-4f;
                            }
                            float dist = math.sqrt(d2);
                            // to đẩy nhỏ: phần bị đẩy chia theo "khối lượng" ~ bán kính²; 2 lính bằng nhau thì như cũ (hệ số 1)
                            // to đẩy nhỏ: phần bị đẩy chia theo "khối lượng" ~ bán kính²; 2 lính bằng nhau thì như cũ (hệ số 1).
                            // Tính đều cho mọi cặp (không rẽ nhánh) — đo thử bản có rẽ nhánh còn chậm hơn.
                            sep += d / dist * (sepR - dist) * (2f * otherR * otherR / (myR2 + otherR * otherR));
                        }
                        if (enemy)
                        {
                            if (d2 < bestD2)
                            {
                                bestD2 = d2;
                                bestIdx = j;
                                bestInfo = info;
                                bestPos = sortedPos[k];
                            }
                        }
                        else if ((info & 6) != 0 && d2 < Fx.AuraRadius * Fx.AuraRadius) aura |= info;
                    }
                }
            }

            float heal = u.regen + ((aura & 2) != 0 ? Fx.HealRate : 0f);
            if (heal > 0f) u.hp = math.min(u.maxHp, u.hp + heal * dt);
            u.buffed = (aura & 4) != 0 ? 1 : 0;

            // --- Quét xa để tìm mục tiêu: mỗi lính chỉ làm 1 lần / 8 frame (chia việc theo thời gian) ---
            bool melee = s.mode == Mode.Melee;
            bool rescan = (((uint)i + frame) & 7u) == 0u;
            if (rescan && (!melee || bestIdx < 0))
            {
                // Tầm nhìn phủ hết chiều cao chiến trường: lính ở góc xa vẫn thấy trận đánh và kéo tới, không đứng im ở vạch giữ trận
                // Địch thì chỉ để ý lính ta ở gần (hoặc trong tầm bắn); không thì cứ tiến về tường, không đuổi theo từng tiểu đội mới ra
                int radius = team == 0 ? SightCells : melee ? EnemySightCells : math.max(EnemySightCells, (int)math.ceil(s.range / Cfg.CellSize) + 1);
                u.hasGoal = FindNearestEnemy(cx, cy, 1 - team, radius, u.pos, out float2 goal) ? 1 : 0;
                u.goal = goal;
            }

            float2 desired = new float2(fwd, 0f);
            bool attacking = false, idle = true, kite = false;
            int atk = -1;
            float3 shot = 0f;

            if (!melee)
            {
                if (bestIdx >= 0) { u.goal = bestPos; u.hasGoal = 1; }
                if (u.hasGoal != 0)
                {
                    idle = false;
                    float2 to = u.goal - u.pos;
                    float dist = math.length(to);
                    if (dist <= s.range)
                    {
                        attacking = true;
                        desired = 0f;
                        u.faceLeft = to.x < 0f ? 1 : 0;
                        if ((s.flags & UFlag.Kite) != 0)
                        {
                            // vừa chạy ngang vừa bắn; địch áp sát thì lùi
                            kite = true;
                            float side = (math.hash(new uint2((uint)i, frame / 150u)) & 1u) == 0u ? 1f : -1f;
                            if (u.pos.y * side > Cfg.HalfH - 4f) side = -side;
                            desired = math.normalize(new float2(dist < s.range * 0.5f ? -fwd : 0f, side));
                        }
                        if (u.cooldown <= 0f && dist >= s.minRange)   // máy công thành không bắn được địch đã áp sát
                        {
                            shot = new float3(u.goal, 1f);
                            u.cooldown = s.cooldown * (0.8f + 0.4f * Hash01((uint)i, frame));
                            if (!kite) u.animT = 0f;
                        }
                    }
                    else desired = to / math.max(dist, 1e-4f);
                }
                else if (team == 1 && u.pos.x - Cfg.WallX <= s.range)
                {
                    // Không còn ai để bắn và tường đã trong tầm: đứng lại bắn tường
                    idle = false;
                    attacking = true;
                    desired = 0f;
                    u.faceLeft = 1;
                    if (u.cooldown <= 0f)
                    {
                        shot = new float3(Cfg.WallX - 0.4f, u.pos.y, 2f);      // z = 2: mục tiêu là tường thành
                        u.cooldown = s.cooldown * (0.8f + 0.4f * Hash01((uint)i, frame));
                        u.animT = 0f;
                    }
                }
            }
            else if (team == 1 && u.pos.x <= Cfg.WallX + s.range + 0.5f + (myR - Cfg.RadiusSmall))
            {
                // Cận chiến đã áp sát tường: đứng đánh tường, mỗi đòn trừ 1 máu thành (không lao qua tự sát nữa).
                // Đã tới tường thì không quay ra đuổi lính nữa → quân ta không thể câu giờ mãi bằng cách ra lính liên tục.
                idle = false;
                attacking = true;
                desired = 0f;
                u.faceLeft = 1;
                if (u.cooldown <= 0f)
                {
                    atk = -2;                                                  // -2 = đánh tường
                    u.cooldown = s.cooldown * (0.8f + 0.4f * Hash01((uint)i, frame));
                    u.animT = 0f;
                }
            }
            else if ((s.flags & UFlag.Flank) != 0 && u.aux < 1f)
            {
                // Đánh vòng: thấy địch thì tạt ra mép bản đồ, chạy dọc mép cho tới khi địch gần nhất đã ở SAU lưng mình
                if (bestIdx >= 0 || u.hasGoal != 0)
                {
                    float2 g = bestIdx >= 0 ? bestPos : u.goal;
                    if (u.aux <= 0f) u.aux = 0.001f;
                    if ((g.x - u.pos.x) * fwd < -4f) u.aux = 1f;
                }
                if (u.aux > 0f)
                {
                    idle = false;
                    u.aux += dt / 25f;                                      // vòng lâu quá thì thôi, nhập trận luôn
                    if (u.pos.x * fwd > Cfg.HalfW - 10f) u.aux = 1f;        // đã tới sát mép bên kia
                    float edgeY = ((i & 1) == 0 ? 1f : -1f) * (Cfg.HalfH - 3f);
                    float dy = edgeY - u.pos.y;
                    desired = math.abs(dy) > 1.5f ? math.normalize(new float2(fwd * 0.5f, math.sign(dy))) : new float2(fwd, 0f);
                }
            }
            else
            {
                if (bestIdx >= 0)
                {
                    idle = false;
                    float dist = math.sqrt(bestD2);
                    float2 to = bestPos - u.pos;
                    float2 dir = to / math.max(dist, 1e-4f);
                    float reach = s.range + (myR - Cfg.RadiusSmall) + (RadiusOfInfo(bestInfo) - Cfg.RadiusSmall);   // tầm tính tới mép thân 2 bên
                    if (dist <= reach)
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
                    idle = false;
                    float2 to = u.goal - u.pos;
                    float dist = math.length(to);
                    if (dist > 1f) desired = to / dist;
                    else u.hasGoal = 0;
                }
            }

            // Quân ta không thấy địch thì dừng giữ trận tuyến, không kéo sang tận chỗ địch xuất hiện
            if (idle && team == 0 && u.pos.x > Cfg.HoldX) desired = 0f;

            // Địch còn ở ngoài mép phải: chỉ được đi thẳng vào, chưa đánh / bắn / né gì cả.
            // Đã vào trong rồi thì không bao giờ ra lại được (kẻo quân vừa bắn vừa lùi trốn ra ngoài, không ai đánh tới).
            bool outside = team == 1 && u.pos.x > Cfg.HalfW - 1f;
            if (outside)
            {
                desired = new float2(-1f, 0f);
                attacking = false; kite = false;
                atk = -1; shot = 0f;
            }

            // --- Di chuyển ---
            float speed = s.speed * (u.slow > 0f ? Fx.SlowFactor : 1f);
            float2 vel = desired * speed + sep * SepStrength;
            float maxV = speed * 1.6f;
            float vl = math.length(vel);
            if (vl > maxV) { vel *= maxV / vl; vl = maxV; }
            if (outside) vel.x = math.min(vel.x, -0.5f * speed);     // chen nhau cũng không bị đẩy ngược ra
            u.pos += vel * dt;
            u.pos.x = math.clamp(u.pos.x, team == 0 ? Cfg.WallX + 0.6f : Cfg.WallX + 0.5f,
                                 team == 0 ? Cfg.HalfW - 0.5f : outside ? Cfg.HalfW + 14f : Cfg.HalfW - 1f);
            u.pos.y = math.clamp(u.pos.y, -Cfg.HalfH + 0.6f, Cfg.HalfH - 0.6f);
            if (!attacking && math.abs(vel.x) > 0.2f) u.faceLeft = vel.x < 0f ? 1 : 0;

            u.state = attacking && !kite ? 1 : 0;
            u.animT += dt;
            u.cooldown -= dt;
            u.flash = math.max(0f, u.flash - dt * 8f);
            u.slow = math.max(0f, u.slow - dt);
            if (s.chargeBonus > 0f && !attacking && vl > speed * 0.5f)
                u.charge = math.min(1f, u.charge + dt * Fx.ChargeRate);

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
                int x0 = cx - r, x1 = cx + r, y0 = cy - r, y1 = cy + r;
                if (x0 < 0 && y0 < 0 && x1 >= Cfg.GridW && y1 >= Cfg.GridH) break;   // vòng này đã nằm ngoài bản đồ hoàn toàn
                int xa = math.max(x0, 0), xb = math.min(x1, Cfg.GridW - 1);
                for (int y = math.max(y0, 0); y <= math.min(y1, Cfg.GridH - 1); y++)
                {
                    if (y == y0 || y == y1)
                    {
                        for (int x = xa; x <= xb; x++) Scan(y * Cfg.GridW + x, enemyTeam, from, ref best, ref found, ref any);   // cạnh trên / dưới của vòng
                    }
                    else
                    {
                        if (x0 >= 0) Scan(y * Cfg.GridW + x0, enemyTeam, from, ref best, ref found, ref any);                    // 2 ô ở cạnh trái / phải
                        if (x1 < Cfg.GridW) Scan(y * Cfg.GridW + x1, enemyTeam, from, ref best, ref found, ref any);
                    }
                }
                if (any) return true;
            }
            return false;
        }

        void Scan(int c, int enemyTeam, float2 from, ref float best, ref float2 found, ref bool any)
        {
            if (teamCount[c * 2 + enemyTeam] == 0) return;      // ô không có địch: bỏ qua ngay
            int end = cellStart[c + 1];
            for (int k = cellStart[c]; k < end; k++)
            {
                int info = sortedInfo[k];
                if ((info & 1) != enemyTeam || (info & 16) != 0) continue;   // bit 16 = còn ở ngoài màn hình, chưa nhắm tới
                float d2 = math.lengthsq(sortedPos[k] - from);
                if (d2 < best) { best = d2; found = sortedPos[k]; any = true; }
            }
        }

        static float Hash01(uint a, uint b) => (math.hash(new uint2(a, b)) & 0xFFFFFFu) / 16777216f;
    }

    // =====================================================================
    // 3) Áp dụng đòn chém, tạo đạn, sóng âm, triệu hồi. Chạy 1 luồng (vài micro giây) để khỏi tranh chấp khi
    //    nhiều lính cùng chém 1 mục tiêu.
    // =====================================================================
    [BurstCompile(CompileSynchronously = true)]
    struct ResolveJob : IJob
    {
        public NativeArray<Unit> units;
        [ReadOnly] public NativeArray<UnitStats> stats;
        [ReadOnly] public NativeArray<int> attackTarget;
        [ReadOnly] public NativeArray<float3> shots;
        [ReadOnly] public NativeArray<int> cellStart;
        [ReadOnly] public NativeArray<int> sortedIndex;
        public NativeArray<Arrow> arrows;
        public NativeArray<Particle> particles;
        public NativeArray<int> counters;
        public uint seed;
        public int summonCount;

        public void Execute()
        {
            var rng = new Random(seed);
            int n = counters[C.Units];
            for (int i = 0; i < n; i++)
            {
                int t = attackTarget[i];
                if (t == -2)
                {
                    // Đánh tường: mỗi đòn đúng 1 máu thành, bất kể con gì đánh
                    Unit a = units[i];
                    counters[C.Breach] = counters[C.Breach] + 1;
                    float2 wallHit = new float2(Cfg.WallX, a.pos.y + 0.3f);
                    Fx.Spray(particles, counters, ref rng, wallHit, 0.6f, 3, PKind.Dust, 2.5f, 4f);
                    Fx.Spray(particles, counters, ref rng, wallHit, 0.8f, 1, PKind.Spark, 3f, 3f);
                    if (a.charge > 0f) { a.charge = 0f; units[i] = a; }
                }
                else if (t >= 0)
                {
                    Unit a = units[i];
                    UnitStats sa = stats[a.type];
                    Unit v = units[t];
                    UnitStats sv = stats[v.type];
                    float2 hit = math.lerp(a.pos, v.pos, 0.6f);
                    float mult = a.dmgMul * (1f + sa.chargeBonus * a.charge) * (a.buffed != 0 ? 1f + Fx.PowerBonus : 1f);
                    float dmg = sa.damage * mult * rng.NextFloat(0.75f, 1.25f);

                    if (sa.splash > 0f)
                    {
                        // đòn quét: mọi địch quanh mục tiêu đều dính
                        Fx.Splash(units, cellStart, sortedIndex, n, v.pos, sa.splash, 1 - a.team, dmg, sa.slow);
                        Fx.Spray(particles, counters, ref rng, hit, 0.5f, 3, sv.blood, 2.5f, 5f);
                    }
                    else if (rng.NextFloat() < sv.dodge)
                    {
                        Fx.Spray(particles, counters, ref rng, hit, 0.6f, 2, PKind.Spark, 3f, 3f);   // né được
                    }
                    else
                    {
                        v.hp -= dmg;
                        v.flash = 1f;
                        if (sa.slow > 0f) v.slow = math.max(v.slow, sa.slow);
                        float knock = sa.knock * (sa.chargeBonus > 0f ? a.charge : 1f);
                        if (knock > 0.2f)
                        {
                            float2 d = v.pos - a.pos;
                            v.pos += d / math.max(math.length(d), 1e-4f) * knock;   // hất văng
                            Fx.Spray(particles, counters, ref rng, hit, 0.2f, 3, PKind.Dust, 3f, 3f);
                        }
                        units[t] = v;
                        Fx.Spray(particles, counters, ref rng, hit, 0.5f, 2, sv.blood, 2.5f, 5f);
                        Fx.Spray(particles, counters, ref rng, hit, 0.6f, 1, PKind.Spark, 3f, 3f);
                    }

                    if (a.charge > 0f)   // đà xung phong luôn xả hết vào đòn này
                    {
                        a.charge = 0f;
                        units[i] = a;
                    }
                }

                float3 sh = shots[i];
                if (sh.z <= 0f) continue;
                {
                    Unit a = units[i];
                    UnitStats sa = stats[a.type];
                    float power = a.dmgMul * (a.buffed != 0 ? 1f + Fx.PowerBonus : 1f);
                    bool atWall = sh.z > 1.5f;

                    if (sa.mode == Mode.Blast && atWall)
                    {
                        counters[C.Breach] = counters[C.Breach] + 1;
                        Fx.Ring(particles, counters, sh.xy, 1.5f, 8, sa.blood);
                    }
                    else if (sa.mode == Mode.Blast)
                    {
                        Fx.Splash(units, cellStart, sortedIndex, n, sh.xy, sa.splash, 1 - a.team, sa.damage * power, sa.slow);
                        if (sa.blood == PKind.Fire)
                        {
                            // lửa rồng: cột lửa từ miệng tới đích + biển lửa ở đích
                            float2 mouth = a.pos + new float2(a.faceLeft != 0 ? -1.4f : 1.4f, 1.9f);   // miệng rồng 32x32, tính từ chân
                            for (int k = 1; k <= 4; k++) Fx.Flames(particles, counters, ref rng, math.lerp(mouth, sh.xy, k / 4f), 0.3f + 0.15f * k, 3, PKind.Fire, 0.5f);
                            Fx.Flames(particles, counters, ref rng, sh.xy, sa.splash * 0.6f, 16, PKind.Fire, 0.9f);
                            Fx.Flames(particles, counters, ref rng, sh.xy, sa.splash * 0.6f, 5, PKind.Smoke, 1.8f);
                        }
                        else Fx.Ring(particles, counters, sh.xy, sa.splash, 14, sa.blood);
                    }
                    else if (sa.mode == Mode.Summon)
                    {
                        float scale = a.maxHp / math.max(sa.maxHp, 1f);   // quân con thừa hưởng độ mạnh của chủ
                        for (int k = 0; k < summonCount; k++)
                        {
                            int m = counters[C.Units];
                            if (m >= units.Length) break;
                            float hp = stats[UnitType.Voidling].maxHp * scale;
                            units[m] = new Unit
                            {
                                pos = a.pos + rng.NextFloat2Direction() * rng.NextFloat(0.6f, 1.4f),
                                hp = hp, maxHp = hp, dmgMul = a.dmgMul, type = UnitType.Voidling, team = a.team,
                                faceLeft = a.team, animT = rng.NextFloat()
                            };
                            units[m] = Clamp(units[m]);
                            counters[C.Units] = m + 1;
                        }
                        Fx.Spray(particles, counters, ref rng, a.pos, 0.4f, 8, PKind.VoidGoo, 3f, 6f);
                    }
                    else
                    {
                        int count = counters[C.Arrows];
                        if (count >= arrows.Length) continue;
                        float speed = 22f, arcK = 0.22f, arc0 = 0.4f, scatter = 1.8f;
                        switch (sa.proj)
                        {
                            case Proj.Bolt: speed = 28f; arcK = 0.1f; scatter = 0.8f; break;
                            case Proj.Rock: speed = 15f; arcK = 0.35f; arc0 = 2f; scatter = 1.5f; break;
                            case Proj.Cannonball: speed = 30f; arcK = 0.1f; arc0 = 0.6f; scatter = 1.5f; break;
                            case Proj.Javelin: speed = 19f; arcK = 0.3f; arc0 = 1.5f; scatter = 1f; break;
                            case Proj.NatureOrb: case Proj.VoidOrb: case Proj.LightOrb: speed = 15f; arcK = 0.05f; arc0 = 0.8f; scatter = 0.8f; break;
                        }
                        float2 from = a.pos + new float2(a.faceLeft != 0 ? -0.3f : 0.3f, sa.liftPx / (float)Cfg.PPU);   // từ giữa thân, không phải từ chân
                        float2 to = atWall ? sh.xy + new float2(0f, rng.NextFloat(-0.8f, 0.8f)) : sh.xy + rng.NextFloat2Direction() * rng.NextFloat(0f, scatter);
                        float dist = math.max(math.distance(from, to), 1f);
                        arrows[count] = new Arrow
                        {
                            from = from, to = to, t = 0f,
                            invDuration = speed / dist,
                            arc = dist * arcK + arc0,
                            damage = sa.damage * power,
                            splash = sa.splash, slow = sa.slow,
                            team = a.team, kind = sa.proj, wall = atWall ? 1 : 0
                        };
                        counters[C.Arrows] = count + 1;
                    }
                }
            }
        }

        static Unit Clamp(Unit u)
        {
            u.pos.x = math.clamp(u.pos.x, Cfg.WallX + 0.6f, Cfg.HalfW - 0.5f);
            u.pos.y = math.clamp(u.pos.y, -Cfg.HalfH + 0.6f, Cfg.HalfH - 0.6f);
            return u;
        }
    }

    // =====================================================================
    // 4) Đạn bay: chỉ là tăng t. Vị trí tính bằng công thức parabol, không có vật lý.
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
    // 5) Đạn chạm đất: tra lưới xem trúng ai (hoặc nổ lan), rồi xoá viên đạn (đổi chỗ với phần tử cuối).
    // =====================================================================
    [BurstCompile(CompileSynchronously = true)]
    struct ArrowLandJob : IJob
    {
        const float HitRadius = 0.6f;

        public NativeArray<Arrow> arrows;
        public NativeArray<Unit> units;
        [ReadOnly] public NativeArray<UnitStats> stats;
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
            int nu = counters[C.Units];
            int i = 0;
            while (i < n)
            {
                Arrow a = arrows[i];
                if (a.t < 1f) { i++; continue; }

                if (a.wall != 0)
                {
                    counters[C.Breach] = counters[C.Breach] + 1;     // trúng tường: 1 máu thành
                    Fx.Spray(particles, counters, ref rng, a.to, 0.6f, 3, PKind.Dust, 2.5f, 4f);
                }
                else if (a.splash > 0f)
                {
                    Fx.Splash(units, cellStart, sortedIndex, nu, a.to, a.splash, 1 - a.team, a.damage * rng.NextFloat(0.85f, 1.15f), a.slow);
                    if (a.kind == Proj.Cannonball)
                    {
                        Fx.Flames(particles, counters, ref rng, a.to, a.splash * 0.6f, 22, PKind.Fire, 0.9f);
                        Fx.Flames(particles, counters, ref rng, a.to, a.splash * 0.6f, 8, PKind.Smoke, 2f);
                        Fx.Stamp(stamps, counters, a.to, StampCell.Scorch + rng.NextInt(0, 4), rng.NextInt(0, 8));
                    }
                    else if (a.kind == Proj.Rock)
                    {
                        Fx.Spray(particles, counters, ref rng, a.to, 0.2f, 14, PKind.Dust, 4f, 6f);
                        Fx.Stamp(stamps, counters, a.to, StampCell.Ash + rng.NextInt(0, 4), rng.NextInt(0, 8));
                    }
                    else Fx.Spray(particles, counters, ref rng, a.to, 0.3f, 10, a.kind == Proj.VoidOrb ? PKind.VoidGoo : PKind.GreenSpark, 3f, 4f);
                }
                else
                {
                    int victim = FindVictim(a.to, 1 - a.team, nu);
                    if (victim >= 0)
                    {
                        Unit v = units[victim];
                        UnitStats sv = stats[v.type];
                        if (rng.NextFloat() < sv.dodge)
                        {
                            Fx.Spray(particles, counters, ref rng, v.pos, 0.6f, 2, PKind.Spark, 3f, 3f);
                        }
                        else
                        {
                            float resist = Proj.Piercing(a.kind) ? sv.arrowResist : 0f;
                            v.hp -= a.damage * (1f - resist) * rng.NextFloat(0.8f, 1.2f);
                            v.flash = 1f;
                            if (resist > 0f) Fx.Spray(particles, counters, ref rng, v.pos, 0.6f, 2, PKind.Spark, 3f, 3f);   // tên dội ra khỏi vảy
                            if (a.slow > 0f) v.slow = math.max(v.slow, a.slow);
                            units[victim] = v;
                            Fx.Spray(particles, counters, ref rng, v.pos, 0.5f, 3, sv.blood, 2f, 6f);
                        }
                    }
                    else if (a.kind <= Proj.Bolt || a.kind == Proj.Javelin)
                    {
                        Fx.Spray(particles, counters, ref rng, a.to, 0f, 2, PKind.Dust, 1.5f, 3f);
                        if (rng.NextFloat() < 0.3f)
                        {
                            int cell = StampCell.StuckArrow + (a.kind == Proj.ElfArrow ? 2 : 0) + rng.NextInt(0, 2);
                            Fx.Stamp(stamps, counters, a.to, cell, rng.NextBool() ? 4 : 0);
                        }
                    }
                    else Fx.Spray(particles, counters, ref rng, a.to, 0.2f, 3, a.kind == Proj.VoidOrb ? PKind.VoidGoo : a.kind == Proj.LightOrb ? PKind.Holy : PKind.GreenSpark, 2f, 3f);
                }

                n--;
                arrows[i] = arrows[n];
            }
            counters[C.Arrows] = n;
        }

        int FindVictim(float2 p, int enemyTeam, int unitCount)
        {
            int cx = Cfg.CellX(p.x), cy = Cfg.CellY(p.y);
            float best = 1f;                                   // điểm = (khoảng cách / tầm trúng)²; < 1 là trúng
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
                        if (j >= unitCount) continue;
                        Unit u = units[j];
                        if (u.team != enemyTeam || u.hp <= 0f) continue;
                        float r = HitRadius + stats[u.type].radius - Cfg.RadiusSmall;   // thân to thì dễ trúng hơn
                        float score = math.lengthsq(u.pos - p) / (r * r);
                        if (score < best) { best = score; bi = j; }
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
        [ReadOnly] public NativeArray<UnitStats> stats;
        [ReadOnly] public NativeArray<int> cellStart;
        [ReadOnly] public NativeArray<int> sortedIndex;
        public NativeArray<Particle> particles;
        public NativeArray<float4> stamps;
        public NativeArray<int> counters;
        public uint seed;

        public void Execute()
        {
            var rng = new Random(seed);
            int n = counters[C.Units];

            // Nổ khi chết (Kẻ dị dạng). Làm TRƯỚC khi xoá để chỉ số trong lưới còn đúng.
            for (int i = 0; i < n; i++)
            {
                Unit u = units[i];
                if (u.hp > 0f) continue;
                UnitStats s = stats[u.type];
                if (s.explodeDmg <= 0f) continue;
                Fx.Splash(units, cellStart, sortedIndex, n, u.pos, s.explodeRadius, 1 - u.team, s.explodeDmg * u.dmgMul, 0f);
                Fx.Ring(particles, counters, u.pos, s.explodeRadius, 8, PKind.VoidGoo);
            }

            int w = 0;
            while (w < n)
            {
                Unit u = units[w];
                if (u.hp > 0f) { w++; continue; }

                {
                    int blood = stats[u.type].blood;
                    counters[C.Dead0 + u.team] = counters[C.Dead0 + u.team] + 1;
                    if (blood == PKind.Blood || blood == PKind.DarkBlood || blood == PKind.VoidGoo)
                    {
                        int splat = blood == PKind.Blood ? StampCell.BloodRed : StampCell.BloodDark;
                        Fx.Stamp(stamps, counters, u.pos + rng.NextFloat2(-0.3f, 0.3f), splat + rng.NextInt(0, 4), rng.NextInt(0, 8));
                    }
                    Fx.Stamp(stamps, counters, u.pos, StampCell.Corpse + u.type * 2 + rng.NextInt(0, 2), rng.NextBool() ? 4 : 0);
                    Fx.Spray(particles, counters, ref rng, u.pos, 0.6f, 7, blood, 3f, 7f);
                }

                n--;
                units[w] = units[n];
            }
            counters[C.Units] = n;

            counters[C.Team0] = 0;
            counters[C.Team1] = 0;
            for (int t = 0; t < UnitType.Count; t++) counters[C.Type0 + t] = 0;
            for (int k = 0; k < n; k++)
            {
                Unit u = units[k];
                counters[C.Type0 + u.type] = counters[C.Type0 + u.type] + 1;
                counters[C.Team0 + u.team] = counters[C.Team0 + u.team] + 1;
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
        [ReadOnly] public NativeArray<UnitStats> stats;
        [ReadOnly] public NativeArray<int> counters;
        public NativeArray<float4> gpu;

        public void Execute(int i)
        {
            if (i >= counters[C.Units]) return;
            Unit u = units[i];
            int frame;
            if (u.state == 0) frame = (int)(u.animT * stats[u.type].animFps) & 3;
            else frame = u.animT < 0.4f ? math.min(3, (int)(u.animT * 10f)) : 0;

            // bit 4.. = số pixel nâng hình lên để chân chạm đúng vị trí
            int flags = u.faceLeft | (u.flash > 0.25f ? 2 : 0) | (u.slow > 0f ? 4 : 0) | (stats[u.type].liftPx << 4);
            if ((stats[u.type].flags & UFlag.Giant) != 0)
            {
                // quân khổng lồ 32x32: mỗi khung chiếm 2x2 ô, 2 khung / 2 hàng atlas, mỗi trạng thái 4 hàng
                int grow = u.type * 2 + u.state * 4 + (frame >> 1) * 2;
                gpu[i] = new float4(u.pos, grow * 4 + (frame & 1) * 2, flags | 8);
            }
            else gpu[i] = new float4(u.pos, (u.type * 2 + u.state) * 4 + frame, flags);
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
                a = new float4(g, h, a.kind),
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
                case PKind.VoidGoo: color = 0; break;
                case PKind.Leaf: color = 3; break;
                case PKind.Splinter: color = 5; break;
                case PKind.Holy: color = 7; break;
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
            float mid = worldX - Cfg.HoldX;                               // trận tuyến thường nằm quanh vạch giữ trận
            n += 0.16f * math.exp(-(mid * mid) / (2f * 14f * 14f));       // chỗ giao tranh bị giẫm nát, nhiều đất hơn
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
