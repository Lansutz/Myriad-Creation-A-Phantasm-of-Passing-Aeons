using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CivilizationEvolution.EditorTools
{
    /// <summary>
    /// 场景重建（batchmode 用——UI 全面重建）：
    /// 新建空场景 → BuildGameScene（一键搭建全 UI[含主菜单/政权总览/区划树/
    /// 宗教/官职……]）→ 保存 Main.unity
    /// </summary>
    public static class SceneRebuilder
    {
        public static void RebuildMainScene()
        {
            // 新建空场景（替换当前——旧对象全清）
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            // 加默认光照（场景无光——Bootstrap 会建？——BuildGameScene 有灯光）
            CivilizationEvolutionMenu.BuildGameScene();
            CivilizationEvolutionMenu.SaveActiveScene();
            Debug.Log("[SceneRebuilder] Main.unity 已重建（全 UI 面板就位）");
        }
    }
}
