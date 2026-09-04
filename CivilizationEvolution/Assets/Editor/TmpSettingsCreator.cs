using UnityEditor;
using UnityEngine;
using TMPro;

namespace CivilizationEvolution.EditorTools
{
    /// <summary>
    /// 创建 TMP Settings（6000.6 新格式——旧版资产字段失配删除后重建）：
    /// new TMP_Settings（新类字段默认）→ Resources/TMP Settings.asset
    /// </summary>
    public static class TmpSettingsCreator
    {
        public static void CreateDefaultSettings()
        {
            var settings = ScriptableObject.CreateInstance<TMP_Settings>();
            string dir = "Assets/TextMesh Pro/Resources";
            if (!AssetDatabase.IsValidFolder(dir))
            {
                AssetDatabase.CreateFolder("Assets/TextMesh Pro", "Resources");
            }
            string path = dir + "/TMP Settings.asset";

            // 私有字段 m_leadingCharacters/m_followingCharacters 反射赋值
            // （LineBreaking 文本——Resources 下已有）
            var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
            var lt = AssetDatabase.LoadAssetAtPath<TextAsset>(dir + "/LineBreaking Leading Characters.txt");
            var ft = AssetDatabase.LoadAssetAtPath<TextAsset>(dir + "/LineBreaking Following Characters.txt");
            if (lt != null)
                typeof(TMP_Settings).GetField("m_leadingCharacters", flags)?.SetValue(settings, lt);
            if (ft != null)
                typeof(TMP_Settings).GetField("m_followingCharacters", flags)?.SetValue(settings, ft);

            AssetDatabase.CreateAsset(settings, path);
            AssetDatabase.SaveAssets();
            Debug.Log($"[TmpSettings] 已创建 6000.6 格式 TMP Settings：{path}");
        }
    }
}
