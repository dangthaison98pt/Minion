using UnityEngine;

namespace HordeLab
{
    /// <summary>
    /// MỌI CON SỐ TẠM nằm ở đây. GDD ghi "các con số sẽ cân bằng sau" và còn một mục "Chưa chốt",
    /// nên những gì GDD chưa quyết đều được gom về một chỗ để dễ chỉnh.
    /// </summary>
    public static class Tuning
    {
        // ---- Lượt chơi (đã chốt trong GDD) ----
        public const int Hearts = 3;
        public const int Waves = 30;
        public const int BossEvery = 6;                 // đợt trùm: 6, 12, 18, 24, 30
        public const int StartHostile = 3, MinHostile = 2, MaxHostile = 4;
        public const int StartGridSide = 3;             // lưới 3x3
        public const int MaxStars = 4;
        public const int RewardChoices = 3;
        public const int FullMoonEvery = 3;             // Người hoá thú: cứ 3 đợt có 1 đợt trăng tròn

        // ---- Chưa chốt trong GDD → số tạm ----
        public const bool DiagonalAdjacency = false;    // "sát cạnh" tạm tính 4 hướng
        public const int HandLimit = 10;
        public const bool MergeFromHand = true;         // kéo thẻ từ tay lên công trình cùng loại, cùng sao để gộp
        public const bool MergeNeedsAdjacent = false;
        public const int ShopSlots = 4;
        public const int RollCost = 1;
        public const float EventChance = 0.25f;         // xác suất có sự kiện đổi phe thù địch sau mỗi đợt
        public const int StartCrystals = 5;             // đủ mua 1 thẻ ngay đợt đầu
        public static int CrystalReward(int wave) => 6 + wave / 2;   // đợt 1: 6, đợt 10: 11, đợt 20: 16, đợt 30: 21
        public static int CardPrice(Cat c) => c == Cat.Expand ? 5 : c == Cat.Barracks || c == Cat.BuffMil ? 4 : 3;

        // ---- Thành & tài nguyên ----
        public const int CastleHp = 300;                // mỗi đòn địch đánh trúng tường trừ 1 máu
        public const int SellValue = 1;                 // bán 1 thẻ / công trình 1 sao được ngần này Tinh thể (nhà nhiều sao nhân theo số nhà đã gộp)
        public static readonly Res StartRes = new Res(30, 30, 30, 30);
        public static readonly float[] StarYield = { 1f, 2f, 4f, 8f };      // sản lượng nhà tài nguyên theo sao
        // Nhà lính lên sao KHÔNG gọi thêm quân (luôn 1 tiểu đội mỗi lần): lính mạnh hơn, mỗi lần gọi đắt hơn một chút
        public static readonly float[] StarTroopPower = { 1f, 1.4f, 1.8f, 2.2f };   // hệ số máu + sát thương của lính
        public static readonly float[] StarTroopCost = { 1f, 1.5f, 2f, 2.5f };      // hệ số chi phí mỗi lần gọi
        public static readonly float[] StarBuff = { 1f, 1.5f, 2f, 2.5f };   // độ mạnh buff theo sao
        public const float BuffCap = 1.5f;              // trần cộng dồn: tối đa +150%
        public const float CostCutCap = 0.6f;           // Kho quân nhu giảm tối đa 60%
        public const int PlayerUnitCap = 4000;              // chiến trường nhỏ: quá ngần này thì nhà lính tạm ngừng gọi
        public const float SpeedScale = 0.6f;           // tốc độ đi cũng nhân theo, để địch không tới tường quá nhanh
        public const float RangeScale = 0.6f;           // tầm bắn nhân với hệ số này cho vừa chiến trường 96 x 54

        // ---- Đợt địch ----
        public static int EnemySquads(int wave) => Mathf.RoundToInt(5f + 2.2f * wave + 0.16f * wave * wave);
        public static float WaveDuration(int wave) => 45f + 2f * wave;       // quân địch kéo đến rải đều trong ngần này giây
        public static float EnemyPower(int wave) => 1f + 0.04f * (wave - 1); // hệ số máu + sát thương của địch
        public const float BossSize = 1.6f, BossPower = 1.25f;               // trùm chưa thiết kế → tạm là đợt đông và mạnh hơn
        public const float FullMoonPower = 1.4f;
        public const float WaveTimeout = 150f;          // quá ngần này giây sau khi địch đến hết mà chưa xong thì coi như địch rút
    }

    public static class ResId
    {
        public const int Pop = 0, Food = 1, Wood = 2, Gold = 3, Count = 4;
        public static readonly string[] Name = { "Pop", "Food", "Wood", "Gold" };
        public static readonly string[] Hint =
        {
            "Needed for every troop call.", "Paid by many melee troops.", "Paid by many ranged troops.", "Paid by elite and siege troops."
        };
        public static readonly Color[] Tint =
        {
            new Color(0.56f, 0.72f, 1f), new Color(0.95f, 0.82f, 0.36f), new Color(0.72f, 0.52f, 0.30f), new Color(1f, 0.82f, 0.22f)
        };
    }

    /// <summary>4 tài nguyên trong trận.</summary>
    public struct Res
    {
        public float pop, food, wood, gold;
        public Res(float pop, float food, float wood, float gold) { this.pop = pop; this.food = food; this.wood = wood; this.gold = gold; }

        public float this[int i]
        {
            get => i == 0 ? pop : i == 1 ? food : i == 2 ? wood : gold;
            set { if (i == 0) pop = value; else if (i == 1) food = value; else if (i == 2) wood = value; else gold = value; }
        }

        public static Res operator *(Res a, float k) => new Res(a.pop * k, a.food * k, a.wood * k, a.gold * k);
        public bool Covers(Res c) => pop >= c.pop - 0.001f && food >= c.food - 0.001f && wood >= c.wood - 0.001f && gold >= c.gold - 0.001f;
        public void Pay(Res c) { pop -= c.pop; food -= c.food; wood -= c.wood; gold -= c.gold; }

        public string Describe()
        {
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < ResId.Count; i++)
            {
                if (this[i] <= 0.001f) continue;
                if (sb.Length > 0) sb.Append(" + ");
                sb.Append(this[i].ToString("0.#")).Append(' ').Append(ResId.Name[i]);
            }
            return sb.Length == 0 ? "free" : sb.ToString();
        }
    }

    public class FactionDef
    {
        public string name, strength, weakness;
        public Color color;
        public float hpMul = 1f, dmgMul = 1f, speedMul = 1f, rangeMul = 1f;
        public int squadSide = 5;          // mỗi lần gọi ra squadSide x squadSide lính
        public float popCostMul = 1f;
    }

    public class UnitDef
    {
        public string name, role, house;
        public UnitStats stats;
        public Res cost;                   // tài nguyên mỗi lần gọi 1 tiểu đội
        public float callTime;             // thời gian hồi của nhà lính
        public int minWave = 1;            // khi làm địch: xuất hiện từ đợt này
        public float waveWeight = 1f;      // khi làm địch: tỉ lệ trong đội hình
    }

    public enum Cat { Food, Wood, Gold, Pop, BuffEco, BuffMil, Barracks, Expand }
    public enum Shape { None, Cross, Ring, Below, Row }

    public class BuildingDef
    {
        public int id;
        public string name, desc;
        public Cat cat;
        public Shape shape = Shape.None;
        public float period;               // chu kỳ sinh (giây); 0 = không tự sinh
        public float amount;               // sản lượng mỗi chu kỳ ở 1 sao
        public int res = -1;               // tài nguyên sinh ra
        public int unit = -1;              // nhà lính: loại quân
        public int faction = -1;           // -1 = dùng chung
    }

    /// <summary>Mã công trình. Nhà lính = Barracks + loại quân.</summary>
    public static class B
    {
        public const int Field = 0, Barn = 1, HunterLodge = 2;
        public const int LumberCamp = 3, Sawmill = 4, Nursery = 5;
        public const int GoldMine = 6, Market = 7, TownHall = 8;
        public const int Cabin = 9, Residential = 10, Tavern = 11;
        public const int Windmill = 12, Well = 13, RangerHut = 14, Mint = 15, TradeRoad = 16, Plaza = 17;
        public const int Forge = 18, TrainingGround = 19, Depot = 20, Banner = 21, Infirmary = 22;
        public const int Barracks = 23;                         // 23..47
        public const int Expand = Barracks + UnitType.Buildable; // thẻ mở thêm ô trống
        public const int Count = Expand + 1;
    }

    public static class GameData
    {
        public static readonly FactionDef[] Factions =
        {
            new FactionDef { name = "Human", color = new Color(0.36f, 0.56f, 0.95f), hpMul = 0.9f,
                strength = "Houses produce Pop 25% faster", weakness = "Frail: troops have 10% less HP" },
            new FactionDef { name = "Elf", color = new Color(0.40f, 0.80f, 0.45f), hpMul = 1.5f, dmgMul = 1.5f, rangeMul = 1.15f, squadSide = 4,
                strength = "Elite troops: 50% higher stats and longer range", weakness = "Each call brings only 16 troops (4x4)" },
            new FactionDef { name = "Dwarf", color = new Color(0.90f, 0.62f, 0.25f), speedMul = 0.8f,
                strength = "Gold Mines dig 30% faster", weakness = "Troops move 20% slower and troop buildings call 20% slower" },
            new FactionDef { name = "Beastfolk", color = new Color(0.78f, 0.50f, 0.34f),
                strength = "Food buildings work 30% faster. Full moon (every 3rd wave): troops are much stronger", weakness = "Houses produce Pop 25% slower" },
            new FactionDef { name = "The Void", color = new Color(0.68f, 0.40f, 0.92f), hpMul = 1.3f, dmgMul = 1.3f, popCostMul = 2f,
                strength = "Troops are 30% stronger", weakness = "Every call costs double Pop" },
        };

        public static readonly UnitDef[] Units = BuildUnits();
        public static readonly BuildingDef[] Buildings = BuildBuildings();

        static UnitStats S(float hp, float dmg, float cd, float range, float speed, int mode = Mode.Melee, int flags = 0,
                           int proj = 0, int blood = PKind.Blood, float fps = 9f)
        {
            return new UnitStats { maxHp = hp, damage = dmg, cooldown = cd, range = range, speed = speed, mode = mode, flags = flags, proj = proj, blood = blood, animFps = fps };
        }

        static UnitDef U(string house, string name, string role, UnitStats s, Res cost, float callTime, int minWave, float weight)
        {
            return new UnitDef { house = house, name = name, role = role, stats = s, cost = cost, callTime = callTime, minWave = minWave, waveWeight = weight };
        }

        static UnitDef[] BuildUnits()
        {
            var u = new UnitDef[UnitType.Count];
            UnitStats s;

            // ---------------- Người ----------------
            u[UnitType.Swordsman] = U("Barracks", "Shield Swordsman", "Frontline", S(44, 7, 0.9f, 1.0f, 3.0f), new Res(5, 5, 0, 0), 8f, 1, 5f);
            u[UnitType.Archer] = U("Archery Range", "Archer", "Ranged", S(18, 6, 1.7f, 24f, 2.8f, Mode.Shoot, 0, Proj.Arrow), new Res(5, 0, 5, 0), 9f, 1, 3f);
            s = S(75, 9, 1.0f, 1.2f, 5.5f, Mode.Melee, UFlag.Big, 0, PKind.Blood, 12f); s.chargeBonus = 2f; s.knock = 0.9f;
            u[UnitType.Cavalry] = U("Stable", "Cavalry", "Mobile (charge)", s, new Res(6, 6, 0, 4), 12f, 3, 1.5f);
            u[UnitType.Monk] = U("Monastery", "Monk", "Support (heals)", S(20, 2, 2.0f, 7f, 2.8f, Mode.Shoot, UFlag.HealAura, Proj.LightOrb), new Res(5, 0, 0, 5), 14f, 5, 1f);
            s = S(60, 12, 7.0f, 34f, 1.6f, Mode.Shoot, UFlag.Big, Proj.Rock, PKind.Splinter, 6f); s.splash = 1.8f; s.minRange = 6f;
            u[UnitType.Catapult] = U("Siege Workshop", "Catapult", "Area damage", s, new Res(8, 0, 14, 12), 36f, 7, 0.5f);

            // ---------------- Tiên ----------------
            u[UnitType.Treant] = U("Sacred Grove", "Treant", "Frontline (very tanky, slow)", S(110, 9, 1.4f, 1.2f, 1.7f, Mode.Melee, UFlag.Big, 0, PKind.Leaf, 5f), new Res(6, 0, 12, 0), 14f, 1, 4f);
            u[UnitType.ForestArcher] = U("Bow Academy", "Forest Archer", "Ranged (longest range)", S(16, 6, 1.6f, 28f, 2.9f, Mode.Shoot, 0, Proj.ElfArrow), new Res(5, 0, 6, 0), 10f, 1, 3f);
            s = S(30, 8, 0.6f, 1.0f, 3.6f, Mode.Melee, 0, 0, PKind.Blood, 11f); s.dodge = 0.4f;
            u[UnitType.BladeDancer] = U("Blade Court", "Blade Dancer", "Striker (dodges)", s, new Res(5, 6, 0, 0), 9f, 3, 2f);
            s = S(18, 3, 2.2f, 18f, 2.7f, Mode.Shoot, 0, Proj.NatureOrb); s.splash = 2f; s.slow = 2.5f;
            u[UnitType.Druid] = U("Moon Temple", "Druid", "Support (slows enemies)", s, new Res(5, 0, 0, 6), 14f, 5, 1f);
            u[UnitType.DeerRider] = U("Deer Glade", "Deer Rider", "Mobile (shoots on the move)", S(45, 6, 1.5f, 14f, 5.0f, Mode.Shoot, UFlag.Big | UFlag.Kite, Proj.ElfArrow, PKind.Blood, 12f), new Res(6, 5, 5, 0), 13f, 4, 1.5f);

            // ---------------- Người lùn ----------------
            u[UnitType.ShieldWarrior] = U("Stone Hall", "Shield Warrior", "Frontline (very tanky)", S(80, 6, 1.0f, 1.0f, 2.7f), new Res(6, 5, 0, 3), 9f, 1, 5f);
            u[UnitType.Crossbow] = U("Crossbow Works", "Heavy Crossbow", "Ranged (high damage, slow reload)", S(24, 18, 3.6f, 22f, 2.6f, Mode.Shoot, 0, Proj.Bolt), new Res(5, 0, 5, 4), 11f, 1, 3f);
            u[UnitType.Axeman] = U("War Forge", "Axeman", "Striker", S(40, 13, 1.0f, 1.0f, 3.1f), new Res(5, 0, 0, 5), 9f, 3, 2f);
            s = S(70, 15, 8.0f, 36f, 1.6f, Mode.Shoot, UFlag.Big, Proj.Cannonball, PKind.Spark, 6f); s.splash = 2f; s.minRange = 6f;
            u[UnitType.Cannon] = U("Cannon Foundry", "Cannon", "Area damage", s, new Res(8, 0, 0, 24), 36f, 7, 0.5f);
            s = S(110, 9, 1.1f, 1.2f, 4.8f, Mode.Melee, UFlag.Big, 0, PKind.Blood, 11f); s.chargeBonus = 1.2f; s.knock = 0.7f;
            u[UnitType.GoatRider] = U("Goat Pen", "Goat Rider", "Mobile (slower than cavalry, tankier)", s, new Res(6, 6, 0, 5), 13f, 4, 1.5f);

            // ---------------- Người hoá thú ----------------
            s = S(90, 10, 1.3f, 1.2f, 2.6f, Mode.Melee, UFlag.Big, 0, PKind.Blood, 7f); s.knock = 0.8f;
            u[UnitType.Bear] = U("Bear Den", "Bearfolk", "Frontline (knockback)", s, new Res(6, 12, 0, 0), 13f, 1, 4f);
            u[UnitType.Wolf] = U("Wolf Den", "Wolffolk", "Striker (rapid attacks)", S(30, 5, 0.45f, 1.0f, 3.8f, Mode.Melee, 0, 0, PKind.Blood, 12f), new Res(5, 6, 0, 0), 8f, 1, 4f);
            u[UnitType.Leopard] = U("Leopard Run", "Leopardfolk", "Mobile (flanks from behind)", S(28, 9, 0.8f, 1.0f, 5.2f, Mode.Melee, UFlag.Flank, 0, PKind.Blood, 13f), new Res(5, 7, 0, 0), 10f, 4, 1.5f);
            u[UnitType.Shaman] = U("Ancestor Stone", "Tribal Shaman", "Support (empowers the pack)", S(20, 3, 2.2f, 8f, 2.8f, Mode.Shoot, UFlag.PowerAura, Proj.NatureOrb), new Res(5, 0, 0, 6), 14f, 5, 1f);
            u[UnitType.Eagle] = U("Eagle Nest", "Eaglefolk", "Ranged (javelins from above)", S(22, 12, 2.4f, 18f, 3.4f, Mode.Shoot, 0, Proj.Javelin), new Res(5, 5, 5, 0), 11f, 2, 2.5f);

            // ---------------- The Void ----------------
            s = S(50, 6, 1.1f, 1.0f, 2.6f, Mode.Melee, 0, 0, PKind.VoidGoo, 7f); s.explodeDmg = 14f; s.explodeRadius = 1.8f;
            u[UnitType.Aberration] = U("Rift", "Aberration", "Frontline (explodes on death)", s, new Res(8, 0, 0, 0), 8f, 1, 5f);
            s = S(26, 9, 0.9f, 1.2f, 4.2f, Mode.Melee, UFlag.Ghost, 0, PKind.VoidGoo, 10f); s.splash = 1.3f;
            u[UnitType.Shade] = U("Tear", "Shade", "Striker (pierces the line)", s, new Res(5, 0, 0, 5), 10f, 3, 2f);
            s = S(16, 5, 1.9f, 22f, 2.6f, Mode.Shoot, 0, Proj.VoidOrb, PKind.VoidGoo, 6f); s.slow = 2f;
            u[UnitType.Eye] = U("Eye Tower", "Gazer", "Ranged (slows)", s, new Res(5, 0, 5, 0), 10f, 1, 3f);
            u[UnitType.Summoner] = U("Void Altar", "Summoner", "Support (spawns minions)", S(24, 0, 6f, 10f, 2.6f, Mode.Summon, 0, 0, PKind.VoidGoo), new Res(8, 0, 0, 6), 15f, 5, 1f);
            s = S(36, 9, 3.0f, 7f, 2.4f, Mode.Blast, 0, 0, PKind.VoidGoo, 7f); s.splash = 3f;
            u[UnitType.Screamer] = U("Howling Pit", "Screamer", "Area damage (sonic wave)", s, new Res(9, 0, 0, 0), 16f, 7, 1f);
            u[UnitType.Voidling] = U("", "Voidling", "Spawned by the Summoner", S(10, 3, 0.8f, 1.0f, 3.6f, Mode.Melee, 0, 0, PKind.VoidGoo, 12f), default, 0f, 99, 0f);

            // Đặc trưng phe được nhân sẵn vào bảng chỉ số
            for (int t = 0; t < UnitType.Count; t++)
            {
                var f = Factions[Faction.Of(t)];
                u[t].stats.maxHp *= f.hpMul;
                u[t].stats.damage *= f.dmgMul;
                u[t].stats.speed *= f.speedMul * Tuning.SpeedScale;
                if (u[t].stats.mode != Mode.Melee) u[t].stats.range *= f.rangeMul * Tuning.RangeScale;
            }
            return u;
        }

        /// <summary>Số quân con mỗi lần Kẻ triệu hồi làm phép.</summary>
        public const int SummonCount = 4;

        static BuildingDef[] BuildBuildings()
        {
            var b = new BuildingDef[B.Count];
            void Eco(int id, string name, Cat cat, int res, float period, float amount, string desc) =>
                b[id] = new BuildingDef { id = id, name = name, cat = cat, res = res, period = period, amount = amount, desc = desc };
            void Buff(int id, string name, Cat cat, Shape shape, string desc) =>
                b[id] = new BuildingDef { id = id, name = name, cat = cat, shape = shape, desc = desc };

            Eco(B.Field, "Wheat Field", Cat.Food, ResId.Food, 3f, 3f, "Steady and cheap. +15% for each adjacent Wheat Field.");
            Eco(B.Barn, "Cattle Barn", Cat.Food, ResId.Food, 10f, 14f, "Slow cycle, big yield. Doubled next to a Wheat Field.");
            Eco(B.HunterLodge, "Hunter's Lodge", Cat.Food, ResId.Food, 3f, 2f, "Only works on the border: 2 Food for each side facing the wilds.");
            Eco(B.LumberCamp, "Lumber Camp", Cat.Wood, ResId.Wood, 3f, 3f, "Steady and cheap.");
            b[B.Sawmill] = new BuildingDef { id = B.Sawmill, name = "Sawmill", cat = Cat.Wood, shape = Shape.Ring, desc = "Produces no Wood itself. Lumber Camps around it work 50% faster." };
            Eco(B.Nursery, "Tree Nursery", Cat.Wood, ResId.Wood, 3f, 0.5f, "Yields very little at first, then grows as the battle goes on.");
            Eco(B.GoldMine, "Gold Mine", Cat.Gold, ResId.Gold, 10f, 12f, "Slow cycle, big yield.");
            Eco(B.Market, "Market", Cat.Gold, ResId.Gold, 5f, 1f, "Each cycle: 1 Gold per building around it (8 tiles).");
            Eco(B.TownHall, "Town Hall", Cat.Gold, ResId.Gold, 5f, 0.05f, "Each cycle: Gold equal to 5% of your current Pop.");
            Eco(B.Cabin, "Cabin", Cat.Pop, ResId.Pop, 4f, 3f, "Cheap, small yield.");
            Eco(B.Residential, "Residential Block", Cat.Pop, ResId.Pop, 4f, 8f, "Big yield. Noisy: adjacent buildings work 20% slower.");
            Eco(B.Tavern, "Tavern", Cat.Pop, ResId.Pop, 3f, 5f, "Fast Pop, but each cycle consumes 2 Food.");

            Buff(B.Windmill, "Windmill", Cat.BuffEco, Shape.Cross, "Wheat Fields yield 50% more.");
            Buff(B.Well, "Well", Cat.BuffEco, Shape.Ring, "All Food buildings yield +15%.");
            Buff(B.RangerHut, "Ranger Hut", Cat.BuffEco, Shape.Below, "The Tree Nursery grows twice as fast.");
            Buff(B.Mint, "Mint", Cat.BuffEco, Shape.Cross, "Gold Mines cycle 40% faster.");
            Buff(B.TradeRoad, "Trade Road", Cat.BuffEco, Shape.Row, "Each Market in the row earns 3 extra Gold per cycle.");
            Buff(B.Plaza, "Plaza", Cat.BuffEco, Shape.Ring, "Houses around it produce Pop 25% faster.");
            Buff(B.Forge, "Forge", Cat.BuffMil, Shape.Below, "Troops from the building below deal +60% damage.");
            Buff(B.TrainingGround, "Training Ground", Cat.BuffMil, Shape.Cross, "Adjacent troop buildings call 25% faster.");
            Buff(B.Depot, "Supply Depot", Cat.BuffMil, Shape.Cross, "Adjacent troop buildings pay 20% less per call.");
            Buff(B.Banner, "War Banner", Cat.BuffMil, Shape.Row, "Troops from buildings in the row have +20% HP.");
            Buff(B.Infirmary, "Infirmary", Cat.BuffMil, Shape.Ring, "Troops from buildings around it regenerate 1 HP per second.");

            for (int t = 0; t < UnitType.Buildable; t++)
            {
                var u = Units[t];
                b[B.Barracks + t] = new BuildingDef
                {
                    id = B.Barracks + t, name = u.house, cat = Cat.Barracks, period = u.callTime, unit = t, faction = Faction.Of(t),
                    desc = $"Calls {u.name} squads. {u.role}."
                };
            }
            b[B.Expand] = new BuildingDef { id = B.Expand, name = "Expand Land", cat = Cat.Expand, desc = "Opens 1 empty tile next to your grid." };
            return b;
        }

        public static readonly Color[] CatColor =
        {
            new Color(0.62f, 0.52f, 0.20f),  // Lương thực
            new Color(0.30f, 0.44f, 0.24f),  // Gỗ
            new Color(0.66f, 0.50f, 0.12f),  // Vàng
            new Color(0.26f, 0.38f, 0.62f),  // Dân
            new Color(0.18f, 0.48f, 0.48f),  // Buff kinh tế
            new Color(0.56f, 0.24f, 0.22f),  // Buff quân sự
            new Color(0.30f, 0.28f, 0.36f),  // Nhà lính
            new Color(0.34f, 0.34f, 0.34f),  // Mở rộng
        };

        public static readonly string[] CatName = { "Food", "Wood", "Gold", "Pop", "Economy buff", "Military buff", "Troop building", "Expansion" };
    }
}
