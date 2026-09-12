using System;

namespace CivilizationEvolution.Core
{
    /// <summary>
    /// 地块上的单个自然资源点。
    /// 物资（GoodsDef）同时是自然资源和经济商品：
    /// 地图生成时在地块上生成资源点，玩家/AI发现并开发后，资源产出进入经济系统。
    /// </summary>
    [Serializable]
    public struct TileResource
    {
        /// <summary>对应物资ID（GoodsDef.goodsId）</summary>
        public int goodsId;

        /// <summary>丰度（0-1，由地理条件质量决定，影响产出上限和革新实践速度）</summary>
        public float abundance;

        /// <summary>是否已被发现（探索/控制时自动发现）</summary>
        public bool discovered;

        /// <summary>是否已被开发（需要对应技术/建筑，开发后产出进入经济系统）</summary>
        public bool developed;

        /// <summary>开发程度（0-1，影响产出效率）</summary>
        public float developmentLevel;

        /// <summary>消耗程度（0-1，仅不可再生资源会逐渐耗尽；可再生资源保持0）</summary>
        public float depletion;

        /// <summary>创建一个新发现的未开发资源点</summary>
        public static TileResource Create(int goodsId, float abundance)
        {
            return new TileResource
            {
                goodsId = goodsId,
                abundance = abundance,
                discovered = false,
                developed = false,
                developmentLevel = 0f,
                depletion = 0f
            };
        }

        /// <summary>是否已耗尽（不可再生资源depletion>=1）</summary>
        public bool IsDepleted => depletion >= 1f;

        /// <summary>有效产出系数（丰度×开发程度×(1-消耗)）</summary>
        public float EffectiveOutput => abundance * Math.Max(developmentLevel, 0.1f) * (1f - depletion);
    }
}
