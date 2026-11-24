using System.IO;
using UnityEditor;
using UnityEngine;

namespace HordeLab.EditorTools
{
    /// <summary>
    /// Mọi PNG trong Assets/_Project/Resources/Art tự được cài đặt đúng kiểu pixel art:
    /// lọc Point (không nhoè), không nén, không mipmap, bật Read/Write (game cần đọc pixel để ghép atlas).
    /// </summary>
    public class ArtImport : AssetPostprocessor
    {
        void OnPreprocessTexture()
        {
            if (!assetPath.Replace('\\', '/').StartsWith(PixelArt.ArtFolder + "/")) return;
            var ti = (TextureImporter)assetImporter;
            ti.textureType = TextureImporterType.Default;
            ti.isReadable = true;
            ti.filterMode = FilterMode.Point;
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            ti.mipmapEnabled = false;
            ti.npotScale = TextureImporterNPOTScale.None;
            ti.alphaIsTransparency = true;
            ti.wrapMode = TextureWrapMode.Clamp;
            ti.sRGBTexture = true;
        }
    }

    public static class ArtTools
    {
        /// <summary>
        /// Ghi hình vẽ bằng code ra PNG cho từng loại quân / công trình CHƯA có file, để hoạ sĩ sửa tiếp.
        /// File đã có (đã sửa) thì giữ nguyên, không bao giờ ghi đè.
        /// </summary>
        [MenuItem("Horde Lab/Xuất art ra PNG để chỉnh sửa")]
        public static void ExportArt()
        {
            int written = 0, kept = 0;
            void Write(string rel, byte[] png)
            {
                string path = Path.Combine(PixelArt.ArtFolder, rel + ".png");
                if (File.Exists(path)) { kept++; return; }
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllBytes(path, png);
                written++;
            }

            for (int t = 0; t < UnitType.Count; t++) Write(PixelArt.UnitFile(t), PixelArt.UnitPngFromCode(t));
            for (int i = 0; i < PixelArt.TileCount; i++) Write(PixelArt.BuildingFile(i), PixelArt.TilePngFromCode(i));

            AssetDatabase.Refresh();
            Debug.Log($"[HordeLab] Art: ghi {written} file mới, giữ nguyên {kept} file đã có trong {PixelArt.ArtFolder}");
        }
    }
}
