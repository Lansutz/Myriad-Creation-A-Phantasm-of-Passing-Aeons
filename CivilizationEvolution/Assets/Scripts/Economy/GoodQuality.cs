using System;
using System.Collections.Generic;

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
    }
}
