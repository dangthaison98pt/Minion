using UnityEngine;

namespace HordeLab
{
    /// <summary>
    /// Số liệu nhà tài nguyên / nhà buff (GD điền): chu kỳ, sản lượng ở từng mốc sao, độ mạnh buff theo sao.
    /// Tên, mô tả và hiệu ứng riêng từng nhà (ví dụ +15% mỗi Wheat Field kề nhau, +50% của Windmill) nằm trong code.
    /// Nhà lính lấy số liệu từ UnitBalance của loại quân.
    /// </summary>
    [CreateAssetMenu(menuName = "Fantasy World War/Building", fileName = "Building")]
    public class BuildingBalance : ScriptableObject
    {
        [HideInInspector] public int building = -1;    // mã công trình trong code (B.*)

        [Tooltip("Giây mỗi chu kỳ sản xuất (0 = nhà buff, không tự sản xuất).")]
        [Min(0f)] public float period;

        [Header("Theo sao: 1★ 2★ 3★ 4★")]
        [Tooltip("Nhà tài nguyên: sản lượng mỗi chu kỳ ở từng mốc sao (Market: số Vàng đổi = số tài nguyên nhận).")]
        public StarValues amount;
        [Tooltip("Nhà buff: hệ số độ mạnh buff (1 = đúng mức ghi trong mô tả, 2 = gấp đôi).")]
        public StarValues buffPower;

        public void ApplyTo(BuildingDef b)
        {
            if (b == null) return;
            b.period = period;
            if (!amount.IsEmpty)
            {
                b.amount = amount.star1;
                if (amount.star1 > 0f) for (int i = 0; i < 4; i++) b.starYield[i] = amount[i] / amount.star1;
            }
            if (!buffPower.IsEmpty) buffPower.CopyTo(b.starBuff);
        }

        /// <summary>Điền số tạm từ code: 1★ = số gốc, các mốc sao khác theo tỉ lệ đang dùng.</summary>
        public void CopyFrom(BuildingDef b)
        {
            building = b.id; period = b.period;
            amount = StarValues.Scaled(b.amount, b.starYield);
            buffPower = new StarValues(b.starBuff);
        }
    }
}
