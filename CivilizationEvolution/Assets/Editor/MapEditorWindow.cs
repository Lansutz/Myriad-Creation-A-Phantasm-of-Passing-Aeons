using UnityEditor;
using UnityEngine;
using CivilizationEvolution.Core;
using CivilizationEvolution.Map;

namespace CivilizationEvolution.EditorTools
{
    /// <summary>
    /// 地图编辑器面板（EditorWindow）
    /// - 画笔设置：通过 SerializedObject 直改场景中 MapEditor 的序列化字段（支持撤销/保存）
    /// - 海陆参数：编辑 DefaultWorldConfig 资产（海平面等5滑块 + 左右连通）
    /// - 地形生成：播放模式中同步配置后重新生成 / 全量重算
    /// </summary>
    public class MapEditorWindow : EditorWindow
    {
        private const string DefaultConfigPath = "Assets/ScriptableObjects/DefaultWorldConfig.asset";

        private MapEditor editor;
        private GameWorld world;

        // MapEditor 私有序列化字段的 SerializedProperty
        private SerializedObject editorSo;
        private SerializedProperty brushModeProp;
        private SerializedProperty brushShapeProp;
        private SerializedProperty brushRadiusProp;
        private SerializedProperty brushStrengthProp;
        private SerializedProperty continuousPaintProp;
        private SerializedProperty editorActiveProp;

        // 世界配置资产
        private WorldConfig configAsset;
        private SerializedObject configSo;

        private int seed = 42;

        [MenuItem("Civilization Evolution/4. 地图编辑器面板", false, 4)]
        public static void Open()
        {
            var win = GetWindow<MapEditorWindow>("地图编辑器");
            win.minSize = new Vector2(320, 480);
            win.Show();
        }

        private void OnEnable()
        {
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
            ResolveReferences();
        }

        private void OnDisable()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
        }

        private void OnPlayModeChanged(PlayModeStateChange state)
        {
            ResolveReferences();
            Repaint();
        }

        /// <summary>定位场景对象与配置资产（进入/退出播放模式后对象会被重建）</summary>
        private void ResolveReferences()
        {
            editor = Object.FindAnyObjectByType<MapEditor>(FindObjectsInactive.Include);
            world = Object.FindAnyObjectByType<GameWorld>(FindObjectsInactive.Include);

            configAsset = AssetDatabase.LoadAssetAtPath<WorldConfig>(DefaultConfigPath);
            if (configAsset == null)
            {
                var guids = AssetDatabase.FindAssets("t:WorldConfig");
                if (guids.Length > 0)
                    configAsset = AssetDatabase.LoadAssetAtPath<WorldConfig>(AssetDatabase.GUIDToAssetPath(guids[0]));
            }

            editorSo = null;
            configSo = null;
            if (editor != null)
            {
                editorSo = new SerializedObject(editor);
                brushModeProp = editorSo.FindProperty("brushMode");
                brushShapeProp = editorSo.FindProperty("brushShape");
                brushRadiusProp = editorSo.FindProperty("brushRadius");
                brushStrengthProp = editorSo.FindProperty("brushStrength");
                continuousPaintProp = editorSo.FindProperty("continuousPaint");
                editorActiveProp = editorSo.FindProperty("isEditorActive");
            }
            if (configAsset != null)
                configSo = new SerializedObject(configAsset);
        }

        private void OnGUI()
        {
            // 播放模式切换或对象销毁后（Unity 假空）重新解析
            if (editor == null || world == null)
                ResolveReferences();

            EditorGUILayout.LabelField(
                EditorApplication.isPlaying ? "播放模式：运行中（画笔/地形生成可用）" : "播放模式：已停止（仅可编辑资产参数）",
                EditorStyles.boldLabel);

            if (editor == null || world == null)
            {
                EditorGUILayout.HelpBox(
                    "场景中未找到 MapEditor/GameWorld。请打开 Main.unity，若场景为空则执行菜单\"1. 一键搭建游戏场景\"。",
                    MessageType.Warning);
                return;
            }

            DrawBrushSection();
            DrawQuickActions();
            DrawConfigSection();
            DrawGenerateSection();
            DrawShortcutHelp();
        }

        // ===== 画笔设置 =====
        private void DrawBrushSection()
        {
            editorSo.Update();
            EditorGUILayout.Space();
            GUILayout.Label("画笔设置", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(editorActiveProp, new GUIContent("启用地图编辑器"));
            EditorGUILayout.PropertyField(brushModeProp, new GUIContent("画笔模式"));
            EditorGUILayout.PropertyField(brushShapeProp, new GUIContent("画笔形状"));
            EditorGUILayout.IntSlider(brushRadiusProp, 0, 20, new GUIContent("画笔半径"));
            EditorGUILayout.Slider(brushStrengthProp, 0.01f, 0.5f, new GUIContent("画笔强度"));
            EditorGUILayout.PropertyField(continuousPaintProp, new GUIContent("拖拽连续绘制"));
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(editor, "修改画笔参数");
                editorSo.ApplyModifiedProperties();
            }

            if (editorActiveProp.boolValue)
            {
                EditorGUILayout.HelpBox($"当前画笔：{editor.GetBrushInfo()}", MessageType.None);
            }
        }

        // ===== 快捷操作 =====
        private void DrawQuickActions()
        {
            EditorGUILayout.Space();
            GUILayout.Label("快捷操作（播放模式）", EditorStyles.boldLabel);
            GUI.enabled = EditorApplication.isPlaying && editorActiveProp != null && editorActiveProp.boolValue;
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("填充全部地块")) editor.FillAllTiles();
            if (GUILayout.Button("清空全部地块")) editor.ClearAllTiles();
            EditorGUILayout.EndHorizontal();
            GUI.enabled = true;
        }

        // ===== 海陆参数（世界配置资产）=====
        private void DrawConfigSection()
        {
            EditorGUILayout.Space();
            GUILayout.Label("海陆参数（WorldConfig 资产）", EditorStyles.boldLabel);

            if (configAsset == null)
            {
                EditorGUILayout.HelpBox(
                    "未找到 WorldConfig 资产，请先执行菜单\"2. 创建世界配置资产\"，再挂到 GameWorld 上。",
                    MessageType.Info);
                return;
            }

            configSo.Update();
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.Slider(configSo.FindProperty("seaLevel"), 0f, 1f, new GUIContent("海平面"));
            EditorGUILayout.Slider(configSo.FindProperty("landAmount"), 0.1f, 0.8f, new GUIContent("陆地总量"));
            EditorGUILayout.Slider(configSo.FindProperty("landFragment"), 0f, 1f, new GUIContent("陆地破碎度"));
            EditorGUILayout.Slider(configSo.FindProperty("coastFragment"), 0f, 1f, new GUIContent("海岸破碎度"));
            EditorGUILayout.Slider(configSo.FindProperty("oceanBuffer"), 0f, 1f, new GUIContent("外海缓冲"));
            EditorGUILayout.PropertyField(configSo.FindProperty("wrapX"), new GUIContent("左右连通（环绕世界）"));
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(configAsset, "修改海陆参数");
                configSo.ApplyModifiedProperties();
                EditorUtility.SetDirty(configAsset);
                AssetDatabase.SaveAssets();
            }

            EditorGUILayout.HelpBox("播放模式下运行时使用配置副本：参数改动需点击下方\"重新生成/应用配置\"才会生效。", MessageType.None);
        }

        // ===== 地形生成 =====
        private void DrawGenerateSection()
        {
            EditorGUILayout.Space();
            GUILayout.Label("地形生成（播放模式）", EditorStyles.boldLabel);
            seed = EditorGUILayout.IntField("随机种子", seed);

            GUI.enabled = EditorApplication.isPlaying && world != null && configAsset != null;
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("重新生成地形"))
            {
                SyncConfigToRuntime();
                world.GenerateTerrain(seed);
            }
            if (GUILayout.Button("应用配置并重算"))
            {
                SyncConfigToRuntime();
                world.RecalculateAll();
            }
            EditorGUILayout.EndHorizontal();
            GUI.enabled = true;
        }

        /// <summary>把海陆参数资产同步到运行时配置副本</summary>
        private void SyncConfigToRuntime()
        {
            if (world == null || configAsset == null) return;
            world.UpdateConfig(c =>
            {
                c.wrapX = configAsset.wrapX;
                c.seaLevel = configAsset.seaLevel;
                c.landAmount = configAsset.landAmount;
                c.landFragment = configAsset.landFragment;
                c.coastFragment = configAsset.coastFragment;
                c.oceanBuffer = configAsset.oceanBuffer;
            });
        }

        private void DrawShortcutHelp()
        {
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(
                "运行时快捷键：E 启停编辑器 | 1-6 画笔模式 | [ ] 半径 | B 形状",
                MessageType.None);
        }
    }
}