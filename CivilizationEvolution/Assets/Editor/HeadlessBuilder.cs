#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CivilizationEvolution.EditorTools
{
    /// <summary>
    /// 无界面（batchmode）构建入口
    /// 命令行：Unity.exe -batchmode -quit -projectPath &lt;项目&gt;
    ///         -executeMethod CivilizationEvolution.EditorTools.HeadlessBuilder.BuildAll
    /// 作用：生成/刷新全部 .meta、确保世界配置资产存在、搭建并保存 Main.unity 场景。
    /// </summary>
    public static class HeadlessBuilder
    {
        private const string ScenePath = "Assets/Scenes/Main.unity";
        private const string ConfigPath = "Assets/ScriptableObjects/DefaultWorldConfig.asset";

        public static void BuildAll()
        {
            Debug.Log("[HeadlessBuilder] 开始无界面构建……");

            // 0. 确保 TMP Settings 资产（TMP 字体运行时依赖——缺失则 CreateFontAsset NRE）
            EnsureTmpSettings();

            // 1. 确保目录存在
            EnsureFolder("Assets", "Scenes");
            EnsureFolder("Assets", "ScriptableObjects");

            // 2. 确保世界配置资产存在
            if (AssetDatabase.LoadAssetAtPath<ScriptableObject>(ConfigPath) == null)
            {
                CivilizationEvolutionMenu.CreateWorldConfigAsset();
            }

            // 3. 新建空场景并搭建
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            CivilizationEvolutionMenu.BuildGameScene();

            // 4. 保存场景
            EditorSceneManager.SaveScene(scene, ScenePath);

            // 5. 刷新并保存所有资产（生成.meta）
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };

            Debug.Log($"[HeadlessBuilder] 构建完成，场景已保存：{ScenePath}");
        }

        /// <summary>
        /// GUI 启动入口：打开项目后自动加载 Main 场景（供 -executeMethod 调用，新手免找场景）。
        /// </summary>
        public static void OpenMainScene()
        {
            if (System.IO.File.Exists(ScenePath))
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            else
                Debug.LogWarning($"[HeadlessBuilder] 场景不存在：{ScenePath}，请先执行菜单 Civilization Evolution/1. 一键搭建游戏场景");
        }

        /// <summary>确保 TMP 运行时依赖就位（shader 在 Assets/TextMesh Pro/、Settings 在 Resources）</summary>
        private static void EnsureTmpSettings()
        {
            // TMP 运行时 shader + Settings 资产缺失时告警（首次导入请运行本构建或导入 TMP Essential Resources）
            if (Shader.Find("TextMeshPro/Distance Field") == null)
                Debug.LogWarning("[HeadlessBuilder] TMP shader 缺失（Assets/TextMesh Pro/Shaders/）——TMP 字体无法生成");
            if (System.IO.File.Exists("Assets/TextMesh Pro/Resources/TMP Settings.asset") == false
                && System.IO.File.Exists("Assets/Resources/TMP Settings.asset") == false)
                Debug.LogWarning("[HeadlessBuilder] TMP Settings 资产缺失（Resources 下）——TMP 字体无法生成");
        }

        /// <summary>
        /// 构建玩家（第三关：打包验证）——Windows x64
        /// 命令行：Unity.exe -batchmode -quit -projectPath &lt;项目&gt;
        ///         -executeMethod CivilizationEvolution.EditorTools.HeadlessBuilder.BuildPlayer
        /// </summary>
        public static void BuildPlayer()
        {
            Debug.Log("[HeadlessBuilder] 开始构建玩家……");

            // 确保场景在 Build Settings（缺场景=打包黑屏的典型原因）
            if (EditorBuildSettings.scenes == null || EditorBuildSettings.scenes.Length == 0
                || !System.Array.Exists(EditorBuildSettings.scenes, s => s.path == ScenePath))
            {
                EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            }

            var options = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = System.IO.Path.Combine(
                    System.IO.Path.GetDirectoryName(System.IO.Path.GetDirectoryName(Application.dataPath)), "Builds", "CivilizationEvolution.exe"),
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            };

            var report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
                Debug.Log($"[HeadlessBuilder] 构建成功：{options.locationPathName}");
            else
            {
                Debug.LogError($"[HeadlessBuilder] 构建失败：{report.summary.result}");
                foreach (var err in report.steps)
                    foreach (var msg in err.messages)
                        if (msg.type == UnityEngine.LogType.Error)
                            Debug.LogError(msg.content);
            }
        }

        private static void EnsureFolder(string parent, string child)
        {
            string path = $"{parent}/{child}";
            if (!AssetDatabase.IsValidFolder(path))
                AssetDatabase.CreateFolder(parent, child);
        }
    }
}
#endif
