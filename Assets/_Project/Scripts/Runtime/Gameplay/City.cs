using System.Collections.Generic;
using UnityEngine;

namespace HordeLab
{
    /// <summary>Một công trình đang đứng trên lưới thành.</summary>
    public class Building
    {
        public int def;
        public int stars = 1;
        public float timer;          // tiến độ chu kỳ hiện tại (giây)
        public bool waiting;         // nhà lính đã hồi xong nhưng kho chưa đủ tài nguyên
        public float lastYield;      // sản lượng lần gần nhất (để hiện lên giao diện)
        public float pulse;          // nháy sáng khi vừa sinh tài nguyên / gọi quân

        // Hệ số sau khi tính buff + kề nhau + đặc trưng phe (City.Recompute)
        public float speed = 1f, amount = 1f, flat, costMul = 1f, dmg = 1f, hp = 1f, regen, grow = 1f;
        public bool doubled;         // Chuồng gia súc đứng cạnh Đồng lúa
        public int edges;            // Lều săn: số cạnh giáp vùng chưa mở
        public int around;           // Chợ: số công trình trong 8 ô xung quanh

        // biến tạm lúc cộng dồn buff
        internal float speedB, amountB, costCut, dmgB, hpB, growB;

        public BuildingDef Def => GameData.Buildings[def];
    }

    /// <summary>
    /// Lưới thành + kho tài nguyên. Mỗi công trình chiếm đúng 1 ô; lưới bắt đầu 3x3 và mọc thêm theo ô người chơi chọn.
    /// Toạ độ ô: x sang phải, y xuống dưới ("ô bên dưới" = y + 1).
    /// </summary>
    public class City
    {
        public readonly Dictionary<Vector2Int, Building> cells = new Dictionary<Vector2Int, Building>();   // có khoá = ô đã mở, null = ô trống
        public Res res;
        public int faction;
        public readonly List<int> augments = new List<int>();   // lõi đã chọn trong lượt chơi này
        public bool Has(int aug) => augments.Contains(aug);
        public float battleTime;

        static readonly Vector2Int[] Dir4 = { new Vector2Int(1, 0), new Vector2Int(-1, 0), new Vector2Int(0, 1), new Vector2Int(0, -1) };
        static readonly Vector2Int[] Dir8 =
        {
            new Vector2Int(1, 0), new Vector2Int(-1, 0), new Vector2Int(0, 1), new Vector2Int(0, -1),
            new Vector2Int(1, 1), new Vector2Int(-1, 1), new Vector2Int(1, -1), new Vector2Int(-1, -1)
        };
        readonly List<Vector2Int> scratch = new List<Vector2Int>();
        readonly List<Vector2Int> keys = new List<Vector2Int>();

        public void Init(int faction)
        {
            this.faction = faction;
            cells.Clear();
            augments.Clear();
            for (int y = 0; y < Tuning.StartGridSide; y++)
                for (int x = 0; x < Tuning.StartGridSide; x++)
                    cells[new Vector2Int(x, y)] = null;
            res = Tuning.StartRes;
            battleTime = 0f;
        }

        public bool IsOpen(Vector2Int p) => cells.ContainsKey(p);
        public Building Get(Vector2Int p) => cells.TryGetValue(p, out var b) ? b : null;

        public void Bounds(out Vector2Int min, out Vector2Int max)
        {
            min = new Vector2Int(int.MaxValue, int.MaxValue);
            max = new Vector2Int(int.MinValue, int.MinValue);
            foreach (var p in cells.Keys) { min = Vector2Int.Min(min, p); max = Vector2Int.Max(max, p); }
        }

        public int EmptyCount()
        {
            int n = 0;
            foreach (var b in cells.Values) if (b == null) n++;
            return n;
        }

        // ------------------------------------------------------------------
        // Sửa lưới (chỉ giữa các đợt)
        // ------------------------------------------------------------------
        public bool CanExpand(Vector2Int p)
        {
            if (IsOpen(p)) return false;
            // không vượt vùng 7x7: tính khung bao của lưới nếu mở thêm ô p
            Bounds(out var min, out var max);
            min = Vector2Int.Min(min, p); max = Vector2Int.Max(max, p);
            if (max.x - min.x + 1 > Tuning.MaxGridSide || max.y - min.y + 1 > Tuning.MaxGridSide) return false;
            var dirs = Tuning.DiagonalAdjacency ? Dir8 : Dir4;
            foreach (var d in dirs) if (IsOpen(p + d)) return true;
            return false;
        }

        public bool Expand(Vector2Int p)
        {
            if (!CanExpand(p)) return false;
            cells[p] = null;
            Recompute();
            return true;
        }

        public bool CanPlace(Vector2Int p) => IsOpen(p) && cells[p] == null;

        public bool Place(Vector2Int p, int def, int stars = 1)
        {
            if (!CanPlace(p)) return false;
            cells[p] = new Building { def = def, stars = Mathf.Clamp(stars, 1, Tuning.MaxStars) };
            Recompute();
            return true;
        }

        /// <summary>Gỡ công trình khỏi ô (bán đi hoặc lấy lại lên tay). Trả về công trình vừa gỡ.</summary>
        public Building Remove(Vector2Int p)
        {
            var b = Get(p);
            if (b == null) return null;
            cells[p] = null;
            Recompute();
            return b;
        }

        /// <summary>Gộp được khi cùng loại, cùng số sao và chưa tới 4 sao.</summary>
        public bool CanMerge(Vector2Int target, int def, int stars)
        {
            var b = Get(target);
            return b != null && b.def == def && b.stars == stars && b.stars < Tuning.MaxStars;
        }

        /// <summary>Gộp thẻ trên tay (có <paramref name="stars"/> sao) vào nhà cùng loại, cùng sao.</summary>
        public bool MergeCard(Vector2Int target, int def, int stars = 1)
        {
            if (!Tuning.MergeFromHand || !CanMerge(target, def, stars)) return false;
            cells[target].stars++;
            Recompute();
            return true;
        }

        /// <summary>Kéo công trình từ ô này sang ô khác: gộp nếu được, không thì đổi chỗ.</summary>
        public bool MoveOrMerge(Vector2Int from, Vector2Int to)
        {
            if (from == to || !IsOpen(to)) return false;
            var a = Get(from);
            if (a == null) return false;
            var b = Get(to);
            bool adjacentOk = !Tuning.MergeNeedsAdjacent || Mathf.Abs(from.x - to.x) + Mathf.Abs(from.y - to.y) == 1;
            if (b != null && adjacentOk && CanMerge(to, a.def, a.stars))
            {
                b.stars++;
                cells[from] = null;
            }
            else
            {
                cells[from] = b;
                cells[to] = a;
            }
            Recompute();
            return true;
        }

        // ------------------------------------------------------------------
        // Buff
        // ------------------------------------------------------------------
        /// <summary>Các ô nằm trong vùng buff của công trình đặt ở <paramref name="p"/> (chỉ tính ô đã mở).</summary>
        public List<Vector2Int> BuffCells(Vector2Int p, Shape shape, List<Vector2Int> into = null)
        {
            into ??= new List<Vector2Int>();
            into.Clear();
            switch (shape)
            {
                case Shape.Cross: foreach (var d in Dir4) if (IsOpen(p + d)) into.Add(p + d); break;
                case Shape.Ring: foreach (var d in Dir8) if (IsOpen(p + d)) into.Add(p + d); break;
                case Shape.Below: if (IsOpen(p + Vector2Int.up)) into.Add(p + Vector2Int.up); break;   // y + 1 = bên dưới
                case Shape.Row: foreach (var q in cells.Keys) if (q.y == p.y && q.x != p.x) into.Add(q); break;
            }
            return into;
        }

        static bool IsFoodHouse(int def) => def == B.Field || def == B.Barn || def == B.HunterLodge;

        /// <summary>Tính lại mọi hệ số: buff theo vùng (cộng dồn, có trần), kề nhau, đặc trưng phe.</summary>
        public void Recompute()
        {
            foreach (var b in cells.Values)
            {
                if (b == null) continue;
                b.speedB = b.amountB = b.costCut = b.dmgB = b.hpB = b.growB = 0f;
                b.flat = b.regen = 0f;
                b.doubled = false;
                b.edges = b.around = 0;
            }

            foreach (var kv in cells)
            {
                var src = kv.Value;
                if (src == null) continue;
                var p = kv.Key;
                float k = Tuning.StarBuff[src.stars - 1] * (Has(Aug.Architect) ? 1f + Aug.ArchitectBuff : 1f);

                // Vùng buff
                if (src.Def.shape != Shape.None)
                {
                    foreach (var q in BuffCells(p, src.Def.shape, scratch))
                    {
                        var t = Get(q);
                        if (t == null) continue;
                        Cat cat = t.Def.cat;
                        switch (src.def)
                        {
                            case B.Sawmill: if (t.def == B.LumberCamp) t.speedB += 0.5f * k; break;
                            case B.Windmill: if (t.def == B.Field) t.amountB += 0.5f * k; break;
                            case B.Well: if (IsFoodHouse(t.def)) t.amountB += 0.15f * k; break;
                            case B.RangerHut: if (t.def == B.Nursery) t.growB += 1f * k; break;
                            case B.Mint: if (t.def == B.GoldMine) t.speedB += 0.4f * k; break;
                            case B.TradeRoad: if (t.def == B.Market) t.flat += 3f * k; break;
                            case B.Plaza: if (cat == Cat.Pop) t.speedB += 0.25f * k; break;
                            case B.Forge: if (cat == Cat.Barracks) t.dmgB += 0.6f * k; break;
                            case B.TrainingGround: if (cat == Cat.Barracks) t.speedB += 0.25f * k; break;
                            case B.Depot: if (cat == Cat.Barracks) t.costCut += 0.2f * k; break;
                            case B.Banner: if (cat == Cat.Barracks) t.hpB += 0.2f * k; break;
                            case B.Infirmary: if (cat == Cat.Barracks) t.regen += 1f * k; break;
                        }
                    }
                }

                // Kề nhau
                foreach (var d in Dir4)
                {
                    var t = Get(p + d);
                    if (!IsOpen(p + d)) { src.edges++; continue; }
                    if (t == null) continue;
                    if (src.def == B.Field && t.def == B.Field) src.amountB += 0.15f;
                    if (src.def == B.Barn && t.def == B.Field) src.doubled = true;
                    if (src.def == B.Residential) t.speedB -= 0.2f;          // ồn: làm chậm nhà bên cạnh
                }
                foreach (var d in Dir8) if (Get(p + d) != null) src.around++;
            }

            // Chốt: áp trần cộng dồn + đặc trưng phe
            foreach (var b in cells.Values)
            {
                if (b == null) continue;
                Cat cat = b.Def.cat;
                float f = 1f;
                if (faction == Faction.Human && cat == Cat.Pop) f = 1.25f;
                if (faction == Faction.Beast && cat == Cat.Food) f = 1.3f;
                if (faction == Faction.Beast && cat == Cat.Pop) f = 0.75f;
                if (faction == Faction.Dwarf && b.def == B.GoldMine) f = 1.3f;
                if (faction == Faction.Dwarf && cat == Cat.Barracks) f = 1f / 1.2f;
                if (cat == Cat.Barracks && Has(Aug.Drill)) f *= 1f + Aug.DrillSpeed;

                b.speed = Mathf.Max(0.4f, 1f + Mathf.Min(b.speedB, Tuning.BuffCap)) * f;
                b.amount = 1f + Mathf.Min(b.amountB, Tuning.BuffCap);
                b.grow = 1f + Mathf.Min(b.growB, Tuning.BuffCap * 2f);
                b.dmg = 1f + Mathf.Min(b.dmgB, Tuning.BuffCap);
                b.hp = 1f + Mathf.Min(b.hpB, Tuning.BuffCap);
                b.costMul = 1f - Mathf.Min(b.costCut, Tuning.CostCutCap);
                b.regen = Mathf.Min(b.regen, 5f);
            }
        }

        // ------------------------------------------------------------------
        // Sản xuất & gọi quân (chạy trong trận)
        // ------------------------------------------------------------------
        /// <summary>Hệ số tài nguyên từ lõi (Bountiful Harvest, Timber Boom, Gold Rush, Baby Boom).</summary>
        public float ResMul(int r) =>
            (r == ResId.Food && Has(Aug.Harvest) || r == ResId.Wood && Has(Aug.Timber) ||
            r == ResId.Gold && Has(Aug.GoldRush) || r == ResId.Pop && Has(Aug.BabyBoom) ? 1f + Aug.ResBonus : 1f) * (Has(Aug.GoldenAge) ? Aug.GoldenAgeMul : 1f);

        /// <summary>Sản lượng mỗi chu kỳ của một nhà tài nguyên, đã tính buff và lõi.</summary>
        public float Yield(Building b) => YieldBase(b) * (b.Def.res >= 0 ? ResMul(b.Def.res) : 1f);

        float YieldBase(Building b)
        {
            float star = Tuning.StarYield[b.stars - 1];
            var d = b.Def;
            switch (b.def)
            {
                case B.Barn: return d.amount * star * b.amount * (b.doubled ? 2f : 1f);
                case B.HunterLodge: return d.amount * star * b.amount * b.edges;
                case B.Nursery: return Mathf.Min(10f, d.amount + battleTime / 20f * b.grow) * star;
                case B.Market: return (b.around + b.flat) * star;
                case B.TownHall: return res.pop * d.amount * star;
                default: return d.period > 0f ? d.amount * star * b.amount : 0f;
            }
        }

        /// <summary>Mỗi lần gọi luôn đúng 1 tiểu đội, bất kể nhà mấy sao.</summary>
        public int Squads(Building b) => Has(Aug.Legion) ? Aug.LegionSquads : 1;

        /// <summary>Hệ số máu + sát thương của lính theo số sao của nhà lính.</summary>
        public float TroopPower(Building b) => Tuning.StarTroopPower[b.stars - 1];

        /// <summary>Tài nguyên cho 1 lần gọi quân của nhà lính (đã nhân số tiểu đội, Kho quân nhu, đặc trưng phe).</summary>
        public Res CallCost(Building b)
        {
            Res c = GameData.Units[b.Def.unit].cost * (Tuning.StarTroopCost[b.stars - 1] * b.costMul * (Has(Aug.Supply) ? 1f - Aug.SupplyCut : 1f));
            c.pop *= GameData.Factions[faction].popCostMul;
            return c;
        }

        public void BeginBattle()
        {
            battleTime = 0f;
            Recompute();
            foreach (var b in cells.Values)
            {
                if (b == null) continue;
                b.timer = 0f;
                b.waiting = false;
                b.pulse = 0f;
            }
        }

        /// <summary>1 bước kinh tế. <paramref name="onCall"/> được gọi mỗi khi một nhà lính gọi quân thành công.</summary>
        public void Tick(float dt, bool canCall, System.Action<Building> onCall)
        {
            battleTime += dt;
            keys.Clear();
            keys.AddRange(cells.Keys);
            foreach (var p in keys)
            {
                var b = cells[p];
                if (b == null) continue;
                var d = b.Def;
                b.pulse = Mathf.Max(0f, b.pulse - dt * 2.5f);

                if (d.cat == Cat.Barracks)
                {
                    b.timer = Mathf.Min(d.period, b.timer + dt * b.speed);
                    if (b.timer < d.period) continue;
                    Res cost = CallCost(b);
                    if (canCall && res.Covers(cost))      // thiếu tài nguyên thì đứng chờ
                    {
                        res.Pay(cost);
                        b.timer = 0f;
                        b.waiting = false;
                        b.pulse = 1f;
                        onCall(b);
                    }
                    else b.waiting = true;
                }
                else if (d.period > 0f)
                {
                    b.timer += dt * b.speed;
                    if (b.timer < d.period) continue;
                    if (b.def == B.Tavern)
                    {
                        float food = 2f * Tuning.StarYield[b.stars - 1];
                        if (res.food < food) { b.timer = d.period; b.waiting = true; continue; }
                        res.food -= food;
                        b.waiting = false;
                    }
                    b.timer -= d.period;
                    float y = Yield(b);
                    res[d.res] += y;
                    b.lastYield = y;
                    if (y > 0f) b.pulse = 1f;
                }
            }
        }
    }
}
