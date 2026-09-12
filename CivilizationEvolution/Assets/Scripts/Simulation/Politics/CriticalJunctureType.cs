namespace CivilizationEvolution.Simulation.Politics
{
    public enum CriticalJunctureType
    {
        SuccessionCrisis,   // 继承危机：绝嗣/幼主/继位争议
        WarDefeat,          // 战争失败：主力被歼/兵临首都，暴露国家无能（斯考切波/蒂利）
        FiscalCollapse,     // 财政破产：国库枯竭、国家停摆
        EliteSplit,         // 精英分裂：统治集团内斗、保守阵营凝聚力崩溃
        PopularUprising,    // 民众起义：最受压迫阶层组织化起事
        ForeignConquest,    // 外部征服：大片领土被占，强加或倒逼制度
        StrongReformer      // 强势改革者：高能力统治者借改革派上位
    }
}
