using System;
using System.Collections.Generic;
using MyriadCreation.Core.Enums;
using MyriadCreation.Simulation.Definitions;
using MyriadCreation.Simulation.Genetics;
using MyriadCreation.Simulation.State;
using MyriadCreation.Simulation.Systems;
using UnityEngine;
using MyriadCreation.Content.Definitions;

namespace MyriadCreation.Infrastructure.Save
{
    /// <summary>
    /// 角色存档 DTO（SaveVersion 11 起）。
    /// 为什么需要 DTO：CharacterData 未标记 [Serializable]，且含 Dictionary（bodyParts/relations/skills）、
    /// 非序列化缓存（dnaExpression/appearance）与派生属性，JsonUtility 无法直接落盘。
    /// 本 DTO 保留"读档后世界可继续演化"所必需的可序列化字段：
    /// 身份/血缘/位置/阶层/六维/上限型数值/人格七维/遗传基因组；
    /// SaveVersion 14/15 起追加运行态：性格特质（只存 traitId，定义由特质量表提供）、人际关系（含关系史）、
    /// 技能等级、进行中疾病、伤残（v14）；身体部位（部件→子结构→诊断条件三层拍平，v15）——
    /// 字典/列表全部拍平为并行数组，见文件下部 CaptureRuntimeState / CaptureBodyParts / RestoreBodyParts。
    /// 已知不入档：dnaExpression/appearance（派生缓存，可由 genome 重算）；
    /// CharacterBond（由 CharacterManager 单独持有，不在单个角色字段内：用文件末的 CharacterBondSaveData
    /// 由 CharacterBondPersistence 统一捕获/恢复）；personalTenets/bodyMarks/成就计数等其它运行态字段
    /// 暂未覆盖，读档后按默认值或后续系统重建。
    /// </summary>
    [Serializable]
    public class CharacterSaveData
    {
        // ===== 身份与生命周期 =====
        public int characterId;
        public string firstName = "";
        public string lastName = "";
        public int age;
        public bool isMale;
        public int birthDay;
        public int birthYear;
        public int deathDay = -1;
        public int deathYear = -1;

        // ===== 位置（权威位置是聚落 anchorId，currentTile 仅旧逻辑/显示） =====
        public int currentSettlementAnchorId = -1;
        public int currentTile = -1;
        /// <summary>所在建筑块 id（D36）；旧档为空串 = 不在具体建筑内。</summary>
        public string currentBlockId = "";
        public bool isPublicFigure;
        public CharacterAICategory aiCategory = CharacterAICategory.Worker;

        // ===== 归属 =====
        public int realmId = -1;
        public int familyId = -1;
        public int cultureId;
        public int raceId;
        public int faithId;
        public int privateFaithId = -1;
        public CharacterRole role = CharacterRole.Commoner;
        public GameEnums.SocialClass socialClass = GameEnums.SocialClass.Peasant;
        public GameEnums.SocialSubclass socialSubclass = GameEnums.SocialSubclass.Freeholder;

        // ===== 血缘 =====
        public int fatherId = -1;
        public int motherId = -1;
        public int spouseId = -1;

        // ===== 六维专精与上限型数值 =====
        public float prowess = 50f;
        public float social = 50f;
        public float military = 50f;
        public float management = 50f;
        public float conspiracy = 50f;
        public float scholarship = 50f;
        public float charm = 50f;

        public float health = 100f;
        public float fertility = 50f;
        public float stress;
        public float dread;
        public float obesity = 20f;

        public float prestige;
        public int prestigeCapacityLevel = 1;
        public float notoriety;
        public int accessionDay = -1;

        public AchievementRecord achievements;

        public List<string> personalTenets = new List<string>();

        public List<string> bodyMarks = new List<string>();

        public float spiritualFulfillment = 50f;
        public float expectedLifespanYears = 75f;
        public float individualResistance = 50f;

        // ===== 称号/状态字符串 =====
        public string epithet = "";
        public string posthumousTitle = "";
        public string legendaryTitle = "";
        public string mentalDisorderId = "";
        public bool isSecretBeliever;
        public bool epithetEvaluated;
        public int highStressDays;
        public int lowStressRecoveryDays;

        // ===== 财产与指挥 =====
        public float gold;
        public int commandedArmyId = -1;
        public List<int> ownedTitles = new List<int>();

        // ===== 运行态（SaveVersion 14 起）=====
        // 为什么拍平成并行数组：JsonUtility 不支持 Dictionary，结构体/类列表的反序列化在不同
        // Unity 版本表现也不稳（同 TransportUnitSaveData 的成员/货物段约定），原始类型数组最稳妥。
        // 任一段在旧档中缺失 → 反序列化后为 null/空，读档端按空集合处理，不抛异常。

        /// <summary>性格特质：只存 traitId，定义由 PersonalityTraitDatabase 按 id 提供。</summary>
        public string[] traitIds = new string[0];

        /// <summary>人际关系（Dictionary&lt;int, CharacterRelation&gt; 键 = 对方角色 id）。</summary>
        public int[] relationCharacterIds = new int[0];
        public float[] relationOpinions = new float[0];
        public int[] relationTypes = new int[0];
        public float[] relationTrusts = new float[0];
        public float[] relationFears = new float[0];
        public float[] relationRomanticAttractions = new float[0];

        /// <summary>关系史的列表长度；具体条目按关系顺序拼接在 relationHistoryEntries。</summary>
        public int[] relationHistoryCounts = new int[0];
        public string[] relationHistoryEntries = new string[0];

        /// <summary>技能等级（Dictionary&lt;string, float&gt;）。</summary>
        public string[] skillIds = new string[0];
        public float[] skillValues = new float[0];

        /// <summary>进行中疾病（List&lt;ActiveCharacterDisease&gt;）。</summary>
        public string[] diseaseIds = new string[0];
        public int[] diseaseStages = new int[0];
        public int[] diseaseElapsedDays = new int[0];
        public int[] diseaseStageDays = new int[0];
        public float[] diseaseSeverities = new float[0];
        public bool[] diseaseTreated = new bool[0];
        public int[] diseaseTreatmentDays = new int[0];
        public float[] diseaseInfectionSources = new float[0];

        /// <summary>伤残（List&lt;ActiveImpairment&gt;，struct 列表 → 并行数组）。</summary>
        public string[] impairmentIds = new string[0];
        public int[] impairmentStageIndices = new int[0];
        public float[] impairmentProgressionRates = new float[0];
        public int[] impairmentStageDays = new int[0];
        public string[] impairmentCauses = new string[0];
        public bool[] impairmentReversible = new bool[0];

        // ----- 身体部位（SaveVersion 15 起；Dictionary<BodyPartType, BodyPartData> 三层拍平）-----
        // 第 1 层：部件（一行一个 BodyPartData；partTypes 存 BodyPartType 数字）。
        public int[] bodyPartTypes = new int[0];
        public float[] bodyPartMaxHps = new float[0];
        public float[] bodyPartCurrentHps = new float[0];
        public float[] bodyPartFunctionLevels = new float[0];
        public int[] bodyPartStates = new int[0];
        public int[] bodyPartSubStructureCounts = new int[0];
        public int[] bodyPartDiagnosedConditionCounts = new int[0];

        // 第 2 层：子结构（subStructurePartIndices 指向第 1 层的部件下标；causingFactors 再按下标拼接）。
        public int[] subStructurePartIndices = new int[0];
        public int[] subStructureTypes = new int[0];
        public float[] subStructureConditions = new float[0];
        public float[] subStructureProgressionRates = new float[0];
        public int[] subStructureCausingFactorCounts = new int[0];
        public string[] subStructureCausingFactors = new string[0];

        // 第 3 层：诊断条件（diagnosedConditionPartIndices 指向第 1 层的部件下标）。
        public int[] diagnosedConditionPartIndices = new int[0];
        public string[] diagnosedConditionIds = new string[0];
        public string[] diagnosedConditionDisplayNames = new string[0];
        public int[] diagnosedConditionSourceSubStructures = new int[0];
        public int[] diagnosedConditionSourceParts = new int[0];
        public int[] diagnosedConditionDiagnosedDays = new int[0];
        public float[] diagnosedConditionSeverities = new float[0];

        /// <summary>人格七维，顺序与 PersonalityDimensions.All 一致（-100~100）。</summary>
        public float[] personality = new float[8];

        /// <summary>基本能力（[Serializable]，可整体入档）。</summary>
        public BasicAbilities basicAbilities = new BasicAbilities();

        /// <summary>遗传权威基因组（[Serializable]，数组字段；旧 dna 字典不入档）。</summary>
        public GenomeInstance genome;

        public static CharacterSaveData FromCharacter(CharacterData c)
        {
            if (c == null) return null;
            var dto = new CharacterSaveData
            {
                characterId = c.characterId,
                firstName = c.firstName ?? "",
                lastName = c.lastName ?? "",
                age = c.age,
                isMale = c.isMale,
                birthDay = c.birthDay,
                birthYear = c.birthYear,
                deathDay = c.deathDay,
                deathYear = c.deathYear,
                currentSettlementAnchorId = c.currentSettlementAnchorId,
                currentTile = c.currentTile,
                currentBlockId = c.currentBlockId ?? "",
                isPublicFigure = c.isPublicFigure,
                aiCategory = c.aiCategory,
                realmId = c.realmId,
                familyId = c.familyId,
                cultureId = c.cultureId,
                raceId = c.raceId,
                faithId = c.faithId,
                privateFaithId = c.privateFaithId,
                role = c.role,
                socialClass = c.socialClass,
                socialSubclass = c.socialSubclass,
                fatherId = c.fatherId,
                motherId = c.motherId,
                spouseId = c.spouseId,
                prowess = c.prowess,
                social = c.social,
                military = c.military,
                management = c.management,
                conspiracy = c.conspiracy,
                scholarship = c.scholarship,
                charm = c.charm,
                health = c.health,
                fertility = c.fertility,
                stress = c.stress,
                dread = c.dread,
                obesity = c.obesity,
                prestige = c.prestige,
                prestigeCapacityLevel = c.prestigeCapacityLevel,
                notoriety = c.notoriety,
                spiritualFulfillment = c.spiritualFulfillment,
                expectedLifespanYears = c.expectedLifespanYears,
                individualResistance = c.individualResistance,
                epithet = c.epithet ?? "",
                posthumousTitle = c.posthumousTitle ?? "",
                legendaryTitle = c.legendaryTitle ?? "",
                mentalDisorderId = c.mentalDisorderId ?? "",
                isSecretBeliever = c.isSecretBeliever,
                epithetEvaluated = c.epithetEvaluated,
                highStressDays = c.highStressDays,
                lowStressRecoveryDays = c.lowStressRecoveryDays,
                gold = c.gold,
                commandedArmyId = c.commandedArmyId,
                accessionDay = c.accessionDay,
                achievements = c.achievements,
                basicAbilities = c.basicAbilities,
                genome = c.genome
            };
            if (c.ownedTitles != null) dto.ownedTitles.AddRange(c.ownedTitles);
            if (c.personalTenets != null) dto.personalTenets.AddRange(c.personalTenets);
            if (c.bodyMarks != null) dto.bodyMarks.AddRange(c.bodyMarks);

            CaptureRuntimeState(c, dto);

            int dimCount = PersonalityDimensions.All.Length;
            dto.personality = new float[dimCount];
            for (int i = 0; i < dimCount; i++)
                dto.personality[i] = c.GetPersonalityValue(PersonalityDimensions.All[i]);
            return dto;
        }

        /// <summary>
        /// 捕获角色运行态（特质/关系/技能/疾病/伤残）。空集合保持空数组，读档端只处理非空段。
        /// 特质只落 traitId：定义表是唯一事实来源，避免把定义数值复制进每份存档。
        /// </summary>
        private static void CaptureRuntimeState(CharacterData c, CharacterSaveData dto)
        {
            if (c.traits != null && c.traits.Count > 0)
            {
                var ids = new List<string>(c.traits.Count);
                foreach (var trait in c.traits)
                {
                    if (trait == null || string.IsNullOrEmpty(trait.traitId)) continue;
                    ids.Add(trait.traitId);
                }
                dto.traitIds = ids.ToArray();
            }

            if (c.relations != null && c.relations.Count > 0)
            {
                int n = c.relations.Count;
                dto.relationCharacterIds = new int[n];
                dto.relationOpinions = new float[n];
                dto.relationTypes = new int[n];
                dto.relationTrusts = new float[n];
                dto.relationFears = new float[n];
                dto.relationRomanticAttractions = new float[n];
                dto.relationHistoryCounts = new int[n];
                var historyEntries = new List<string>();
                int i = 0;
                foreach (var kv in c.relations)
                {
                    CharacterRelation rel = kv.Value;
                    dto.relationCharacterIds[i] = kv.Key;
                    dto.relationOpinions[i] = rel.opinion;
                    dto.relationTypes[i] = (int)rel.type;
                    dto.relationTrusts[i] = rel.trust;
                    dto.relationFears[i] = rel.fear;
                    dto.relationRomanticAttractions[i] = rel.romanticAttraction;
                    int historyCount = rel.history != null ? rel.history.Count : 0;
                    dto.relationHistoryCounts[i] = historyCount;
                    for (int h = 0; h < historyCount; h++)
                        historyEntries.Add(rel.history[h] ?? "");
                    i++;
                }
                dto.relationHistoryEntries = historyEntries.ToArray();
            }

            if (c.skills != null && c.skills.Count > 0)
            {
                int n = c.skills.Count;
                dto.skillIds = new string[n];
                dto.skillValues = new float[n];
                int i = 0;
                foreach (var kv in c.skills)
                {
                    dto.skillIds[i] = kv.Key ?? "";
                    dto.skillValues[i] = kv.Value;
                    i++;
                }
            }

            if (c.activeDiseases != null && c.activeDiseases.Count > 0)
            {
                int n = c.activeDiseases.Count;
                dto.diseaseIds = new string[n];
                dto.diseaseStages = new int[n];
                dto.diseaseElapsedDays = new int[n];
                dto.diseaseStageDays = new int[n];
                dto.diseaseSeverities = new float[n];
                dto.diseaseTreated = new bool[n];
                dto.diseaseTreatmentDays = new int[n];
                dto.diseaseInfectionSources = new float[n];
                for (int i = 0; i < n; i++)
                {
                    ActiveCharacterDisease disease = c.activeDiseases[i];
                    // 空 id 条目读档端会跳过；这里保留占位以维持并行数组下标一致。
                    if (disease == null)
                    {
                        dto.diseaseIds[i] = "";
                        continue;
                    }
                    dto.diseaseIds[i] = disease.diseaseId ?? "";
                    dto.diseaseStages[i] = (int)disease.stage;
                    dto.diseaseElapsedDays[i] = disease.elapsedDays;
                    dto.diseaseStageDays[i] = disease.stageDays;
                    dto.diseaseSeverities[i] = disease.severity;
                    dto.diseaseTreated[i] = disease.isTreated;
                    dto.diseaseTreatmentDays[i] = disease.treatmentDays;
                    dto.diseaseInfectionSources[i] = disease.infectionSource;
                }
            }

            if (c.impairments != null && c.impairments.Count > 0)
            {
                int n = c.impairments.Count;
                dto.impairmentIds = new string[n];
                dto.impairmentStageIndices = new int[n];
                dto.impairmentProgressionRates = new float[n];
                dto.impairmentStageDays = new int[n];
                dto.impairmentCauses = new string[n];
                dto.impairmentReversible = new bool[n];
                for (int i = 0; i < n; i++)
                {
                    ActiveImpairment impairment = c.impairments[i];
                    dto.impairmentIds[i] = impairment.impairmentId ?? "";
                    dto.impairmentStageIndices[i] = impairment.stageIndex;
                    dto.impairmentProgressionRates[i] = impairment.progressionRate;
                    dto.impairmentStageDays[i] = impairment.daysAtCurrentStage;
                    dto.impairmentCauses[i] = impairment.cause ?? "";
                    dto.impairmentReversible[i] = impairment.isReversible;
                }
            }

            CaptureBodyParts(c, dto);
        }

        /// <summary>
        /// 捕获身体部位三层结构：部件标量 → 子结构（含 causingFactors）→ 诊断条件，
        /// 全部拍平成并行数组。bodyParts 为 null/空时保持空数组（旧档读档端按默认结构处理）。
        /// </summary>
        private static void CaptureBodyParts(CharacterData c, CharacterSaveData dto)
        {
            if (c.bodyParts == null || c.bodyParts.Count == 0) return;

            int partCount = c.bodyParts.Count;
            dto.bodyPartTypes = new int[partCount];
            dto.bodyPartMaxHps = new float[partCount];
            dto.bodyPartCurrentHps = new float[partCount];
            dto.bodyPartFunctionLevels = new float[partCount];
            dto.bodyPartStates = new int[partCount];
            dto.bodyPartSubStructureCounts = new int[partCount];
            dto.bodyPartDiagnosedConditionCounts = new int[partCount];

            var subPartIndices = new List<int>();
            var subTypes = new List<int>();
            var subConditions = new List<float>();
            var subProgressionRates = new List<float>();
            var subCausingFactorCounts = new List<int>();
            var subCausingFactors = new List<string>();

            var conditionPartIndices = new List<int>();
            var conditionIds = new List<string>();
            var conditionDisplayNames = new List<string>();
            var conditionSourceSubStructures = new List<int>();
            var conditionSourceParts = new List<int>();
            var conditionDiagnosedDays = new List<int>();
            var conditionSeverities = new List<float>();

            int partIndex = 0;
            foreach (var kv in c.bodyParts)
            {
                BodyPartData part = kv.Value;
                dto.bodyPartTypes[partIndex] = (int)kv.Key;
                dto.bodyPartMaxHps[partIndex] = part.maxHp;
                dto.bodyPartCurrentHps[partIndex] = part.currentHp;
                dto.bodyPartFunctionLevels[partIndex] = part.functionLevel;
                dto.bodyPartStates[partIndex] = (int)part.state;

                int subCount = part.subStructures != null ? part.subStructures.Count : 0;
                dto.bodyPartSubStructureCounts[partIndex] = subCount;
                for (int i = 0; i < subCount; i++)
                {
                    SubStructureData sub = part.subStructures[i];
                    subPartIndices.Add(partIndex);
                    subTypes.Add((int)sub.type);
                    subConditions.Add(sub.condition);
                    subProgressionRates.Add(sub.progressionRate);
                    int factorCount = sub.causingFactors != null ? sub.causingFactors.Count : 0;
                    subCausingFactorCounts.Add(factorCount);
                    for (int k = 0; k < factorCount; k++)
                        subCausingFactors.Add(sub.causingFactors[k] ?? "");
                }

                int conditionCount = part.diagnosedConditions != null ? part.diagnosedConditions.Count : 0;
                dto.bodyPartDiagnosedConditionCounts[partIndex] = conditionCount;
                for (int i = 0; i < conditionCount; i++)
                {
                    DiagnosedCondition condition = part.diagnosedConditions[i];
                    conditionPartIndices.Add(partIndex);
                    conditionIds.Add(condition.conditionId ?? "");
                    conditionDisplayNames.Add(condition.displayName ?? "");
                    conditionSourceSubStructures.Add((int)condition.sourceSubStructure);
                    conditionSourceParts.Add((int)condition.sourcePart);
                    conditionDiagnosedDays.Add(condition.diagnosedDay);
                    conditionSeverities.Add(condition.severityAtDiagnosis);
                }
                partIndex++;
            }

            dto.subStructurePartIndices = subPartIndices.ToArray();
            dto.subStructureTypes = subTypes.ToArray();
            dto.subStructureConditions = subConditions.ToArray();
            dto.subStructureProgressionRates = subProgressionRates.ToArray();
            dto.subStructureCausingFactorCounts = subCausingFactorCounts.ToArray();
            dto.subStructureCausingFactors = subCausingFactors.ToArray();
            dto.diagnosedConditionPartIndices = conditionPartIndices.ToArray();
            dto.diagnosedConditionIds = conditionIds.ToArray();
            dto.diagnosedConditionDisplayNames = conditionDisplayNames.ToArray();
            dto.diagnosedConditionSourceSubStructures = conditionSourceSubStructures.ToArray();
            dto.diagnosedConditionSourceParts = conditionSourceParts.ToArray();
            dto.diagnosedConditionDiagnosedDays = conditionDiagnosedDays.ToArray();
            dto.diagnosedConditionSeverities = conditionSeverities.ToArray();
        }

        /// <summary>还原为运行时可用的 CharacterData（不做身份派生重算，保留存档值）。</summary>
        public CharacterData ToCharacter()
        {
            var c = new CharacterData
            {
                characterId = characterId,
                firstName = firstName ?? "",
                lastName = lastName ?? "",
                age = age,
                isMale = isMale,
                birthDay = birthDay,
                birthYear = birthYear,
                deathDay = deathDay,
                deathYear = deathYear,
                currentSettlementAnchorId = currentSettlementAnchorId,
                currentTile = currentTile,
                currentBlockId = currentBlockId ?? "",
                isPublicFigure = isPublicFigure,
                aiCategory = aiCategory,
                realmId = realmId,
                familyId = familyId,
                cultureId = cultureId,
                raceId = raceId,
                faithId = faithId,
                privateFaithId = privateFaithId,
                role = role,
                socialClass = socialClass,
                socialSubclass = socialSubclass,
                fatherId = fatherId,
                motherId = motherId,
                spouseId = spouseId,
                prowess = prowess,
                social = social,
                military = military,
                management = management,
                conspiracy = conspiracy,
                scholarship = scholarship,
                charm = charm,
                health = health,
                fertility = fertility,
                stress = stress,
                dread = dread,
                obesity = obesity,
                prestige = prestige,
                prestigeCapacityLevel = prestigeCapacityLevel,
                notoriety = notoriety,
                spiritualFulfillment = spiritualFulfillment,
                expectedLifespanYears = expectedLifespanYears,
                individualResistance = individualResistance,
                epithet = epithet ?? "",
                posthumousTitle = posthumousTitle ?? "",
                legendaryTitle = legendaryTitle ?? "",
                mentalDisorderId = mentalDisorderId ?? "",
                isSecretBeliever = isSecretBeliever,
                epithetEvaluated = epithetEvaluated,
                highStressDays = highStressDays,
                lowStressRecoveryDays = lowStressRecoveryDays,
                gold = gold,
                commandedArmyId = commandedArmyId,
                accessionDay = accessionDay,
                achievements = achievements,
                basicAbilities = basicAbilities ?? new BasicAbilities(),
                genome = genome,
                // 旧 dna 兼容层：留空，靠 GetGenome 从 genome 派生。
                dna = null
            };
            if (ownedTitles != null) c.ownedTitles.AddRange(ownedTitles);
            if (personalTenets != null) c.personalTenets.AddRange(personalTenets);
            if (bodyMarks != null) c.bodyMarks.AddRange(bodyMarks);

            if (personality != null)
            {
                int dimCount = PersonalityDimensions.All.Length;
                int n = personality.Length < dimCount ? personality.Length : dimCount;
                for (int i = 0; i < n; i++)
                    c.SetPersonalityValue(PersonalityDimensions.All[i], personality[i]);
            }

            RestoreRuntimeState(c);

            // 防御：旧档字段缺失/未来版本缺段时，确保 bodyParts 可用（系统每日 Tick 会访问）。
            if (c.bodyParts == null) c.bodyParts = BodyPartSystem.InitializeBodyParts();
            return c;
        }

        /// <summary>
        /// 还原运行态。旧档缺段（数组为 null/空）时保持 CharacterData 构造出的空集合，不抛异常；
        /// 分段长度不一致时以最短段为准，避免越界读到其它段的残留值。
        /// </summary>
        private void RestoreRuntimeState(CharacterData c)
        {
            if (traitIds != null)
            {
                for (int i = 0; i < traitIds.Length; i++)
                {
                    string traitId = traitIds[i];
                    if (string.IsNullOrEmpty(traitId) || c.HasTrait(traitId)) continue;
                    // 全限定：CivilizationEvolution.Role 下另有同名特质量表，避免将来补 using 时歧义。
                    PersonalityTrait trait = MyriadCreation.Simulation.Definitions.PersonalityTraitDatabase.Get(traitId);
                    if (trait == null)
                    {
                        Debug.LogWarning("[CharacterSaveData] 读档跳过未知特质 id：" + traitId
                            + "（角色 " + c.characterId + "）");
                        continue;
                    }
                    c.traits.Add(trait);
                }
            }

            int relationCount = MinArrayLength(relationCharacterIds, relationOpinions, relationTypes,
                relationTrusts, relationFears, relationRomanticAttractions);
            int historyCursor = 0;
            for (int i = 0; i < relationCount; i++)
            {
                int historyCount = 0;
                if (relationHistoryCounts != null && i < relationHistoryCounts.Length)
                    historyCount = relationHistoryCounts[i] > 0 ? relationHistoryCounts[i] : 0;
                var history = new List<string>(historyCount);
                for (int h = 0; h < historyCount; h++)
                {
                    if (relationHistoryEntries != null && historyCursor < relationHistoryEntries.Length)
                        history.Add(relationHistoryEntries[historyCursor] ?? "");
                    historyCursor++;
                }

                int otherId = relationCharacterIds[i];
                if (otherId <= 0) continue;
                c.relations[otherId] = new CharacterRelation
                {
                    otherCharacterId = otherId,
                    opinion = relationOpinions[i],
                    type = (RelationshipType)relationTypes[i],
                    trust = relationTrusts[i],
                    fear = relationFears[i],
                    romanticAttraction = relationRomanticAttractions[i],
                    history = history
                };
            }

            int skillCount = MinArrayLength(skillIds, skillValues);
            for (int i = 0; i < skillCount; i++)
            {
                string skillId = skillIds[i];
                if (string.IsNullOrEmpty(skillId)) continue;
                c.skills[skillId] = skillValues[i];
            }

            int diseaseCount = MinArrayLength(diseaseIds, diseaseStages, diseaseElapsedDays, diseaseStageDays,
                diseaseSeverities, diseaseTreated, diseaseTreatmentDays, diseaseInfectionSources);
            for (int i = 0; i < diseaseCount; i++)
            {
                string diseaseId = diseaseIds[i];
                if (string.IsNullOrEmpty(diseaseId)) continue;
                c.activeDiseases.Add(new ActiveCharacterDisease
                {
                    diseaseId = diseaseId,
                    stage = (DiseaseStage)diseaseStages[i],
                    elapsedDays = diseaseElapsedDays[i],
                    stageDays = diseaseStageDays[i],
                    severity = diseaseSeverities[i],
                    isTreated = diseaseTreated[i],
                    treatmentDays = diseaseTreatmentDays[i],
                    infectionSource = diseaseInfectionSources[i]
                });
            }

            int impairmentCount = MinArrayLength(impairmentIds, impairmentStageIndices, impairmentProgressionRates,
                impairmentStageDays, impairmentCauses, impairmentReversible);
            for (int i = 0; i < impairmentCount; i++)
            {
                string impairmentId = impairmentIds[i];
                if (string.IsNullOrEmpty(impairmentId)) continue;
                c.impairments.Add(new ActiveImpairment
                {
                    impairmentId = impairmentId,
                    stageIndex = impairmentStageIndices[i],
                    progressionRate = impairmentProgressionRates[i],
                    daysAtCurrentStage = impairmentStageDays[i],
                    cause = impairmentCauses[i] ?? "",
                    isReversible = impairmentReversible[i]
                });
            }

            RestoreBodyParts(c);
        }

        /// <summary>
        /// 还原身体部位：先按 BodyPartSystem.InitializeBodyParts 的默认结构建表，再用存档覆盖
        /// 已有部件/子结构；存档中没有的部件或子结构保留默认值（旧档、内容增删部件均安全）。
        /// </summary>
        private void RestoreBodyParts(CharacterData c)
        {
            // 旧档缺段/空段：不在这里建表，交给 ToCharacter 末尾的默认初始化（与旧行为一致）。
            if (bodyPartTypes == null || bodyPartTypes.Length == 0) return;

            Dictionary<BodyPartType, BodyPartData> parts = BodyPartSystem.InitializeBodyParts();
            // 计数数组只用于存档自描述/调试，恢复不依赖它们，因此不参与可用长度裁剪（旧/半截档更宽容）。
            int partCount = MinArrayLength(bodyPartTypes, bodyPartMaxHps, bodyPartCurrentHps,
                bodyPartFunctionLevels, bodyPartStates);
            for (int i = 0; i < partCount; i++)
            {
                BodyPartType partType = (BodyPartType)bodyPartTypes[i];
                // 保存里出现当前枚举没有的部件：保留默认结构，不新建未知键。
                if (!parts.TryGetValue(partType, out BodyPartData part)) continue;
                part.maxHp = bodyPartMaxHps[i];
                part.currentHp = bodyPartCurrentHps[i];
                part.functionLevel = bodyPartFunctionLevels[i];
                part.state = (BodyPartState)bodyPartStates[i];
                parts[partType] = part;
            }

            int subCount = MinArrayLength(subStructurePartIndices, subStructureTypes, subStructureConditions,
                subStructureProgressionRates);
            int factorCursor = 0;
            for (int i = 0; i < subCount; i++)
            {
                int causingFactorCount = 0;
                if (subStructureCausingFactorCounts != null && i < subStructureCausingFactorCounts.Length)
                    causingFactorCount = subStructureCausingFactorCounts[i] > 0 ? subStructureCausingFactorCounts[i] : 0;
                var causingFactors = new List<string>(causingFactorCount);
                for (int k = 0; k < causingFactorCount; k++)
                {
                    if (subStructureCausingFactors != null && factorCursor < subStructureCausingFactors.Length)
                        causingFactors.Add(subStructureCausingFactors[factorCursor] ?? "");
                    factorCursor++;
                }

                int partIndex = subStructurePartIndices[i];
                if (partIndex < 0 || partIndex >= partCount) continue;
                BodyPartType partType = (BodyPartType)bodyPartTypes[partIndex];
                if (!parts.TryGetValue(partType, out BodyPartData part)) continue;
                if (part.subStructures == null) part.subStructures = new List<SubStructureData>();

                SubStructureType subType = (SubStructureType)subStructureTypes[i];
                int existing = part.subStructures.FindIndex(s => s.type == subType);
                SubStructureData sub = existing >= 0 ? part.subStructures[existing] : new SubStructureData(subType);
                sub.condition = subStructureConditions[i];
                sub.progressionRate = subStructureProgressionRates[i];
                sub.causingFactors = causingFactors;
                if (existing >= 0) part.subStructures[existing] = sub;
                else part.subStructures.Add(sub); // 存档多出的子结构类型（内容版本差异）：追加保留
                parts[partType] = part;
            }

            int conditionCount = MinArrayLength(diagnosedConditionPartIndices, diagnosedConditionIds,
                diagnosedConditionDisplayNames, diagnosedConditionSourceSubStructures,
                diagnosedConditionSourceParts, diagnosedConditionDiagnosedDays, diagnosedConditionSeverities);
            for (int i = 0; i < conditionCount; i++)
            {
                int partIndex = diagnosedConditionPartIndices[i];
                if (partIndex < 0 || partIndex >= partCount) continue;
                BodyPartType partType = (BodyPartType)bodyPartTypes[partIndex];
                if (!parts.TryGetValue(partType, out BodyPartData part)) continue;
                if (part.diagnosedConditions == null) part.diagnosedConditions = new List<DiagnosedCondition>();
                part.diagnosedConditions.Add(new DiagnosedCondition
                {
                    conditionId = diagnosedConditionIds[i] ?? "",
                    displayName = diagnosedConditionDisplayNames[i] ?? "",
                    sourceSubStructure = (SubStructureType)diagnosedConditionSourceSubStructures[i],
                    sourcePart = (BodyPartType)diagnosedConditionSourceParts[i],
                    diagnosedDay = diagnosedConditionDiagnosedDays[i],
                    severityAtDiagnosis = diagnosedConditionSeverities[i]
                });
                parts[partType] = part;
            }

            c.bodyParts = parts;
        }

        /// <summary>
        /// 多段并行数组取共同可用的最短长度；任一段为 null（旧档缺该段）视为 0。
        /// 只在读档路径调用，每次捕获为一个小数组，分配可忽略。
        /// </summary>
        private static int MinArrayLength(params System.Array[] segments)
        {
            int min = int.MaxValue;
            foreach (var segment in segments)
            {
                if (segment == null) return 0;
                if (segment.Length < min) min = segment.Length;
            }
            return min == int.MaxValue ? 0 : min;
        }
    }

    /// <summary>
    /// 角色纽带存档 DTO（SaveVersion 15 起）。纽带属于 CharacterManager 的全局列表而非单个角色字段，
    /// 因此不挂在 CharacterSaveData 上，由 CharacterBondPersistence 统一捕获/恢复。
    /// </summary>
    [Serializable]
    public class CharacterBondSaveData
    {
        public int bondId;
        public int characterAId;
        public int characterBId;
        public int type; // BondType（按数字存，避免枚举重排影响旧档）
        public int establishedDay;
        public float strength = 50f;
        public float combatBonusWhenTogether;
        public float diplomacyBonus;
        public float stressReduction;
        public bool sharedFate;

        public static CharacterBondSaveData FromBond(CharacterBond bond)
        {
            if (bond == null) return null;
            return new CharacterBondSaveData
            {
                bondId = bond.bondId,
                characterAId = bond.characterAId,
                characterBId = bond.characterBId,
                type = (int)bond.type,
                establishedDay = bond.establishedDay,
                strength = bond.strength,
                combatBonusWhenTogether = bond.combatBonusWhenTogether,
                diplomacyBonus = bond.diplomacyBonus,
                stressReduction = bond.stressReduction,
                sharedFate = bond.sharedFate
            };
        }

        public CharacterBond ToBond()
        {
            return new CharacterBond
            {
                bondId = bondId,
                characterAId = characterAId,
                characterBId = characterBId,
                type = (BondType)type,
                establishedDay = establishedDay,
                strength = strength,
                combatBonusWhenTogether = combatBonusWhenTogether,
                diplomacyBonus = diplomacyBonus,
                stressReduction = stressReduction,
                sharedFate = sharedFate
            };
        }
    }
}
