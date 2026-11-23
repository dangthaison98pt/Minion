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

        [MenuItem("Horde Lab/Build Windows (.exe)")]
        public static void BuildWindows()
        {
            if (!File.Exists(ScenePath)) CreateScene();
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };

            PlayerSettings.companyName = "HordeLab";
            PlayerSettings.productName = "Horde Lab";
            PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow; // Alt+Enter để ra cửa sổ
            PlayerSettings.defaultScreenWidth = 1920;
            PlayerSettings.defaultScreenHeight = 1080;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.runInBackground = true;
            PlayerSettings.enableFrameTimingStats = true;   // để đo thời gian GPU/CPU mỗi frame
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);

            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = "Build/HordeLab.exe",
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
