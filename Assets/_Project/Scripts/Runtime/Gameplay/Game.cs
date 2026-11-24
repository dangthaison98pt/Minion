using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace HordeLab
{
    public enum Phase { Menu, PickFaction, Prep, Battle, Reward, Lost, Won, Sandbox }

    /// <summary>Lượt chơi đang dở, ghi ra file JSON. Chỉ lưu ở pha chuẩn bị / phần thưởng (thoát giữa trận thì quay lại lúc chuẩn bị của đợt đó).</summary>
    [System.Serializable]
    public class SaveData
    {
        public int version = 1;
        public int faction, hearts, wave, crystals, attacker, phase;
        public int[] hostile, hand, shop, reward;
        public int[] handStars;                          // bản lưu cũ không có → coi như 1 sao
        public bool[] shopLocked;
        public string eventNote;
        public bool lastWon;
        public int lastWave, lastCrystals, lastInterest, lastWinBonus;
        public float pop, food, wood, gold;
        public int[] cellX, cellY, cellDef, cellStars;   // cellDef = -1: ô đã mở nhưng còn trống
    }

    /// <summary>
    /// Điều phối một lượt chơi theo GDD: chọn phe → (chuẩn bị → trận → phần thưởng) x 30 đợt.
    /// Giữ thẻ trên tay, cửa hàng, tim, danh sách phe thù địch; gọi <see cref="City"/> để chạy kinh tế
    /// và <see cref="Battle"/> để sinh quân. Giao diện nằm ở UI/GameUi.cs, chỉ đọc trạng thái ở đây và gọi các hàm public.
    /// </summary>
    [DefaultExecutionOrder(-100)]   // chạy TRƯỚC Battle.Update: lúc đó chưa có job nào được xếp lịch nên sửa dữ liệu thoải mái
    public class Game : MonoBehaviour
    {
        public static Game I { get; private set; }

        public Battle battle;
        public readonly City city = new City();
        public Phase phase = Phase.Menu;
        public GameUi ui;

        // ---- Lượt chơi ----
        public int faction;
        public int hearts;
        public int wave;                 // đợt sắp đánh / đang đánh (1..30)
        public int crystals;             // Tinh thể năng lượng: chỉ dùng ngoài trận
        public readonly List<int> hostile = new List<int>();
        public int attacker;             // phe tấn công đợt này (thấy trước lúc chuẩn bị)
        public readonly List<int> hand = new List<int>();        // mã công trình của từng thẻ trên tay
        public readonly List<int> handStars = new List<int>();   // số sao của từng thẻ, cùng chỉ số với hand
        public int HandStars(int i) => i >= 0 && i < handStars.Count ? handStars[i] : 1;
        void AddCard(int def, int stars = 1) { hand.Add(def); handStars.Add(stars); }
        void RemoveCard(int i) { hand.RemoveAt(i); handStars.RemoveAt(i); }
        void ClearHand() { hand.Clear(); handStars.Clear(); }
        public readonly int[] shop = new int[Tuning.ShopSlots];          // -1 = đã bán
        public readonly bool[] shopLocked = new bool[Tuning.ShopSlots];   // ô bị khoá thì roll / tự làm mới không đổi thẻ ở ô đó
        public readonly int[] reward = new int[Tuning.RewardChoices];
        public string eventNote;         // sự kiện đổi phe thù địch, báo sau khi đánh xong một đợt
        public bool lastWon;
        public int lastWave, lastCrystals;
        public int lastWinBonus;         // thưởng thêm khi giữ được thành (thua = 0)
        public int lastInterest;         // lãi Tinh thể nhận ở cuối đợt vừa xong (tính trên số đang giữ, trước khi cộng thưởng)

        // ---- Đợt đang đánh ----
        public int castleHp;
        public int squadsTotal, squadsLeft;
        public float waveTime, allSpawnedAt;
        /// <summary>Thời điểm (giờ thật) đợt vừa kết thúc; -1 = không có. Giao diện dùng để làm mờ sân → dọn sân → bật bảng kết quả.</summary>
        public float endedAt = -1f;
        bool fieldCleared;
        public const float ClearAt = 0.5f;   // giây sau khi hết đợt thì dọn sạch sân (lúc màn che đang tối nhất)
        float spawnAcc, enemyPower;
        readonly List<int> enemyTypes = new List<int>();
        readonly List<float> enemyWeights = new List<float>();

        // ---- Sandbox (stress test / benchmark): hai phe tự bơm quân, không có thẻ bài ----
        public int sandboxUnits;

        System.Random rnd = new System.Random();

        public bool IsBoss(int w) => w % Tuning.BossEvery == 0;
        public bool IsFullMoon(int w) => w % Tuning.FullMoonEvery == 0;
        public int EnemiesAlive => battle.Counter(C.Team1);
        public int AlliesAlive => battle.Counter(C.Team0);
        int EnemySquadSize { get { int side = GameData.Factions[attacker].squadSide; return side * side; } }
        /// <summary>Tổng quân địch cả đợt (tính trước, không chờ quân kéo đến).</summary>
        public int EnemiesTotal => squadsTotal * EnemySquadSize;
        /// <summary>Địch còn lại: đang trên sân + các tiểu đội chưa kéo đến (quân con của Kẻ triệu hồi cũng tính).</summary>
        public int EnemiesRemaining => EnemiesAlive + squadsLeft * EnemySquadSize;

        // Bấm Play ở scene trống cũng chạy được
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void AutoCreate()
        {
            if (FindAnyObjectByType<Game>() != null) return;
            var b = FindAnyObjectByType<Battle>();
            if (b == null) b = new GameObject("Battle").AddComponent<Battle>();
            b.gameObject.AddComponent<Game>();
        }

        void Awake()
        {
            I = this;
            battle = GetComponent<Battle>();
            battle.onStep = OnStep;
            battle.simulate = false;
        }

        void Start()
        {
            ui = gameObject.AddComponent<GameUi>();
            ui.Init(this);
            if (phase == Phase.Menu) EnterMenu();
        }

        // ------------------------------------------------------------------
        // Main menu + lưu / nạp lượt chơi
        // ------------------------------------------------------------------
        /// <summary>Công cụ test tắt cái này để bot khỏi ghi đè lượt chơi thật của người chơi.</summary>
        [System.NonSerialized] public bool savingEnabled = true;
        public static string saveFile = "run.json";
        public static string SavePath => System.IO.Path.Combine(Application.persistentDataPath, saveFile);
        public bool HasSave => savingEnabled && System.IO.File.Exists(SavePath);

        /// <summary>Về main menu. Phía sau menu là một trận đánh trình diễn nho nhỏ.</summary>
        public void EnterMenu()
        {
            StartSandbox(1400);
            battle.speed = 1;
            phase = Phase.Menu;
        }

        public void NewGame()
        {
            battle.simulate = false;
            battle.ResetField();
            phase = Phase.PickFaction;
        }

        /// <summary>Thoát ra menu giữa chừng. Lượt chơi đã được tự lưu ở lần chuẩn bị gần nhất.</summary>
        public void QuitToMenu()
        {
            endedAt = -1f;
            EnterMenu();
        }

        public void DeleteSave()
        {
            if (savingEnabled && System.IO.File.Exists(SavePath)) System.IO.File.Delete(SavePath);
        }

        /// <summary>Tự lưu: gọi sau mỗi thay đổi lúc chuẩn bị và khi vừa xong một đợt.</summary>
        public void SaveRun()
        {
            if (!savingEnabled || (phase != Phase.Prep && phase != Phase.Reward)) return;
            var d = new SaveData
            {
                faction = faction, hearts = hearts, wave = wave, crystals = crystals, attacker = attacker, phase = (int)phase,
                hostile = hostile.ToArray(), hand = hand.ToArray(), handStars = handStars.ToArray(), shop = (int[])shop.Clone(), reward = (int[])reward.Clone(),
                shopLocked = (bool[])shopLocked.Clone(), eventNote = eventNote, lastWon = lastWon, lastWave = lastWave, lastCrystals = lastCrystals, lastInterest = lastInterest, lastWinBonus = lastWinBonus,
                pop = city.res.pop, food = city.res.food, wood = city.res.wood, gold = city.res.gold
            };
            int n = city.cells.Count, k = 0;
            d.cellX = new int[n]; d.cellY = new int[n]; d.cellDef = new int[n]; d.cellStars = new int[n];
            foreach (var kv in city.cells)
            {
                d.cellX[k] = kv.Key.x; d.cellY[k] = kv.Key.y;
                d.cellDef[k] = kv.Value != null ? kv.Value.def : -1;
                d.cellStars[k] = kv.Value != null ? kv.Value.stars : 0;
                k++;
            }
            try { System.IO.File.WriteAllText(SavePath, JsonUtility.ToJson(d)); }
            catch (System.Exception e) { Debug.LogWarning("Save failed: " + e.Message); }
        }

        /// <summary>Đọc file lưu mà chưa nạp (để menu hiện "Wave 7 — Dwarf"). null nếu không có hoặc hỏng.</summary>
        public SaveData PeekSave()
        {
            if (!HasSave) return null;
            try
            {
                var d = JsonUtility.FromJson<SaveData>(System.IO.File.ReadAllText(SavePath));
                bool ok = d != null && d.version == 1 && d.cellX != null && d.hand != null && d.hostile != null && d.hostile.Length > 0
                          && d.shop != null && d.shop.Length == shop.Length && d.reward != null && d.reward.Length == reward.Length
                          && d.faction >= 0 && d.faction < Faction.Count;
                return ok ? d : null;
            }
            catch { return null; }
        }

        public bool ContinueRun()
        {
            var d = PeekSave();
            if (d == null) return false;
            faction = d.faction; hearts = d.hearts; wave = d.wave; crystals = d.crystals; attacker = d.attacker;
            hostile.Clear(); hostile.AddRange(d.hostile);
            ClearHand();
            for (int i = 0; i < d.hand.Length; i++)
                AddCard(d.hand[i], d.handStars != null && i < d.handStars.Length ? Mathf.Clamp(d.handStars[i], 1, Tuning.MaxStars) : 1);
            d.shop.CopyTo(shop, 0); d.reward.CopyTo(reward, 0);
            System.Array.Clear(shopLocked, 0, shopLocked.Length);
            if (d.shopLocked != null && d.shopLocked.Length == shopLocked.Length) d.shopLocked.CopyTo(shopLocked, 0);
            eventNote = d.eventNote; lastWon = d.lastWon; lastWave = d.lastWave; lastCrystals = d.lastCrystals; lastInterest = d.lastInterest; lastWinBonus = d.lastWinBonus;

            city.Init(faction);
            city.cells.Clear();
            for (int i = 0; i < d.cellX.Length; i++)
                city.cells[new Vector2Int(d.cellX[i], d.cellY[i])] =
                    d.cellDef[i] >= 0 && d.cellDef[i] < B.Expand ? new Building { def = d.cellDef[i], stars = Mathf.Clamp(d.cellStars[i], 1, Tuning.MaxStars) } : null;
            city.res = new Res(d.pop, d.food, d.wood, d.gold);
            city.Recompute();

            battle.simulate = false;
            battle.paused = false;
            battle.speed = 1;
            battle.ResetField();
            endedAt = -1f;
            phase = d.phase == (int)Phase.Reward ? Phase.Reward : Phase.Prep;
            return true;
        }

        // ------------------------------------------------------------------
        // Bắt đầu lượt chơi
        // ------------------------------------------------------------------
        public void SetSeed(int seed) => rnd = new System.Random(seed);

        public void StartRun(int playerFaction)
        {
            faction = playerFaction;
            hearts = Tuning.Hearts;
            wave = 1;
            crystals = Tuning.StartCrystals;
            eventNote = null;
            city.Init(faction);

            // 3 phe thù địch, không bao giờ có phe của người chơi
            hostile.Clear();
            var others = new List<int>();
            for (int f = 0; f < Faction.Count; f++) if (f != faction) others.Add(f);
            while (hostile.Count < Tuning.StartHostile)
            {
                int k = rnd.Next(others.Count);
                hostile.Add(others[k]);
                others.RemoveAt(k);
            }
            hostile.Sort();

            // Bộ thẻ khởi đầu (số tạm): đủ để chạy 2 nhà lính đầu tiên của phe
            ClearHand();
            AddCard(B.Barracks + faction * 5);
            AddCard(B.Barracks + faction * 5 + 1);
            AddCard(B.Cabin);
            AddCard(B.Cabin);
            AddCard(B.Field);
            AddCard(B.LumberCamp);
            AddCard(B.GoldMine);

            System.Array.Clear(shopLocked, 0, shopLocked.Length);
            RollShop();
            attacker = hostile[rnd.Next(hostile.Count)];
            endedAt = -1f;
            battle.simulate = false;
            battle.paused = false;
            battle.ResetField();
            battle.speed = 1;
            phase = Phase.Prep;
            SaveRun();
        }

        // ------------------------------------------------------------------
        // Thẻ bài
        // ------------------------------------------------------------------
        /// <summary>Bốc 1 thẻ: thẻ dùng chung + nhà lính của phe mình. Không bao giờ ra thẻ phe khác.</summary>
        public int RandomCard()
        {
            const int shared = B.Barracks;            // 0..22: nhà tài nguyên + nhà buff
            const float barracksW = 1.6f, expandW = 2.5f;
            float total = shared + 5 * barracksW + expandW;
            float r = (float)rnd.NextDouble() * total;
            if (r < shared) return (int)r;
            r -= shared;
            if (r < 5 * barracksW) return B.Barracks + faction * 5 + Mathf.Min(4, (int)(r / barracksW));
            return B.Expand;
        }

        /// <summary>Làm mới cửa hàng. Ô đang khoá (và còn thẻ) được giữ nguyên, kể cả khi tự làm mới sau mỗi đợt.</summary>
        public void RollShop()
        {
            for (int i = 0; i < shop.Length; i++)
            {
                if (shopLocked[i] && shop[i] >= 0) continue;
                shop[i] = RandomCard();
                shopLocked[i] = false;
            }
        }

        public void ToggleLock(int slot)
        {
            if (phase == Phase.Prep && shop[slot] >= 0) shopLocked[slot] = !shopLocked[slot];
            SaveRun();
        }

        public bool CanRoll => phase == Phase.Prep && crystals >= Tuning.RollCost;

        public bool PayRoll()
        {
            if (!CanRoll) return false;
            crystals -= Tuning.RollCost;
            RollShop();
            SaveRun();
            return true;
        }

        public int Price(int card) => Tuning.CardPrice(GameData.Buildings[card].cat);

        public bool CanBuy(int slot) =>
            phase == Phase.Prep && shop[slot] >= 0 && crystals >= Price(shop[slot]) && hand.Count < Tuning.HandLimit;

        public bool Buy(int slot)
        {
            if (!CanBuy(slot)) return false;
            crystals -= Price(shop[slot]);
            AddCard(shop[slot]);
            shop[slot] = -1;
            shopLocked[slot] = false;
            SaveRun();
            return true;
        }

        /// <summary>Thả thẻ thứ <paramref name="index"/> trên tay vào ô <paramref name="p"/>. Trả về true nếu thẻ đã được dùng.</summary>
        public bool PlayCard(int index, Vector2Int p)
        {
            if (phase != Phase.Prep || index < 0 || index >= hand.Count) return false;
            int card = hand[index], stars = HandStars(index);
            if (CanReplace(card, stars, p)) return Replace(index, p);
            bool ok = card == B.Expand ? city.Expand(p)
                    : city.CanPlace(p) ? city.Place(p, card, stars)
                    : city.MergeCard(p, card, stars);
            if (ok) { RemoveCard(index); SaveRun(); }
            return ok;
        }

        /// <summary>
        /// Thả thẻ lên một công trình mà không gộp được thì đổi: thẻ lên ô (giữ số sao của thẻ),
        /// nhà cũ về tay thành thẻ giữ nguyên số sao. Không bao giờ mất nhà.
        /// </summary>
        public bool CanReplace(int card, int stars, Vector2Int p)
        {
            var b = city.Get(p);
            if (phase != Phase.Prep || card == B.Expand || b == null) return false;
            if (b.def == card && b.stars == stars) return false;                  // y hệt nhau: gộp (hoặc đã max sao) chứ không đổi
            return !(Tuning.MergeFromHand && city.CanMerge(p, card, stars));
        }

        bool Replace(int index, Vector2Int p)
        {
            int card = hand[index], stars = HandStars(index);
            var old = city.Remove(p);
            city.Place(p, card, stars);
            hand[index] = old.def;           // nhà cũ về đúng chỗ thẻ vừa dùng, giữ nguyên số sao
            handStars[index] = old.stars;
            SaveRun();
            return true;
        }

        static string StarName(int def, int stars) => GameData.Buildings[def].name + (stars > 1 ? $" ({stars}-star)" : "");

        /// <summary>Câu báo khi cầm thẻ kê lên công trình ở ô <paramref name="p"/>; null nếu không phải đổi (ô trống, gộp được...).</summary>
        public string ReplaceHint(int card, int stars, Vector2Int p)
        {
            if (!CanReplace(card, stars, p)) return null;
            var old = city.Get(p);
            return $"<b>Swap</b>: {StarName(card, stars)} goes here,\n{StarName(old.def, old.stars)} goes back to your hand.";
        }


        /// <summary>Tinh thể nhận được khi bán: nhà n sao là 2^(n-1) nhà gộp lại nên được ngần ấy lần.</summary>
        public int SellValue(int stars) => Tuning.SellValue * (int)Tuning.StarYield[Mathf.Clamp(stars, 1, Tuning.MaxStars) - 1];

        public bool SellCard(int index)
        {
            if (phase != Phase.Prep || index < 0 || index >= hand.Count) return false;
            crystals += SellValue(HandStars(index));
            RemoveCard(index);
            SaveRun();
            return true;
        }

        public bool SellBuilding(Vector2Int p)
        {
            if (phase != Phase.Prep) return false;
            var b = city.Remove(p);
            if (b == null) return false;
            crystals += SellValue(b.stars);
            SaveRun();
            return true;
        }

        /// <summary>Kéo công trình về tay: thành thẻ giữ nguyên số sao.</summary>
        public bool CanTakeBack(Vector2Int p)
        {
            var b = city.Get(p);
            return phase == Phase.Prep && b != null && hand.Count < Tuning.HandLimit;
        }

        public bool TakeBack(Vector2Int p)
        {
            if (!CanTakeBack(p)) return false;
            var b = city.Remove(p);
            AddCard(b.def, b.stars);
            SaveRun();
            return true;
        }

        /// <summary>Kéo công trình sang ô khác: gộp nếu được, không thì đổi chỗ.</summary>
        public bool MoveBuilding(Vector2Int from, Vector2Int to)
        {
            if (phase != Phase.Prep || !city.MoveOrMerge(from, to)) return false;
            SaveRun();
            return true;
        }

        public void PickReward(int i)
        {
            if (phase != Phase.Reward) return;
            AddCard(reward[i]);
            phase = Phase.Prep;
            SaveRun();
        }

        // ------------------------------------------------------------------
        // Đợt
        // ------------------------------------------------------------------
        public void StartWave()
        {
            if (phase != Phase.Prep) return;
            battle.ResetField();
            city.BeginBattle();
            castleHp = Tuning.CastleHp;
            endedAt = -1f;
            squadsTotal = Mathf.Max(1, Mathf.RoundToInt(Tuning.EnemySquads(wave) * (IsBoss(wave) ? Tuning.BossSize : 1f)));
            squadsLeft = squadsTotal;
            spawnAcc = 2f;                    // 2 tiểu đội đầu tới ngay
            waveTime = 0f;
            allSpawnedAt = -1f;
            enemyPower = Tuning.EnemyPower(wave) * (IsBoss(wave) ? Tuning.BossPower : 1f);

            enemyTypes.Clear();
            enemyWeights.Clear();
            for (int s = 0; s < 5; s++)
            {
                int t = attacker * 5 + s;
                if (GameData.Units[t].minWave > wave) continue;
                enemyTypes.Add(t);
                enemyWeights.Add(GameData.Units[t].waveWeight);
            }

            battle.simulate = true;
            battle.paused = false;
            phase = Phase.Battle;
        }

        float MoonMul(int unitType) =>
            Faction.Of(unitType) == Faction.Beast && IsFullMoon(wave) ? Tuning.FullMoonPower : 1f;

        /// <summary>Chạy ngay trước mỗi bước mô phỏng: kinh tế, nhà lính gọi quân, quân địch kéo đến.</summary>
        void OnStep(float dt)
        {
            if (phase == Phase.Sandbox || phase == Phase.Menu) { SandboxStep(dt); return; }
            if (phase != Phase.Battle) return;
            waveTime += dt;

            city.Tick(dt, AlliesAlive < Tuning.PlayerUnitCap, CallSquads);

            if (squadsLeft > 0)
            {
                spawnAcc += squadsTotal / Tuning.WaveDuration(wave) * dt;
                while (spawnAcc >= 1f && squadsLeft > 0)
                {
                    spawnAcc -= 1f;
                    squadsLeft--;
                    SpawnEnemySquad();
                }
                if (squadsLeft == 0) allSpawnedAt = waveTime;
            }
        }

        void CallSquads(Building b)
        {
            int type = b.Def.unit;
            float moon = MoonMul(type) * city.TroopPower(b);   // trăng tròn + số sao của nhà lính
            int side = GameData.Factions[faction].squadSide;
            ref var rng = ref battle.Rng;
            for (int k = 0; k < city.Squads(b); k++)
            {
                var pos = new float2(Cfg.PlayerSpawnX + rng.NextFloat(0f, 5f), rng.NextFloat(-Cfg.HalfH + 5f, Cfg.HalfH - 5f));
                battle.SpawnSquad(type, 0, pos, side, b.hp * moon, b.dmg * moon, b.regen);
            }
        }

        void SpawnEnemySquad()
        {
            float total = 0f;
            foreach (float w in enemyWeights) total += w;
            ref var rng = ref battle.Rng;
            float r = rng.NextFloat() * total;
            int type = enemyTypes[enemyTypes.Count - 1];
            for (int i = 0; i < enemyTypes.Count; i++)
            {
                if (r < enemyWeights[i]) { type = enemyTypes[i]; break; }
                r -= enemyWeights[i];
            }
            float power = enemyPower * MoonMul(type);
            var pos = new float2(Cfg.EnemySpawnX + rng.NextFloat(0f, 6f), rng.NextFloat(-Cfg.HalfH + 5f, Cfg.HalfH - 5f));
            battle.SpawnSquad(type, 1, pos, GameData.Factions[attacker].squadSide, power, power);
        }

        void EndWave(bool won)
        {
            battle.simulate = false;
            endedAt = Time.unscaledTime;
            fieldCleared = false;
            lastWon = won;
            lastWave = wave;
            if (!won) hearts--;
            if (hearts <= 0) { phase = Phase.Lost; DeleteSave(); return; }
            if (wave >= Tuning.Waves) { phase = Phase.Won; DeleteSave(); return; }

            // Thua một đợt (còn tim) vẫn sang đợt sau và vẫn nhận đủ phần thưởng
            lastInterest = Tuning.Interest(crystals);          // lãi tính trên số đang giữ, trước khi cộng thưởng
            lastCrystals = Tuning.CrystalReward;
            lastWinBonus = won ? Tuning.WinBonus : 0;
            crystals += lastCrystals + lastWinBonus + lastInterest;
            for (int i = 0; i < reward.Length; i++) reward[i] = RandomCard();

            eventNote = RollHostileEvent();
            wave++;
            attacker = hostile[rnd.Next(hostile.Count)];
            RollShop();
            phase = Phase.Reward;
            SaveRun();
        }

        public static string The(int f) { string n = GameData.Factions[f].name; return n.StartsWith("The ") ? n : "The " + n; }

        /// <summary>Sự kiện ngẫu nhiên: một phe thôi thù địch hoặc một phe mới trở thành thù địch (luôn giữ 2..4 phe).</summary>
        string RollHostileEvent()
        {
            if (rnd.NextDouble() >= Tuning.EventChance) return null;
            var neutral = new List<int>();
            for (int f = 0; f < Faction.Count; f++) if (f != faction && !hostile.Contains(f)) neutral.Add(f);
            bool canRemove = hostile.Count > Tuning.MinHostile;
            bool canAdd = hostile.Count < Tuning.MaxHostile && neutral.Count > 0;
            if (!canRemove && !canAdd) return null;

            if (canAdd && (!canRemove || rnd.Next(2) == 0))
            {
                int f = neutral[rnd.Next(neutral.Count)];
                hostile.Add(f);
                hostile.Sort();
                return $"{The(f)} have turned hostile!";
            }
            else
            {
                int k = rnd.Next(hostile.Count);
                int f = hostile[k];
                hostile.RemoveAt(k);
                return $"{The(f)} are no longer hostile.";
            }
        }

        // ------------------------------------------------------------------
        void Update()
        {
            // Hết đợt: dọn sạch sân ngay (lính, xác, máu), không chờ tới lúc bắt đầu đợt sau
            if (endedAt >= 0f && !fieldCleared && Time.unscaledTime - endedAt >= ClearAt)
            {
                battle.ResetField();
                fieldCleared = true;
            }
            if (phase == Phase.Battle)
            {
                castleHp = Mathf.Max(0, Tuning.CastleHp - battle.Counter(C.Breach));
                if (castleHp <= 0) EndWave(false);
                else if (squadsLeft == 0 && EnemiesAlive == 0) EndWave(true);
                else if (squadsLeft == 0 && waveTime - allSpawnedAt > Tuning.WaveTimeout) EndWave(true);   // phòng khi hai bên kẹt nhau mãi
            }

            bool menuOpen = ui != null && ui.PauseOpen;
            if ((phase == Phase.Battle || phase == Phase.Sandbox) && !menuOpen)
            {
                if (Input.GetKeyDown(KeyCode.Space)) battle.paused = !battle.paused;
                if (Input.GetKeyDown(KeyCode.Tab)) CycleSpeed();
            }
        }

        public void CycleSpeed() => battle.speed = battle.speed >= 4 ? 1 : battle.speed * 2;

        // ------------------------------------------------------------------
        // Sandbox: giữ quanh mức sandboxUnits lính, mỗi bên trộn nhiều phe, để đo hiệu năng
        // ------------------------------------------------------------------
        public void StartSandbox(int units)
        {
            sandboxUnits = math.clamp(units, 1000, Cfg.MaxUnits - 2000);
            battle.ResetField();
            ref var rng = ref battle.Rng;
            int perSide = sandboxUnits / 2;
            for (int team = 0; team < 2; team++)
            {
                int spawned = 0;
                while (spawned < perSide * 0.9f)
                {
                    float x = team == 0 ? rng.NextFloat(Cfg.WallX + 5f, Cfg.HoldX) : rng.NextFloat(Cfg.HoldX + 8f, Cfg.HalfW - 8f);
                    spawned += battle.SpawnSquad(SandboxType(team, ref rng), team, new float2(x, rng.NextFloat(-Cfg.HalfH + 5f, Cfg.HalfH - 5f)), 5);
                }
            }
            battle.simulate = true;
            battle.paused = false;
            phase = Phase.Sandbox;
        }

        static int SandboxType(int team, ref Unity.Mathematics.Random rng)
        {
            // bên trái: Người + Tiên + Người lùn, bên phải: Người hoá thú + The Void. Chủ yếu quân tuyến đầu và tầm xa.
            int f = team == 0 ? rng.NextInt(0, 3) : rng.NextInt(3, 5);
            float r = rng.NextFloat();
            int slot = r < 0.45f ? 0 : r < 0.75f ? 1 : rng.NextInt(2, 5);
            return f * 5 + slot;
        }

        void SandboxStep(float dt)
        {
            ref var rng = ref battle.Rng;
            int perSide = sandboxUnits / 2;
            spawnAcc += sandboxUnits * 0.03f / 25f * dt;   // bù quân chết thật nhanh để luôn sát mức trần
            while (spawnAcc >= 1f)
            {
                spawnAcc -= 1f;
                for (int team = 0; team < 2; team++)
                {
                    if (battle.Counter(C.Team0 + team) >= perSide) continue;
                    float x = team == 0 ? Cfg.PlayerSpawnX + rng.NextFloat(0f, 5f) : Cfg.EnemySpawnX + rng.NextFloat(0f, 6f);
                    battle.SpawnSquad(SandboxType(team, ref rng), team, new float2(x, rng.NextFloat(-Cfg.HalfH + 5f, Cfg.HalfH - 5f)), 5);
                }
            }
        }

        // ------------------------------------------------------------------
        // Bot chơi thử (Tools/AutoShot dùng để kiểm tra cả vòng lặp mà không cần người bấm)
        // ------------------------------------------------------------------
        /// <summary>Làm 1 việc mà người chơi sẽ làm ở pha hiện tại. Trong trận thì không làm gì.</summary>
        public void BotAct(int botFaction)
        {
            switch (phase)
            {
                case Phase.Menu: NewGame(); break;
                case Phase.PickFaction: StartRun(botFaction); break;
                case Phase.Reward:
                {
                    // ưu tiên nhà lính, rồi thẻ mở đất khi hết chỗ
                    int pick = 0;
                    for (int i = 0; i < reward.Length; i++)
                        if (GameData.Buildings[reward[i]].cat == Cat.Barracks) pick = i;
                    if (city.EmptyCount() == 0)
                        for (int i = 0; i < reward.Length; i++)
                            if (reward[i] == B.Expand) pick = i;
                    PickReward(pick);
                    break;
                }
                case Phase.Prep:
                    BotPlaceAndShop();
                    StartWave();
                    break;
            }
        }

        /// <summary>Bot: đặt hết thẻ trên tay, mua những gì mua được rồi đặt tiếp.</summary>
        public void BotPlaceAndShop()
        {
            if (phase != Phase.Prep) return;
            BotPlaceHand();
            for (int i = 0; i < shop.Length; i++) if (CanBuy(i)) Buy(i);
            BotPlaceHand();
        }

        public int EnemyPreviewCount()
        {
            int side = GameData.Factions[attacker].squadSide;
            return Mathf.RoundToInt(Tuning.EnemySquads(wave) * (IsBoss(wave) ? Tuning.BossSize : 1f)) * side * side;
        }

        void BotPlaceHand()
        {
            for (int i = hand.Count - 1; i >= 0; i--)
            {
                int card = hand[i];
                Vector2Int? target = null;
                foreach (var kv in city.cells)
                {
                    var p = kv.Key;
                    if (card == B.Expand)
                    {
                        foreach (var d in new[] { Vector2Int.right, Vector2Int.up, Vector2Int.left, Vector2Int.down })
                            if (city.CanExpand(p + d)) { target = p + d; break; }
                    }
                    else if (city.CanMerge(p, card, HandStars(i)) || (kv.Value == null && target == null)) target = p;
                    if (target != null && (card == B.Expand || city.CanMerge(target.Value, card, HandStars(i)))) break;
                }
                if (target != null) PlayCard(i, target.Value);
            }
        }
    }
}
