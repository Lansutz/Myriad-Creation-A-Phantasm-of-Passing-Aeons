namespace CivilizationEvolution.Simulation.Characters
{
    /// <summary>
    /// 衰退类型——感官或能力的渐进性衰退
    /// 借鉴CK3的Clouded Eyes/Withering Mind/Fragile Bones思路，但扩展到多种病因
    /// 不只是衰老，疾病、外伤、遗传都可以导致
    /// </summary>
    public enum ImpairmentType
    {
        Vision,         // 视力衰退——最终可致失明（白内障、青光眼、衰老、外伤、糖尿病、麻风等）
        Hearing,        // 听力衰退——最终可致全聋（衰老、疾病、外伤、长期噪音）
        Cognition,      // 认知衰退——最终可致失能（衰老、疾病、脑损伤、精神疾病）
        Mobility,       // 运动能力衰退——最终可致卧床（关节炎、衰老、外伤、中风）
        Speech          // 语言能力衰退——最终可致失语（中风、脑损伤、精神疾病）
    }

    /// <summary>
    /// 衰退等级——1到4级递进
    /// 等级越高影响越大，到4级通常获得永久特质
    /// </summary>
    public enum ImpairmentLevel
    {
        None = 0,       // 无衰退
        Mild = 1,       // 轻度——轻微影响，几乎察觉不到
        Moderate = 2,   // 中度——明显影响日常活动
        Severe = 3,     // 重度——严重影响，需要辅助
        Profound = 4    // 极重度/完全丧失——获得永久特质（失明、全聋、失能等）
    }
}
