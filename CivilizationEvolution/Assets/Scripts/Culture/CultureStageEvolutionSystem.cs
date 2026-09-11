using System.Collections.Generic;
using CivilizationEvolution.Core;
using CivilizationEvolution.Politics;
using CivilizationEvolution.World;
using UnityEngine;

namespace CivilizationEvolution.Culture
{
    /// <summary>
    /// 国家形成路径（比较研究结论：同一文化阶段可通过不同路径形成国家）。
    /// </summary>
    public enum StateFormationPath
    {
        AgrarianSettlement, // 定居农业路径：农业剩余→人口→分层→官僚（法兰克/古埃及/中国/玛雅）
        NomadicConquest,    // 游牧征服路径：军事征服→吸收被征服者制度（蒙古/阿拉伯/哥特）
        TradeControl,       // 贸易控制路径：控制商路→财富→雇佣军（加纳/马里/腓尼基）
        ReligiousUnification,// 宗教统一路径：宗教认同超越部落→政教合一（早期伊斯兰/阿尔摩拉维德）
        MilitaryLeague      // 军事联盟路径：部落/城邦联盟固化为国家（阿兹特克三国同盟/罗马）
    }

    /// <summary>
    /// 阶段升级评估结果。
    /// </summary>
    public struct StageEvolutionResult
    {
        public bool canEvolve;
        public GameEnums.CultureStage targetStage;
        public List<string> unmetConditions; // 未满足条件（用于UI提示）
        public float progress;               // 综合进度 0-1
    }

    /// <summary>
    /// 文化阶段演化系统。
    ///
    /// 五阶段：游群Band→部落Tribe→酋邦Chiefdom→族群EthnicGroup→高度文明HighCivilization
    /// 升级标准（塞维斯/弗里德框架 + 新标准：专业化生产+完整社会分层）：
    /// - 社会分层完整度是核心标志
    /// - 文字/金属器是成就加速器，不是必要门槛（印加无文字、玛雅无青铜器仍是国家）
    /// - 移动模式独立于阶段（游牧社会也能从Band发展到HighCivilization=行国）
    ///
    /// MapActor是前国家阶段（Band/Tribe/Chiefdom）的人类群体载体，
    /// 满足条件后通过StateFormationPath转化为正式政权。
    /// </summary>
    public static class CultureStageEvolutionSystem
    {
        // ===== 人口阈值 =====
        public const int POP_BAND_TO_TRIBE = 100;
        public const int POP_TRIBE_TO_CHIEFDOM = 1000;
        public const int POP_CHIEFDOM_TO_ETHNIC = 5000;
        public const int POP_ETHNIC_TO_CIVILIZATION = 10000;

        /// <summary>
        /// 评估某文化群体能否升级到下一阶段。
        /// </summary>
        /// <param name="currentStage">当前阶段</param>
        /// <param name="population">总人口</param>
        /// <param name="ownedInnovations">已拥有革新ID集合</param>
        /// <param name="socialStratification">社会分层完整度 0-1（由SocialClassAvailability计算）</param>
        /// <param name="specialization">专业化生产程度 0-1（非食物生产者比例）</param>
        /// <param name="hasUrbanCenter">是否有城市级定居点（城国）或固定营地体系（行国）</param>
        public static StageEvolutionResult EvaluateEvolution(
            GameEnums.CultureStage currentStage,
            int population,
            HashSet<int> ownedInnovations,
            float socialStratification,
            float specialization,
            bool hasUrbanCenter)
        {
            var result = new StageEvolutionResult
            {
                targetStage = currentStage + 1,
                unmetConditions = new List<string>()
            };
            ownedInnovations = ownedInnovations ?? new HashSet<int>();
            int metCount = 0;
            int totalCount = 0;

            switch (currentStage)
            {
                case GameEnums.CultureStage.Band:
                    // Band→Tribe：人口 + 基础革新（用火900/语言）
                    totalCount = 2;
                    if (population >= POP_BAND_TO_TRIBE) metCount++;
                    else result.unmetConditions.Add($"人口 {population}/{POP_BAND_TO_TRIBE}");
                    if (ownedInnovations.Contains(900)) metCount++; // 用火
                    else result.unmetConditions.Add("需要掌握用火");
                    break;

                case GameEnums.CultureStage.Tribe:
                    // Tribe→Chiefdom：人口 + 农业/畜牧 + 陶器 + 初步分层
                    totalCount = 4;
                    if (population >= POP_TRIBE_TO_CHIEFDOM) metCount++;
                    else result.unmetConditions.Add($"人口 {population}/{POP_TRIBE_TO_CHIEFDOM}");
                    bool hasFoodProd = ownedInnovations.Contains(100) || // 农业
                                       HasAnyInnovation(ownedInnovations, 916, 917, 918) ||
                                       HasAnimalDomestication(ownedInnovations);
                    if (hasFoodProd) metCount++;
                    else result.unmetConditions.Add("需要农业或畜牧技术");
                    if (HasAnyInnovation(ownedInnovations, 200, 201)) metCount++; // 陶器
                    else result.unmetConditions.Add("需要陶器制作");
                    if (socialStratification >= 0.2f) metCount++;
                    else result.unmetConditions.Add("社会需要初步分层（贵族/平民萌芽）");
                    break;

                case GameEnums.CultureStage.Chiefdom:
                    // Chiefdom→EthnicGroup：金属冶炼 + 文字(或替代) + 分层 + 专业化
                    totalCount = 4;
                    if (population >= POP_CHIEFDOM_TO_ETHNIC) metCount++;
                    else result.unmetConditions.Add($"人口 {population}/{POP_CHIEFDOM_TO_ETHNIC}");
                    bool hasMetal = HasAnyInnovation(ownedInnovations, 1101, 202, 1105, 201); // 炼铜/冶铁/青铜
                    if (hasMetal) metCount++;
                    else result.unmetConditions.Add("需要金属冶炼技术");
                    if (socialStratification >= 0.5f) metCount++;
                    else result.unmetConditions.Add("需要完整社会分层（贵族/平民/奴隶）");
                    if (specialization >= 0.3f) metCount++;
                    else result.unmetConditions.Add("需要专业化生产（工匠/商人/军人脱离食物生产）");
                    // 文字是加速器不是门槛：有文字则进度加成
                    break;

                case GameEnums.CultureStage.EthnicGroup:
                    // EthnicGroup→HighCivilization：官僚/议事 + 城市化 + 领土控制 + 强制权力
                    totalCount = 4;
                    if (population >= POP_ETHNIC_TO_CIVILIZATION) metCount++;
                    else result.unmetConditions.Add($"人口 {population}/{POP_ETHNIC_TO_CIVILIZATION}");
                    bool hasBureaucracy = HasAnyInnovation(ownedInnovations, 503, 504, 505); // 官僚制
                    if (hasBureaucracy) metCount++;
                    else result.unmetConditions.Add("需要官僚制度或贵族议事制度");
                    if (hasUrbanCenter) metCount++;
                    else result.unmetConditions.Add("需要城市级定居点或固定营地体系");
                    if (socialStratification >= 0.7f && specialization >= 0.5f) metCount++;
                    else result.unmetConditions.Add("需要成熟的社会分层与专业化分工");
                    break;

                default:
                    result.canEvolve = false;
                    result.progress = 1f;
                    return result;
            }

            result.progress = totalCount > 0 ? (float)metCount / totalCount : 0f;
            result.canEvolve = metCount >= totalCount;
            return result;
        }

        /// <summary>
        /// MapActor（前国家群体）转化为正式政权。
        /// 由AI/事件在满足阶段条件 + 形成路径触发时调用。
        /// </summary>
        /// <param name="world">游戏世界</param>
        /// <param name="actor">前国家群体（游牧部落/蛮族酋邦等）</param>
        /// <param name="path">国家形成路径</param>
        /// <param name="cultureId">文化ID</param>
        /// <returns>新政权ID，-1表示转化失败</returns>
        public static int FormRealmFromActor(GameWorld world, MapActor actor,
            StateFormationPath path, int cultureId)
        {
            if (world == null || actor == null || !actor.IsAlive) return -1;

            // 创建政权数据（直接new并注册，ID取现有最大值+1）
            int newRealmId = 0;
            foreach (int id in world.realms.Keys)
                if (id >= newRealmId) newRealmId = id + 1;
            var realm = new RealmData { realmId = newRealmId };
            world.realms[newRealmId] = realm;

            realm.realmName = actor.actorName;
            realm.primaryCultureId = cultureId;

            // 根据文化移动模式确定政权形态
            if (ContentRegistry.TryGetCulture(cultureId, out var culturePack))
            {
                int mobility = culturePack.data.mobilityType;
                realm.realmForm = NomadicRealmSystem.FormFromMobility(mobility);
            }
            else
            {
                realm.realmForm = actor.type == MapActorType.Nomad
                    ? RealmForm.Nomadic : RealmForm.Sedentary;
            }

            // 行国：建立活动范围
            if (realm.realmForm == RealmForm.Nomadic)
            {
                realm.nomadicRange = new NomadicRange
                {
                    centerTile = actor.currentTile,
                    summerCampTile = actor.currentTile,
                    winterCampTile = actor.currentTile
                };
            }
            else
            {
                // 城国：占据当前地块
                var tile = world.tiles[actor.currentTile];
                tile.ownerRealmId = newRealmId;
                realm.coreTiles.Add(actor.currentTile);
                world.tiles[actor.currentTile] = tile;
            }

            // 路径特性加成
            ApplyPathBonuses(realm, path);

            // MapActor解散（人口转化为政权人口）
            World.MapActorManager.AddPopulationToTile(
                ref world.tiles[actor.currentTile], actor.population);
            actor.population = 0;

            Debug.Log($"[StageEvolution] {realm.realmName} 通过{path}路径建国（{realm.realmForm}），地块={actor.currentTile}");
            return newRealmId;
        }

        /// <summary>国家形成路径的初始特性加成</summary>
        private static void ApplyPathBonuses(RealmData realm, StateFormationPath path)
        {
            switch (path)
            {
                case StateFormationPath.AgrarianSettlement:
                    realm.stability = 55f;  // 农业国稳定度高
                    break;
                case StateFormationPath.NomadicConquest:
                    realm.centralization = 0.7f; // 军事征服集权度高
                    break;
                case StateFormationPath.TradeControl:
                    realm.treasury = 2000f; // 贸易国初始财富高
                    break;
                case StateFormationPath.ReligiousUnification:
                    realm.stability = 60f; // 宗教国凝聚力高
                    break;
                case StateFormationPath.MilitaryLeague:
                    realm.centralization = 0.3f; // 联盟初期分权
                    break;
            }
        }

        /// <summary>文化倒退（黑暗时代）：核心城市被夷平可能导致阶段倒退</summary>
        public static GameEnums.CultureStage CheckRegress(
            GameEnums.CultureStage currentStage,
            int population,
            float socialStratification,
            bool lostUrbanCenter)
        {
            if (!lostUrbanCenter) return currentStage;

            // 失去城市中心 + 人口锐减 + 分层崩溃 → 倒退一级
            bool populationCollapse = currentStage switch
            {
                GameEnums.CultureStage.HighCivilization => population < POP_ETHNIC_TO_CIVILIZATION * 0.5f,
                GameEnums.CultureStage.EthnicGroup => population < POP_CHIEFDOM_TO_ETHNIC * 0.5f,
                GameEnums.CultureStage.Chiefdom => population < POP_TRIBE_TO_CHIEFDOM * 0.5f,
                _ => false
            };
            bool stratificationCollapse = socialStratification < 0.2f;

            if (populationCollapse && stratificationCollapse && currentStage > GameEnums.CultureStage.Band)
            {
                return currentStage - 1;
            }
            return currentStage;
        }

        private static bool HasAnyInnovation(HashSet<int> owned, params int[] ids)
        {
            foreach (int id in ids)
                if (owned.Contains(id)) return true;
            return false;
        }

        private static bool HasAnimalDomestication(HashSet<int> owned)
        {
            // 驯化马/牛/羊等畜牧革新（简化：检查常见畜牧革新ID段）
            // 实际ID由Innovations.json定义，这里做范围检查
            foreach (int id in owned)
                if (id >= 910 && id <= 930) return true;
            return false;
        }
    }
}
