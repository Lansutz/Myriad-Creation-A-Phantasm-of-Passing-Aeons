#if UNITY_EDITOR
using UnityEditor;

namespace MyriadCreation.Editor
{
    /// <summary>批处理编译检查入口：仅用于本机验证编译是否通过，不参与游戏运行。</summary>
    public static class BatchCompileCheck
    {
        public static void Run()
        {
            UnityEngine.Debug.Log("[BatchCompileCheck] COMPILE_OK");
            EditorApplication.Exit(0);
        }
    }
}
#endif
