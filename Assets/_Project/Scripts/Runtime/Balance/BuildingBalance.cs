using UnityEngine;

namespace HordeLab
{
    /// <summary>
    /// Số liệu nhà tài nguyên / nhà buff (GD điền): chu kỳ, sản lượng 1 sao và hệ số nâng cấp theo sao.
    /// Tên, mô tả và hiệu ứng riêng từng nhà nằm trong code. Nhà lính lấy số liệu từ UnitBalance của loại quân.
    /// </summary>
    [CreateAssetMenu(menuName = "Fantasy World War/Building", fileName = "Building")]
    public class BuildingBalance : ScriptableObject
    {
        [HideInInspector] public int building = -1;    // mã công trình trong code (B.*)

        [Header("Sản xuất (1 sao)")]
        [Tooltip("Giây mỗi chu kỳ sản xuất (0 = nhà buff, không tự sản xuất).")]
        [Min(0f)] public float period;
        [Tooltip("Sản lượng mỗi chu kỳ khi 1 sao.")]
        [Min(0f)] public float amount;

        [Header("Nâng cấp theo sao (nhân với số 1 sao)")]
        [Tooltip("Nhà tài nguyên: hệ số sản lượng khi 1 / 2 / 3 / 4 sao (Market: cả Vàng trả lẫn tài nguyên nhận).")]
        public StarValues yieldPerStar = new StarValues(1f, 2f, 4f, 8f);
        [Tooltip("Nhà buff: hệ số độ mạnh buff khi 1 / 2 / 3 / 4 sao.")]
        public StarValues buffPerStar = new StarValues(1f, 1.5f, 2f, 2.5f);

        public void ApplyTo(BuildingDef b)
        {
            if (b == null) return;
            b.period = period; b.amount = amount;
            if (!yieldPerStar.IsEmpty) yieldPerStar.CopyTo(b.starYield);
            if (!buffPerStar.IsEmpty) buffPerStar.CopyTo(b.starBuff);
        }

        public void CopyFrom(BuildingDef b)
        {
            building = b.id; period = b.period; amount = b.amount;
            yieldPerStar = new StarValues(b.starYield); buffPerStar = new StarValues(b.starBuff);
        }

        public bool FillMissingStars(BuildingDef b)
        {
            bool changed = false;
            if (yieldPerStar.IsEmpty) { yieldPerStar = new StarValues(b.starYield); changed = true; }
            if (buffPerStar.IsEmpty) { buffPerStar = new StarValues(b.starBuff); changed = true; }
            return changed;
        }
    }
}
