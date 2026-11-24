using UnityEngine;

namespace HordeLab
{
    /// <summary>
    /// Số liệu THÔ của 1 loại quân (GD điền). Đặc trưng riêng của quân (kiểu đánh, đánh lan, hất văng, giáp, né, kỹ năng...)
    /// nằm trong code (GameData.DefaultUnits). Vào game, số ở đây còn nhân đặc trưng phe, Tuning.SpeedScale / RangeScale,
    /// sao của nhà lính hoặc hệ số theo đợt của địch.
    /// </summary>
    [CreateAssetMenu(menuName = "Fantasy World War/Unit", fileName = "Unit")]
    public class UnitBalance : ScriptableObject
    {
        [Tooltip("Mã loại quân trong code (UnitType). KHÔNG sửa.")]
        public int unitType = -1;

        [Header("Chỉ số")]
        [Min(1f)] public float maxHp = 40f;
        [Min(0f)] public float damage = 6f;
        [Tooltip("Giây giữa 2 đòn đánh.")]
        [Min(0.05f)] public float cooldown = 1f;
        [Tooltip("Tầm đánh (quân đánh xa còn nhân Tuning.RangeScale).")]
        [Min(0f)] public float range = 1f;
        [Tooltip("Tốc độ đi (còn nhân Tuning.SpeedScale).")]
        [Min(0f)] public float speed = 3f;

        [Header("Nhà lính (bỏ qua với quân con / boss)")]
        [Tooltip("Chi phí mỗi lần gọi 1 tiểu đội.")]
        [Min(0f)] public float costPop, costFood, costWood, costGold;
        [Tooltip("Giây hồi giữa 2 lần gọi.")]
        [Min(0f)] public float callTime = 10f;

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
        }

        /// <summary>Chép số gốc trong code vào asset (công cụ tạo asset).</summary>
        public void CopyFrom(int type, UnitDef u)
        {
            unitType = type;
            var s = u.stats;
            maxHp = s.maxHp; damage = s.damage; cooldown = s.cooldown; range = s.range; speed = s.speed;
            costPop = u.cost.pop; costFood = u.cost.food; costWood = u.cost.wood; costGold = u.cost.gold;
            callTime = u.callTime; minWave = u.minWave; waveWeight = u.waveWeight;
        }
    }
}
