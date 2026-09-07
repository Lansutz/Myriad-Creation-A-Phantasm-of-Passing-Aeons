using CivilizationEvolution.Tech;
using System.Collections.Generic;

namespace CivilizationEvolution.AI
{
    [System.Serializable]
    public struct AIPersonality
    {
        public string personalityName;

        [UnityEngine.Range(0f, 1f)] public float expansionBias;      // 扩张偏好
        [UnityEngine.Range(0f, 1f)] public float economicBias;        // 经济偏好
        [UnityEngine.Range(0f, 1f)] public float diplomaticBias;      // 外交偏好
        [UnityEngine.Range(0f, 1f)] public float militaryBias;        // 军事偏好
        [UnityEngine.Range(0f, 1f)] public float aggression;           // 侵略性
        [UnityEngine.Range(0f, 1f)] public float riskTolerance;       // 风险承受
        [UnityEngine.Range(0f, 2f)] public float researchMultiplier;  // 研究倍率

        public List<InnovationDomain> preferredDomains;
        /// <summary>固定原型（true=原型预设固定——SyncPersonality 跳过性格覆盖——
        /// 剧本/模组想完全固定某原型时用；默认 false=性格驱动每日漂移——
        /// struct 无初始化器——默认值 false 由构造/预设显式设置）</summary>
        public bool fixedArchetype;

        /// <summary>原型命名（按偏好最高维分类——涌现——非标签驱动）</summary>
        private static string ClassifyName(AIPersonality p)
        {
            float[] dims = { p.expansionBias, p.economicBias, p.diplomaticBias, p.militaryBias, p.aggression };
            int maxIdx = 0;
            for (int i = 1; i < dims.Length; i++)
                if (dims[i] > dims[maxIdx]) maxIdx = i;
            string[] names = { "征服王", "建设王", "外交家", "军事统帅", "侵略者" };
            return names[maxIdx];
        }

        /// <summary>
        /// 命名原型预设（模组/剧本快捷配置——底层仍是性格参数——
        /// 不是行为标签：预设只是起始参数，行为仍随环境/事件变化）
        /// </summary>
        public static AIPersonality Preset(string archetypeId, bool fixedArchetype = false)
        {
            switch (archetypeId)
            {
                case "conqueror": // 征服王：高扩张+高侵略+高军事
                    return new AIPersonality
                    {
                        personalityName = "征服王",
                        expansionBias = 0.9f, economicBias = 0.35f, diplomaticBias = 0.3f,
                        militaryBias = 0.85f, aggression = 0.8f, riskTolerance = 0.7f,
                        researchMultiplier = 1.0f, fixedArchetype = fixedArchetype
                    };
                case "adventurer": // 冒险王：高风险+军事+扩张（探险式）
                    return new AIPersonality
                    {
                        personalityName = "冒险王",
                        expansionBias = 0.7f, economicBias = 0.4f, diplomaticBias = 0.4f,
                        militaryBias = 0.8f, aggression = 0.6f, riskTolerance = 0.95f,
                        researchMultiplier = 0.9f, fixedArchetype = fixedArchetype
                    };
                case "builder": // 建设者：经济+研究
                    return new AIPersonality
                    {
                        personalityName = "建设者",
                        expansionBias = 0.3f, economicBias = 0.95f, diplomaticBias = 0.6f,
                        militaryBias = 0.35f, aggression = 0.2f, riskTolerance = 0.35f,
                        researchMultiplier = 1.4f, fixedArchetype = fixedArchetype
                    };
                case "diplomat": // 外交家
                    return new AIPersonality
                    {
                        personalityName = "外交家",
                        expansionBias = 0.4f, economicBias = 0.5f, diplomaticBias = 0.95f,
                        militaryBias = 0.3f, aggression = 0.15f, riskTolerance = 0.4f,
                        researchMultiplier = 1.0f, fixedArchetype = fixedArchetype
                    };
                default:
                    return RandomPersonality();
            }
        }

        /// <summary>生成随机人格</summary>
        public static AIPersonality RandomPersonality()
        {
            var p = new AIPersonality
            {
                personalityName = "随机",
                expansionBias = UnityEngine.Random.Range(0.2f, 0.8f),
                economicBias = UnityEngine.Random.Range(0.2f, 0.8f),
                diplomaticBias = UnityEngine.Random.Range(0.2f, 0.8f),
                militaryBias = UnityEngine.Random.Range(0.2f, 0.8f),
                aggression = UnityEngine.Random.Range(0.2f, 0.8f),
                riskTolerance = UnityEngine.Random.Range(0.2f, 0.8f),
                researchMultiplier = UnityEngine.Random.Range(0.8f, 1.5f),
                preferredDomains = new List<InnovationDomain>()
            };

            // 随机偏好2个革新大类（技术/思维/制度/传统）
            var allDomains = System.Enum.GetValues(typeof(InnovationDomain));
            int first = UnityEngine.Random.Range(0, allDomains.Length);
            int second = UnityEngine.Random.Range(0, allDomains.Length);
            p.preferredDomains.Add((InnovationDomain)allDomains.GetValue(first));
            if (second != first)
                p.preferredDomains.Add((InnovationDomain)allDomains.GetValue(second));

            return p;
        }
    }
}
