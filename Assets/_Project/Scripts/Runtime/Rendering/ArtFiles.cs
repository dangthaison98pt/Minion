using System.Text;
using UnityEngine;

namespace HordeLab
{
    /// <summary>
    /// Art dạng file PNG để hoạ sĩ tự sửa. Mỗi loại quân / công trình 1 file trong Assets/_Project/Resources/Art/.
    /// Có file thì dùng file (giữ nguyên, KHÔNG tự thêm viền: hoạ sĩ tự vẽ viền), thiếu file thì vẽ bằng code như cũ.
    ///
    ///   Art/Units/&lt;Tên quân&gt;.png       4 cột (khung hình) × 2 hàng (trên = đi, dưới = đánh), quay mặt sang phải.
    ///                                    Mỗi khung 16×16; quân khổng lồ (Rồng) 32×32.
    ///                                    Lính bộ chỉ được vẽ trong 12×12 ở giữa khung (chừa 2 pixel mỗi bên), phần ngoài bị cắt.
    ///   Art/Buildings/&lt;Tên nhà&gt;.png    1 ô 32×32. _Empty = ô trống, _Unopened = ô chưa mở.
    ///
    /// Tên file = tên trong game bỏ dấu cách và ký tự lạ, ví dụ "Shield Swordsman" → ShieldSwordsman.png.
    /// Xuất bộ file khởi đầu bằng menu Horde Lab → Xuất art ra PNG để chỉnh sửa (không ghi đè file đã có).
    /// </summary>
    public static partial class PixelArt
    {
        public const string ArtFolder = "Assets/_Project/Resources/Art";   // đường dẫn trong project (công cụ xuất file dùng)
        const string ArtResources = "Art";                                 // đường dẫn cho Resources.Load

        public static string FileName(string name)
        {
            var sb = new StringBuilder();
            foreach (char ch in name) if (char.IsLetterOrDigit(ch)) sb.Append(ch);
            return sb.ToString();
        }

        public static string UnitFile(int type) => "Units/" + FileName(GameData.Units[type].name);

        public static string BuildingFile(int id) =>
            "Buildings/" + (id == TileEmpty ? "_Empty" : id == TileCandidate ? "_Unopened" : FileName(GameData.Buildings[id].name));

        /// <summary>Cỡ 1 khung hình trong file quân: 16, quân khổng lồ 32.</summary>
        public static int UnitFramePx(int type) => (GameData.Units[type].stats.flags & UFlag.Giant) != 0 ? GiantPx : UnitCell;

        static bool IsInfantryType(int type) => (GameData.Units[type].stats.flags & (UFlag.Big | UFlag.Giant)) == 0;

        /// <summary>Đọc 1 file art thành Canvas; null nếu không có file hoặc đọc không được.</summary>
        static Canvas LoadArt(string path, int w, int h)
        {
            var tex = Resources.Load<Texture2D>(ArtResources + "/" + path);
            if (tex == null) return null;
            if (!tex.isReadable) { Debug.LogWarning($"[HordeLab] Art/{path}.png chưa bật Read/Write — dùng hình vẽ bằng code."); return null; }
            if (tex.width != w || tex.height != h)
            {
                Debug.LogWarning($"[HordeLab] Art/{path}.png phải là {w}x{h} (đang là {tex.width}x{tex.height}) — dùng hình vẽ bằng code.");
                return null;
            }
            var c = new Canvas(w, h);
            tex.GetPixels32().CopyTo(c.Px, 0);      // cả Texture2D lẫn Canvas đều lưu hàng dưới cùng trước
            return c;
        }

        /// <summary>Khung <paramref name="f"/> của trạng thái <paramref name="state"/> lấy từ file quân; null nếu không có file.</summary>
        static Canvas UnitFrameFromFile(int type, int state, int f)
        {
            int cell = UnitFramePx(type);
            var sheet = LoadArt(UnitFile(type), cell * 4, cell * 2);
            if (sheet == null) return null;
            int pad = IsInfantryType(type) ? (UnitCell - InfantryPx - 2) / 2 : 0;   // lính bộ: lấy 12×12 ở giữa (khớp hình vẽ bằng code)
            int size = cell - pad * 2;
            var frame = new Canvas(size, size);
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                    frame.Set(x, y, sheet.Get(f * cell + pad + x, state * cell + pad + y));
            return frame;
        }

        /// <summary>Tranh công trình từ file; null nếu không có.</summary>
        static Canvas TileFromFile(int id) => LoadArt(BuildingFile(id), TilePx, TilePx);

        // ------------------------------------------------------------------
        // Xuất hình vẽ bằng code ra PNG (công cụ Editor gọi)
        // ------------------------------------------------------------------
        /// <summary>Tấm hình của 1 loại quân đúng định dạng file art, vẽ bằng code (đã có viền).</summary>
        public static byte[] UnitPngFromCode(int type)
        {
            int cell = UnitFramePx(type);
            var sheet = new Canvas(cell * 4, cell * 2);
            for (int state = 0; state < 2; state++)
                for (int f = 0; f < 4; f++)
                {
                    var s = DrawUnit(type, state, f, false);
                    int pad = (cell - s.W) / 2;
                    sheet.Blit(s, f * cell + pad, state * cell + pad);
                }
            return Png(sheet);
        }

        public static byte[] TilePngFromCode(int id)
        {
            var t = DrawTile(id);
            EdgeShade(t);
            return Png(t);
        }

        static byte[] Png(Canvas c)
        {
            var tex = new Texture2D(c.W, c.H, TextureFormat.RGBA32, false);   // ToTexture() khoá đọc nên không mã hoá PNG được
            tex.SetPixels32(c.Px);
            tex.Apply();
            var png = tex.EncodeToPNG();
            if (Application.isPlaying) Object.Destroy(tex); else Object.DestroyImmediate(tex);
            return png;
        }
    }
}
