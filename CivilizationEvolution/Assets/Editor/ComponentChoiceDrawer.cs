using UnityEditor;
using UnityEngine;
using System;
using CivilizationEvolution.Politics;

namespace CivilizationEvolution.EditorTools
{
    /// <summary>
    /// 政体成分选择器（ComponentChoice）自定义 Inspector 绘制。
    /// 核心问题：ComponentChoice 用裸 int 存储枚举序号，且七个政体维度复用同一个类，
    /// Unity 默认 Inspector 无法知道某个字段（如 supremeSuccession）该用哪个枚举，只能显示数字输入框。
    /// 本 Drawer 通过 SerializedProperty.propertyPath 的末段字段名判断所属维度，
    /// 把 primary / secondary 绘制为对应枚举的中文下拉选项（中文名取自 GovernmentComponentNames）。
    ///
    /// 字段名 → 维度映射（与 GovernmentComposition 的七个字段严格对齐）：
    ///   supremeSuccession / supremeScope / centralSuccession / centralInstitution /
    ///   localSuccession / localScope / spatialStructure
    /// </summary>
    [CustomPropertyDrawer(typeof(ComponentChoice))]
    public class ComponentChoiceDrawer : PropertyDrawer
    {
        /// <summary>维度解析结果：枚举类型 + 中文名查询委托</summary>
        private struct DimInfo
        {
            public Type enumType;
            public Func<int, string> nameFn;
            public DimInfo(Type t, Func<int, string> f) { enumType = t; nameFn = f; }
        }

        /// <summary>按字段名解析维度（未知字段返回 null，回退提示）</summary>
        private static DimInfo? ResolveDimension(string fieldName)
        {
            return fieldName switch
            {
                "supremeSuccession"   => new DimInfo(typeof(SupremeSuccession),   GovernmentComponentNames.NameSupremeSuccession),
                "supremeScope"        => new DimInfo(typeof(SupremeScope),        GovernmentComponentNames.NameSupremeScope),
                "centralSuccession"   => new DimInfo(typeof(CentralSuccession),   GovernmentComponentNames.NameCentralSuccession),
                "centralInstitution"  => new DimInfo(typeof(CentralInstitution),  GovernmentComponentNames.NameCentralInstitution),
                "localSuccession"     => new DimInfo(typeof(LocalSuccession),     GovernmentComponentNames.NameLocalSuccession),
                "localScope"          => new DimInfo(typeof(LocalScope),          GovernmentComponentNames.NameLocalScope),
                "spatialStructure"    => new DimInfo(typeof(SpatialStructure),    GovernmentComponentNames.NameSpatialStructure),
                _ => null
            };
        }

        /// <summary>生成某维度的中文选项数组（枚举值顺序 = 序号，Popup 选中索引即枚举 int 值）</summary>
        private static string[] GetOptions(DimInfo dim)
        {
            var values = Enum.GetValues(dim.enumType);
            var opts = new string[values.Length];
            for (int i = 0; i < values.Length; i++)
                opts[i] = dim.nameFn((int)values.GetValue(i));
            return opts;
        }

        /// <summary>从 propertyPath 取末段字段名（兼容 "composition.supremeSuccession" 嵌套路径）</summary>
        private static string FieldName(SerializedProperty p)
        {
            string path = p.propertyPath;
            int dot = path.LastIndexOf('.');
            return dot >= 0 ? path.Substring(dot + 1) : path;
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var secondary = property.FindPropertyRelative("secondary");
            int secCount = secondary != null ? secondary.arraySize : 0;
            // 1 行主导 + N 行次要 + 1 行添加按钮
            return (1 + secCount + 1) * EditorGUIUtility.singleLineHeight + 2f;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            string fieldName = FieldName(property);
            var dim = ResolveDimension(fieldName);

            var primary = property.FindPropertyRelative("primary");
            var secondary = property.FindPropertyRelative("secondary");

            float lineH = EditorGUIUtility.singleLineHeight;
            Rect r = new Rect(position.x, position.y, position.width, lineH);

            // 非七维字段（ComponentChoice 被误用于其他地方）：回退提示，不绘制数字框
            if (dim == null || primary == null)
            {
                EditorGUI.HelpBox(r, $"ComponentChoice 仅用于政体七维字段，当前字段 \"{fieldName}\" 未注册维度", MessageType.Warning);
                EditorGUI.EndProperty();
                return;
            }

            string[] options = GetOptions(dim.Value);

            // ── 主导成分（●）：字段名标签 + 中文下拉 ──
            float labelW = position.width * 0.32f;
            Rect labelR = new Rect(r.x, r.y, labelW, lineH);
            Rect popupR = new Rect(r.x + labelW, r.y, position.width - labelW, lineH);
            EditorGUI.LabelField(labelR, label, EditorStyles.boldLabel);
            primary.intValue = EditorGUI.Popup(popupR, primary.intValue, options);
            r.y += lineH;

            // ── 次要成分（○，0~2 个）：每个中文下拉 + 删除按钮 ──
            if (secondary != null)
            {
                for (int i = 0; i < secondary.arraySize; i++)
                {
                    var elem = secondary.GetArrayElementAtIndex(i);
                    Rect secPopupR = new Rect(r.x + labelW, r.y, position.width - labelW - 55f, lineH);
                    Rect delR = new Rect(r.x + position.width - 50f, r.y, 45f, lineH);

                    EditorGUI.LabelField(new Rect(r.x, r.y, labelW, lineH), $"  次要 {i + 1}");
                    elem.intValue = EditorGUI.Popup(secPopupR, elem.intValue, options);
                    if (GUI.Button(delR, "删"))
                    {
                        secondary.DeleteArrayElementAtIndex(i);
                        i--; // 删除后数组收缩，回退索引
                    }
                    r.y += lineH;
                }

                // 添加按钮（上限 2 个次要成分；达到上限隐藏）
                if (secondary.arraySize < 2)
                {
                    Rect addR = new Rect(r.x + labelW, r.y, 140f, lineH);
                    if (GUI.Button(addR, "+ 添加次要成分"))
                    {
                        secondary.arraySize++;
                        secondary.GetArrayElementAtIndex(secondary.arraySize - 1).intValue = 0;
                    }
                }
            }

            EditorGUI.EndProperty();
        }
    }
}
