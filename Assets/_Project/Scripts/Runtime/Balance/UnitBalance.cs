using UnityEngine;

namespace HordeLab
{
    /// <summary>
    /// Chỉ số 1 loại quân. Số ở đây là số GỐC: lúc vào game còn nhân đặc trưng phe (FactionBalance),
    /// tốc độ nhân Tuning.speedScale, tầm bắn (quân đánh xa) nhân Tuning.rangeScale, rồi nhân theo sao của nhà lính / theo đợt của địch.
    /// </summary>
    [CreateAssetMenu(menuName = "Fantasy World War/Unit", fileName = "Unit")]
    public class UnitBalance : ScriptableObject
    {
        [Tooltip("Mã loại quân trong code (UnitType). KHÔNG sửa.")]
        public int unitType = -1;

        [Header("Tên hiển thị")]
        public string displayName;
        [Tooltip("Tên nhà lính gọi ra loại quân này (để trống nếu không có nhà lính).")]
        public string house;
        [Tooltip("Mô tả ngắn vai trò, hiện ở màn chọn phe và tooltip.")]
        public string role;

        [Header("Chỉ số chiến đấu")]
        [Min(1f)] public float maxHp = 40f;
        [Min(0f)] public float damage = 6f;
        [Tooltip("Giây giữa 2 đòn đánh.")]
        [Min(0.05f)] public float cooldown = 1f;
        [Tooltip("Tầm đánh. Quân đánh xa còn nhân Tuning.rangeScale.")]
        [Min(0f)] public float range = 1f;
        [Tooltip("Tốc độ đi. Còn nhân Tuning.speedScale.")]
        [Min(0f)] public float speed = 3f;
        public AttackMode mode = AttackMode.Melee;
        [Tooltip("Big / Giant quyết định cỡ hình và cỡ thân: đổi 2 cờ này có thể làm hỏng hình vẽ.")]
        public UnitFlags flags;
        [Tooltip("Loại đạn (chỉ dùng khi mode = Shoot).")]
        public ProjectileKind projectile = ProjectileKind.Arrow;
        [Tooltip("Hiệu ứng khi trúng đòn / chết.")]
        public BloodKind blood = BloodKind.Blood;
        [Min(1f)] public float animFps = 9f;

        [Header("Hiệu ứng thêm (0 = không có)")]
        [Tooltip("Bán kính đánh lan / nổ lan.")]
        [Min(0f)] public float splash;
        [Tooltip("Số giây làm chậm nạn nhân.")]
        [Min(0f)] public float slow;
        [Tooltip("Hất văng nạn nhân mỗi đòn.")]
        [Min(0f)] public float knock;
        [Tooltip("Kỵ binh: đòn xung phong đầy đà mạnh gấp (1 + chargeBonus) lần.")]
        [Min(0f)] public float chargeBonus;
        [Tooltip("Giảm sát thương từ tên / nỏ / lao (0..1).")]
        [Range(0f, 1f)] public float arrowResist;
        [Tooltip("Xác suất né đòn (0..1).")]
        [Range(0f, 1f)] public float dodge;
        [Tooltip("Máy công thành: địch áp sát gần hơn ngần này thì không bắn được.")]
        [Min(0f)] public float minRange;
        [Tooltip("Nổ khi chết: sát thương / bán kính.")]
        [Min(0f)] public float explodeDamage, explodeRadius;

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

        /// <summary>Ghi số trong asset đè lên dữ liệu gốc trong code.</summary>
        public void ApplyTo(UnitDef u)
        {
            if (u == null) return;
            if (!string.IsNullOrEmpty(displayName)) u.name = displayName;
            if (house != null) u.house = house;
            if (!string.IsNullOrEmpty(role)) u.role = role;
            var s = u.stats;
            int keepGiant = s.flags & UFlag.Giant;             // cỡ khổng lồ gắn với hình vẽ + atlas: không cho đổi
            s.maxHp = maxHp; s.damage = damage; s.cooldown = cooldown; s.range = range; s.speed = speed;
            s.mode = (int)mode; s.flags = ((int)flags & ~UFlag.Giant) | keepGiant; s.proj = (int)projectile; s.blood = (int)blood; s.animFps = animFps;
            s.splash = splash; s.slow = slow; s.knock = knock; s.chargeBonus = chargeBonus; s.arrowResist = arrowResist; s.dodge = dodge;
            s.minRange = minRange; s.explodeDmg = explodeDamage; s.explodeRadius = explodeRadius;
            u.stats = s;
            u.cost = new Res(costPop, costFood, costWood, costGold);
            u.callTime = callTime; u.minWave = minWave; u.waveWeight = waveWeight;
        }

        /// <summary>Chép số gốc trong code vào asset (công cụ tạo asset).</summary>
        public void CopyFrom(int type, UnitDef u)
        {
            unitType = type;
            displayName = u.name; house = u.house; role = u.role;
            var s = u.stats;
            maxHp = s.maxHp; damage = s.damage; cooldown = s.cooldown; range = s.range; speed = s.speed;
            mode = (AttackMode)s.mode; flags = (UnitFlags)s.flags; projectile = (ProjectileKind)s.proj; blood = (BloodKind)s.blood; animFps = s.animFps;
            splash = s.splash; slow = s.slow; knock = s.knock; chargeBonus = s.chargeBonus; arrowResist = s.arrowResist; dodge = s.dodge;
            minRange = s.minRange; explodeDamage = s.explodeDmg; explodeRadius = s.explodeRadius;
            costPop = u.cost.pop; costFood = u.cost.food; costWood = u.cost.wood; costGold = u.cost.gold;
            callTime = u.callTime; minWave = u.minWave; waveWeight = u.waveWeight;
        }
    }
}
