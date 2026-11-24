using UnityEngine;

namespace HordeLab
{
    /// <summary>Đặc trưng 1 phe: hệ số nhân vào chỉ số mọi loại quân của phe + chữ mô tả ở màn chọn phe.</summary>
    [CreateAssetMenu(menuName = "Fantasy World War/Faction", fileName = "Faction")]
    public class FactionBalance : ScriptableObject
    {
        [Tooltip("Mã phe trong code (0 Human, 1 Elf, 2 Dwarf, 3 Beastfolk, 4 Void, 5 Orc). KHÔNG sửa.")]
        public int faction = -1;
        public string displayName;
        public Color color = Color.white;

        [Header("Hệ số nhân vào quân của phe")]
        [Min(0f)] public float hpMul = 1f;
        [Min(0f)] public float damageMul = 1f;
        [Min(0f)] public float speedMul = 1f;
        [Tooltip("Chỉ nhân tầm của quân đánh xa.")]
        [Min(0f)] public float rangeMul = 1f;
        [Tooltip("Chi phí Dân mỗi lần gọi nhân hệ số này.")]
        [Min(0f)] public float popCostMul = 1f;
        [Tooltip("Mỗi lần gọi ra squadSide x squadSide lính.")]
        [Range(1, 8)] public int squadSide = 5;

        [Header("Mô tả (màn chọn phe)")]
        [TextArea] public string strength;
        [TextArea] public string weakness;

        public void ApplyTo(FactionDef f)
        {
            if (f == null) return;
            if (!string.IsNullOrEmpty(displayName)) f.name = displayName;
            f.color = color;
            f.hpMul = hpMul; f.dmgMul = damageMul; f.speedMul = speedMul; f.rangeMul = rangeMul; f.popCostMul = popCostMul; f.squadSide = squadSide;
            if (!string.IsNullOrEmpty(strength)) f.strength = strength;
            if (!string.IsNullOrEmpty(weakness)) f.weakness = weakness;
        }

        public void CopyFrom(int id, FactionDef f)
        {
            faction = id; displayName = f.name; color = f.color;
            hpMul = f.hpMul; damageMul = f.dmgMul; speedMul = f.speedMul; rangeMul = f.rangeMul; popCostMul = f.popCostMul; squadSide = f.squadSide;
            strength = f.strength; weakness = f.weakness;
        }
    }
}
