using System;

namespace MyriadCreation.Content.Definitions
{
    /// <summary>
    /// 软前置：由关联物品的物理属性推导能力阈值，而不是要求某个特定前置革新。
    /// 例：炼铁需要总最大温度 ≥ 铁熔点的某比例；任何提供热量的已解锁项都可叠加满足。
    /// </summary>
    [Serializable]
    public class InnovationSoftPrerequisite
    {
        /// <summary>关联物品/物产 id。</summary>
        public int goodsId;

        /// <summary>物理属性 id，例如 meltingPoint。</summary>
        public string propertyId = string.Empty;

        /// <summary>该属性对应的能力通道，例如 heat.maxTemperature。</summary>
        public string capabilityChannel = string.Empty;

        /// <summary>阈值系数：要求 = 物品属性值 × factor（默认 1）。</summary>
        public float propertyFactor = 1f;

        public string rationale = string.Empty;
    }
}
