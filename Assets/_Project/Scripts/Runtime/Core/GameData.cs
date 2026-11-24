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
        public const int MaxGridSide = 7;               // mở đất tối đa trong vùng 7x7
        public const int MaxStars = 4;
        public const int RewardChoices = 3;
        public const int FullMoonEvery = 3;             // Người hoá thú: cứ 3 đợt có 1 đợt trăng tròn

        // ---- Chưa chốt trong GDD → số tạm ----
        public const bool DiagonalAdjacency = false;    // "sát cạnh" tạm tính 4 hướng
        public const int HandLimit = 10;
        public const bool MergeFromHand = true;         // kéo thẻ từ tay lên công trình cùng loại, cùng sao để gộp
        public const bool MergeNeedsAdjacent = false;
        public const int ShopSlots = 5;
        public const int RollCost = 1;
        public const float EventChance = 0.25f;         // xác suất có sự kiện đổi phe thù địch sau mỗi đợt
        public const int StartCrystals = 5;
        // ---- Độ khó (chọn ở màn chọn phe): Easy / Normal / Hard. Hard = cân bằng gốc ----
        public static readonly string[] DiffName = { "Easy", "Normal", "Hard" };
        public static readonly float[] DiffSquads = { 0.6f, 0.8f, 1f };      // số tiểu đội địch
        public static readonly float[] DiffPower = { 0.75f, 0.9f, 1f };      // máu + sát thương quân địch
        public static readonly float[] DiffBoss = { 0.6f, 0.8f, 1f };        // máu boss đợt trùm (nhân thêm)
        public static readonly int[] DiffCrystals = { 5, 2, 0 };             // tinh thể khởi đầu cộng thêm             // đủ mua 1 thẻ ngay đợt đầu
        public const int CrystalReward = 5;             // mỗi đợt cố định như TFT, thắng hay thua đều nhận
        public const int WinBonus = 1;                  // giữ được thành thì thêm, thua không có
        // Lãi: hết mỗi đợt, cứ InterestStep Tinh thể đang giữ thì được thêm 1, tối đa InterestMax → để dành mới có lợi
        public const int InterestStep = 10, InterestMax = 5;   // giống TFT
        public static int Interest(int held, int extraCap = 0) => Mathf.Min(Mathf.Max(held, 0) / InterestStep, InterestMax + extraCap);

        // ---- Lõi (giống Augment của TFT) ----
        public static readonly int[] AugmentWaves = { 1, 6, 12, 18, 24 };
        // Xác suất ra bậc Silver / Gold / Prismatic ở lần chọn lõi thứ 1..5 (càng về sau càng dễ ra lõi xịn)
        public static readonly int[][] AugTierOdds =
        {
            new[] { 50, 45, 5 }, new[] { 35, 50, 15 }, new[] { 20, 55, 25 }, new[] { 10, 55, 35 }, new[] { 5, 50, 45 },
        };   // sau đợt 1 + mỗi đợt trùm (đợt 30 là đợt cuối) → 5 lõi
        public const int AugmentChoices = 3;
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
        public const float BossSize = 1.25f, BossPower = 1f;                // đợt trùm: đông hơn chút, sức như thường — điểm nhấn là con boss
        public const float BossHp = 3f;                                     // máu boss nhân thêm (ngoài hệ số theo đợt)
        public const float BossDelay = 4f;                                  // boss vào trận sau 4 giây
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
        public const int Barracks = 23;                         // 23..47: nhà lính 5 phe đầu (mã cũ, giữ cho bản lưu)
        public const int Expand = Barracks + 25;                // 48: thẻ mở thêm ô trống
        public const int OrcBarracks = Expand + 1;              // 49..53: nhà lính phe Orc (nối sau để bản lưu cũ vẫn đúng)
        public const int Count = OrcBarracks + 5;
        /// <summary>Mã nhà lính gọi loại quân <paramref name="unitType"/>.</summary>
        public static int BarracksOf(int unitType) => unitType < 25 ? Barracks + unitType : OrcBarracks + (unitType - 25);
    }

    public enum AugCat { Economy, Military, Utility, Legendary, Specialist }

    /// <summary>Bậc lõi như TFT: mỗi lần chọn lõi quay ra 1 bậc, cả 3 ô (và lượt đổi) đều cùng bậc đó.</summary>
    public enum AugTier { Silver, Gold, Prismatic }

    /// <summary>Một lõi: hiệu ứng vĩnh viễn cho cả lượt chơi. Hiệu ứng nằm ở City / Game (tìm Aug.Tên).</summary>
    public class AugmentDef
    {
        public int id;
        public string name, desc;
        public AugCat cat;
        public int unit = -1;          // lõi chuyên quân: loại quân; chỉ xuất hiện khi có nhà lính của quân này
        public bool leaderSkill;       // kỹ năng của trưởng đạo (không phải mọi lính)
        // Thẻ lõi gọn cho người lười đọc: 1 con số to + 1 dòng ngắn (desc đầy đủ hiện khi rê chuột)
        public string big, line;
        public AugTier tier;
    }

    /// <summary>Mã lõi.</summary>
    public static class Aug
    {
        // Kinh tế
        public const int Harvest = 0, Timber = 1, GoldRush = 2, BabyBoom = 3, Stockpile = 4, Treasury = 5, RichGetRicher = 6;
        // Quân sự
        public const int Sharpened = 7, IronSkin = 8, Drill = 9, Supply = 10, Medics = 11, Walls = 12, Scouts = 13;
        // Tiện ích
        public const int FreeRoll = 14, Bargain = 15, LandGrant = 16, Recruit = 17, Architect = 18;
        // Huyền thoại: cơ chế riêng, không chỉ cộng chỉ số
        public const int Dragon = 19, Meteor = 20, Ballista = 21, Mercenary = 22, LastStand = 23;
        // Chuyên quân: 25 lõi, mã = UnitBase + loại quân
        public const int UnitBase = 24;
        public static int ForUnit(int unitType) => unitType < 25 ? UnitBase + unitType : unitType < UnitType.Buildable ? OrcUnitBase + (unitType - 25) : -1;
        // Bí mật (easter egg): không bao giờ ra theo kiểu thường, phải mở khoá bằng điều kiện ẩn trong ván
        // Mỗi phe 1 lõi bí mật gọi boss của phe mình đánh cho người chơi. Mã = Emperor + phe (giữ nguyên mã cũ của Emperor cho bản lưu)
        public const int Emperor = UnitBase + 25;                                 // Người: The Golden Emperor (mã cố định cho bản lưu)
        public const int WorldTreeAug = Emperor + 1, ThorAug = Emperor + 2, BerserkerAug = Emperor + 3, BroodAug = Emperor + 4;
        // Prismatic thêm (mã nối sau lõi bí mật để bản lưu cũ vẫn đúng)
        public const int Legion = Emperor + 5, TitanBlood = Legion + 1, WarDrums = Legion + 2, Undying = Legion + 3,
                         SkyLances = Legion + 4, GoldenAge = Legion + 5, IronFortress = Legion + 6, DragonHoard = Legion + 7;
        // Phe Orc (thêm sau): 5 lõi chuyên quân + lõi bí mật, nối cuối bảng
        public const int OrcUnitBase = DragonHoard + 1, OrcSecret = OrcUnitBase + 5;
        public const int Count = OrcSecret + 1;
        public const int WarbossStreak = 5;                         // Orc: thắng 5 đợt liên tiếp
        public const float GruntExecute = 1.5f;                     // Bloodlust: +50% sát thương lên địch dưới nửa máu
        public const float SpearDmg = 1.5f;                         // Heavy Spears
        public const float TrampleStun = 0.8f;                      // Trample: xung phong đầy đà húc choáng
        public const float DrumRadius = 3.5f, DrumRadiusAug = 7f;   // War Drummer: quân ta trong vùng đánh nhanh (Haste)
        public const float TrollRegen = 3f, TrollRegenAug = 8f;     // Troll tự hồi máu
        public const float WarCryEvery = 12f, WarCryRadius = 8f, WarCryHaste = 5f, WarCryHeal = 0.15f, WarCryFear = 1.5f, WarCryFearRadius = 4f;   // Warboss
        public const int LegionSquads = 2;
        public const float TitanHp = 0.6f, TitanDmg = 0.3f;
        public const float WarDrumsSpeed = 1.4f;                    // đánh nhanh x1.4 (hồi đòn chia 1.4)
        public const float UndyingChance = 0.3f;
        public const float LanceEvery = 1.5f, LanceDamage = 60f, LanceSplash = 2f; public const int LanceCount = 3;
        public const float GoldenAgeMul = 2f;
        public const int FortressHp = 500; public const float FortressThorns = 15f;
        public const int HoardCrystals = 30, HoardInterest = 5;
        public static int SecretOf(int faction) => faction < 5 ? Emperor + faction : OrcSecret;
        public static bool IsSecret(int a) => (a >= Emperor && a < Emperor + 5) || a == OrcSecret;
        public static int SecretUnit(int a) => UnitType.BossOf(a == OrcSecret ? Faction.Orc : a - Emperor);

        // Điều kiện mở khoá (bí mật, mỗi phe 1 kiểu). Đạt thì lần chọn lõi kế tiếp chắc chắn có lõi bí mật của phe mình
        public const int EmperorStreak = 5;        // Người: giữ thành không mất máu 5 đợt liên tiếp
        public const int TreeWoodBuildings = 4;    // Tiên: có 4 nhà Gỗ trên lưới
        public const int ThorGoldBuildings = 4;    // Lùn: có 4 nhà Vàng trên lưới
        public const int BerserkMoonWins = 3;      // Thú: thắng 3 đợt trăng tròn
        public const int BroodKills = 1500;        // Hư không: diệt tổng cộng 1500 quân địch

        // Boss đánh cho người chơi: mạnh hơn hẳn bản gốc (bản boss phe địch dùng Tuning.BossHp)
        public const float AllyHp = 2.8f, AllyDmg = 1.6f, AllyArmor = 0.75f, AllySplash = 0.8f, AllyRevive = 0.5f;
        public const float EmperorDelay = 3f;                                   // vào trận sau 3 giây; chết thì chờ đợt sau
        public const float EmperorAura = 4.5f, EmperorAuraHeal = 1f, EmperorAuraDps = 3f;   // hào quang: quân ta hồi 1 máu/s, địch mất 3 máu/s (nhân theo đợt)
        public const float DecreeEvery = 12f, DecreeRadius = 5.5f, DecreeDamage = 40f, DecreeStun = 1.5f, DecreeHeal = 0.25f;

        // Chiêu của boss đợt trùm (sát thương nhân theo đợt)
        public const float TreantRegen = 6f;                                    // The World Tree: tự hồi máu (nhân theo đợt)
        public const float TreeRegen = 3f;                                      // ... mọi Treant cùng phe hồi 3 máu/s, ở đâu cũng được
        public const float PunchEvery = 7f, PunchRange = 7f, PunchDamage = 20f, PunchPush = 3.5f, PunchCone = 0.45f;   // cú đấm: sóng đẩy lùi phía trước
        public const int ThorChain = 4; public const float ThorChainRange = 3.5f, ThorChainDmg = 0.6f;   // Thor: mỗi nhát búa phóng sét lan qua 4 địch
        public const float ThorHasteRadius = 5f, Haste = 1.6f;                  // ... quân cùng phe đứng gần đánh nhanh x1.6
        public const float BerserkLifesteal = 0.1f, BerserkRage = 0.25f, RageFearRadius = 6f, RageFear = 2f;   // Berserker: hút máu 10%; dưới 25% máu hoá Fenrir
        public const float BroodEvery = 10f, BroodSight = 12f, PoisonTime = 5f, PoisonDps = 4f; public const int BroodCount = 4;   // Broodmother
        public const float EmperorRevive = 0.5f;                                // boss Hoàng đế: chết thì hồi sinh 1 lần với 50% máu

        // Số liệu lõi chuyên quân (số tạm)
        public const float ShieldWallResist = 0.4f;
        public const float VolleyEvery = 8f; public const int VolleyArrows = 7;
        public const float ChargePierceRadius = 1.2f;
        public const float ReviveRadius = 4f, ReviveChance = 0.25f; public const int ReviveMax = 10;
        public const float FireBurn = 4f;
        public const float RootAfter = 3f, RootRegen = 1f, RootSlow = 1.5f;
        public const float PierceLength = 2.5f; public const int PierceExtra = 1;   // tên xuyên: tổng cộng trúng tối đa 2 con
        public const float CounterRadius = 1.2f;
        public const float VineEvery = 6f, VineTime = 1.5f;
        public const float KiteFireRate = 2f;
        public const float Thorns = 0.3f;
        public const float AntiBigMul = 2f;
        public const float BerserkLow = 0.3f;
        public const int ClusterBombs = 3; public const float ClusterDamage = 0.4f;
        public const float RamEvery = 8f, RamStun = 1f, RamRadius = 1.5f;
        public const float RoarEvery = 12f, RoarFear = 1f, RoarRadius = 3f;
        public const float PackBonus = 0.08f, PackMax = 0.4f, PackRadius = 2.5f;
        public const float Backstab = 3f;
        public const float DiveEvery = 15f, DiveDamage = 3f, DiveSplash = 2.5f;
        public const float Lifesteal = 0.2f;
        public const float GazeEvery = 7f, GazeStun = 1f;
        public const float PortalEvery = 20f; public const int PortalMax = 3;
        public const float ResonanceRange = 7f, ResonanceDamage = 0.6f;

        // Các con số (số tạm, chưa cân bằng)
        public const float ResBonus = 0.35f;          // Harvest / Timber / GoldRush / BabyBoom: +35% tài nguyên tương ứng
        public const float StockpileRes = 50f;        // đầu mỗi đợt +50 mỗi loại tài nguyên
        public const int TreasuryCrystals = 15;
        public const int RichExtraCap = 3, RichCrystals = 5;
        public const float SharpenedDmg = 0.2f, IronSkinHp = 0.25f;
        public const float DrillSpeed = 0.2f, SupplyCut = 0.2f, MedicsRegen = 1.5f;
        public const int WallsHp = 150;
        public const float ScoutsCut = 0.15f;
        public const int FreeRolls = 1, BargainCut = 1, LandGrantCards = 2;
        public const float ArchitectBuff = 0.4f;
        public const float DragonDelay = 3f, DragonRespawn = 30f;            // rồng vào trận sau 3 giây, chết thì 30 giây sau quay lại
        public const float MeteorEvery = 3.5f, MeteorDamage = 120f, MeteorSplash = 4f;          // thiên thạch dày + to hơn
        public const float BallistaEvery = 0.35f, BallistaDamage = 45f, BallistaRange = 40f, BallistaSplash = 1.2f;   // ~3 phát / giây, nổ lan nhỏ; tầm tính từ tường
        public const float MercEvery = 12f, MercPower = 1.5f; public const int MercSide = 5;  // mỗi 12 giây 1 đội 5x5 tinh nhuệ
        // Last Stand: thành sắp vỡ → hồi đầy máu + sóng thánh dọc tường hất văng, choáng và đốt mọi địch gần tường
        public const float LastStandRange = 12f, LastStandPush = 7f, LastStandStun = 3f, LastStandDamage = 80f;
    }

    public static class GameData
    {
        public static readonly FactionDef[] Factions =
        {
            new FactionDef { name = "Human", color = new Color(0.36f, 0.56f, 0.95f), hpMul = 0.9f,
                strength = "Houses produce Pop 25% faster", weakness = "Frail: troops have 10% less HP" },
            new FactionDef { name = "Elf", color = new Color(0.40f, 0.80f, 0.45f), dmgMul = 1.5f,   // tinh nhuệ ở sát thương, không trâu rangeMul = 1.15f, squadSide = 4,
                strength = "Elite troops: 50% more damage and longer range", weakness = "Each call brings only 16 troops (4x4)" },
            new FactionDef { name = "Dwarf", color = new Color(0.90f, 0.62f, 0.25f), speedMul = 0.8f,
                strength = "Gold Mines dig 30% faster", weakness = "Troops move 20% slower and troop buildings call 20% slower" },
            new FactionDef { name = "Beastfolk", color = new Color(0.78f, 0.50f, 0.34f),
                strength = "Food buildings work 30% faster. Full moon (every 3rd wave): troops are much stronger", weakness = "Houses produce Pop 25% slower" },
            new FactionDef { name = "The Void", color = new Color(0.68f, 0.40f, 0.92f), hpMul = 1.3f, dmgMul = 1.3f, popCostMul = 2f,
                strength = "Troops are 30% stronger", weakness = "Every call costs double Pop" },
            new FactionDef { name = "Orc", color = new Color(0.46f, 0.74f, 0.28f), hpMul = 1.2f, rangeMul = 0.75f,
                strength = "Brutes: troops have 20% more HP", weakness = "Wild aim: ranged troops have 25% less range" },
        };

        public static readonly UnitDef[] Units = BuildUnits();
        public static readonly BuildingDef[] Buildings = BuildBuildings();
        public static readonly AugmentDef[] Augments = BuildAugments();

        static AugmentDef[] BuildAugments()
        {
            var a = new AugmentDef[Aug.Count];
            void A(int id, AugCat cat, string name, string desc) => a[id] = new AugmentDef { id = id, cat = cat, name = name, desc = desc };
            int pct(float f) => Mathf.RoundToInt(f * 100f);
            A(Aug.Harvest, AugCat.Economy, "Bountiful Harvest", $"All Food buildings produce {pct(Aug.ResBonus)}% more.");
            A(Aug.Timber, AugCat.Economy, "Timber Boom", $"All Wood buildings produce {pct(Aug.ResBonus)}% more.");
            A(Aug.GoldRush, AugCat.Economy, "Gold Rush", $"All Gold buildings produce {pct(Aug.ResBonus)}% more.");
            A(Aug.BabyBoom, AugCat.Economy, "Baby Boom", $"All Pop buildings produce {pct(Aug.ResBonus)}% more.");
            A(Aug.Stockpile, AugCat.Economy, "Stockpile", $"Every wave starts with +{Aug.StockpileRes:0} of each resource.");
            A(Aug.Treasury, AugCat.Economy, "Treasury", $"Gain {Aug.TreasuryCrystals} Energy Crystals now.");
            A(Aug.RichGetRicher, AugCat.Economy, "Rich Get Richer", $"Gain {Aug.RichCrystals} crystals now. Max interest +{Aug.RichExtraCap} (up to +{Tuning.InterestMax + Aug.RichExtraCap}).");
            A(Aug.Sharpened, AugCat.Military, "Sharpened Steel", $"All your troops deal {pct(Aug.SharpenedDmg)}% more damage.");
            A(Aug.IronSkin, AugCat.Military, "Iron Skin", $"All your troops have {pct(Aug.IronSkinHp)}% more HP.");
            A(Aug.Drill, AugCat.Military, "Drill Sergeant", $"Troop buildings call {pct(Aug.DrillSpeed)}% faster.");
            A(Aug.Supply, AugCat.Military, "Supply Lines", $"Every troop call costs {pct(Aug.SupplyCut)}% less.");
            A(Aug.Medics, AugCat.Military, "Field Medics", $"All your troops regenerate {Aug.MedicsRegen:0.#} HP per second.");
            A(Aug.Walls, AugCat.Military, "Stone Walls", $"The castle has +{Aug.WallsHp} HP (max {Tuning.CastleHp + Aug.WallsHp}).");
            A(Aug.Scouts, AugCat.Military, "Scouts", $"Enemy waves bring {pct(Aug.ScoutsCut)}% fewer troops.");
            A(Aug.FreeRoll, AugCat.Utility, "Lucky Dice", $"Your first {(Aug.FreeRolls == 1 ? "reroll" : Aug.FreeRolls + " rerolls")} between waves is free.");
            A(Aug.Bargain, AugCat.Utility, "Bargain Hunter", $"Shop cards cost {Aug.BargainCut} crystal less (min 1).");
            A(Aug.LandGrant, AugCat.Utility, "Land Grant", $"Gain {Aug.LandGrantCards} Expand Land cards now.");
            A(Aug.Recruit, AugCat.Utility, "Veteran Recruits", "Gain a random 2-star troop building of your faction now.");
            A(Aug.Architect, AugCat.Utility, "Master Architect", $"Buff buildings are {pct(Aug.ArchitectBuff)}% stronger.");
            var ds = Units[UnitType.Dragon].stats;
            A(Aug.Dragon, AugCat.Legendary, "Dragon's Pact",
              $"A dragon fights for you every wave: {ds.maxHp:0} HP, breathes fire for {ds.damage:0} area damage every {ds.cooldown:0.#}s. " +
              $"Its scales take {pct(ds.arrowResist)}% less damage from arrows, bolts and javelins. Grows stronger each wave; returns {Aug.DragonRespawn:0}s after it falls.");
            A(Aug.Meteor, AugCat.Legendary, "Meteor Shower", $"Every {Aug.MeteorEvery:0.#}s a meteor crashes into the enemy horde for {Aug.MeteorDamage:0} damage, burning everything around it.");
            A(Aug.Ballista, AugCat.Legendary, "Ballista Towers", $"Your walls fire about {1f / Aug.BallistaEvery:0} heavy bolts per second at the closest enemies, {Aug.BallistaDamage:0} damage each with a small blast.");
            A(Aug.Mercenary, AugCat.Legendary, "Mercenary Camp", $"Every {Aug.MercEvery:0}s a squad of {Aug.MercSide * Aug.MercSide} veteran mercenaries ({pct(Aug.MercPower - 1f)}% stronger) from a random other faction joins you.");
            A(Aug.LastStand, AugCat.Legendary, "Last Stand",
              $"Once per wave, when the castle would fall it is fully restored and a holy blast runs along the walls: every enemy within {Aug.LastStandRange:0} " +
              $"is hurled back, stunned for {Aug.LastStandStun:0}s and takes {Aug.LastStandDamage:0} damage.");
            // ---- Prismatic thêm ----
            A(Aug.Legion, AugCat.Legendary, "Legion", $"Every troop building calls {Aug.LegionSquads} squads each time instead of 1, for the same cost.");
            A(Aug.TitanBlood, AugCat.Legendary, "Titan Blood", $"All your troops have {pct(Aug.TitanHp)}% more HP and deal {pct(Aug.TitanDmg)}% more damage.");
            A(Aug.WarDrums, AugCat.Legendary, "War Drums", $"All your troops attack {pct(Aug.WarDrumsSpeed - 1f)}% faster.");
            A(Aug.Undying, AugCat.Legendary, "Undying Legion", $"Each of your fallen troops has a {pct(Aug.UndyingChance)}% chance to rise again at half HP (once each).");
            A(Aug.SkyLances, AugCat.Legendary, "Sky Lances", $"Every {Aug.LanceEvery:0.#}s, {Aug.LanceCount} holy lances fall from the sky onto the enemy, {Aug.LanceDamage:0} damage each with a blast.");
            A(Aug.GoldenAge, AugCat.Legendary, "Golden Age", $"All your resource buildings produce {pct(Aug.GoldenAgeMul - 1f)}% more.");
            A(Aug.IronFortress, AugCat.Legendary, "Iron Fortress", $"The castle has +{Aug.FortressHp} HP and its walls are spiked: every enemy blow on the wall hurts the attacker for {Aug.FortressThorns:0} damage.");
            A(Aug.DragonHoard, AugCat.Legendary, "Dragon Hoard", $"Gain {Aug.HoardCrystals} Energy Crystals now. Max interest +{Aug.HoardInterest}.");

            // ---- Lõi bí mật: mỗi phe gọi boss của phe mình đánh cho người chơi ----
            string allyCommon = $" Takes {pct(Aug.AllyArmor)}% less damage from arrows, bolts and javelins and cannot be stunned, rooted or feared. " +
                                $"Grows stronger each wave. Rises again once per wave at {pct(Aug.AllyRevive)}% HP; if it falls again, it returns next wave.";
            float AHp(int t) => Units[t].stats.maxHp * Aug.AllyHp;
            float ADmg(int t) => Units[t].stats.damage * Aug.AllyDmg;
            A(Aug.Emperor, AugCat.Legendary, "The Golden Emperor",
              $"The Emperor himself takes the field every wave: {AHp(UnitType.Emperor):0} HP, his greatsword cleaves for {ADmg(UnitType.Emperor):0}. " +
              $"Golden aura: nearby allies regenerate {Aug.EmperorAuraHeal:0} HP/s, nearby enemies burn. Every {Aug.DecreeEvery:0}s, Imperial Decree: " +
              $"a golden shockwave hurts and stuns enemies for {Aug.DecreeStun:0.#}s and heals allies for {pct(Aug.DecreeHeal)}% of their HP." + allyCommon);
            A(Aug.WorldTreeAug, AugCat.Legendary, "The World Tree",
              $"The World Tree takes root on your side every wave: {AHp(UnitType.WorldTree):0} HP, its fists hit for {ADmg(UnitType.WorldTree):0}. " +
              $"Every Treant you own regenerates {Aug.TreeRegen:0} HP/s anywhere on the field. Every {Aug.PunchEvery:0}s its punch sends a shockwave that hurls enemies back." + allyCommon);
            A(Aug.ThorAug, AugCat.Legendary, "Thor",
              $"Thor fights for you every wave: {AHp(UnitType.Thor):0} HP, his hammer hits for {ADmg(UnitType.Thor):0}. " +
              $"Every blow sends chain lightning through {Aug.ThorChain} more enemies. Your troops near him attack {pct(Aug.Haste - 1f)}% faster." + allyCommon);
            A(Aug.BerserkerAug, AugCat.Legendary, "The Berserker",
              $"The Berserker fights for you every wave: {AHp(UnitType.Berserker):0} HP, his axe cleaves for {ADmg(UnitType.Berserker):0} and heals him for {pct(Aug.BerserkLifesteal)}% of the damage. " +
              $"Below {pct(Aug.BerserkRage)}% HP he becomes Fenrir: enemies around flee in fear for {Aug.RageFear:0}s and he attacks much faster." + allyCommon);
            A(Aug.OrcSecret, AugCat.Legendary, "The Warboss",
              $"The Warboss stomps to war for you every wave: {AHp(UnitType.Warboss):0} HP, his cleaver hits for {ADmg(UnitType.Warboss):0}. " +
              $"Every {Aug.WarCryEvery:0}s, War Cry: your troops within {Aug.WarCryRadius:0} attack {pct(Aug.Haste - 1f)}% faster for {Aug.WarCryHaste:0}s and heal {pct(Aug.WarCryHeal)}% HP, nearby enemies flee in fear." + allyCommon);
            A(Aug.BroodAug, AugCat.Legendary, "The Broodmother",
              $"The Broodmother crawls to war for you every wave: {AHp(UnitType.Broodmother):0} HP, her venomous bite hits for {ADmg(UnitType.Broodmother):0} and poisons. " +
              $"Every {Aug.BroodEvery:0}s she lays {Aug.BroodCount} spiderlings that bite with venom too." + allyCommon);

            // ---- Chuyên quân: 1 lõi / loại quân. 👑 = kỹ năng của trưởng đạo ----
            void SU(int t, bool lead, string name, string desc)
            {
                int id = Aug.ForUnit(t);
                a[id] = new AugmentDef { id = id, cat = AugCat.Specialist, unit = t, leaderSkill = lead, name = name,
                                         desc = (lead ? $"Squad leaders of {Units[t].name}: " : $"All {Units[t].name}: ") + desc };
            }
            SU(UnitType.Swordsman, false, "Shield Wall", $"take {pct(Aug.ShieldWallResist)}% less damage from arrows, bolts and javelins.");
            SU(UnitType.Archer, true, "Arrow Volley", $"every {Aug.VolleyEvery:0}s fire a fan of {Aug.VolleyArrows} arrows.");
            SU(UnitType.Cavalry, false, "Breakthrough", "a full-speed charge pierces the line and hits everything around the target.");
            SU(UnitType.Monk, true, "Resurrection", $"allies who die nearby have a {pct(Aug.ReviveChance)}% chance to rise again at half HP (once each, up to {Aug.ReviveMax} per leader).");
            SU(UnitType.Catapult, false, "Fire Shot", $"rocks set enemies on fire for {Aug.FireBurn:0}s (burning does not stack).");
            SU(UnitType.Treant, false, "Take Root", $"after standing still for {Aug.RootAfter:0}s: cannot be knocked back, regenerates {Aug.RootRegen:0} HP/s and its blows slow enemies.");
            SU(UnitType.ForestArcher, false, "Piercing Arrows", $"arrows go through and hit {Aug.PierceExtra} more enemy behind the target (2 in total).");
            SU(UnitType.BladeDancer, false, "Blade Dance", "every dodge is answered with a sweeping counter-attack.");
            SU(UnitType.Druid, true, "Entangling Vines", $"every {Aug.VineEvery:0}s the next spell roots enemies in place for {Aug.VineTime:0.#}s.");
            SU(UnitType.DeerRider, false, "Running Fire", $"shoot {Aug.KiteFireRate:0}x faster while riding around the enemy.");
            SU(UnitType.ShieldWarrior, false, "Spiked Shields", $"reflect {pct(Aug.Thorns)}% of melee damage back to the attacker.");
            SU(UnitType.Crossbow, false, "Armor Piercing", $"deal {Aug.AntiBigMul:0}x damage to large and giant enemies.");
            SU(UnitType.Axeman, false, "Berserk", $"the lower their HP the harder they hit; under {pct(Aug.BerserkLow)}% HP they attack twice as fast.");
            SU(UnitType.Cannon, false, "Cluster Shot", $"shells burst into {Aug.ClusterBombs} smaller explosions around the impact.");
            SU(UnitType.GoatRider, true, "Mountain Ram", $"every {Aug.RamEvery:0}s a ram stuns nearby enemies for {Aug.RamStun:0}s.");
            SU(UnitType.Bear, true, "Terrifying Roar", $"every {Aug.RoarEvery:0}s roar: nearby enemies flee in fear for {Aug.RoarFear:0}s.");
            SU(UnitType.Wolf, false, "Pack Hunters", $"+{pct(Aug.PackBonus)}% damage for every wolf nearby (up to +{pct(Aug.PackMax)}%).");
            SU(UnitType.Leopard, false, "Predator", $"hunt enemy ranged troops first; attacks from behind deal {Aug.Backstab:0}x damage.");
            SU(UnitType.Shaman, false, "Blood Moon", "on full-moon waves the shaman's aura also heals the pack.");
            SU(UnitType.Eagle, true, "Dive Bomb", $"every {Aug.DiveEvery:0}s the next javelin is a dive: {Aug.DiveDamage:0}x damage in a wide area.");
            SU(UnitType.Aberration, false, "Mitosis", "on death, split into 2 Voidlings (Voidlings do not split again).");
            SU(UnitType.Shade, false, "Soul Drain", $"heal for {pct(Aug.Lifesteal)}% of the damage dealt.");
            SU(UnitType.Eye, true, "Paralyzing Gaze", $"every {Aug.GazeEvery:0}s the next bolt paralyzes its target for {Aug.GazeStun:0}s.");
            SU(UnitType.Summoner, true, "Void Portal", $"every {Aug.PortalEvery:0}s summon an Aberration instead of Voidlings (up to {Aug.PortalMax} per leader each wave).");
            SU(UnitType.Screamer, false, "Resonance", "every sonic wave bounces to one more enemy group nearby.");
            SU(UnitType.Grunt, false, "Bloodlust", $"deal {pct(Aug.GruntExecute - 1f)}% more damage to enemies below half HP.");
            SU(UnitType.SpearThrower, false, "Heavy Spears", $"spears deal {pct(Aug.SpearDmg - 1f)}% more damage.");
            SU(UnitType.BoarRider, false, "Trample", $"a full-speed charge stuns the target for {Aug.TrampleStun:0.#}s.");
            SU(UnitType.Drummer, false, "Battle Rhythm", $"the drum reaches twice as far ({Aug.DrumRadiusAug:0} instead of {Aug.DrumRadius:0.#}).");
            SU(UnitType.Troll, false, "Regeneration", $"regenerate {Aug.TrollRegenAug:0} HP per second instead of {Aug.TrollRegen:0}.");

            // ---- Số to + dòng ngắn trên thẻ lõi ----
            void V(int id, string big, string line) { a[id].big = big; a[id].line = line; }
            string P(float f) => $"+{pct(f)}%";
            V(Aug.Harvest, P(Aug.ResBonus), "More Food");
            V(Aug.Timber, P(Aug.ResBonus), "More Wood");
            V(Aug.GoldRush, P(Aug.ResBonus), "More Gold");
            V(Aug.BabyBoom, P(Aug.ResBonus), "More Pop");
            V(Aug.Stockpile, $"+{Aug.StockpileRes:0}", "Every resource, each wave");
            V(Aug.Treasury, $"+{Aug.TreasuryCrystals}", "Crystals now");
            V(Aug.RichGetRicher, $"+{Aug.RichCrystals}", $"Crystals now, interest up to +{Tuning.InterestMax + Aug.RichExtraCap}");
            V(Aug.Sharpened, P(Aug.SharpenedDmg), "Troop damage");
            V(Aug.IronSkin, P(Aug.IronSkinHp), "Troop HP");
            V(Aug.Drill, P(Aug.DrillSpeed), "Faster troop calls");
            V(Aug.Supply, $"-{pct(Aug.SupplyCut)}%", "Troop call cost");
            V(Aug.Medics, $"+{Aug.MedicsRegen:0.#}/s", "Troops regenerate HP");
            V(Aug.Walls, $"+{Aug.WallsHp}", "Castle HP");
            V(Aug.Scouts, $"-{pct(Aug.ScoutsCut)}%", "Enemy troops");
            V(Aug.FreeRoll, "Free", "First shop reroll each wave");
            V(Aug.Bargain, $"-{Aug.BargainCut}", "Shop card price");
            V(Aug.LandGrant, $"+{Aug.LandGrantCards}", "Expand Land cards");
            V(Aug.Recruit, "2-star", "Random troop building");
            V(Aug.Architect, P(Aug.ArchitectBuff), "Buff building power");
            V(Aug.Dragon, $"{Units[UnitType.Dragon].stats.maxHp:0} HP", "A fire dragon joins every wave");
            V(Aug.Meteor, $"{Aug.MeteorEvery:0.#}s", "Meteors rain on the enemy");
            V(Aug.Ballista, $"{1f / Aug.BallistaEvery:0}/s", "Walls fire heavy bolts");
            V(Aug.Mercenary, $"x{Aug.MercSide * Aug.MercSide}", $"Mercenaries every {Aug.MercEvery:0}s");
            V(Aug.LastStand, "100%", "Castle rises again, blasts the horde");
            V(Aug.Emperor, $"{Units[UnitType.Emperor].stats.maxHp * Aug.AllyHp:0} HP", "The Emperor marches to war");
            V(Aug.Legion, $"x{Aug.LegionSquads}", "Squads per troop call");
            V(Aug.TitanBlood, $"+{pct(Aug.TitanHp)}%", "Troop HP, +30% damage");
            V(Aug.WarDrums, $"+{pct(Aug.WarDrumsSpeed - 1f)}%", "Troop attack speed");
            V(Aug.Undying, $"{pct(Aug.UndyingChance)}%", "Fallen troops rise again");
            V(Aug.SkyLances, $"x{Aug.LanceCount}", $"Holy lances every {Aug.LanceEvery:0.#}s");
            V(Aug.GoldenAge, $"x{Aug.GoldenAgeMul:0}", "All resource production");
            V(Aug.IronFortress, $"+{Aug.FortressHp}", "Castle HP, spiked walls");
            V(Aug.DragonHoard, $"+{Aug.HoardCrystals}", "Crystals now, interest +5");
            V(Aug.WorldTreeAug, $"{Units[UnitType.WorldTree].stats.maxHp * Aug.AllyHp:0} HP", "The World Tree awakens");
            V(Aug.ThorAug, $"{Units[UnitType.Thor].stats.maxHp * Aug.AllyHp:0} HP", "Thunder fights for you");
            V(Aug.BerserkerAug, $"{Units[UnitType.Berserker].stats.maxHp * Aug.AllyHp:0} HP", "The Berserker joins the hunt");
            V(Aug.BroodAug, $"{Units[UnitType.Broodmother].stats.maxHp * Aug.AllyHp:0} HP", "The Broodmother hungers");
            // ---- Bậc lõi: Silver = cộng chỉ số nhỏ / tiện ích, Gold = mạnh + mọi lõi chuyên quân, Prismatic = huyền thoại + bí mật ----
            int[] silver = { Aug.Harvest, Aug.Timber, Aug.GoldRush, Aug.BabyBoom, Aug.FreeRoll, Aug.Bargain, Aug.Scouts, Aug.Supply, Aug.Drill, Aug.Walls };
            for (int id = 0; id < a.Length; id++)
                a[id].tier = a[id].cat == AugCat.Legendary ? AugTier.Prismatic : System.Array.IndexOf(silver, id) >= 0 ? AugTier.Silver : AugTier.Gold;
            void VU(int t, string big, string line) => V(Aug.ForUnit(t), big, line);
            VU(UnitType.Swordsman, $"-{pct(Aug.ShieldWallResist)}%", "Arrow damage taken");
            VU(UnitType.Archer, $"x{Aug.VolleyArrows}", "Leader fires an arrow volley");
            VU(UnitType.Cavalry, "Pierce", "Full charge breaks the line");
            VU(UnitType.Monk, $"{pct(Aug.ReviveChance)}%", "Leader revives fallen allies");
            VU(UnitType.Catapult, $"{Aug.FireBurn:0}s", "Rocks set enemies on fire");
            VU(UnitType.Treant, $"+{Aug.RootRegen:0}/s", "Take root: heal, no knockback");
            VU(UnitType.ForestArcher, "x2", "Arrows pierce 2 enemies");
            VU(UnitType.BladeDancer, "Counter", "Dodge, then strike back");
            VU(UnitType.Druid, $"{Aug.VineTime:0.#}s", "Leader's vines root enemies");
            VU(UnitType.DeerRider, $"x{Aug.KiteFireRate:0}", "Fire rate while riding");
            VU(UnitType.ShieldWarrior, $"{pct(Aug.Thorns)}%", "Reflect melee damage");
            VU(UnitType.Crossbow, $"x{Aug.AntiBigMul:0}", "Damage to big enemies");
            VU(UnitType.Axeman, "Rage", "Stronger as HP drops");
            VU(UnitType.Cannon, $"+{Aug.ClusterBombs}", "Shells split into blasts");
            VU(UnitType.GoatRider, $"{Aug.RamStun:0}s", "Leader's ram stuns");
            VU(UnitType.Bear, $"{Aug.RoarFear:0}s", "Leader's roar makes foes flee");
            VU(UnitType.Wolf, $"+{pct(Aug.PackMax)}%", "Stronger in packs");
            VU(UnitType.Leopard, $"x{Aug.Backstab:0}", "Hunts archers, backstabs");
            VU(UnitType.Shaman, "Heal", "Full moon: aura also heals");
            VU(UnitType.Eagle, $"x{Aug.DiveDamage:0}", "Leader's dive hits an area");
            VU(UnitType.Aberration, "+2", "Splits into Voidlings");
            VU(UnitType.Shade, $"{pct(Aug.Lifesteal)}%", "Lifesteal");
            VU(UnitType.Eye, $"{Aug.GazeStun:0}s", "Leader's gaze paralyzes");
            VU(UnitType.Summoner, $"x{Aug.PortalMax}", "Leader opens Aberration portals");
            VU(UnitType.Screamer, "x2", "Sonic wave bounces");
            VU(UnitType.Grunt, $"+{pct(Aug.GruntExecute - 1f)}%", "Damage to wounded foes");
            VU(UnitType.SpearThrower, $"+{pct(Aug.SpearDmg - 1f)}%", "Spear damage");
            VU(UnitType.BoarRider, $"{Aug.TrampleStun:0.#}s", "Charge stuns");
            VU(UnitType.Drummer, "x2", "Drum range");
            VU(UnitType.Troll, $"{Aug.TrollRegenAug:0}/s", "Troll regeneration");
            V(Aug.OrcSecret, $"{Units[UnitType.Warboss].stats.maxHp * Aug.AllyHp:0} HP", "The Warboss wants a fight");
            return a;
        }

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
            s = S(44, 7, 0.9f, 1.0f, 3.0f); s.splash = 1.3f;                                  // chém quét nhẹ: có đòn lan từ đợt 1
            u[UnitType.Swordsman] = U("Barracks", "Shield Swordsman", "Frontline (sweeping cuts)", s, new Res(5, 5, 0, 0), 8f, 1, 5f);
            u[UnitType.Archer] = U("Archery Range", "Archer", "Ranged", S(18, 6, 1.7f, 24f, 2.8f, Mode.Shoot, 0, Proj.Arrow), new Res(5, 0, 5, 0), 9f, 1, 3f);
            s = S(75, 9, 1.0f, 1.2f, 5.5f, Mode.Melee, UFlag.Big, 0, PKind.Blood, 12f); s.chargeBonus = 2f; s.knock = 0.9f;
            u[UnitType.Cavalry] = U("Stable", "Cavalry", "Mobile (charge)", s, new Res(6, 6, 0, 4), 12f, 3, 1.5f);
            u[UnitType.Monk] = U("Monastery", "Monk", "Support (heals)", S(20, 2, 2.0f, 7f, 2.8f, Mode.Shoot, UFlag.HealAura, Proj.LightOrb), new Res(5, 0, 0, 5), 14f, 5, 1f);
            s = S(60, 12, 7.0f, 34f, 1.6f, Mode.Shoot, UFlag.Big, Proj.Rock, PKind.Splinter, 6f); s.splash = 1.8f; s.minRange = 6f;
            u[UnitType.Catapult] = U("Siege Workshop", "Catapult", "Area damage", s, new Res(8, 0, 14, 12), 36f, 7, 0.5f);

            // ---------------- Tiên ----------------
            s = S(110, 9, 1.4f, 1.2f, 1.7f, Mode.Melee, UFlag.Big, 0, PKind.Leaf, 5f); s.splash = 1.4f;   // đập lan
            u[UnitType.Treant] = U("Sacred Grove", "Treant", "Frontline (very tanky, area slam)", s, new Res(6, 0, 12, 0), 14f, 1, 4f);
            u[UnitType.ForestArcher] = U("Bow Academy", "Forest Archer", "Ranged (longest range)", S(16, 6, 1.6f, 28f, 2.9f, Mode.Shoot, 0, Proj.ElfArrow), new Res(5, 0, 6, 0), 10f, 1, 3f);
            s = S(30, 8, 0.6f, 1.0f, 3.6f, Mode.Melee, 0, 0, PKind.Blood, 11f); s.dodge = 0.4f;
            u[UnitType.BladeDancer] = U("Blade Court", "Blade Dancer", "Striker (dodges)", s, new Res(5, 6, 0, 0), 9f, 3, 2f);
            s = S(18, 7, 2.2f, 18f, 2.7f, Mode.Shoot, 0, Proj.NatureOrb); s.splash = 2f; s.slow = 2.5f;   // phép lan đủ đau để dọn quân con
            u[UnitType.Druid] = U("Moon Temple", "Druid", "Area spells (slow enemies)", s, new Res(5, 0, 0, 6), 14f, 5, 1f);
            u[UnitType.DeerRider] = U("Deer Glade", "Deer Rider", "Mobile (shoots on the move)", S(45, 6, 2.2f, 14f, 5.0f, Mode.Shoot, UFlag.Big | UFlag.Kite, Proj.ElfArrow, PKind.Blood, 12f), new Res(6, 5, 5, 0), 13f, 4, 1.5f);

            // ---------------- Người lùn ----------------
            u[UnitType.ShieldWarrior] = U("Stone Hall", "Shield Warrior", "Frontline (very tanky)", S(80, 6, 1.0f, 1.0f, 2.7f), new Res(6, 5, 0, 3), 9f, 1, 5f);
            u[UnitType.Crossbow] = U("Crossbow Works", "Heavy Crossbow", "Ranged (high damage, slow reload)", S(24, 18, 3.6f, 22f, 2.6f, Mode.Shoot, 0, Proj.Bolt), new Res(5, 0, 5, 4), 11f, 1, 3f);
            s = S(40, 13, 1.0f, 1.0f, 3.1f); s.splash = 1.3f;                                 // bổ quét
            u[UnitType.Axeman] = U("War Forge", "Axeman", "Striker (cleaves)", s, new Res(5, 0, 0, 5), 9f, 3, 2f);
            s = S(70, 15, 8.0f, 36f, 1.6f, Mode.Shoot, UFlag.Big, Proj.Cannonball, PKind.Spark, 6f); s.splash = 2f; s.minRange = 6f;
            u[UnitType.Cannon] = U("Cannon Foundry", "Cannon", "Area damage", s, new Res(8, 0, 0, 24), 36f, 7, 0.5f);
            s = S(110, 9, 1.1f, 1.2f, 4.8f, Mode.Melee, UFlag.Big, 0, PKind.Blood, 11f); s.chargeBonus = 1.2f; s.knock = 0.7f;
            u[UnitType.GoatRider] = U("Goat Pen", "Goat Rider", "Mobile (slower than cavalry, tankier)", s, new Res(6, 6, 0, 5), 13f, 4, 1.5f);

            // ---------------- Người hoá thú ----------------
            s = S(90, 10, 1.3f, 1.2f, 2.6f, Mode.Melee, UFlag.Big, 0, PKind.Blood, 7f); s.knock = 0.8f; s.splash = 1.5f;   // vả quét + hất văng
            u[UnitType.Bear] = U("Bear Den", "Bearfolk", "Frontline (sweeping knockback)", s, new Res(6, 12, 0, 0), 13f, 1, 4f);
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
            // ---------------- Orc ----------------
            s = S(52, 8, 1.0f, 1.0f, 3.0f); s.splash = 1.2f;
            u[UnitType.Grunt] = U("War Camp", "Orc Grunt", "Frontline (cleaves)", s, new Res(5, 6, 0, 0), 8f, 1, 5f);
            u[UnitType.SpearThrower] = U("Spear Pit", "Spear Thrower", "Ranged (heavy spears)", S(22, 10, 2.0f, 18f, 2.8f, Mode.Shoot, 0, Proj.Javelin), new Res(5, 0, 5, 0), 9f, 1, 3f);
            s = S(90, 10, 1.1f, 1.2f, 4.6f, Mode.Melee, UFlag.Big, 0, PKind.Blood, 11f); s.chargeBonus = 1.5f; s.knock = 1.0f;
            u[UnitType.BoarRider] = U("Boar Pen", "Boar Rider", "Mobile (charge, knockback)", s, new Res(6, 7, 0, 3), 12f, 3, 1.5f);
            u[UnitType.Drummer] = U("War Drum", "War Drummer", "Support (nearby allies attack faster)", S(30, 3, 1.5f, 1.0f, 2.8f), new Res(5, 0, 0, 6), 14f, 5, 1f);
            s = S(160, 16, 1.8f, 1.3f, 2.2f, Mode.Melee, UFlag.Big, 0, PKind.Blood, 6f); s.splash = 1.6f; s.knock = 0.8f;
            u[UnitType.Troll] = U("Troll Cave", "Troll", "Brute (regenerates, area smash)", s, new Res(8, 10, 0, 6), 16f, 7, 1f);

            u[UnitType.Voidling] = U("", "Voidling", "Spawned by the Summoner", S(10, 3, 0.8f, 1.0f, 3.6f, Mode.Melee, 0, 0, PKind.VoidGoo, 12f), default, 0f, 99, 0f);
            u[UnitType.Spiderling] = U("", "Spiderling", "Spawned by the Broodmother (venomous bite)", S(14, 4, 0.8f, 1.0f, 4.2f, Mode.Melee, 0, 0, PKind.VoidGoo, 14f), default, 0f, 99, 0f);

            // ---------------- Trung lập ----------------
            // Rồng (lõi Dragon's Pact): bay qua hàng địch, phun lửa lan. Máu / sát thương còn nhân theo đợt (Game.SpawnDragon)
            s = S(900, 24, 2.0f, 8f, 3.4f, Mode.Blast, UFlag.Big | UFlag.Giant, 0, PKind.Fire, 8f); s.splash = 3.2f;   // đi bộ: chiếm chỗ, chặn đường
            s.arrowResist = 0.8f;                                                                                       // vảy rồng: tên / nỏ / lao chỉ còn 20% sát thương
            u[UnitType.Dragon] = U("", "Dragon", "Legendary ally (fire breath)", s, default, 0f, 99, 0f);
            // Hoàng đế vàng (lõi bí mật The Golden Emperor): khổng lồ giáp vàng, đại kiếm chém quét; hào quang + Chiếu chỉ ở ResolveJob
            s = S(1400, 30, 1.6f, 1.2f, 2.6f, Mode.Melee, UFlag.Big | UFlag.Giant, 0, PKind.Blood, 7f); s.splash = 2.2f;
            s.arrowResist = 0.5f; s.knock = 0.6f;                                                                       // giáp vàng: tên / nỏ / lao chỉ còn 50%; đòn chém hất lùi
            u[UnitType.Emperor] = U("", "The Golden Emperor", "Legendary (Human). Golden aura heals allies, burns foes. Imperial Decree stuns. As a boss he rises again once at 50% HP", s, default, 0f, 99, 0f);

            // ---------------- Boss đợt trùm (mỗi phe 1 con, chiêu ở ResolveJob.Boss / GiantHit, DeathJob) ----------------
            s = S(2400, 22, 2.0f, 1.6f, 2.0f, Mode.Melee, UFlag.Big | UFlag.Giant, 0, PKind.Splinter, 6f); s.splash = 1.8f; s.knock = 1.2f; s.arrowResist = 0.3f;
            u[UnitType.WorldTree] = U("", "The World Tree", "Elf boss. Every Treant regenerates 3 HP/s. Its punch sends a shockwave that hurls troops back", s, default, 0f, 99, 0f);
            s = S(1800, 36, 1.6f, 1.3f, 2.4f, Mode.Melee, UFlag.Big | UFlag.Giant, 0, PKind.Blood, 7f); s.splash = 2f; s.knock = 0.8f; s.arrowResist = 0.4f;
            u[UnitType.Thor] = U("", "Thor", "Dwarf boss. Every hammer blow sends chain lightning. Allies near him attack faster", s, default, 0f, 99, 0f);
            s = S(1600, 30, 1.3f, 1.3f, 2.8f, Mode.Melee, UFlag.Big | UFlag.Giant, 0, PKind.Blood, 8f); s.splash = 2f; s.knock = 0.6f;
            u[UnitType.Berserker] = U("", "The Berserker", "Beastfolk boss. Heals 10% of the damage he deals. Below 25% HP he becomes Fenrir", s, default, 0f, 99, 0f);
            s = S(1600, 30, 0.55f, 1.4f, 5f, Mode.Melee, UFlag.Big | UFlag.Giant, 0, PKind.Blood, 12f); s.splash = 1.6f; s.knock = 0.4f;
            u[UnitType.Fenrir] = U("", "Fenrir", "The Berserker's wolf form: a terrifying howl, then frenzied fast attacks", s, default, 0f, 99, 0f);
            s = S(2000, 24, 1.4f, 1.4f, 2.6f, Mode.Melee, UFlag.Big | UFlag.Giant, 0, PKind.VoidGoo, 10f); s.splash = 1.6f; s.arrowResist = 0.2f;
            s = S(2200, 34, 1.4f, 1.4f, 2.6f, Mode.Melee, UFlag.Big | UFlag.Giant, 0, PKind.Blood, 8f); s.splash = 2.2f; s.knock = 1f; s.arrowResist = 0.3f;
            u[UnitType.Warboss] = U("", "The Warboss", "Orc boss. War Cry: nearby allies attack much faster and heal, nearby foes flee", s, default, 0f, 99, 0f);
            u[UnitType.Broodmother] = U("", "The Broodmother", "Void boss. Venomous bite. Every 10s she lays 4 spiderlings", s, default, 0f, 99, 0f);

            // Đặc trưng phe được nhân sẵn vào bảng chỉ số
            for (int t = 0; t < UnitType.Count; t++)
            {
                int fi = Faction.Of(t);
                var f = fi >= 0 ? Factions[fi] : new FactionDef();     // quân trung lập: không có đặc trưng phe
                u[t].stats.maxHp *= f.hpMul;
                u[t].stats.damage *= f.dmgMul;
                u[t].stats.speed *= f.speedMul * Tuning.SpeedScale;
                if (u[t].stats.mode != Mode.Melee) u[t].stats.range *= f.rangeMul * Tuning.RangeScale;
                u[t].stats.radius = Cfg.RadiusOf(u[t].stats.flags);
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
                b[B.BarracksOf(t)] = new BuildingDef
                {
                    id = B.BarracksOf(t), name = u.house, cat = Cat.Barracks, period = u.callTime, unit = t, faction = Faction.Of(t),
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
