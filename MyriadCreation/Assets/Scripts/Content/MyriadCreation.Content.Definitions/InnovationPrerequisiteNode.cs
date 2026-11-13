using System;
using System.Collections.Generic;

namespace MyriadCreation.Content.Definitions
{
    public enum InnovationPrerequisiteKind
    {
        Leaf = 0,
        All = 1,
        Any = 2,
    }

    /// <summary>
    /// 前置表达式：Leaf=单个革新；All=全部满足；Any=多选一。All/Any 可嵌套，
    /// 但内容规范建议只嵌套 1–2 层（不要无限递归）。
    /// </summary>
    [Serializable]
    public class InnovationPrerequisiteNode
    {
        public InnovationPrerequisiteKind kind;

        /// <summary>Leaf 节点的革新 id；非 Leaf 忽略。</summary>
        public int innovationId;

        public List<InnovationPrerequisiteNode> children = new List<InnovationPrerequisiteNode>();

        public static InnovationPrerequisiteNode Leaf(int id)
        {
            return new InnovationPrerequisiteNode { kind = InnovationPrerequisiteKind.Leaf, innovationId = id };
        }

        public static InnovationPrerequisiteNode All(params InnovationPrerequisiteNode[] nodes)
        {
            var result = new InnovationPrerequisiteNode { kind = InnovationPrerequisiteKind.All };
            if (nodes != null) result.children.AddRange(nodes);
            return result;
        }

        public static InnovationPrerequisiteNode Any(params InnovationPrerequisiteNode[] nodes)
        {
            var result = new InnovationPrerequisiteNode { kind = InnovationPrerequisiteKind.Any };
            if (nodes != null) result.children.AddRange(nodes);
            return result;
        }

        public bool IsLeaf => kind == InnovationPrerequisiteKind.Leaf || children == null || children.Count == 0;
    }
}
