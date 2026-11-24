using UnityEngine;

namespace HordeLab
{
    /// <summary>
    /// Số liệu nhà tài nguyên / nhà buff (GD điền): chu kỳ và sản lượng mỗi chu kỳ khi 1 sao.
    /// Tên, mô tả và hiệu ứng riêng từng nhà nằm trong code. Nhà lính lấy thời gian gọi / chi phí từ UnitBalance.
    /// </summary>
    [CreateAssetMenu(menuName = "Fantasy World War/Building", fileName = "Building")]
    public class BuildingBalance : ScriptableObject
    {
        [Tooltip("Mã công trình trong code (B.*). KHÔNG sửa.")]
        public int building = -1;
        [Tooltip("Giây mỗi chu kỳ sản xuất (0 = nhà buff, không tự sản xuất).")]
        [Min(0f)] public float period;
        [Tooltip("Sản lượng mỗi chu kỳ khi 1 sao (nhà nhiều sao nhân Tuning.StarYield).")]
        [Min(0f)] public float amount;

        public void ApplyTo(BuildingDef b)
        {
            if (b == null) return;
            b.period = period; b.amount = amount;
        }

        public void CopyFrom(BuildingDef b)
        {
            building = b.id; period = b.period; amount = b.amount;
        }
    }
}
