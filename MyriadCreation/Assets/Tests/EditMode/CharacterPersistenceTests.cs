using System;
using System.Collections.Generic;
using System.Reflection;
using MyriadCreation.Infrastructure.Save;
using MyriadCreation.Simulation.Definitions;
using MyriadCreation.Simulation.State;
using MyriadCreation.Simulation.Systems;
using NUnit.Framework;
using UnityEngine;
using MyriadCreation.Content.Definitions;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>
    /// 角色运行态入档（SaveVersion 14/15）：traits/relations/skills/activeDiseases/impairments（v14）、
    /// bodyParts（v15）与管理器级 CharacterBond，经 FromCharacter / CharacterBondPersistence → JsonUtility
    /// → 还原后逐项保持；旧档缺少运行态段时降级为空集合或默认结构。
    /// 说明：FromCharacter/ToCharacter 是纯函数（不依赖 CharacterManager 容器或 GameObject），
    /// 所以直接用 CharacterData 构造被测角色；纽带测试需要把角色放进 CharacterManager 的私有字典
    /// （无公开注册既有角色的 API，测试用反射注入），完整容器安装链路由 SaveEntityBridge 覆盖。
    /// </summary>
    public class CharacterPersistenceTests
    {
        private CharacterManager _manager;

        [SetUp]
        public void SetUp()
        {
            _manager = new CharacterManager();
        }

        [TearDown]
        public void TearDown()
        {
            // 本类不创建 GameObject、不注册静态表；防御性清理角色知识表，避免跨测试类静态残留。
            CharacterKnowledgeRegistry.Clear();
            _manager = null;
        }

        private static CharacterData BuildRuntimeCharacter()
        {
            var c = new CharacterData
            {
                characterId = 42,
                firstName = "测试",
                lastName = "角色",
                age = 33,
                isMale = true,
                health = 73.5f,
                fertility = 41.5f,
                prowess = 62.25f,
                gold = 123.75f,
                epithet = "智者"
            };
            c.SetPersonalityValue(PersonalityDimension.Boldness, 55f);
            c.SetPersonalityValue(PersonalityDimension.Compassion, -30f);

            PersonalityTrait brave = PersonalityTraitDatabase.Get("brave_2");
            PersonalityTrait craven = PersonalityTraitDatabase.Get("craven_1");
            Assert.IsNotNull(brave, "测试前置数据：特质量表应包含 brave_2");
            Assert.IsNotNull(craven, "测试前置数据：特质量表应包含 craven_1");
            c.traits.Add(brave);
            c.traits.Add(craven);

            c.relations[77] = new CharacterRelation
            {
                otherCharacterId = 77,
                opinion = 42.5f,
                type = RelationshipType.Spouse,
                trust = 12.25f,
                fear = -3.5f,
                romanticAttraction = 66f,
                history = new List<string> { "结为夫妇: 30", "共度难关: 5" }
            };
            c.relations[88] = new CharacterRelation
            {
                otherCharacterId = 88,
                opinion = -120.25f,
                type = RelationshipType.Vassal,
                trust = 0f,
                fear = 80.5f,
                romanticAttraction = 0f,
                history = new List<string>()
            };

            c.skills["combat"] = 12.5f;
            c.skills["diplomacy"] = 7.25f;
            c.skills["stewardship"] = 0f;

            c.activeDiseases.Add(new ActiveCharacterDisease("plague", 3f)
            {
                stage = DiseaseStage.Acute,
                elapsedDays = 5,
                stageDays = 2,
                severity = 0.75f,
                isTreated = true,
                treatmentDays = 3
            });
            c.activeDiseases.Add(new ActiveCharacterDisease("flu")
            {
                stage = DiseaseStage.Recovery,
                elapsedDays = 11,
                stageDays = 4,
                severity = 0.25f,
                isTreated = false,
                treatmentDays = 0
            });

            c.impairments.Add(new ActiveImpairment
            {
                impairmentId = "lost_eye",
                stageIndex = 2,
                progressionRate = 0.25f,
                daysAtCurrentStage = 14,
                cause = "战场创伤",
                isReversible = false
            });
            c.impairments.Add(new ActiveImpairment
            {
                impairmentId = "broken_arm",
                stageIndex = 1,
                progressionRate = 0.5f,
                daysAtCurrentStage = 3,
                cause = "坠马",
                isReversible = true
            });
            return c;
        }

        [Test]
        public void RuntimeState_RoundTripsThroughJsonUtility()
        {
            CharacterData original = BuildRuntimeCharacter();
            Assert.AreEqual(0, _manager.GetAllCharacters().Count,
                "角色 DTO 捕获/还原不应写入 CharacterManager 容器（容器安装属于 SaveEntityBridge 职责）");

            CharacterSaveData dto = CharacterSaveData.FromCharacter(original);
            Assert.IsNotNull(dto);
            string json = JsonUtility.ToJson(dto);
            StringAssert.Contains("traitIds", json, "运行态字段必须是可序列化的 public 数组");
            StringAssert.Contains("relationCharacterIds", json);
            StringAssert.Contains("skillIds", json);
            StringAssert.Contains("diseaseIds", json);
            StringAssert.Contains("impairmentIds", json);

            CharacterSaveData restoredDto = JsonUtility.FromJson<CharacterSaveData>(json);
            Assert.IsNotNull(restoredDto);
            CharacterData restored = restoredDto.ToCharacter();
            Assert.IsNotNull(restored);

            // 既有字段不能被新增运行态段破坏
            Assert.AreEqual(42, restored.characterId);
            Assert.AreEqual("测试 角色", restored.fullName);
            Assert.AreEqual(73.5f, restored.health, 0.001f);
            Assert.AreEqual(41.5f, restored.fertility, 0.001f);
            Assert.AreEqual(62.25f, restored.prowess, 0.001f);
            Assert.AreEqual(123.75f, restored.gold, 0.001f);
            Assert.AreEqual("智者", restored.epithet);
            Assert.AreEqual(55f, restored.GetPersonalityValue(PersonalityDimension.Boldness), 0.001f);
            Assert.AreEqual(-30f, restored.GetPersonalityValue(PersonalityDimension.Compassion), 0.001f);

            // traits：按 id 从特质量表还原，顺序保持
            Assert.AreEqual(2, restored.traits.Count);
            Assert.AreEqual("brave_2", restored.traits[0].traitId);
            Assert.AreEqual("craven_1", restored.traits[1].traitId);

            // relations：数值、类型、关系史逐项保持
            Assert.AreEqual(2, restored.relations.Count);
            Assert.IsTrue(restored.relations.TryGetValue(77, out CharacterRelation spouse));
            Assert.AreEqual(77, spouse.otherCharacterId);
            Assert.AreEqual(42.5f, spouse.opinion, 0.001f);
            Assert.AreEqual(RelationshipType.Spouse, spouse.type);
            Assert.AreEqual(12.25f, spouse.trust, 0.001f);
            Assert.AreEqual(-3.5f, spouse.fear, 0.001f);
            Assert.AreEqual(66f, spouse.romanticAttraction, 0.001f);
            Assert.IsNotNull(spouse.history);
            Assert.AreEqual(2, spouse.history.Count);
            Assert.AreEqual("结为夫妇: 30", spouse.history[0]);
            Assert.AreEqual("共度难关: 5", spouse.history[1]);

            Assert.IsTrue(restored.relations.TryGetValue(88, out CharacterRelation vassal));
            Assert.AreEqual(-120.25f, vassal.opinion, 0.001f);
            Assert.AreEqual(RelationshipType.Vassal, vassal.type);
            Assert.AreEqual(80.5f, vassal.fear, 0.001f);
            Assert.AreEqual(0, vassal.history.Count);

            // skills
            Assert.AreEqual(3, restored.skills.Count);
            Assert.AreEqual(12.5f, restored.skills["combat"], 0.001f);
            Assert.AreEqual(7.25f, restored.skills["diplomacy"], 0.001f);
            Assert.IsTrue(restored.skills.ContainsKey("stewardship"));
            Assert.AreEqual(0f, restored.skills["stewardship"], 0.001f);

            // activeDiseases
            Assert.AreEqual(2, restored.activeDiseases.Count);
            ActiveCharacterDisease plague = restored.activeDiseases[0];
            Assert.AreEqual("plague", plague.diseaseId);
            Assert.AreEqual(DiseaseStage.Acute, plague.stage);
            Assert.AreEqual(5, plague.elapsedDays);
            Assert.AreEqual(2, plague.stageDays);
            Assert.AreEqual(0.75f, plague.severity, 0.001f);
            Assert.IsTrue(plague.isTreated);
            Assert.AreEqual(3, plague.treatmentDays);
            Assert.AreEqual(3f, plague.infectionSource, 0.001f);
            Assert.AreEqual("flu", restored.activeDiseases[1].diseaseId);
            Assert.AreEqual(DiseaseStage.Recovery, restored.activeDiseases[1].stage);

            // impairments
            Assert.AreEqual(2, restored.impairments.Count);
            ActiveImpairment lostEye = restored.impairments[0];
            Assert.AreEqual("lost_eye", lostEye.impairmentId);
            Assert.AreEqual(2, lostEye.stageIndex);
            Assert.AreEqual(0.25f, lostEye.progressionRate, 0.001f);
            Assert.AreEqual(14, lostEye.daysAtCurrentStage);
            Assert.AreEqual("战场创伤", lostEye.cause);
            Assert.IsFalse(lostEye.isReversible);
            Assert.AreEqual("broken_arm", restored.impairments[1].impairmentId);
            Assert.IsTrue(restored.impairments[1].isReversible);
        }

        [Test]
        public void LegacyJsonWithoutRuntimeSegments_LoadsWithDefaults()
        {
            // 典型 SaveVersion 11-13 角色段：只有旧字段，没有本版新增的运行态数组。
            string legacyJson = @"{
  ""characterId"": 7,
  ""firstName"": ""旧"",
  ""lastName"": ""档"",
  ""health"": 88.0,
  ""fertility"": 42.0,
  ""prowess"": 61.0,
  ""gold"": 12.5,
  ""personality"": [10.0, 20.0, 30.0, 40.0, 50.0, 60.0, 70.0],
  ""ownedTitles"": [3, 4]
}";
            CharacterSaveData dto = JsonUtility.FromJson<CharacterSaveData>(legacyJson);
            Assert.IsNotNull(dto);
            CharacterData c = dto.ToCharacter();
            Assert.IsNotNull(c);

            Assert.AreEqual(7, c.characterId);
            Assert.AreEqual("旧", c.firstName);
            Assert.AreEqual(88f, c.health, 0.001f);
            Assert.AreEqual(42f, c.fertility, 0.001f);
            Assert.AreEqual(61f, c.prowess, 0.001f);
            Assert.AreEqual(12.5f, c.gold, 0.001f);
            Assert.AreEqual(10f, c.GetPersonalityValue(PersonalityDimension.Boldness), 0.001f);
            Assert.AreEqual(30f, c.GetPersonalityValue(PersonalityDimension.Compassion), 0.001f);
            Assert.AreEqual(2, c.ownedTitles.Count);

            Assert.AreEqual(0, c.traits.Count, "旧档无 traitIds：不应凭空产生特质");
            Assert.AreEqual(0, c.relations.Count);
            Assert.AreEqual(0, c.skills.Count);
            Assert.AreEqual(0, c.activeDiseases.Count);
            Assert.AreEqual(0, c.impairments.Count);
            Assert.IsNotNull(c.bodyParts, "旧档无 bodyParts：必须由 InitializeBodyParts 重建，避免每日 Tick NRE");
            Assert.Greater(c.bodyParts.Count, 0);
        }

        [Test]
        public void ToCharacter_NullOrRaggedRuntimeArrays_DoesNotThrow()
        {
            var dto = new CharacterSaveData { characterId = 9 };
            dto.traitIds = null;
            dto.relationCharacterIds = null;
            dto.relationOpinions = null;
            dto.relationTypes = null;
            dto.relationTrusts = null;
            dto.relationFears = null;
            dto.relationRomanticAttractions = null;
            dto.relationHistoryCounts = null;
            dto.relationHistoryEntries = null;
            dto.skillIds = null;
            dto.skillValues = null;
            dto.diseaseIds = null;
            dto.diseaseStages = null;
            dto.diseaseElapsedDays = null;
            dto.diseaseStageDays = null;
            dto.diseaseSeverities = null;
            dto.diseaseTreated = null;
            dto.diseaseTreatmentDays = null;
            dto.diseaseInfectionSources = null;
            dto.impairmentIds = null;
            dto.impairmentStageIndices = null;
            dto.impairmentProgressionRates = null;
            dto.impairmentStageDays = null;
            dto.impairmentCauses = null;
            dto.impairmentReversible = null;
            dto.bodyPartTypes = null;
            dto.bodyPartMaxHps = null;
            dto.bodyPartCurrentHps = null;
            dto.bodyPartFunctionLevels = null;
            dto.bodyPartStates = null;
            dto.subStructurePartIndices = null;
            dto.subStructureTypes = null;
            dto.subStructureConditions = null;
            dto.subStructureProgressionRates = null;
            dto.diagnosedConditionPartIndices = null;
            dto.diagnosedConditionIds = null;

            CharacterData c = dto.ToCharacter();
            Assert.IsNotNull(c);
            Assert.AreEqual(0, c.traits.Count);
            Assert.AreEqual(0, c.relations.Count);
            Assert.AreEqual(0, c.skills.Count);
            Assert.AreEqual(0, c.activeDiseases.Count);
            Assert.AreEqual(0, c.impairments.Count);

            // 并行数组长度不一致：只还原共同前缀，不越界读取
            dto.skillIds = new[] { "combat", "archery" };
            dto.skillValues = new[] { 5f };
            CharacterData ragged = dto.ToCharacter();
            Assert.AreEqual(1, ragged.skills.Count);
            Assert.AreEqual(5f, ragged.skills["combat"], 0.001f);
            Assert.IsFalse(ragged.skills.ContainsKey("archery"));
        }

        [Test]
        public void ToCharacter_SkipsUnknownTraitId()
        {
            var dto = new CharacterSaveData { characterId = 10 };
            dto.traitIds = new[] { "brave_2", "__missing_trait__" };
            CharacterData c = dto.ToCharacter();
            Assert.AreEqual(1, c.traits.Count);
            Assert.AreEqual("brave_2", c.traits[0].traitId);
            Assert.IsFalse(c.HasTrait("__missing_trait__"));
        }

        [Test]
        public void EmptyRuntimeState_RoundTripsAsEmptyCollections()
        {
            var original = new CharacterData { characterId = 3 };
            CharacterSaveData dto = CharacterSaveData.FromCharacter(original);
            Assert.IsNotNull(dto);
            Assert.IsNotNull(dto.traitIds);
            Assert.AreEqual(0, dto.traitIds.Length);
            Assert.AreEqual(0, dto.relationCharacterIds.Length);
            Assert.AreEqual(0, dto.skillIds.Length);
            Assert.AreEqual(0, dto.diseaseIds.Length);
            Assert.AreEqual(0, dto.impairmentIds.Length);

            string json = JsonUtility.ToJson(dto);
            CharacterData restored = JsonUtility.FromJson<CharacterSaveData>(json).ToCharacter();
            Assert.AreEqual(0, restored.traits.Count);
            Assert.AreEqual(0, restored.relations.Count);
            Assert.AreEqual(0, restored.skills.Count);
            Assert.AreEqual(0, restored.activeDiseases.Count);
            Assert.AreEqual(0, restored.impairments.Count);
        }

        [Test]
        public void FromCharacter_NullCharacter_ReturnsNull()
        {
            Assert.IsNull(CharacterSaveData.FromCharacter(null));
        }

        // ===== bodyParts：部件 → 子结构（含造成因素）→ 诊断条件，三层往返 =====

        [Test]
        public void BodyParts_RoundTripsIncludingSubStructuresAndDiagnosedConditions()
        {
            var original = new CharacterData { characterId = 21 };
            original.bodyParts = BodyPartSystem.InitializeBodyParts();
            Assert.AreEqual(Enum.GetValues(typeof(BodyPartType)).Length, original.bodyParts.Count,
                "测试前置：InitializeBodyParts 应为每个 BodyPartType 建一个部件");

            // 左眼：视网膜受损，并有一条诊断记录
            BodyPartData leftEye = original.bodyParts[BodyPartType.LeftEye];
            int retinaIndex = leftEye.subStructures.FindIndex(s => s.type == SubStructureType.EyeRetina);
            Assert.GreaterOrEqual(retinaIndex, 0, "测试前置：左眼应包含视网膜子结构");
            SubStructureData retina = leftEye.subStructures[retinaIndex];
            retina.condition = 62.5f;
            retina.progressionRate = 0.25f;
            retina.causingFactors.Add("外伤");
            leftEye.subStructures[retinaIndex] = retina;
            leftEye.currentHp = 84.375f;
            leftEye.functionLevel = 0.84375f;
            leftEye.state = BodyPartState.Injured;
            leftEye.diagnosedConditions.Add(new DiagnosedCondition
            {
                conditionId = "retina_damage",
                displayName = "视网膜损伤",
                sourceSubStructure = SubStructureType.EyeRetina,
                sourcePart = BodyPartType.LeftEye,
                diagnosedDay = 321,
                severityAtDiagnosis = 62.5f
            });
            original.bodyParts[BodyPartType.LeftEye] = leftEye;

            // 心脏：瓣膜病变（只有子结构进度与造成因素，部件本身仍健康）
            BodyPartData heart = original.bodyParts[BodyPartType.Heart];
            int valveIndex = heart.subStructures.FindIndex(s => s.type == SubStructureType.HeartValve);
            Assert.GreaterOrEqual(valveIndex, 0, "测试前置：心脏应包含瓣膜子结构");
            SubStructureData valve = heart.subStructures[valveIndex];
            valve.progressionRate = 0.5f;
            valve.causingFactors.Add("风湿热");
            heart.subStructures[valveIndex] = valve;
            original.bodyParts[BodyPartType.Heart] = heart;

            CharacterSaveData dto = CharacterSaveData.FromCharacter(original);
            Assert.IsNotNull(dto);
            Assert.AreEqual(original.bodyParts.Count, dto.bodyPartTypes.Length);
            Assert.AreEqual(1, dto.diagnosedConditionIds.Length);

            string json = JsonUtility.ToJson(dto);
            StringAssert.Contains("bodyPartTypes", json, "三层结构字段必须进入 JSON");
            StringAssert.Contains("subStructureTypes", json);
            StringAssert.Contains("diagnosedConditionIds", json);

            CharacterData restored = JsonUtility.FromJson<CharacterSaveData>(json).ToCharacter();
            Assert.IsNotNull(restored.bodyParts);
            Assert.AreEqual(original.bodyParts.Count, restored.bodyParts.Count);

            BodyPartData restoredEye = restored.bodyParts[BodyPartType.LeftEye];
            Assert.AreEqual(84.375f, restoredEye.currentHp, 0.001f);
            Assert.AreEqual(0.84375f, restoredEye.functionLevel, 0.001f);
            Assert.AreEqual(BodyPartState.Injured, restoredEye.state);
            Assert.AreEqual(leftEye.subStructures.Count, restoredEye.subStructures.Count);
            int restoredRetinaIndex = restoredEye.subStructures.FindIndex(s => s.type == SubStructureType.EyeRetina);
            Assert.GreaterOrEqual(restoredRetinaIndex, 0);
            Assert.AreEqual(62.5f, restoredEye.subStructures[restoredRetinaIndex].condition, 0.001f);
            Assert.AreEqual(0.25f, restoredEye.subStructures[restoredRetinaIndex].progressionRate, 0.001f);
            Assert.AreEqual(1, restoredEye.subStructures[restoredRetinaIndex].causingFactors.Count);
            Assert.AreEqual("外伤", restoredEye.subStructures[restoredRetinaIndex].causingFactors[0]);
            Assert.AreEqual(1, restoredEye.diagnosedConditions.Count);
            DiagnosedCondition restoredCondition = restoredEye.diagnosedConditions[0];
            Assert.AreEqual("retina_damage", restoredCondition.conditionId);
            Assert.AreEqual("视网膜损伤", restoredCondition.displayName);
            Assert.AreEqual(SubStructureType.EyeRetina, restoredCondition.sourceSubStructure);
            Assert.AreEqual(BodyPartType.LeftEye, restoredCondition.sourcePart);
            Assert.AreEqual(321, restoredCondition.diagnosedDay);
            Assert.AreEqual(62.5f, restoredCondition.severityAtDiagnosis, 0.001f);

            BodyPartData restoredHeart = restored.bodyParts[BodyPartType.Heart];
            int restoredValveIndex = restoredHeart.subStructures.FindIndex(s => s.type == SubStructureType.HeartValve);
            Assert.GreaterOrEqual(restoredValveIndex, 0);
            Assert.AreEqual(0.5f, restoredHeart.subStructures[restoredValveIndex].progressionRate, 0.001f);
            Assert.AreEqual(1, restoredHeart.subStructures[restoredValveIndex].causingFactors.Count);
            Assert.AreEqual("风湿热", restoredHeart.subStructures[restoredValveIndex].causingFactors[0]);

            // 未改动的部件保持默认健康
            BodyPartData restoredRightEye = restored.bodyParts[BodyPartType.RightEye];
            Assert.AreEqual(100f, restoredRightEye.currentHp, 0.001f);
            Assert.AreEqual(1f, restoredRightEye.functionLevel, 0.001f);
            Assert.AreEqual(BodyPartState.Healthy, restoredRightEye.state);
        }

        [Test]
        public void LegacyJsonWithoutBodyParts_UsesDefaultStructure()
        {
            const string legacyCharacterJson = @"{
  ""characterId"": 22,
  ""firstName"": ""旧"",
  ""lastName"": ""档""
}";
            CharacterSaveData dto = JsonUtility.FromJson<CharacterSaveData>(legacyCharacterJson);
            Assert.IsNotNull(dto);
            CharacterData c = dto.ToCharacter();
            Assert.IsNotNull(c.bodyParts, "旧档缺 bodyParts 段：应回退 InitializeBodyParts 默认结构");
            Assert.AreEqual(Enum.GetValues(typeof(BodyPartType)).Length, c.bodyParts.Count);

            BodyPartData leftEye = c.bodyParts[BodyPartType.LeftEye];
            Assert.AreEqual(100f, leftEye.currentHp, 0.001f);
            Assert.AreEqual(1f, leftEye.functionLevel, 0.001f);
            Assert.AreEqual(BodyPartState.Healthy, leftEye.state);
            Assert.AreEqual(4, leftEye.subStructures.Count);
            Assert.AreEqual(0, leftEye.diagnosedConditions.Count);
        }

        [Test]
        public void BodyParts_PartialSegment_KeepsDefaultsForMissingPartsAndSubStructures()
        {
            // 只含一个覆盖左眼的部件行、两条子结构行（模拟旧版本存档 / 中间版本部件增减）：
            // 未入档的部件与未覆盖的子结构必须保留 InitializeBodyParts 的默认值。
            var dto = new CharacterSaveData { characterId = 23 };
            dto.bodyPartTypes = new[] { (int)BodyPartType.LeftEye };
            dto.bodyPartMaxHps = new[] { 100f };
            dto.bodyPartCurrentHps = new[] { 55f };
            dto.bodyPartFunctionLevels = new[] { 0.55f };
            dto.bodyPartStates = new[] { (int)BodyPartState.Critical };
            dto.bodyPartSubStructureCounts = new[] { 4 };
            dto.bodyPartDiagnosedConditionCounts = new[] { 0 };
            dto.subStructurePartIndices = new[] { 0, 0 };
            dto.subStructureTypes = new[]
            {
                (int)SubStructureType.EyeLens,
                (int)SubStructureType.EyeRetina
            };
            dto.subStructureConditions = new[] { 90f, 20f };
            dto.subStructureProgressionRates = new[] { 0f, 0f };
            dto.subStructureCausingFactorCounts = new[] { 0, 0 };

            CharacterData c = dto.ToCharacter();
            Assert.IsNotNull(c.bodyParts);
            Assert.AreEqual(Enum.GetValues(typeof(BodyPartType)).Length, c.bodyParts.Count,
                "未入档的部件必须保留默认结构");

            BodyPartData leftEye = c.bodyParts[BodyPartType.LeftEye];
            Assert.AreEqual(55f, leftEye.currentHp, 0.001f);
            Assert.AreEqual(BodyPartState.Critical, leftEye.state);
            Assert.AreEqual(4, leftEye.subStructures.Count, "存档只覆盖 2 个子结构，其余保留默认");
            Assert.AreEqual(90f, leftEye.subStructures.Find(s => s.type == SubStructureType.EyeLens).condition, 0.001f);
            Assert.AreEqual(20f, leftEye.subStructures.Find(s => s.type == SubStructureType.EyeRetina).condition, 0.001f);
            Assert.AreEqual(0f, leftEye.subStructures.Find(s => s.type == SubStructureType.EyeAqueous).condition, 0.001f);

            BodyPartData rightEye = c.bodyParts[BodyPartType.RightEye];
            Assert.AreEqual(100f, rightEye.currentHp, 0.001f);
            Assert.AreEqual(BodyPartState.Healthy, rightEye.state);
            Assert.AreEqual(4, rightEye.subStructures.Count);
        }

        // ===== CharacterBond：管理器级列表的捕获/恢复 =====

        [Test]
        public void Bonds_RoundTripAndNextIdContinuation()
        {
            var manager = new CharacterManager();
            RegisterCharacter(manager, new CharacterData { characterId = 101 });
            RegisterCharacter(manager, new CharacterData { characterId = 102 });
            RegisterCharacter(manager, new CharacterData { characterId = 103 });

            CharacterBond first = manager.CreateBond(101, 102, BondType.SwornBrotherhood);
            first.strength = 73.5f;
            first.establishedDay = 456;
            first.combatBonusWhenTogether = 3.25f;
            first.diplomacyBonus = 2.5f;
            first.stressReduction = 1.75f;
            first.sharedFate = true;
            CharacterBond second = manager.CreateBond(101, 103, BondType.Rivalry);
            second.strength = 12.5f;

            List<CharacterBondSaveData> captured = CharacterBondPersistence.Capture(manager);
            Assert.AreEqual(2, captured.Count);

            // 每条纽带走一次 JsonUtility 往返
            var roundTripped = new List<CharacterBondSaveData>();
            foreach (CharacterBondSaveData dto in captured)
            {
                string json = JsonUtility.ToJson(dto);
                CharacterBondSaveData back = JsonUtility.FromJson<CharacterBondSaveData>(json);
                Assert.IsNotNull(back);
                Assert.AreEqual(dto.bondId, back.bondId);
                Assert.AreEqual(dto.characterAId, back.characterAId);
                Assert.AreEqual(dto.characterBId, back.characterBId);
                Assert.AreEqual(dto.type, back.type);
                Assert.AreEqual(dto.strength, back.strength, 0.001f);
                roundTripped.Add(back);
            }

            var restoredManager = new CharacterManager();
            RegisterCharacter(restoredManager, new CharacterData { characterId = 101 });
            RegisterCharacter(restoredManager, new CharacterData { characterId = 102 });
            RegisterCharacter(restoredManager, new CharacterData { characterId = 103 });
            int restoredCount = CharacterBondPersistence.Restore(restoredManager, roundTripped);
            Assert.AreEqual(2, restoredCount);

            IReadOnlyList<CharacterBond> bonds = restoredManager.GetAllBonds();
            Assert.AreEqual(2, bonds.Count);
            CharacterBond restoredFirst = null;
            foreach (CharacterBond bond in bonds)
                if (bond.bondId == first.bondId) restoredFirst = bond;
            Assert.IsNotNull(restoredFirst, "应恢复 bondId 与原档一致的纽带");
            Assert.AreEqual(101, restoredFirst.characterAId);
            Assert.AreEqual(102, restoredFirst.characterBId);
            Assert.AreEqual(BondType.SwornBrotherhood, restoredFirst.type);
            Assert.AreEqual(73.5f, restoredFirst.strength, 0.001f);
            Assert.AreEqual(456, restoredFirst.establishedDay);
            Assert.AreEqual(3.25f, restoredFirst.combatBonusWhenTogether, 0.001f);
            Assert.AreEqual(2.5f, restoredFirst.diplomacyBonus, 0.001f);
            Assert.AreEqual(1.75f, restoredFirst.stressReduction, 0.001f);
            Assert.IsTrue(restoredFirst.sharedFate);

            // 自增 id 续接到 max(bondId)+1：新建纽带不与恢复的实体撞 id
            int maxRestoredId = first.bondId > second.bondId ? first.bondId : second.bondId;
            CharacterBond created = restoredManager.CreateBond(102, 103, BondType.ComradesInArms);
            Assert.AreEqual(maxRestoredId + 1, created.bondId);
        }

        [Test]
        public void Bonds_MissingCharactersSkipped_NoThrow()
        {
            var manager = new CharacterManager();
            RegisterCharacter(manager, new CharacterData { characterId = 201 });
            RegisterCharacter(manager, new CharacterData { characterId = 202 });

            var savedBonds = new List<CharacterBondSaveData>
            {
                new CharacterBondSaveData
                {
                    bondId = 1,
                    characterAId = 201,
                    characterBId = 202,
                    type = (int)BondType.Romance,
                    strength = 40f
                },
                new CharacterBondSaveData
                {
                    bondId = 2,
                    characterAId = 201,
                    characterBId = 203,
                    type = (int)BondType.OathBond,
                    strength = 60f
                },
                null,
                new CharacterBondSaveData
                {
                    bondId = 0,
                    characterAId = 201,
                    characterBId = 201,
                    type = (int)BondType.BloodBond
                }
            };

            int restored = CharacterBondPersistence.Restore(manager, savedBonds);
            Assert.AreEqual(1, restored, "角色 203 不存在、bondId=0 无效：只应恢复 1 条");
            Assert.AreEqual(1, manager.GetAllBonds().Count);
            Assert.AreEqual(201, manager.GetAllBonds()[0].characterAId);
            Assert.AreEqual(202, manager.GetAllBonds()[0].characterBId);

            // 被跳过的最大 id=2 不计入续接位；有效最大 id=1 → 下一条为 2
            CharacterBond created = manager.CreateBond(201, 201, BondType.MentorBond);
            Assert.AreEqual(2, created.bondId);
        }

        // ===== 测试脚手架 =====

        /// <summary>
        /// 把已有 CharacterData 放进 CharacterManager 的角色字典。正常路径只有 CreateCharacter
        /// （需要内容/遗传初始化且随机），因此这里反射写私有容器，仅测试使用。
        /// </summary>
        private static void RegisterCharacter(CharacterManager manager, CharacterData character)
        {
            FieldInfo charactersField = typeof(CharacterManager).GetField(
                "_characters", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(charactersField, "测试脚手架：CharacterManager._characters 字段应存在");
            var characters = charactersField.GetValue(manager) as Dictionary<int, CharacterData>;
            Assert.IsNotNull(characters, "测试脚手架：CharacterManager._characters 应为角色字典");
            characters[character.characterId] = character;
        }
    }
}
