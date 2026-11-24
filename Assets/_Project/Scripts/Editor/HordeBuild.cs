using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace HordeLab.EditorTools
{
    public static class HordeBuild
    {
        public const string ScenePath = "Assets/_Project/Scenes/Battle.unity";

        [MenuItem("Horde Lab/Tạo lại scene Battle")]
        public static void CreateScene()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            new GameObject("Battle").AddComponent<HordeLab.Battle>();
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
        }

        /// <summary>Xuất atlas sprite ra Docs/Screenshots để xem hình các loại quân mà không cần chạy game.</summary>
        [MenuItem("Horde Lab/Xuất atlas sprite ra PNG")]
        public static void DumpAtlases()
        {
            Directory.CreateDirectory("Docs/Screenshots");
            HordeLab.PixelArt.SaveAtlases("Docs/Screenshots");
            Debug.Log("[HordeLab] Đã xuất Docs/Screenshots/atlas_units.png và atlas_stamps.png");
        }

        /// <summary>Mặc định Build/FantasyWorldWar.exe; chạy dòng lệnh có thể thêm  -buildPath "thư mục/FantasyWorldWar.exe"  (dùng khi bản trong Build/ đang mở nên bị khoá).</summary>
        static string BuildPath()
        {
            var args = System.Environment.GetCommandLineArgs();
            int i = System.Array.IndexOf(args, "-buildPath");
            return i >= 0 && i + 1 < args.Length ? args[i + 1] : "Build/FantasyWorldWar.exe";
        }

        const string IconDir = "Assets/_Project/Icon";

        /// <summary>
        /// Xuất icon game (vẽ bằng code, PixelArt.DrawAppIcon) ra PNG cho từng cỡ Windows cần + bản xem ở Docs/Icon,
        /// rồi gán làm icon của bản build.
        /// </summary>
        [MenuItem("Horde Lab/Xuất icon game")]
        public static void ApplyIcons()
        {
            Directory.CreateDirectory(IconDir);
            Directory.CreateDirectory("Docs/Icon");
            var target = NamedBuildTarget.Standalone;
            int[] sizes = PlayerSettings.GetIconSizes(target, IconKind.Application);
            foreach (int sz in new[] { 16, 32, 48, 64, 128, 256, 512 })
            {
                var t = PixelArt.AppIconTexture(sz);
                File.WriteAllBytes($"Docs/Icon/icon_{sz}.png", t.EncodeToPNG());
                Object.DestroyImmediate(t);
            }
            var icons = new Texture2D[sizes.Length];
            for (int i = 0; i < sizes.Length; i++)
            {
                string path = $"{IconDir}/AppIcon_{sizes[i]}.png";
                var t = PixelArt.AppIconTexture(sizes[i]);
                File.WriteAllBytes(path, t.EncodeToPNG());
                Object.DestroyImmediate(t);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                var imp = (TextureImporter)AssetImporter.GetAtPath(path);
                imp.textureType = TextureImporterType.Default;
                imp.mipmapEnabled = false;
                imp.filterMode = FilterMode.Point;
                imp.textureCompression = TextureImporterCompression.Uncompressed;
                imp.alphaIsTransparency = true;
                imp.SaveAndReimport();
                icons[i] = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            }
            PlayerSettings.SetIcons(target, icons, IconKind.Application);
            var main = AssetDatabase.LoadAssetAtPath<Texture2D>($"{IconDir}/AppIcon_{sizes[0]}.png");
            PlayerSettings.SetIcons(NamedBuildTarget.Unknown, new[] { main }, IconKind.Any);   // icon mặc định
            Debug.Log($"[HordeLab] Đã gán icon game: {sizes.Length} cỡ ({string.Join(", ", sizes)})");
        }

        [MenuItem("Horde Lab/Build Windows (.exe)")]
        public static void BuildWindows()
        {
            if (!File.Exists(ScenePath)) CreateScene();
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };

            PlayerSettings.companyName = "FantasyWorldWar";
            PlayerSettings.productName = "Fantasy World War";
            ApplyIcons();
            PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow; // Alt+Enter để ra cửa sổ
            PlayerSettings.defaultScreenWidth = 1920;
            PlayerSettings.defaultScreenHeight = 1080;
            PlayerSettings.resizableWindow = false;          // cỡ cửa sổ do người chơi chọn trong Settings, không kéo giãn
            PlayerSettings.defaultIsNativeResolution = true;    // toàn màn hình = cỡ gốc (không kéo giãn, chữ không mờ)
            PlayerSettings.runInBackground = true;
            PlayerSettings.enableFrameTimingStats = true;   // để đo thời gian GPU/CPU mỗi frame
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);

            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = BuildPath(),
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            });
            Debug.Log($"[HordeLab] BUILD {report.summary.result}: {report.summary.totalErrors} lỗi, {report.summary.totalSize / (1024 * 1024)} MB");
            if (Application.isBatchMode) EditorApplication.Exit(report.summary.result == BuildResult.Succeeded ? 0 : 1);
        }
    }

    /// <summary>Lần đầu mở project: tự mở scene Battle để bạn chỉ việc bấm Play.</summary>
    [InitializeOnLoad]
    static class OpenBattleSceneOnStart
    {
        static OpenBattleSceneOnStart()
        {
            if (Application.isBatchMode) return;
            EditorApplication.delayCall += () =>
            {
                if (SessionState.GetBool("HordeLab.Opened", false)) return;
                SessionState.SetBool("HordeLab.Opened", true);
                if (string.IsNullOrEmpty(EditorSceneManager.GetActiveScene().path) && File.Exists(HordeBuild.ScenePath))
                    EditorSceneManager.OpenScene(HordeBuild.ScenePath);
            };
        }
    }
}
