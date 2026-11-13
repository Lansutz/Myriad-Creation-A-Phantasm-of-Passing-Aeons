using System;

namespace MyriadCreation.Content.Definitions
{
    /// <summary>单条前置的理由：没有它为什么不行。新 schema 要求每个前置都有。</summary>
    [Serializable]
    public class InnovationPrerequisiteRationale
    {
        public int prerequisiteId;
        public string rationale = string.Empty;
    }
}
