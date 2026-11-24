using UnityEngine;

namespace HordeLab
{
    // Kiểu dữ liệu dễ đọc cho Inspector (map 1-1 với hằng số trong code: Mode, UFlag, Proj, PKind)
    public enum AttackMode { Melee = 0, Shoot = 1, Blast = 2, Summon = 3 }

    [System.Flags]
    public enum UnitFlags
    {
        None = 0, Flank = 1, Kite = 2, Ghost = 4, Big = 8, HealAura = 16, PowerAura = 32, Giant = 64,
    }

    public enum ProjectileKind { Arrow, ElfArrow, Bolt, Rock, Cannonball, Javelin, NatureOrb, VoidOrb, LightOrb, Meteor, BigBolt }

    public enum BloodKind { Blood, DarkBlood, Spark, Dust, Bone, Fire, Smoke, Ember, GreenSpark, VoidGoo, Leaf, Splinter, Holy }

    /// <summary>
    /// Nạp dữ liệu cân bằng từ ScriptableObject (Resources/Balance/BalanceDatabase.asset).
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

        public static void ApplyTuning()
        {
            var t = IgnoreAssets ? null : Db != null ? Db.tuning : null;
            if (t != null) t.ApplyTo();
        }

        public static void ApplyFactions(FactionDef[] f)
        {
            if (IgnoreAssets || Db == null || Db.factions == null) return;
            foreach (var a in Db.factions) if (a != null && a.faction >= 0 && a.faction < f.Length) a.ApplyTo(f[a.faction]);
        }

        public static void ApplyUnits(UnitDef[] u)
        {
            if (IgnoreAssets || Db == null || Db.units == null) return;
            foreach (var a in Db.units) if (a != null && a.unitType >= 0 && a.unitType < u.Length) a.ApplyTo(u[a.unitType]);
        }

        public static void ApplyBuildings(BuildingDef[] b)
        {
            if (IgnoreAssets || Db == null || Db.buildings == null) return;
            foreach (var a in Db.buildings) if (a != null && a.building >= 0 && a.building < B.Barracks && b[a.building] != null) a.ApplyTo(b[a.building]);
        }
    }
}
