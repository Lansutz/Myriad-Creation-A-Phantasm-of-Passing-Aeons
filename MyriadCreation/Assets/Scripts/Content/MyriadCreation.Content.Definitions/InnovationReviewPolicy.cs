namespace MyriadCreation.Content.Definitions
{
    /// <summary>
    /// 革新审查范围：宗教/信仰/思想域暂时冻结，等待后续重构；其余域按技术/制度/工艺继续审计。
    /// </summary>
    public static class InnovationReviewPolicy
    {
        public static bool IsFrozen(InnovationDef def)
        {
            if (def == null) return false;
            switch (def.field)
            {
                case InnovationField.Philosophy:
                case InnovationField.Theology:
                case InnovationField.ReligiousInstitution:
                case InnovationField.Ritual:
                case InnovationField.SocialThought:
                    return true;
                default:
                    return false;
            }
        }
    }
}
