using UnityEngine;

namespace HordeLab
{
    /// <summary>
    /// Số chung của cả game: kinh tế, tinh thể, đợt địch, boss, độ khó, xác suất bậc lõi...
    /// (Số quyết định cấu trúc như số tim, số đợt, số sao tối đa, số ô cửa hàng vẫn cố định trong code.)
    /// </summary>
    [CreateAssetMenu(menuName = "Fantasy World War/Tuning", fileName = "Tuning")]
    public class TuningBalance : ScriptableObject
    {
        [Header("Tinh thể & cửa hàng")]
        public int startCrystals = 5;
        [Tooltip("Mỗi đợt nhận cố định (thắng hay thua).")] public int crystalReward = 5;
        [Tooltip("Thêm khi giữ được thành.")] public int winBonus = 1;
        [Tooltip("Lãi: cứ interestStep tinh thể đang giữ thì +1, tối đa interestMax.")] public int interestStep = 10, interestMax = 5;
        public int rollCost = 1;
        [Tooltip("Giá thẻ: mở đất / nhà lính + nhà buff quân / các thẻ khác.")] public int priceExpand = 5, priceMilitary = 4, priceOther = 3;
        [Tooltip("Bán 1 thẻ / công trình 1 sao được ngần này tinh thể.")] public int sellValue = 1;
        [Tooltip("Xác suất có sự kiện đổi phe thù địch sau mỗi đợt.")] [Range(0f, 1f)] public float eventChance = 0.25f;

        [Header("Thành & kinh tế")]
        public int castleHp = 300;
        [Tooltip("Tài nguyên khởi đầu: Dân, Lương, Gỗ, Vàng.")] public float startPop = 30, startFood = 30, startWood = 30, startGold = 30;
        [Tooltip("Sản lượng nhà tài nguyên theo sao (1..4 sao).")] public float[] starYield = { 1f, 2f, 4f, 8f };
        [Tooltip("Hệ số máu + sát thương lính theo sao của nhà lính.")] public float[] starTroopPower = { 1f, 1.4f, 1.8f, 2.2f };
        [Tooltip("Hệ số chi phí mỗi lần gọi theo sao của nhà lính.")] public float[] starTroopCost = { 1f, 1.5f, 2f, 2.5f };
        [Tooltip("Độ mạnh buff theo sao.")] public float[] starBuff = { 1f, 1.5f, 2f, 2.5f };
        [Tooltip("Trần cộng dồn buff (1.5 = tối đa +150%).")] public float buffCap = 1.5f;
        [Tooltip("Giảm giá gọi quân tối đa.")] [Range(0f, 1f)] public float costCutCap = 0.6f;
        [Tooltip("Market: mỗi chu kỳ đổi ngần này Vàng lấy ngần này tài nguyên (x sao).")] public float marketTrade = 5f;
        [Tooltip("Quá ngần này lính trên sân thì nhà lính tạm ngừng gọi.")] public int playerUnitCap = 4000;
        [Tooltip("Tốc độ đi mọi loại quân nhân hệ số này.")] public float speedScale = 0.6f;
        [Tooltip("Tầm bắn quân đánh xa nhân hệ số này.")] public float rangeScale = 0.6f;

        [Header("Đợt địch")]
        [Tooltip("Số tiểu đội = a + b x đợt + c x đợt²")] public float squadsA = 5f, squadsB = 2.2f, squadsC = 0.16f;
        [Tooltip("Quân địch kéo đến rải đều trong (a + b x đợt) giây")] public float durationA = 45f, durationB = 2f;
        [Tooltip("Máu + sát thương địch = 1 + hệ số x (đợt - 1)")] public float powerPerWave = 0.04f;
        [Tooltip("Quá ngần này giây sau khi địch đến hết mà chưa xong thì coi như địch rút.")] public float waveTimeout = 150f;
        [Tooltip("Đợt trăng tròn: quân Beastfolk mạnh gấp.")] public float fullMoonPower = 1.4f;

        [Header("Đợt trùm (boss)")]
        [Tooltip("Số quân đợt trùm nhân thêm.")] public float bossSize = 1.25f;
        [Tooltip("Sức quân đợt trùm nhân thêm.")] public float bossPower = 1f;
        [Tooltip("Máu boss nhân thêm (ngoài hệ số theo đợt).")] public float bossHp = 3f;
        [Tooltip("Boss vào trận sau ngần này giây.")] public float bossDelay = 4f;

        [Header("Độ khó: Easy / Normal / Hard")]
        public float[] difficultySquads = { 0.7f, 0.85f, 1f };
        public float[] difficultyBossHp = { 0.7f, 0.85f, 1f };
        public int[] difficultyCrystals = { 5, 2, 0 };

        [Header("Lõi: xác suất bậc Silver / Gold / Prismatic ở lần chọn 1..5")]
        public Vector3Int[] augmentTierOdds = { new Vector3Int(50, 45, 5), new Vector3Int(35, 50, 15), new Vector3Int(20, 55, 25), new Vector3Int(10, 55, 35), new Vector3Int(5, 50, 45) };

        static void Copy(float[] src, float[] dst) { if (src == null) return; for (int i = 0; i < Mathf.Min(src.Length, dst.Length); i++) dst[i] = src[i]; }
        static void Copy(int[] src, int[] dst) { if (src == null) return; for (int i = 0; i < Mathf.Min(src.Length, dst.Length); i++) dst[i] = src[i]; }

        public void ApplyTo()
        {
            Tuning.StartCrystals = startCrystals; Tuning.CrystalReward = crystalReward; Tuning.WinBonus = winBonus;
            Tuning.InterestStep = Mathf.Max(1, interestStep); Tuning.InterestMax = interestMax; Tuning.RollCost = rollCost;
            Tuning.PriceExpand = priceExpand; Tuning.PriceMilitary = priceMilitary; Tuning.PriceOther = priceOther;
            Tuning.SellValue = sellValue; Tuning.EventChance = eventChance;
            Tuning.CastleHp = castleHp; Tuning.StartRes = new Res(startPop, startFood, startWood, startGold);
            Copy(starYield, Tuning.StarYield); Copy(starTroopPower, Tuning.StarTroopPower); Copy(starTroopCost, Tuning.StarTroopCost); Copy(starBuff, Tuning.StarBuff);
            Tuning.BuffCap = buffCap; Tuning.CostCutCap = costCutCap; Tuning.MarketTrade = marketTrade; Tuning.PlayerUnitCap = playerUnitCap;
            Tuning.SpeedScale = speedScale; Tuning.RangeScale = rangeScale;
            Tuning.SquadsA = squadsA; Tuning.SquadsB = squadsB; Tuning.SquadsC = squadsC; Tuning.DurationA = durationA; Tuning.DurationB = durationB;
            Tuning.PowerPerWave = powerPerWave; Tuning.WaveTimeout = waveTimeout; Tuning.FullMoonPower = fullMoonPower;
            Tuning.BossSize = bossSize; Tuning.BossPower = bossPower; Tuning.BossHp = bossHp; Tuning.BossDelay = bossDelay;
            Copy(difficultySquads, Tuning.DiffSquads); Copy(difficultyBossHp, Tuning.DiffBoss); Copy(difficultyCrystals, Tuning.DiffCrystals);
            if (augmentTierOdds != null)
                for (int i = 0; i < Mathf.Min(augmentTierOdds.Length, Tuning.AugTierOdds.Length); i++)
                    Tuning.AugTierOdds[i] = new[] { augmentTierOdds[i].x, augmentTierOdds[i].y, augmentTierOdds[i].z };
        }

        public void CopyFromTuning()
        {
            startCrystals = Tuning.StartCrystals; crystalReward = Tuning.CrystalReward; winBonus = Tuning.WinBonus;
            interestStep = Tuning.InterestStep; interestMax = Tuning.InterestMax; rollCost = Tuning.RollCost;
            priceExpand = Tuning.PriceExpand; priceMilitary = Tuning.PriceMilitary; priceOther = Tuning.PriceOther;
            sellValue = Tuning.SellValue; eventChance = Tuning.EventChance;
            castleHp = Tuning.CastleHp; startPop = Tuning.StartRes.pop; startFood = Tuning.StartRes.food; startWood = Tuning.StartRes.wood; startGold = Tuning.StartRes.gold;
            starYield = (float[])Tuning.StarYield.Clone(); starTroopPower = (float[])Tuning.StarTroopPower.Clone(); starTroopCost = (float[])Tuning.StarTroopCost.Clone(); starBuff = (float[])Tuning.StarBuff.Clone();
            buffCap = Tuning.BuffCap; costCutCap = Tuning.CostCutCap; marketTrade = Tuning.MarketTrade; playerUnitCap = Tuning.PlayerUnitCap;
            speedScale = Tuning.SpeedScale; rangeScale = Tuning.RangeScale;
            squadsA = Tuning.SquadsA; squadsB = Tuning.SquadsB; squadsC = Tuning.SquadsC; durationA = Tuning.DurationA; durationB = Tuning.DurationB;
            powerPerWave = Tuning.PowerPerWave; waveTimeout = Tuning.WaveTimeout; fullMoonPower = Tuning.FullMoonPower;
            bossSize = Tuning.BossSize; bossPower = Tuning.BossPower; bossHp = Tuning.BossHp; bossDelay = Tuning.BossDelay;
            difficultySquads = (float[])Tuning.DiffSquads.Clone(); difficultyBossHp = (float[])Tuning.DiffBoss.Clone(); difficultyCrystals = (int[])Tuning.DiffCrystals.Clone();
            augmentTierOdds = new Vector3Int[Tuning.AugTierOdds.Length];
            for (int i = 0; i < augmentTierOdds.Length; i++) augmentTierOdds[i] = new Vector3Int(Tuning.AugTierOdds[i][0], Tuning.AugTierOdds[i][1], Tuning.AugTierOdds[i][2]);
        }
    }
}
