using System.Collections.Generic;




using UnityEngine;
using CivilizationEvolution.Core;
using CivilizationEvolution.Core.Constants;
using CivilizationEvolution.Core.Data;
using CivilizationEvolution.Core.Dto;
using CivilizationEvolution.Core.Enums;
using CivilizationEvolution.Simulation.Culture;
using CivilizationEvolution.Simulation.Events;
using CivilizationEvolution.Simulation.Generation;
using CivilizationEvolution.Simulation.Innovation;
using CivilizationEvolution.Simulation.Modding;
using CivilizationEvolution.Simulation.Politics;
using CivilizationEvolution.Simulation.Population;
using CivilizationEvolution.Simulation.Religion;
using CivilizationEvolution.Simulation.Society;
using CivilizationEvolution.Simulation.WorldState;


namespace CivilizationEvolution.Simulation.Economy
{
    /// <summary>
    /// 社会分层完整度与专业化生产程度的量化计算器。
    ///
    /// 设计依据（塞维斯/弗里德社会进化框架 + 新标准"专业化生产+完整社会分层"）：
    /// - 社会分层完整度 socialStratification(0-1)：社会实际分化出多少个独立阶层。
    ///   核心是"脱离生产的统治者"与"被统治的生产者/奴隶"两极的形成程度。
    /// - 专业化程度 specialization(0-1)：非食物生产者占经济活动人口的比例。
    ///   工匠/商人/士人/官吏/军人/祭司不再自己生产食物，靠赋税贸易养活。
    ///
    /// 两个维度都同时考察：
    /// ① 制度潜力（革新是否已解锁该阶层——SocialClassAvailability）
    /// ② 人口现实（该阶层是否真的有人口——遍历地块 PopulationBlock）
    /// 只有"制度允许且实际存在"才算该阶层真正成型。
    /// </summary>
    public static class SocialStratificationCalculator
    {
        /// <summary>计算结果</summary>
        public struct Result
        {
            /// <summary>社会分层完整度 0-1</summary>
            public float socialStratification;
            /// <summary>专业化生产程度 0-1</summary>
            public float specialization;
            /// <summary>各阶层实际人口</summary>
            public Dictionary<GameEnums.SocialClass, float> classPopulation;
            /// <summary>总人口</summary>
            public float totalPopulation;
            /// <summary>非食物生产者人口</summary>
            public float nonFoodProducerPopulation;
        }

        // 分层权重：统治层的出现是分层的核心标志，生产者是基础，奴隶是对立另一极
        private const float W_RULER = 0.35f;       // 王室+贵族教士（脱离生产的统治层）
        private const float W_FREEMAN = 0.25f;     // 自由民（士农工商，专业化载体）
        private const float W_PEASANT = 0.15f;     // 农民（生产者，几乎总有）
        private const float W_SLAVE = 0.25f;       // 奴隶（被统治另一极）

        // 专业化归一化：非食物生产者占比达到此值视为完全专业化（成熟前现代城市文明约15-20%）
        private const float FULL_SPECIALIZATION_RATIO = 0.18f;
        // 判定某阶层"实际成型"的最低人口（避免个别人口误判为阶层存在）
        private const float CLASS_PRESENT_MIN_POP = 5f;

        /// <summary>
        /// 计算某政权的社会分层与专业化程度。
        /// </summary>
        public static Result Calculate(GameWorld world, int realmId,
            CultureData culture, InnovationTree innovations)
        {
            var classPop = TallyClassPopulation(world, realmId);
            float total = 0f;
            foreach (var v in classPop.Values) total += v;

            var result = new Result
            {
                classPopulation = classPop,
                totalPopulation = total
            };

            if (total <= 0f)
            {
                result.socialStratification = 0f;
                result.specialization = 0f;
                return result;
            }

            // ===== 社会分层完整度 =====
            float stratification = 0f;

            // 统治层：王室 或 贵族教士（制度允许 + 实际有人口）
            bool rulerInstitution =
                SocialClassAvailability.IsClassAvailable(GameEnums.SocialClass.Royalty, culture, innovations, realmId) ||
                SocialClassAvailability.IsClassAvailable(GameEnums.SocialClass.NobilityClergy, culture, innovations, realmId);
            float rulerPop = classPop.GetValueOrDefault(GameEnums.SocialClass.Royalty)
                           + classPop.GetValueOrDefault(GameEnums.SocialClass.NobilityClergy);
            if (rulerInstitution && rulerPop >= CLASS_PRESENT_MIN_POP)
                stratification += W_RULER;

            // 自由民层
            bool freemanInstitution =
                SocialClassAvailability.IsClassAvailable(GameEnums.SocialClass.MerchantFreeman, culture, innovations, realmId);
            float freemanPop = classPop.GetValueOrDefault(GameEnums.SocialClass.MerchantFreeman);
            if (freemanInstitution && freemanPop >= CLASS_PRESENT_MIN_POP)
                stratification += W_FREEMAN;

            // 生产层（农民）
            bool peasantInstitution =
                SocialClassAvailability.IsClassAvailable(GameEnums.SocialClass.Peasant, culture, innovations, realmId);
            float peasantPop = classPop.GetValueOrDefault(GameEnums.SocialClass.Peasant);
            // 农民是基础：只要有食物生产人口即给分（狩猎采集群体也视为生产者）
            if (peasantPop >= CLASS_PRESENT_MIN_POP || (!peasantInstitution && total >= CLASS_PRESENT_MIN_POP))
                stratification += W_PEASANT;

            // 奴隶层
            bool slaveInstitution =
                SocialClassAvailability.IsClassAvailable(GameEnums.SocialClass.Slave, culture, innovations, realmId);
            float slavePop = classPop.GetValueOrDefault(GameEnums.SocialClass.Slave);
            if (slaveInstitution && slavePop >= CLASS_PRESENT_MIN_POP)
                stratification += W_SLAVE;

            result.socialStratification = Mathf.Clamp01(stratification);

            // ===== 专业化程度（非食物生产者占比）=====
            // 食物生产者 = 农民（Peasant）；其余均为非食物生产者
            float foodProducer = peasantPop;
            float nonFood = total - foodProducer;
            result.nonFoodProducerPopulation = Mathf.Max(0f, nonFood);

            float ratio = total > 0f ? result.nonFoodProducerPopulation / total : 0f;
            result.specialization = Mathf.Clamp01(ratio / FULL_SPECIALIZATION_RATIO);

            return result;
        }

        /// <summary>
        /// 统计政权实际控制地块上各阶层的人口总和。
        /// 行国（游牧）按活动范围统计；城国按 coreTiles 统计。
        /// </summary>
        public static Dictionary<GameEnums.SocialClass, float> TallyClassPopulation(GameWorld world, int realmId)
        {
            var result = new Dictionary<GameEnums.SocialClass, float>();
            if (world == null) return result;

            bool isNomadic = world.realms.TryGetValue(realmId, out var realm)
                && realm.realmForm == RealmForm.Nomadic && realm.nomadicRange != null;

            for (int i = 0; i < world.tiles.Length; i++)
            {
                ref TileData tile = ref world.tiles[i];
                if (!tile.exists || tile.populationBlocks == null) continue;

                bool inScope;
                if (isNomadic)
                    inScope = realm.nomadicRange.IsInRange(i, world.mapWidth, world.mapHeight);
                else
                    inScope = tile.ownerRealmId == realmId;
                if (!inScope) continue;

                foreach (var pb in tile.populationBlocks)
                {
                    if (pb.count <= 0f) continue;
                    result.TryGetValue(pb.socialClass, out float cur);
                    result[pb.socialClass] = cur + pb.count;
                }
            }
            return result;
        }

        /// <summary>
        /// 根据分层完整度推断当前社会应处的阶段（供文化演化/AI 参考，不直接强制改阶段）。
        /// </summary>
        public static GameEnums.CultureStage StratificationToStage(float stratification, float specialization)
        {
            if (stratification >= 0.7f && specialization >= 0.5f)
                return GameEnums.CultureStage.HighCivilization;
            if (stratification >= 0.5f && specialization >= 0.3f)
                return GameEnums.CultureStage.EthnicGroup;
            if (stratification >= 0.35f)
                return GameEnums.CultureStage.Chiefdom;
            if (stratification >= 0.15f)
                return GameEnums.CultureStage.Tribe;
            return GameEnums.CultureStage.Band;
        }
    }
}
