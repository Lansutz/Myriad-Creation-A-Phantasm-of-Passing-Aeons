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

        private static void EnsureFolder(string parent, string child)
        {
            string path = $"{parent}/{child}";
            if (!AssetDatabase.IsValidFolder(path))
                AssetDatabase.CreateFolder(parent, child);
        }
    }
}
#endif
