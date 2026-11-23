using Unity.Mathematics;

namespace HordeLab
{
    /// <summary>Hằng số chung của trận đánh.</summary>
    public static class Cfg
    {
        // 1 đơn vị thế giới = 10 pixel nghệ thuật. Mỗi lính là 1 ô 10x10 pixel.
        public const int PPU = 10;

        // Chiến trường 192 x 108 đơn vị = 1920 x 1080 pixel nghệ thuật
        public const float HalfW = 96f, HalfH = 54f;
        public const int GroundW = 1920, GroundH = 1080;

        // Lưới không gian để tìm hàng xóm
        public const float CellSize = 2f;
        public const int GridW = 96, GridH = 54, Cells = GridW * GridH;

        public const int MaxUnits = 32768;     // đủ cho màn 20k lính (mỗi lính ~48 byte → vài MB tổng cộng)
        public const int MaxArrows = 16384;
        public const int MaxParticles = 65536;
        public const int MaxStamps = 16384;

        // Xác sống vượt qua vạch này là phá tường thành
        public const float WallX = -91f;

        public static int CellX(float x) => math.clamp((int)math.floor((x + HalfW) / CellSize), 0, GridW - 1);
        public static int CellY(float y) => math.clamp((int)math.floor((y + HalfH) / CellSize), 0, GridH - 1);
        public static int TeamOf(int type) => (type == UnitType.Zombie || type == UnitType.Skeleton) ? 1 : 0;
    }

    /// <summary>Chỉ số trong mảng counters dùng chung giữa các Job.</summary>
    public static class C
    {
        public const int Units = 0, Arrows = 1, Particles = 2, Stamps = 3;
        public const int DeadKingdom = 4, DeadUndead = 5, Breach = 6;
        public const int Type0 = 8; // 8..12: số lượng mỗi loại lính
        public const int Length = 16;
    }

    /// <summary>Loại lính. Vương quốc: Footman, Archer, Cavalry. Xác sống: Zombie, Skeleton (dùng Cfg.TeamOf để biết phe).</summary>
    public static class UnitType
    {
        public const int Footman = 0, Archer = 1, Zombie = 2, Skeleton = 3, Cavalry = 4;
        public const int Count = 5;
    }

    public static class PKind
    {
        public const int Blood = 0, DarkBlood = 1, Spark = 2, Dust = 3, Bone = 4, Fire = 5, Smoke = 6, Ember = 7, GreenSpark = 8;
    }

    /// <summary>Ô trong atlas "dấu in" lên mặt đất.</summary>
    public static class StampCell
    {
        public const int BloodRed = 0, BloodDark = 4, Corpse = 8, CorpseAlt = 12, Drop = 16,
                         ArrowKingdom = 20, ArrowUndead = 22, Scorch = 24, Ash = 28,
                         HorseCorpse = 32; // 32 = ngựa chết, 33 = ngựa + kỵ sĩ chết
    }

    public struct UnitStats
    {
        public float maxHp, damage, cooldown, range, speed;
        public int ranged;
    }

    /// <summary>Một người lính chỉ là struct này, không có GameObject nào cả.</summary>
    public struct Unit
    {
        public float2 pos;
        public float2 goal;     // vị trí địch gần nhất tìm được lần quét gần nhất
        public float hp;
        public float cooldown;
        public float animT;
        public float flash;     // nháy trắng khi trúng đòn
        public int type;
        public int state;       // 0 = đi, 1 = đánh
        public int hasGoal;
        public int faceLeft;
        public float charge;    // kỵ binh: đà xung phong 0..1, tích khi phi, xả hết vào đòn đâm kế tiếp
    }

    public struct Arrow
    {
        public float2 from, to;
        public float t, invDuration, arc, damage;
        public int team;
    }

    public struct Particle
    {
        public float2 pos, vel;
        public float h, vh, life, maxLife;
        public int kind, landed;
    }

    /// <summary>Dữ liệu 1 mũi tên gửi lên GPU (32 byte).</summary>
    public struct ArrowGpu
    {
        public float4 a; // xy = vị trí trên mặt đất, z = độ cao, w = phe
        public float4 b; // x = góc bay trên màn hình, y = góc của bóng
    }
}
