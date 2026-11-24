using Unity.Mathematics;

namespace HordeLab
{
    /// <summary>Hằng số chung của trận đánh.</summary>
    public static class Cfg
    {
        // 1 đơn vị thế giới = 10 pixel nghệ thuật. Lính bộ là 1 ô 10x10 pixel, quân to (kỵ binh, máy bắn đá...) 16x16.
        public const int PPU = 10;

        // Thế giới 96 x 50,4 đơn vị = 960 x 504 pixel nghệ thuật, mặc định phóng x2: ở 1080p vừa khít vùng DƯỚI thanh trên cùng
        // (1080 - 72 = 1008 = 504 x 2), không có chỗ nào của chiến trường chui xuống dưới giao diện.
        // 26 đơn vị bên trái là bên trong thành (chỗ đặt lưới công trình), phần còn lại là chiến trường.
        public const float HalfW = 48f, HalfH = 25.2f;
        public const int GroundW = 960, GroundH = 504;
        public const int CastlePx = 260;       // bề rộng phần "trong thành" trên texture mặt đất
        public const int WallPx = 16;          // bề dày tường thành

        // Lưới không gian để tìm hàng xóm
        public const float CellSize = 2f;
        public const int GridW = 48, GridH = 27, Cells = GridW * GridH;

        public const int MaxUnits = 32768;     // đủ cho màn 20k lính (mỗi lính ~80 byte → vài MB tổng cộng)
        public const int MaxArrows = 16384;
        public const int MaxParticles = 65536;
        public const int MaxStamps = 16384;

        // Mặt ngoài tường thành. Quân địch (phe 1) vượt qua vạch này là lọt vào thành.
        public const float WallX = -HalfW + (CastlePx + WallPx) / (float)PPU;   // = -20.4
        public const float PlayerSpawnX = WallX + 3.5f;   // quân ta xuất hiện ngay ngoài cổng thành
        public const float EnemySpawnX = HalfW + 4f;      // quân địch xuất hiện NGOÀI mép phải rồi đi bộ vào màn hình
        public const float HoldX = WallX + 22f;                 // quân ta không thấy địch thì dừng ở đây giữ trận tuyến

        // Bán kính thân (đơn vị thế giới), đo từ chân: lính bộ / quân to 16x16 / quân khổng lồ 32x32.
        // Hai lính đẩy nhau khi chân gần hơn tổng bán kính; quân to đẩy quân nhỏ mạnh hơn bị đẩy lại.
        public const float RadiusSmall = 0.425f, RadiusBig = 0.7f, RadiusGiant = 1.4f;
        public static float RadiusOf(int flags) =>
            (flags & UFlag.Giant) != 0 ? RadiusGiant : (flags & UFlag.Big) != 0 ? RadiusBig : RadiusSmall;

        public static int CellX(float x) => math.clamp((int)math.floor((x + HalfW) / CellSize), 0, GridW - 1);
        public static int CellY(float y) => math.clamp((int)math.floor((y + HalfH) / CellSize), 0, GridH - 1);
    }

    /// <summary>Chỉ số trong mảng counters dùng chung giữa các Job.</summary>
    public static class C
    {
        public const int Units = 0, Arrows = 1, Particles = 2, Stamps = 3;
        // Đếm số lần lõi chuyên quân ra tay (công cụ kiểm tra đọc; xoá mỗi khi dọn sân)
        public const int LeaderSkill = 10, CcHits = 11, Revives = 12, Splits = 13, PierceHits = 14, Counters = 15;
        public const int Dead0 = 4, Dead1 = 5, Breach = 6;
        public const int Decrees = 7;                        // số lần Hoàng đế vàng ban Chiếu chỉ
        public const int BossSkills = 16;                    // số lần boss đợt trùm dùng chiêu   // Dead0 = quân ta tử trận, Dead1 = quân địch bị diệt, Breach = số đòn địch đã đánh trúng tường thành
        public const int Team0 = 8, Team1 = 9;               // số lính còn sống mỗi phe
        public const int Type0 = 17;                         // 16.. : số lượng mỗi loại lính
        public const int Length = Type0 + UnitType.Count;
    }

    /// <summary>5 phe trong GDD. Mỗi phe 5 loại quân: loại = phe * 5 + vị trí.</summary>
    public static class Faction
    {
        public const int Human = 0, Elf = 1, Dwarf = 2, Beast = 3, Void = 4;
        public const int Count = 5;
        /// <summary>Phe của một loại quân; -1 = quân trung lập (Rồng của lõi Dragon's Pact).</summary>
        public static int Of(int unitType) => unitType == UnitType.Voidling ? Void : unitType >= UnitType.FirstGiant ? -1 : unitType / 5;
    }

    public static class UnitType
    {
        // Người
        public const int Swordsman = 0, Archer = 1, Cavalry = 2, Monk = 3, Catapult = 4;
        // Tiên
        public const int Treant = 5, ForestArcher = 6, BladeDancer = 7, Druid = 8, DeerRider = 9;
        // Người lùn
        public const int ShieldWarrior = 10, Crossbow = 11, Axeman = 12, Cannon = 13, GoatRider = 14;
        // Người hoá thú
        public const int Bear = 15, Wolf = 16, Leopard = 17, Shaman = 18, Eagle = 19;
        // The Void
        public const int Aberration = 20, Shade = 21, Eye = 22, Summoner = 23, Screamer = 24;
        public const int Voidling = 25;      // quân con do Kẻ triệu hồi sinh ra, không có nhà lính
        public const int Dragon = 26;        // rồng của lõi Dragon's Pact, không có nhà lính, không thuộc phe nào
        public const int Emperor = 27;       // Hoàng đế vàng: lõi bí mật The Golden Emperor (phe Người) + boss của phe Người
        // Boss của đợt trùm, mỗi phe 1 con (Người dùng Hoàng đế vàng)
        public const int AncientTreant = 28, IronKing = 29, Fenrir = 30, Sleeper = 31;
        public const int Buildable = 25;
        public const int Count = 32;
        /// <summary>Boss đi cùng đợt trùm của phe <paramref name="faction"/>.</summary>
        public static int BossOf(int faction) => faction == 0 ? Emperor : faction == 1 ? AncientTreant : faction == 2 ? IronKing : faction == 3 ? Fenrir : Sleeper;

        // Quân khổng lồ (32x32) nằm ở CUỐI bảng, từ Dragon trở đi: mỗi con chiếm 8 hàng atlas 16px thay vì 2
        public const int FirstGiant = Dragon, GiantCount = Count - FirstGiant;
        /// <summary>Hàng atlas (ô 16px) bắt đầu khung hình của quân khổng lồ <paramref name="t"/>.</summary>
        public static int GiantRow(int t) => t * 2 + 6 * (t - FirstGiant);
    }

    /// <summary>Cách tấn công của một loại quân.</summary>
    public static class Mode
    {
        public const int Melee = 0;      // áp sát rồi chém
        public const int Shoot = 1;      // đứng xa bắn đạn bay theo parabol
        public const int Blast = 2;      // đứng xa, gây sát thương vùng tức thì (sóng âm)
        public const int Summon = 3;     // đứng xa, sinh quân con
    }

    public static class UFlag
    {
        public const int Flank = 1;      // chạy vòng ra mép bản đồ rồi đánh từ sau lưng
        public const int Kite = 2;       // vừa chạy vừa bắn
        public const int Ghost = 4;      // đi xuyên qua hàng địch
        public const int Big = 8;        // sprite 16x16
        public const int HealAura = 16;  // hồi máu đồng đội đứng gần
        public const int PowerAura = 32; // tăng sát thương đồng đội đứng gần
        public const int Giant = 64;     // sprite 32x32, chiếm 2x2 ô atlas (chỉ các loại quân CUỐI bảng, từ UnitType.FirstGiant)
    }

    /// <summary>Hình dạng viên đạn (hàng trong texture đạn).</summary>
    public static class Proj
    {
        public const int Arrow = 0, ElfArrow = 1, Bolt = 2, Rock = 3, Cannonball = 4, Javelin = 5, NatureOrb = 6, VoidOrb = 7, LightOrb = 8;
        public const int Meteor = 9, BigBolt = 10;   // lõi Meteor Shower / Ballista Towers
        public const int Count = 11;
        /// <summary>Đạn xuyên (tên, tên tiên, nỏ, lao) — bị arrowResist giảm sát thương.</summary>
        public static bool Piercing(int kind) => kind <= Bolt || kind == Javelin || kind == BigBolt;
    }

    public static class PKind
    {
        public const int Blood = 0, DarkBlood = 1, Spark = 2, Dust = 3, Bone = 4, Fire = 5, Smoke = 6, Ember = 7, GreenSpark = 8,
                         VoidGoo = 9, Leaf = 10, Splinter = 11, Holy = 12;
    }

    /// <summary>Ô trong atlas "dấu in" lên mặt đất (8 cột).</summary>
    public static class StampCell
    {
        public const int BloodRed = 0, BloodDark = 4, Drop = 8, StuckArrow = 12, Scorch = 16, Ash = 20,
                         Corpse = 24;   // 24 + loại * 2 + (0/1: hai kiểu nằm)
    }

    public struct UnitStats
    {
        public float maxHp, damage, cooldown, range, speed;
        public int mode, flags, proj, blood;
        public float minRange;       // máy công thành: địch áp sát gần hơn ngần này thì không bắn được
        public float splash;         // bán kính sát thương vùng (đạn nổ / đòn quét)
        public float slow;           // số giây làm chậm nạn nhân
        public float dodge;          // xác suất né đòn
        public float knock;          // hất văng nạn nhân mỗi đòn
        public float chargeBonus;    // kỵ binh: đòn xung phong mạnh gấp (1 + chargeBonus) lần khi đầy đà
        public float explodeDmg, explodeRadius;   // nổ khi chết
        public float animFps;
        public float radius;         // bán kính thân, đo từ chân (Cfg.RadiusOf)
        public float arrowResist;    // giảm sát thương từ tên / nỏ / lao (0..1), ví dụ vảy rồng
        public int augOn;            // 1 = người chơi có lõi chuyên quân của loại này (chỉ quân phe người chơi mới có lõi)
        public int liftPx;           // vẽ hình cao hơn vị trí bao nhiêu pixel để chân chạm đúng vị trí (Battle đo từ sprite)
    }

    /// <summary>Một người lính chỉ là struct này, không có GameObject nào cả. <c>pos</c> là chỗ CHÂN chạm đất.</summary>
    public struct Unit
    {
        public float2 pos;
        public float2 goal;     // vị trí địch gần nhất tìm được lần quét gần nhất
        public float hp, maxHp;
        public float cooldown;
        public float animT;
        public float flash;     // nháy trắng khi trúng đòn
        public float charge;    // kỵ binh: đà xung phong 0..1, tích khi phi, xả hết vào đòn đâm kế tiếp
        public float slow;      // > 0: đang bị làm chậm
        public float dmgMul;    // hệ số sát thương (đặc trưng phe, Xưởng rèn, độ khó của đợt...)
        public float regen;     // máu hồi mỗi giây (Bệnh xá)
        public float aux;       // quân đánh vòng: 0 = chưa thấy địch, 0..1 = đang vòng, >= 1 = đã nhập trận
        public int type;
        public int team;        // 0 = quân của người chơi (đi sang phải), 1 = quân địch (đi sang trái)
        public int state;       // 0 = đi, 1 = đánh
        public int hasGoal;
        public int faceLeft;
        public int buffed;      // đang đứng trong vùng tăng sức mạnh của pháp sư bộ lạc

        // ---- Lõi chuyên quân ----
        public int leader;      // 1 = trưởng đạo (con giữa đạo lúc gọi ra): +50% máu, viền vàng, dùng kỹ năng của lõi
        public float skillT;    // trưởng đạo: còn bao lâu mới dùng lại được kỹ năng
        public int charges;     // trưởng đạo: số lần đã dùng kỹ năng có giới hạn (hồi sinh, cổng hư không)
        public float cc;        // còn bị khống chế bao lâu (choáng / trói / sợ hãi)
        public int ccKind;      // CC.Stun / CC.Root / CC.Fear
        public float ccImmune;  // vừa hết khống chế: miễn khống chế thêm ngần này giây
        public float2 ccSrc;    // sợ hãi: chạy ra xa điểm này
        public float burn;      // còn bị bỏng bao lâu (không cộng dồn)
        public float still;     // Người cây: đã đứng yên bao lâu (đủ 3 giây thì bén rễ)
        public int pack;        // Người sói: số sói đồng đội đứng gần
        public int revived;     // đã được hồi sinh 1 lần
    }

    /// <summary>Khống chế. Hết khống chế thì miễn khống chế <see cref="Immune"/> giây, khống chế không cộng dồn.</summary>
    public static class CC
    {
        public const int None = 0, Stun = 1, Root = 2, Fear = 3;   // choáng: không đi, không đánh | trói: không đi, vẫn đánh | sợ: chạy ra xa
        public const float Immune = 2f;
    }

    public struct Arrow
    {
        public float2 from, to;
        public float t, invDuration, arc, damage, splash, slow;
        public int team, kind;
        public int wall;        // 1 = bắn vào tường thành: chạm đích thì trừ 1 máu thành
        // lõi chuyên quân
        public float burn;      // chạm đất thì gây bỏng ngần này giây
        public float cc; public int ccKind;   // chạm đất thì khống chế
        public int fx;          // ArrowFx.*
    }

    public static class ArrowFx
    {
        public const int Pierce = 1;     // tên xuyên: trúng thêm tối đa 2 con phía sau
        public const int AntiBig = 2;    // x2 sát thương lên quân to / khổng lồ
        public const int Cluster = 4;    // nổ xong văng 3 quả nhỏ
    }

    public struct Particle
    {
        public float2 pos, vel;
        public float h, vh, life, maxLife;
        public int kind, landed;
    }

    /// <summary>Dữ liệu 1 viên đạn gửi lên GPU (32 byte).</summary>
    public struct ArrowGpu
    {
        public float4 a; // xy = vị trí trên mặt đất, z = độ cao, w = loại đạn
        public float4 b; // x = góc bay trên màn hình, y = góc của bóng
    }
}
