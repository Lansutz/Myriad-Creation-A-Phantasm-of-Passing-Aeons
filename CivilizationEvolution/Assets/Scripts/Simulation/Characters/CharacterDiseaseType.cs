namespace CivilizationEvolution.Simulation.Characters
{
    /// <summary>
    /// 角色疾病类型——按病因和性质分类
    /// 与人口级瘟疫系统（DiseaseSystem）分层：人口级是群体传播，角色级是个体患病
    /// </summary>
    public enum CharacterDiseaseCategory
    {
        Infectious,     // 传染病：可从人口级瘟疫感染，或角色间传播
        Chronic,        // 慢性病/老年病：与年龄、生活方式相关，长期存在
        Injury,         // 伤病：战斗/意外导致，急性发作
        Genetic,        // 遗传病/先天疾病：由DNA决定，出生即有
        Mental          // 精神疾病：已有系统，这里统一纳入疾病框架
    }

    /// <summary>
    /// 角色疾病状态
    /// </summary>
    public enum DiseaseStage
    {
        Incubation,     // 潜伏期：已感染但无症状
        Acute,          // 急性期：症状明显
        Chronic,        // 慢性期：长期存在（慢性病转为慢性）
        Recovery,       // 恢复期：症状减轻，逐渐恢复
        Permanent       // 永久期：不可逆（如失智、断肢）
    }

    /// <summary>
    /// 传播方式
    /// </summary>
    public enum TransmissionType
    {
        None,           // 不传播（慢性病、伤病、遗传病）
        Respiratory,    // 呼吸道传播（飞沫、空气）
        Waterborne,     // 水源传播（污染的水）
        Foodborne,      // 食物传播（污染的食物）
        Vector,         // 虫媒传播（蚊子、虱子等）
        Contact,        // 接触传播（直接接触、体液）
        Sexual,         // 性传播
        Perinatal       // 母婴传播（遗传病、先天感染）
    }
}
