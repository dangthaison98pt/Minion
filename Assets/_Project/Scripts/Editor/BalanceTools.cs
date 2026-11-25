using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace HordeLab.EditorTools
{
    /// <summary>
    /// Tạo / bổ sung asset số liệu thô (quân + nhà) từ số đang có trong code, để GD điền trong Inspector.
    /// Chỉ TẠO asset còn thiếu, không bao giờ ghi đè asset đã có (số GD đã chỉnh được giữ nguyên).
    /// </summary>
    public static class BalanceTools
    {
        public const string Root = "Assets/_Project/Resources/Balance";

        [MenuItem("Horde Lab/Balance/Create missing balance assets")]
        public static void CreateMissing()
        {
            Folder(Root);
            int created = 0;

            var factionDefs = GameData.DefaultFactions();

            // ---- Quân (số gốc trong code, chưa nhân đặc trưng phe) ----
            var unitDefs = GameData.DefaultUnits();
            var units = Index(Find<UnitBalance>(), a => a.unitType);
            for (int t = 0; t < unitDefs.Length; t++)
            {
                if (unitDefs[t] == null) continue;
                if (units.TryGetValue(t, out var have)) { if (have.FillMissing(unitDefs[t])) { EditorUtility.SetDirty(have); created++; } continue; }   // giữ số GD đã điền, chỉ bổ sung ô mới
                int fi = Faction.Of(t);
                string group = t >= UnitType.FirstGiant ? "Bosses & Legendary" : fi >= 0 ? $"{fi}_{Safe(factionDefs[fi].name)}" : "Special";
                string dir = $"{Root}/Units/{group}";
                Folder($"{Root}/Units"); Folder(dir);
                var a = ScriptableObject.CreateInstance<UnitBalance>();
                a.CopyFrom(t, unitDefs[t]);
                AssetDatabase.CreateAsset(a, $"{dir}/{t:00}_{Safe(unitDefs[t].name)}.asset");
                units[t] = a; created++;
            }

            // ---- Nhà tài nguyên + nhà buff ----
            var buildingDefs = GameData.DefaultBuildings();
            var buildings = Index(Find<BuildingBalance>(), a => a.building);
            Folder($"{Root}/Buildings");
            for (int id = 0; id < B.Count; id++)
            {
                if (!B.HasBalance(id) || buildingDefs[id] == null) continue;
                if (buildings.ContainsKey(id)) continue;
                var a = ScriptableObject.CreateInstance<BuildingBalance>();
                a.CopyFrom(buildingDefs[id]);
                AssetDatabase.CreateAsset(a, $"{Root}/Buildings/{id:00}_{Safe(buildingDefs[id].name)}.asset");
                buildings[id] = a; created++;
            }

            // ---- Danh sách chung (game nạp file này lúc chạy) ----
            string dbPath = $"{Root}/BalanceDatabase.asset";
            var db = AssetDatabase.LoadAssetAtPath<BalanceDatabase>(dbPath);
            if (db == null) { db = ScriptableObject.CreateInstance<BalanceDatabase>(); AssetDatabase.CreateAsset(db, dbPath); created++; }
            db.units = Sorted(units);
            db.buildings = Sorted(buildings);
            EditorUtility.SetDirty(db);
            AssetDatabase.SaveAssets();
            // ghi lại mọi asset theo định dạng mới nhất (asset cũ có thêm các ô mới, ví dụ nâng cấp theo sao)
            var paths = new List<string>();
            foreach (var guid in AssetDatabase.FindAssets("t:ScriptableObject", new[] { Root })) paths.Add(AssetDatabase.GUIDToAssetPath(guid));
            AssetDatabase.ForceReserializeAssets(paths, ForceReserializeAssetsOptions.ReserializeAssets);
            Debug.Log($"[HordeLab] Balance: tạo mới / bổ sung {created} asset; tổng {db.units.Length} loại quân, {db.buildings.Length} nhà ({Root})");
        }

        /// <summary>Kiểm tra: chỉ số game đọc từ asset có khớp chỉ số gốc trong code không (dùng sau khi vừa tạo asset).</summary>
        [MenuItem("Horde Lab/Balance/Verify assets match code")]
        public static void Verify()
        {
            var def = GameData.DefaultUnits(); var fdef = GameData.DefaultFactions();
            int bad = 0;
            for (int t = 0; t < def.Length; t++)
            {
                var a = GameData.Units[t]; var d = def[t];
                int fi = Faction.Of(t); var f = fi >= 0 ? fdef[fi] : new FactionDef();
                float hp = d.stats.maxHp * f.hpMul, dmg = d.stats.damage * f.dmgMul, sp = d.stats.speed * f.speedMul * Tuning.SpeedScale;
                float rg = d.stats.mode != Mode.Melee ? d.stats.range * f.rangeMul * Tuning.RangeScale : d.stats.range;
                bool ok = Mathf.Approximately(a.stats.maxHp, hp) && Mathf.Approximately(a.stats.damage, dmg) && Mathf.Approximately(a.stats.speed, sp)
                          && Mathf.Approximately(a.stats.range, rg) && Mathf.Approximately(a.stats.cooldown, d.stats.cooldown) && a.stats.flags == d.stats.flags
                          && a.stats.mode == d.stats.mode && a.stats.proj == d.stats.proj && Mathf.Approximately(a.stats.splash, d.stats.splash)
                          && Mathf.Approximately(a.stats.knock, d.stats.knock) && Mathf.Approximately(a.stats.arrowResist, d.stats.arrowResist)
                          && Mathf.Approximately(a.cost.pop + a.cost.food * 10 + a.cost.wood * 100 + a.cost.gold * 1000, d.cost.pop + d.cost.food * 10 + d.cost.wood * 100 + d.cost.gold * 1000)
                          && a.name == d.name && a.minWave == d.minWave && Mathf.Approximately(a.callTime, d.callTime);
                if (!ok) { bad++; Debug.LogWarning($"[HordeLab] Balance lệch: {t} {d.name}"); }
            }
            var bdef = GameData.DefaultBuildings();
            for (int id = 0; id < B.Count; id++)
                if (B.HasBalance(id) && bdef[id].name != GameData.Buildings[id].name || !Mathf.Approximately(bdef[id].period, GameData.Buildings[id].period) || !Mathf.Approximately(bdef[id].amount, GameData.Buildings[id].amount))
                { bad++; Debug.LogWarning($"[HordeLab] Balance lệch: nhà {id} {bdef[id].name}"); }
            Debug.Log($"[HordeLab] Balance verify: {(bad == 0 ? "KHỚP" : bad + " chỗ lệch")} ({def.Length} quân, {B.Barracks} nhà, Tuning castleHp={Tuning.CastleHp}, speedScale={Tuning.SpeedScale})");
        }

        static List<T> Find<T>() where T : ScriptableObject
        {
            var list = new List<T>();
            if (!AssetDatabase.IsValidFolder(Root)) return list;
            foreach (var guid in AssetDatabase.FindAssets($"t:{typeof(T).Name}", new[] { Root }))
            {
                var a = AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid));
                if (a != null) list.Add(a);
            }
            return list;
        }

        static Dictionary<int, T> Index<T>(List<T> list, System.Func<T, int> key)
        {
            var d = new Dictionary<int, T>();
            foreach (var a in list) { int k = key(a); if (k >= 0 && !d.ContainsKey(k)) d[k] = a; }
            return d;
        }

        static T[] Sorted<T>(Dictionary<int, T> d)
        {
            var keys = new List<int>(d.Keys); keys.Sort();
            var arr = new T[keys.Count];
            for (int i = 0; i < keys.Count; i++) arr[i] = d[keys[i]];
            return arr;
        }

        static void Folder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/'), name = Path.GetFileName(path);
            Folder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }

        static string Safe(string s)
        {
            var sb = new System.Text.StringBuilder();
            foreach (char c in s ?? "") if (char.IsLetterOrDigit(c)) sb.Append(c);
            return sb.Length > 0 ? sb.ToString() : "Unnamed";
        }
    }
}
