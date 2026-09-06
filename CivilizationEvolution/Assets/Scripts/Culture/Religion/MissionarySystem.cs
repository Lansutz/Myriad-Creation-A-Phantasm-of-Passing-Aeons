using System.Collections.Generic;
using CivilizationEvolution.Core;
using CivilizationEvolution.Politics;

namespace CivilizationEvolution.Culture
{
    /// <summary>
    /// 传教机制（政权传教渠道——传播机制）：
    /// 传教成功率 = 与当地主流信仰的冲突度（同宗教不同传统=易传；
    /// 异教=难——需特殊手段[宗教税吉兹亚/政权支持]）
    /// 成功 → 目标地块该信仰人口块增长（转信）——占比变化→主流可能易位
    /// 征服不直接改宗——需政权派传教士传教（用户定稿）
    /// </summary>
    public static class MissionarySystem
    {
        /// <summary>
        /// 传教成功率（0-1）：
        /// 同信仰=1.0（无意义）｜同宗教不同教统=0.5-0.8（冲突小）
        /// 异教=0.1-0.3（冲突大）——宗教税压力下加成（经济诱导改宗）
        /// </summary>
        public static float CalculateSuccessChance(TileData tile, int missionaryFaithId,
            System.Func<int, ReligionDef> getReligion, System.Func<int, int> getRootFaith)
        {
            if (tile.populationBlocks == null) return 0f;
            int localFaith = PopulationStats.GetDominantFaith(tile);
            if (localFaith < 0) return 0.6f; // 无主流信仰（蛮荒）——易传
            if (localFaith == missionaryFaithId) return 1f;

            // 冲突度判定：同宗教根（基督教内不同教统）=小冲突；异教=大冲突
            var localRoot = getRootFaith != null ? getRootFaith(localFaith) : -1;
            var missionRoot = getRootFaith != null ? getRootFaith(missionaryFaithId) : -1;
            if (localRoot >= 0 && localRoot == missionRoot)
                return 0.6f; // 同宗教不同教统（天主教传东正教区域——较易）
            return 0.2f; // 异教（十字军传穆斯林区域——难——需长期传教）
        }

        /// <summary>
        /// 执行传教（成功→目标地块信仰块 count 增长——简化：增长主流块 5% 或
        /// 新建传教信仰块——占比变化驱动主流易位）
        /// </summary>
        public static bool ConvertTile(TileData tile, int missionaryFaithId, int cultureId,
            float successChance, System.Random rng)
        {
            if (tile.populationBlocks == null) return false;
            if (rng.NextDouble() > successChance) return false;

            // 找该信仰已有块（增长）或新建块（新信仰传入——人口迁移/传教）
            for (int i = 0; i < tile.populationBlocks.Count; i++)
            {
                if (tile.populationBlocks[i].faithId == missionaryFaithId)
                {
                    var grow = tile.populationBlocks[i];
                    grow.count *= 1.05f; // 已有信徒增长 5%
                    tile.populationBlocks[i] = grow;
                    return true;
                }
            }
            // 新建传教块（初期信徒——从主流块分出 3%）
            var localFaith = PopulationStats.GetDominantFaith(tile);
            for (int i = 0; i < tile.populationBlocks.Count; i++)
            {
                var pb = tile.populationBlocks[i];
                if (pb.faithId == localFaith && pb.count > 5f)
                {
                    float transfer = pb.count * 0.03f;
                    pb.count -= transfer;
                    tile.populationBlocks[i] = pb;
                    tile.populationBlocks.Add(new PopulationBlock
                    {
                        count = transfer,
                        cultureId = cultureId >= 0 ? cultureId : pb.cultureId,
                        faithId = missionaryFaithId,
                        socialClass = pb.socialClass
                    });
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 宗教税压力改宗（吉兹亚式——异教徒保留信仰但交高税——
        /// 经济压力诱导改宗：低阶层/贫困块先改——用户定稿）
        /// </summary>
        public static bool TaxPressureConversion(TileData tile, int stateFaithId, float taxRate)
        {
            if (tile.populationBlocks == null) return false;
            bool any = false;
            for (int i = 0; i < tile.populationBlocks.Count; i++)
            {
                var pb = tile.populationBlocks[i];
                if (pb.faithId == stateFaithId) continue;
                // 高税率下按概率改宗（农民/奴隶先改——阶层维度参与）
                float convertChance = taxRate * 0.01f;
                if (pb.socialClass == GameEnums.SocialClass.Peasant ||
                    pb.socialClass == GameEnums.SocialClass.Slave)
                    convertChance *= 1.5f;
                if (UnityEngine.Random.value < convertChance)
                {
                    pb.faithId = stateFaithId;
                    tile.populationBlocks[i] = pb;
                    any = true;
                }
            }
            return any;
        }
    }
}
