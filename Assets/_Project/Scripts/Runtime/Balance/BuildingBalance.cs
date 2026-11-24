using UnityEngine;

namespace HordeLab
{
    /// <summary>
    /// Nhà tài nguyên / nhà buff: tên, chu kỳ, sản lượng mỗi chu kỳ (1 sao) và mô tả.
    /// Hiệu ứng đặc biệt của từng nhà (ví dụ +15% mỗi Wheat Field kề nhau, độ mạnh buff) vẫn nằm trong code (City.cs).
    /// Nhà lính lấy tên / thời gian gọi từ UnitBalance của loại quân.
    /// </summary>
    [CreateAssetMenu(menuName = "Fantasy World War/Building", fileName = "Building")]
    public class BuildingBalance : ScriptableObject
    {
        [Tooltip("Mã công trình trong code (B.*). KHÔNG sửa.")]
        public int building = -1;
        public string displayName;
        [Tooltip("Giây mỗi chu kỳ sản xuất (0 = nhà buff, không tự sản xuất).")]
        [Min(0f)] public float period;
        [Tooltip("Sản lượng mỗi chu kỳ khi 1 sao (nhà nhiều sao nhân Tuning.starYield).")]
        [Min(0f)] public float amount;
        [TextArea(2, 5)] public string description;

        public void ApplyTo(BuildingDef b)
        {
            if (b == null) return;
            if (!string.IsNullOrEmpty(displayName)) b.name = displayName;
            b.period = period; b.amount = amount;
            if (!string.IsNullOrEmpty(description)) b.desc = description;
        }

        public void CopyFrom(BuildingDef b)
        {
            building = b.id; displayName = b.name; period = b.period; amount = b.amount; description = b.desc;
        }
    }
}
