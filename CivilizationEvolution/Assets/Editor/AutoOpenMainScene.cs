using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine;

namespace CivilizationEvolution.EditorTools
{
    /// <summary>
    /// 编辑器启动自动打开主场景（解决"每次打开都停在空白 3D 界面"）：
    /// [InitializeOnLoad] 在编辑器加载/编译后执行——SessionState 标记防
    /// 重复（每次会话只自动开一次——编译重载不重复打断）
    /// </summary>
    [InitializeOnLoad]
    public static class AutoOpenMainScene
    {
        private const string ScenePath = "Assets/Scenes/Main.unity";
        private const string SessionFlag = "AutoOpenMainScene.Done";

        static AutoOpenMainScene()
        {
            // 等编辑器真正就绪再开（延迟——启动早期场景未加载完）
            EditorApplication.delayCall += () =>
            {
                if (SessionState.GetBool(SessionFlag, false)) return;
                SessionState.SetBool(SessionFlag, true);

                if (!System.IO.File.Exists(ScenePath))
                {
                    Debug.LogWarning("[AutoOpen] 未找到主场景 " + ScenePath);
                    return;
                }
                // 当前无已打开场景或打开的是默认 Untitled 时自动开主场景
                if (SceneManager.GetActiveScene().path != ScenePath)
                {
                    EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                    Debug.Log("[AutoOpen] 已自动打开主场景：" + ScenePath + "——按 Play 开始游戏");
                }
            };
        }
    }
}
