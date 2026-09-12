using System;
using System.Collections.Generic;
using UnityEngine;

namespace CivilizationEvolution.Economy
{
    /// <summary>
    /// 加工品质量数据。
    /// 加工品（金属、器械、奢侈品等）不是同质的，有质量差异。
    /// 质量影响：研究经验获取、武器战斗力、贸易价值、生产效率。
    /// 质量是逐步提升的——通过大量实践积累、技术人员指导、设施改善。
    /// </summary>
    [Serializable]
    public class GoodQuality
    {
        /// <summary>对应物资ID</summary>
        public int goodsId;

        /// <summary>当前质量等级（0-10，5为标准质量）</summary>
        public float quality;

        /// <summary>质量提升进度（0-100，满了提升0.5级质量）</summary>
        public float qualityProgress;

        /// <summary>历史最高质量（用于质量回退判断）</summary>
        public float peakQuality;

        /// <summary>累计产量（用于品质稳定性计算和数量门槛）</summary>
        public float cumulativeOutput;

        public GoodQuality(int goodsId)
        {
            this.goodsId = goodsId;
            this.quality = 3f; // 初始质量较低，前现代早期加工品质量普遍不高
            this.qualityProgress = 0f;
            this.peakQuality = 3f;
        }

        /// <summary>质量系数（用于研究经验、贸易价值等计算）</summary>
        /// 标准质量(5)为1.0，每高1级+10%，每低1级-10%
        public float QualityMultiplier => 1f + (quality - 5f) * 0.1f;

        /// <summary>是否为高质量（≥7）</summary>
        public bool IsHighQuality => quality >= 7f;

        /// <summary>是否为低质量（≤3）</summary>
        public bool IsLowQuality => quality <= 3f;
    }

    /// <summary>
    /// 加工品质量管理器（每个政权一个）。
    /// 管理该政权所有加工品的质量等级和提升进度。
    /// 质量提升逻辑：
    /// 1. 生产实践：每生产一定量的加工品，积累质量提升进度
    /// 2. 技术人员：工匠/学者阶层加速质量提升
    /// 3. 设施：工坊/学校加速质量提升
    /// 4. 边际递减：质量越高，提升越慢
    /// </summary>
    public class QualityManager
    {
        /// <summary>各加工品的质量数据（key=goodsId）</summary>
        private readonly Dictionary<int, GoodQuality> _qualities = new Dictionary<int, GoodQuality>();

        /// <summary>获取加工品质量（不存在则创建，初始质量3）</summary>
        public GoodQuality GetQuality(int goodsId)
        {
            if (!_qualities.TryGetValue(goodsId, out var q))
            {
                q = new GoodQuality(goodsId);
                _qualities[goodsId] = q;
            }
            return q;
        }

        /// <summary>获取加工品的质量系数（不存在返回标准1.0）</summary>
        public float GetQualityMultiplier(int goodsId)
        {
            return _qualities.TryGetValue(goodsId, out var q) ? q.QualityMultiplier : 1f;
        }

        /// <summary>
        /// 每月更新加工品质量（生产实践驱动质量提升）。
        /// </summary>
        /// <param name="goodsId">加工品ID</param>
        /// <param name="monthlyOutput">本月产量</param>
        /// <param name="hasArtisans">是否有工匠阶层</param>
        /// <param name="hasFacility">是否有相关设施</param>
        public void MonthlyTickQuality(int goodsId, float monthlyOutput, bool hasArtisans, bool hasFacility)
        {
            if (monthlyOutput <= 0f) return;

            var q = GetQuality(goodsId);

            // 质量提升进度 = log(1 + 产量) × 基础系数 × 人员加成 × 设施加成 × 边际递减
            float progressGain = Mathf.Log(1f + monthlyOutput) * 0.5f;

            if (hasArtisans) progressGain *= 1.5f;
            if (hasFacility) progressGain *= 1.3f;

            // 边际递减：质量越高，提升越慢
            float diminishing = 1f / (1f + q.quality / 5f);
            progressGain *= diminishing;

            q.qualityProgress += progressGain;

            // 进度满100，提升0.5级质量
            while (q.qualityProgress >= 100f && q.quality < 10f)
            {
                q.qualityProgress -= 100f;
                q.quality = Mathf.Min(10f, q.quality + 0.5f);
                if (q.quality > q.peakQuality) q.peakQuality = q.quality;
            }

            if (q.quality >= 10f) q.qualityProgress = 0f;
        }

        /// <summary>获取所有加工品的平均质量（用于革新研究的质量参数）</summary>
        public float GetAverageQuality(List<int> goodsIds)
        {
            if (goodsIds == null || goodsIds.Count == 0) return 5f;
            float sum = 0f;
            foreach (int id in goodsIds)
            {
                sum += GetQuality(id).quality;
            }
            return sum / goodsIds.Count;
        }

        /// <summary>获取某加工品累计产量</summary>
        public float GetCumulativeOutput(int goodsId)
        {
            return _qualities.TryGetValue(goodsId, out var q) ? q.cumulativeOutput : 0f;
        }

        /// <summary>
        /// 计算当前品质离散度（σ）。
        /// 初期产量少、技术不成熟 → σ大（波动大，偶尔出精品也偶尔出废品）；
        /// 累计产量越多 → σ越小（品质趋于稳定）。
        /// σ范围：1.0 ~ 3.0
        /// </summary>
        public float GetQualitySpread(int goodsId)
        {
            float cum = GetCumulativeOutput(goodsId);
            float sigma = 3.0f - Mathf.Log(1f + cum) * 0.3f;
            return Mathf.Clamp(sigma, 1.0f, 3.0f);
        }

        /// <summary>
        /// 生产一次加工品，掷骰子决定本次实际品质（0-10整数）。
        /// 以当前平均品质为中心的截断正态分布：
        /// - 平均品质低时：低品质概率最高，高品质概率低但非零（偶尔出精品）
        /// - 平均品质高时：高品质概率最高，低品质概率低
        /// - 累计产量越多，分布越集中（品质越稳定）
        /// - 原材料短缺时：品质中心下移，波动增大（工匠用替代品/偷工减料/仓促生产）
        /// </summary>
        /// <param name="materialAvailability">原材料可用度 0-1（1=充足，<1=短缺，由经济系统根据库存/消耗比计算传入）</param>
        public int ProduceQuality(int goodsId, float materialAvailability = 1f)
        {
            var q = GetQuality(goodsId);
            float sigma = GetQualitySpread(goodsId);
            float mat = Mathf.Clamp01(materialAvailability);
            // 原材料短缺：品质中心下移（短缺越严重，中心越低）
            float effectiveMean = q.quality * mat;
            // 原材料短缺：波动增大（仓促生产，品质更不稳定），最多增大50%
            if (mat < 1f)
                sigma *= 1f + (1f - mat) * 0.5f;
            // Box-Muller 正态采样
            float u1 = 1f - UnityEngine.Random.value;
            float u2 = UnityEngine.Random.value;
            float z = Mathf.Sqrt(-2f * Mathf.Log(u1)) * Mathf.Cos(2f * Mathf.PI * u2);
            float sample = effectiveMean + z * sigma;
            return Mathf.Clamp(Mathf.RoundToInt(sample), 0, 10);
        }

        /// <summary>
        /// 记录一次生产：累计产量 + 用实际产出品质更新平均品质（加权移动平均）。
        /// </summary>
        /// <param name="goodsId">加工品ID</param>
        /// <param name="amount">本次产量</param>
        /// <param name="actualQuality">本次实际产出品质（ProduceQuality返回值）</param>
        public void RecordProduction(int goodsId, float amount, int actualQuality)
        {
            if (amount <= 0f) return;
            var q = GetQuality(goodsId);
            q.cumulativeOutput += amount;
            // 加权移动平均：新品质权重 = amount / (累计产量+amount)，避免单次大幅跳变
            float totalWeight = q.cumulativeOutput + amount;
            float newWeight = amount / totalWeight;
            q.quality = q.quality * (1f - newWeight) + actualQuality * newWeight;
            if (q.quality > q.peakQuality) q.peakQuality = q.quality;
        }
    }
}
