using UnityEngine;

namespace HordeLab
{
    /// <summary>
    /// Số liệu THÔ của 1 loại quân (GD điền). Đặc trưng riêng của quân (kiểu đánh, đánh lan, hất văng, giáp, né, kỹ năng...)
    /// nằm trong code (GameData.DefaultUnits). Vào game, số ở đây còn nhân đặc trưng phe, Tuning.SpeedScale / RangeScale,
    /// hệ số theo sao của nhà lính (bên dưới) hoặc hệ số theo đợt của địch.
    /// </summary>
    [CreateAssetMenu(menuName = "Fantasy World War/Unit", fileName = "Unit")]
    public class UnitBalance : ScriptableObject
    {
        [HideInInspector] public int unitType = -1;     // mã loại quân trong code (UnitType)

        [Header("Chỉ số (1 sao)")]
        [Min(1f)] public float maxHp = 40f;
        [Min(0f)] public float damage = 6f;
        [Tooltip("Giây giữa 2 đòn đánh.")]
        [Min(0.05f)] public float cooldown = 1f;
        [Tooltip("Tầm đánh (quân đánh xa còn nhân Tuning.RangeScale).")]
        [Min(0f)] public float range = 1f;
        [Tooltip("Tốc độ đi (còn nhân Tuning.SpeedScale).")]
        [Min(0f)] public float speed = 3f;

        [Header("Gọi quân (1 sao) — quân con / boss bỏ qua")]
        [Tooltip("Chi phí Dân mỗi lần gọi 1 tiểu đội.")]
        [Min(0f)] public float costPop;
        [Tooltip("Chi phí Lương mỗi lần gọi.")]
        [Min(0f)] public float costFood;
        [Tooltip("Chi phí Gỗ mỗi lần gọi.")]
        [Min(0f)] public float costWood;
        [Tooltip("Chi phí Vàng mỗi lần gọi.")]
        [Min(0f)] public float costGold;
        [Tooltip("Giây hồi giữa 2 lần gọi.")]
        [Min(0f)] public float callTime = 10f;

        [Header("Nâng cấp theo sao nhà lính (nhân với số 1 sao)")]
        [Tooltip("Hệ số máu của lính khi nhà lính 1 / 2 / 3 / 4 sao.")]
        public StarValues hpPerStar = new StarValues(1f, 1.4f, 1.8f, 2.2f);
        [Tooltip("Hệ số sát thương của lính khi nhà lính 1 / 2 / 3 / 4 sao.")]
        public StarValues damagePerStar = new StarValues(1f, 1.4f, 1.8f, 2.2f);
        [Tooltip("Hệ số chi phí mỗi lần gọi khi nhà lính 1 / 2 / 3 / 4 sao.")]
        public StarValues costPerStar = new StarValues(1f, 1.5f, 2f, 2.5f);

        [Header("Khi làm quân địch")]
        [Tooltip("Xuất hiện trong đội hình địch từ đợt này.")]
        [Min(1)] public int minWave = 1;
        [Tooltip("Tỉ lệ trong đội hình địch (so với các loại khác cùng phe).")]
        [Min(0f)] public float waveWeight = 1f;

        /// <summary>Ghi số trong asset đè lên số gốc trong code.</summary>
        public void ApplyTo(UnitDef u)
        {
            if (u == null) return;
            var s = u.stats;
            s.maxHp = maxHp; s.damage = damage; s.cooldown = cooldown; s.range = range; s.speed = speed;
            u.stats = s;
            u.cost = new Res(costPop, costFood, costWood, costGold);
            u.callTime = callTime; u.minWave = minWave; u.waveWeight = waveWeight;
            if (!hpPerStar.IsEmpty) hpPerStar.CopyTo(u.starHp);
            if (!damagePerStar.IsEmpty) damagePerStar.CopyTo(u.starDamage);
            if (!costPerStar.IsEmpty) costPerStar.CopyTo(u.starCost);
        }

        /// <summary>Chép số gốc trong code vào asset (công cụ tạo asset).</summary>
        public void CopyFrom(int type, UnitDef u)
        {
            unitType = type;
            var s = u.stats;
            maxHp = s.maxHp; damage = s.damage; cooldown = s.cooldown; range = s.range; speed = s.speed;
            costPop = u.cost.pop; costFood = u.cost.food; costWood = u.cost.wood; costGold = u.cost.gold;
            callTime = u.callTime; minWave = u.minWave; waveWeight = u.waveWeight;
            hpPerStar = new StarValues(u.starHp); damagePerStar = new StarValues(u.starDamage); costPerStar = new StarValues(u.starCost);
        }

        /// <summary>Asset tạo trước khi có phần nâng cấp theo sao: điền hệ số mặc định (không động vào số khác).</summary>
        public bool FillMissingStars(UnitDef u)
        {
            bool changed = false;
            if (hpPerStar.IsEmpty) { hpPerStar = new StarValues(u.starHp); changed = true; }
            if (damagePerStar.IsEmpty) { damagePerStar = new StarValues(u.starDamage); changed = true; }
            if (costPerStar.IsEmpty) { costPerStar = new StarValues(u.starCost); changed = true; }
            return changed;
        }
    }
}
