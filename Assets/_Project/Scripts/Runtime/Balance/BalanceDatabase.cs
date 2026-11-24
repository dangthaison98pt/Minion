using UnityEngine;

namespace HordeLab
{
    /// <summary>Danh sách mọi asset cân bằng. Nằm ở Resources/Balance để game tự nạp lúc chạy.</summary>
    [CreateAssetMenu(menuName = "Fantasy World War/Balance Database", fileName = "BalanceDatabase")]
    public class BalanceDatabase : ScriptableObject
    {
        public TuningBalance tuning;
        public FactionBalance[] factions;
        public UnitBalance[] units;
        public BuildingBalance[] buildings;
    }
}
