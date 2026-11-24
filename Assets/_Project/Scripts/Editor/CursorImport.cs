using UnityEditor;

namespace HordeLab.EditorTools
{
    /// <summary>
    /// Ảnh con trỏ chuột (Assets/_Project/Resources/Art/Cursor/*.png): tự đặt kiểu nhập để game đọc được pixel
    /// (co giãn theo độ phân giải + dò điểm bấm): đọc được, không nén, không mipmap, giữ nền trong suốt.
    /// </summary>
    public class CursorImport : AssetPostprocessor
    {
        void OnPreprocessTexture()
        {
            if (!assetPath.Replace('\\', '/').Contains("/Resources/Art/Cursor/")) return;
            var imp = (TextureImporter)assetImporter;
            imp.textureType = TextureImporterType.Default;
            imp.isReadable = true;
            imp.mipmapEnabled = false;
            imp.alphaIsTransparency = true;
            imp.npotScale = TextureImporterNPOTScale.None;
            imp.textureCompression = TextureImporterCompression.Uncompressed;
            imp.maxTextureSize = 1024;
        }
    }
}
