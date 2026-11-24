using UnityEngine;

namespace HordeLab
{
    /// <summary>
    /// Số liệu THÔ của 1 loại quân (GD điền). Máu, sát thương, chi phí gọi nhập thẳng con số ở từng mốc sao của nhà lính (1★..4★).
    /// Đặc trưng riêng của quân (kiểu đánh, đánh lan, hất văng, giáp, né, kỹ năng...) nằm trong code (GameData.DefaultUnits).
    /// Vào game còn nhân đặc trưng phe, Tuning.SpeedScale / RangeScale; quân địch dùng số 1★ nhân hệ số theo đợt.
    /// </summary>
    [CreateAssetMenu(menuName = "Fantasy World War/Unit", fileName = "Unit")]
    public class UnitBalance : ScriptableObject
    {
        [HideInInspector] public int unitType = -1;     // mã loại quân trong code (UnitType)

        [Header("Theo sao nhà lính: 1★ 2★ 3★ 4★ (quân địch / quân con / boss dùng 1★)")]
        [Tooltip("Máu mỗi lính.")]
        public StarValues maxHp;
        [Tooltip("Sát thương mỗi đòn.")]
        public StarValues damage;
        [Tooltip("Chi phí Dân mỗi lần gọi 1 tiểu đội.")]
        public StarValues costPop;
        [Tooltip("Chi phí Lương mỗi lần gọi.")]
        public StarValues costFood;
        [Tooltip("Chi phí Gỗ mỗi lần gọi.")]
        public StarValues costWood;
        [Tooltip("Chi phí Vàng mỗi lần gọi.")]
        public StarValues costGold;
        [Tooltip("Giây hồi giữa 2 lần gọi của nhà lính.")]
        public StarValues callTime;

        [Header("Chỉ số chung")]
        [Tooltip("Giây giữa 2 đòn đánh.")]
        [Min(0.05f)] public float cooldown = 1f;
        [Tooltip("Tầm đánh (quân đánh xa còn nhân Tuning.RangeScale).")]
        [Min(0f)] public float range = 1f;
        [Tooltip("Tốc độ đi (còn nhân Tuning.SpeedScale).")]
        [Min(0f)] public float speed = 3f;

        [Header("Khi làm quân địch")]
        [Tooltip("Xuất hiện trong đội hình địch từ đợt này.")]
        [Min(1)] public int minWave = 1;
        [Tooltip("Tỉ lệ trong đội hình địch (so với các loại khác cùng phe).")]
        [Min(0f)] public float waveWeight = 1f;

        /// <summary>Ghi số trong asset đè lên số gốc trong code. Số 1★ là chỉ số gốc; các mốc sao khác thành hệ số so với 1★.</summary>
        public void ApplyTo(UnitDef u)
        {
            if (u == null) return;
            var s = u.stats;
            if (!maxHp.IsEmpty && maxHp.star1 > 0f) { s.maxHp = maxHp.star1; for (int i = 0; i < 4; i++) u.starHp[i] = maxHp[i] / maxHp.star1; }
            if (!damage.IsEmpty && damage.star1 > 0f) { s.damage = damage.star1; for (int i = 0; i < 4; i++) u.starDamage[i] = damage[i] / damage.star1; }
            else if (!damage.IsEmpty) s.damage = 0f;
            s.cooldown = cooldown; s.range = range; s.speed = speed;
            u.stats = s;
            u.costAt = new Res[4];
            for (int i = 0; i < 4; i++) u.costAt[i] = new Res(costPop[i], costFood[i], costWood[i], costGold[i]);
            u.cost = u.costAt[0];
            if (!callTime.IsEmpty) { u.callTime = callTime.star1; u.callTimeAt = new[] { callTime.star1, callTime.star2, callTime.star3, callTime.star4 }; }
            u.minWave = minWave; u.waveWeight = waveWeight;
        }

        /// <summary>Asset tạo trước khi có thời gian gọi theo sao: điền số cũ cho cả 4 mốc (không động vào số khác).</summary>
        public bool FillMissing(UnitDef u)
        {
            if (!callTime.IsEmpty) return false;
            callTime = new StarValues(u.callTime, u.callTime, u.callTime, u.callTime);
            return true;
        }

        /// <summary>Điền số tạm từ code: 1★ = chỉ số gốc, các mốc sao khác theo tỉ lệ đang dùng.</summary>
        public void CopyFrom(int type, UnitDef u)
        {
            unitType = type;
            var s = u.stats;
            maxHp = StarValues.Scaled(s.maxHp, u.starHp);
            maxHp = new StarValues(Mathf.Round(maxHp.star1), Mathf.Round(maxHp.star2), Mathf.Round(maxHp.star3), Mathf.Round(maxHp.star4));   // máu: số nguyên
            damage = StarValues.Scaled(s.damage, u.starDamage);
            costPop = StarValues.Scaled(u.cost.pop, u.starCost); costFood = StarValues.Scaled(u.cost.food, u.starCost);
            costWood = StarValues.Scaled(u.cost.wood, u.starCost); costGold = StarValues.Scaled(u.cost.gold, u.starCost);
            cooldown = s.cooldown; range = s.range; speed = s.speed;
            callTime = new StarValues(u.callTime, u.callTime, u.callTime, u.callTime);   // tạm: lên sao chưa gọi nhanh hơn
            minWave = u.minWave; waveWeight = u.waveWeight;
        }
    }
}
