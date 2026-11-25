using UnityEngine;

namespace HordeLab
{
    /// <summary>
    /// Nạp số liệu thô của quân và nhà từ ScriptableObject (Resources/Balance/BalanceDatabase.asset).
    /// Số trong code là giá trị mặc định: thiếu asset nào thì dùng số trong code chỗ đó.
    /// Tạo / bổ sung asset: menu Horde Lab → Balance → Create missing balance assets.
    /// </summary>
    public static class Balance
    {
        public const string ResourcePath = "Balance/BalanceDatabase";
        static BalanceDatabase db;
        static bool loaded;

        public static BalanceDatabase Db
        {
            get
            {
                if (!loaded) { db = Resources.Load<BalanceDatabase>(ResourcePath); loaded = true; }
                return db;
            }
        }

        /// <summary>Công cụ editor: bỏ qua asset để đọc số gốc trong code (khi tạo asset lần đầu).</summary>
        public static bool IgnoreAssets;

        public static void ApplyUnits(UnitDef[] u)
        {
            if (IgnoreAssets || Db == null || Db.units == null) return;
            foreach (var a in Db.units) if (a != null && a.unitType >= 0 && a.unitType < u.Length) a.ApplyTo(u[a.unitType]);
        }

        public static void ApplyBuildings(BuildingDef[] b)
        {
            if (IgnoreAssets || Db == null || Db.buildings == null) return;
            foreach (var a in Db.buildings) if (a != null && B.HasBalance(a.building) && a.building < b.Length && b[a.building] != null) a.ApplyTo(b[a.building]);
        }
    }
}
