namespace CivilizationEvolution.Simulation.Characters
{
    /// <summary>
    /// 衰退阶段定义——每个衰退类型有自己的阶段
    /// </summary>
    [System.Serializable]
    public struct ImpairmentStage
    {
        public string stageId;             // 阶段ID
        public string stageName;           // 阶段名称（显示用）
        public string description;         // 阶段描述

        // 属性修正
        public float healthMod;
        public float prowessMod;
        public float socialMod;
        public float militaryMod;
        public float managementMod;
        public float conspiracyMod;
        public float scholarshipMod;
        public float charmMod;
        public float fertilityMod;

        // 达到此阶段获得的永久特质ID（最高级阶段用）
        public string permanentTraitId;
    }

    /// <summary>
    /// 衰退类型定义接口——每种衰退有自己的阶段定义
    /// </summary>
    public interface IImpairmentDef
    {
        string ImpairmentId { get; }
        string ImpairmentName { get; }
        ImpairmentStage[] Stages { get; }
    }
}
