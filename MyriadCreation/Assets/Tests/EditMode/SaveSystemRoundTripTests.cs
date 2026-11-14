using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using MyriadCreation.Core.Constants;
using MyriadCreation.Core.Contracts;
using MyriadCreation.Core.Data;
using MyriadCreation.Core.Enums;
using MyriadCreation.Content.Definitions;
using MyriadCreation.Infrastructure.Save;
using MyriadCreation.Simulation.AI;
using MyriadCreation.Simulation.Economy;
using MyriadCreation.Simulation.State;
using MyriadCreation.Simulation.Systems;
using MyriadCreation.Simulation.Planning;
using MyriadCreation.Simulation.Politics;
using MyriadCreation.Simulation.Units;
using MyriadCreation.Simulation.WorldState;
using MyriadCreation.World;
using MyriadCreation.World.Generation;
using MyriadCreation.World.Settlement;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>
    /// 存读档字段级回归（审计 D：现有全链路测试只比表数量，字段丢失发现不了）。
    /// 覆盖：日历 / 地块 / 省份 / 聚落(人口财富守军市场区划设施) / 军队(士气补给编制) /
    /// 政权金库 / 行政区划 holder / 革新完成状态 / SaveVersion。
    /// </summary>
    public class SaveSystemRoundTripTests
    {
        private const string SaveName = "__roundtrip_field_test";
        private readonly List<GameObject> _objects = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            foreach (var go in _objects)
            {
                if (go != null) Object.DestroyImmediate(go);
            }
            _objects.Clear();
            MapUnitRegistry.Clear();
            CharacterKnowledgeRegistry.Clear();
            RelationRegistry.Clear();
            ContractRegistry.Clear();
            TitleInstanceRegistry.Clear();
            SaveSystem.DeleteSave(SaveName);
        }

        private GameWorld NewWorld()
        {
            var go = new GameObject("RoundTripWorld");
            _objects.Add(go);
            var world = go.AddComponent<GameWorld>();
            world.mapWidth = 48;
            world.mapHeight = 24;
            world.SetupOptions = WorldSetupOptions.CreateStandard();
            world.InitializeWorld();
            world.GenerateTerrain(11);
            return world;
        }

        private static int FirstRealmId(GameWorld world)
        {
            foreach (var key in world.realms.Keys) return key;
            return -1;
        }

        private static SettlementData FirstSettlement(GameWorld world)
        {
            foreach (var s in world.settlements.Values) return s;
            return null;
        }

        [Test]
        public void SaveLoad_FieldLevel_RoundTripsAllRepresentativeState()
        {
            var world = NewWorld();
            int realmId = FirstRealmId(world);
            Assert.GreaterOrEqual(realmId, 0, "前置：应有政权");
            var settlement = FirstSettlement(world);
            Assert.IsNotNull(settlement, "前置：应有聚落");

            // ---- 制造可识别状态 ----
            world.currentYear = 321;
            world.currentDay = 77;

            int modifiedTile = settlement.tileIndex;
            var tile = world.tiles[modifiedTile];
            tile.development = 0.77f;
            tile.stability = 61f;
            world.tiles[modifiedTile] = tile;

            Province province = null;
            foreach (var p in world.provinces.Values) { province = p; break; }
            Assert.IsNotNull(province, "前置：应有省份");
            province.provinceName = "测试省";
            province.centerTileIndex = modifiedTile;

            // D40：人口权威是地块人口块；聚落人口只是聚合快照。
            var populationTile = world.tiles[settlement.tileIndex];
            populationTile.populationBlocks = new List<PopulationBlock>
            {
                new PopulationBlock
                {
                    count = 4321f,
                    raceId = 0,
                    cultureId = -1,
                    faithId = -1,
                    socialClass = GameEnums.SocialClass.Peasant,
                    satisfaction = 60f
                }
            };
            world.tiles[settlement.tileIndex] = populationTile;
            SettlementPopulationSystem.Recompute(world, settlement);
            settlement.wealth = 987f;
            settlement.garrison = 654;
            settlement.constructionTier = 4;
            settlement.settlementLevel = SettlementLevel.LevelIII;
            var market = settlement.GetOrCreateMarket();
            market.capacity = 500f; // 前置：保证库存放得下（仓储块可能尚未建成，容量为 0）
            Assert.IsTrue(market.AddStock(53, 42f), "前置：市场应能入库");
            settlement.districts.Add(new CityDistrict(CityDistrictType.Commercial, "测试东市", 0.3f, 0.4f, 5f, 100f, 12f));
            settlement.facilities.Add(new FacilityData
            {
                facilityType = FacilityType.Wall,
                constructionState = ConstructionState.Complete,
                damageState = FacilityDamageState.Damaged,
                repairProgress = 40f
            });

            var army = world.CreateArmy(realmId, -1, modifiedTile);
            army.morale = 61f;
            army.supply = 44f;
            army.organization = 78f;
            army.training = 55f;
            army.unitCounts[100] = 3f;
            army.moveTargetTile = modifiedTile;
            army.moveProgress = 0.25f;

            world.PlayerRealmId = realmId;
            var realm = world.realms[realmId];
            realm.treasury = 12345f;
            realm.primaryTitleKey = "title_roundtrip";
            realm.heirId = realm.monarchId >= 0 ? realm.monarchId : 4242;
            realm.realmForm = RealmForm.Nomadic;
            realm.nomadicSubtype = NomadicSubtype.Steppe;
            int primaryCultureId = -1;
            foreach (int cultureId in world.cultures.Keys) { primaryCultureId = cultureId; break; }
            Assert.GreaterOrEqual(primaryCultureId, 0, "前置：应有文化");
            realm.primaryCultureId = primaryCultureId;
            realm.successionLawFromCulture = false;
            realm.senateSeats = 42;

            TitleInstanceRegistry.Clear();
            var chronicle = world.GetChronicle();
            chronicle.CurrentTick = 900;
            chronicle.CurrentYear = 321;
            int chronicleEntryId = chronicle.Add("roundtrip", "编年史往返", true, realmId).entryId;
            var title = TitleInstanceRegistry.GetOrCreate("title_save_roundtrip");
            title.definitionId = "realm_title";
            title.TransferSenior(realm.monarchId >= 0 ? realm.monarchId : 4242, 77, 66f);
            title.AddHolding(new TitleHolding
            {
                characterId = 7,
                role = TitleHolderRole.CoRuler,
                share = 0.25f,
                sinceDay = 10,
                endDay = 50,
                legitimacy = 33f
            });

            var diplomacy = world.GetDiplomacyManager();
            Assert.IsNotNull(diplomacy, "前置：外交管理器应存在");
            int otherRealmId = -1;
            foreach (int key in world.realms.Keys)
            {
                if (key == realmId) continue;
                otherRealmId = key;
                break;
            }
            Assert.GreaterOrEqual(otherRealmId, 0, "前置：应有第二个政权");
            DiplomaticRelation relation = diplomacy.GetOrCreateRelation(realmId, otherRealmId);
            relation.relation = 77f;
            relation.trust = 88f;
            relation.threat = 66f;
            relation.isAtWar = true;
            relation.hostilityLevel = 91f;
            relation.activeAlliances.Add(new Alliance
            {
                type = AllianceType.DefensiveAlliance,
                realmAId = realmId,
                realmBId = otherRealmId,
                signedDay = 77,
                durationDays = 365,
                isActive = true,
                mutualDefense = true,
                relationRequirement = 30f
            });
            relation.casusBelliList.Add(new CasusBelli
            {
                cbId = 555,
                type = 0,
                holderRealmId = realmId,
                targetRealmId = otherRealmId,
                generatedDay = 10,
                expiryDay = 100,
                description = "测试宣战理由",
                justificationStrength = 0.5f,
                relatedTileIndex = modifiedTile
            });
            relation.activeTreaties.Add(new Treaty
            {
                treatyId = 901,
                treatyName = "测试条约",
                signerAId = realmId,
                signerBId = otherRealmId,
                signedDay = 77,
                isActive = true
            });
            Subordination subordination = diplomacy.EstablishSubordination(realmId, otherRealmId, SubordinationType.Vassal);
            Assert.IsNotNull(subordination, "前置：从属应建立");
            subordination.tributeRatio = 0.125f;
            subordination.autonomy = 0.4f;

            var plan = world.Plans.CreatePlan(PlanType.Research, realmId, 600, 100,
                "研究计划", "测试计划", 30f, "roundtrip", "innovation");
            Assert.IsTrue(world.Plans.AcceptPlan(plan.planId), "前置：计划应接受");
            Assert.IsTrue(world.Plans.BeginPreparation(plan.planId), "前置：计划应进入准备");
            Assert.IsTrue(world.Plans.BeginExecution(plan.planId), "前置：计划应进入执行");
            plan.ownerId = realmId;
            plan.progress = 0.35f;
            plan.elapsedDays = 7f;
            plan.lastStateChangeDay = 100;
            plan.participants.Add(new PlanParticipant(4242, "scholar"));
            plan.requirements.Add(new PlanRequirement("manpower", 12f));
            plan.activity.Set("study", "study_def", "executing");
            plan.currentActivity = "study";
            plan.waitCondition = new SimulationWaitCondition("has_library", "需要图书馆", true);
            plan.isWaiting = true;
            plan.resultCode = "partial";
            plan.resultSummary = "测试中间态";

            var aiManager = world.GetAIManager();
            Assert.IsNotNull(aiManager, "前置：AI 管理器应存在");
            AIController aiController = aiManager.GetController(realmId) ?? aiManager.CreateController(realmId);
            aiController.RestoreState(new AIControllerSaveData
            {
                realmId = realmId,
                personality = new AIPersonality
                {
                    personalityName = "测试AI",
                    expansionBias = 0.7f,
                    economicBias = 0.2f,
                    diplomaticBias = 0.3f,
                    militaryBias = 0.4f,
                    aggression = 0.6f,
                    riskTolerance = 0.5f,
                    researchMultiplier = 1.2f,
                    preferredDomains = new List<InnovationDomain> { (InnovationDomain)0 },
                    fixedArchetype = true
                },
                decisionTimer = 12.5f,
                currentGoal = (int)AIGoal.ExpandTerritory,
                targetRealmId = otherRealmId,
                targetTileIndex = modifiedTile,
                raidCooldown = 9
            });

            InnovationKnowledgeSystem knowledge = world.ResearchPlans.Knowledge;
            Assert.IsTrue(knowledge.SetMastery(4242, 101, 2), "前置：革新掌握应写入");
            Assert.AreEqual(3.5f, knowledge.RecordPractice(4242, 101, 3.5f), 0.001f, "前置：练习量应写入");

            var judicialSystem = world.GetOrCreateJudicialSystem(realmId);
            Assert.IsNotNull(judicialSystem, "前置：司法系统应存在");
            judicialSystem.judicialSystemName = "测试司法";
            judicialSystem.judicialEfficiency = 0.77f;
            judicialSystem.corruption = 0.11f;
            judicialSystem.lawEnforcement = 0.66f;
            judicialSystem.severity = 0.44f;
            judicialSystem.useCapitalPunishment = false;
            judicialSystem.useCorporalPunishment = false;
            judicialSystem.useExile = false;
            judicialSystem.useSlavery = false;

            var division = new AdminDivision
            {
                divisionId = 9001,
                realmId = realmId,
                level = 1,
                name = "测试国",
                titleId = "title_ruler",
                holderCharacterId = 42
            };
            world.realms[realmId].adminDivisions.Add(division);

            // 完成一项革新（如可研究）
            int researchedInnovation = -1;
            var available = world.Innovations.GetAvailableInnovations(realmId, world);
            if (available != null)
            {
                foreach (var def in available)
                {
                    if (def == null) continue;
                    world.Innovations.AddPractice(realmId, def.field, InnovationTree.GetRequiredPractice(def));
                    if (!world.Innovations.StartResearch(realmId, def.innovationId, world)) continue;
                    world.Innovations.DailyTick(realmId, def.researchCost + 1f);
                    if (world.Innovations.HasInnovation(realmId, def.innovationId))
                    {
                        researchedInnovation = def.innovationId;
                        break;
                    }
                }
            }

            settlement.environmentalPressureDays = 777;
            settlement.lastDeclineCause = "water_loss";
            world.CityFoundedCount = 3;
            world.CityCollapsedCount = 2;
            world.RiverAvulsionCount = 4;
            world.LastCityDeclineCause = "water_loss";
            world.Innovations.AddPractice(realmId, InnovationField.Agriculture, 12.5f);
            float practiceBeforeSave = world.Innovations.GetPractice(realmId, InnovationField.Agriculture);

            // ---- 存盘 ----
            Assert.IsTrue(SaveSystem.SaveGame(world, SaveName), "存盘应成功");

            // 版本写入校验
            string path = Path.Combine(SaveSystem.GetSaveDirectory(), SaveName + ".sav");
            Assert.IsTrue(File.Exists(path), "存档文件应存在");
            var raw = JsonUtility.FromJson<SaveData>(File.ReadAllText(path));
            Assert.IsNotNull(raw);
            Assert.AreEqual(GameConstants.SaveVersion, raw.version, "存档版本应为当前 SaveVersion");

            // ---- 读档并逐字段断言 ----
            var loaded = SaveSystem.LoadGame(SaveName);
            Assert.IsNotNull(loaded, "读档应返回世界");
            _objects.Add(loaded.gameObject);

            Assert.AreEqual(321, loaded.currentYear, "年");
            Assert.AreEqual(77, loaded.currentDay, "日");
            Assert.AreEqual(0.77f, loaded.tiles[modifiedTile].development, 0.001f, "地块发展度");
            Assert.AreEqual(61f, loaded.tiles[modifiedTile].stability, 0.001f, "地块稳定度");

            Province loadedProvince = null;
            foreach (var p in loaded.provinces.Values) { loadedProvince = p; break; }
            Assert.IsNotNull(loadedProvince, "省份应还原");
            Assert.AreEqual("测试省", loadedProvince.provinceName, "省份名");
            Assert.AreEqual(modifiedTile, loadedProvince.centerTileIndex, "省中心");

            Assert.IsTrue(loaded.settlements.TryGetValue(settlement.anchorId, out var loadedSettlement), "聚落应还原");
            Assert.AreEqual(4321f, loadedSettlement.population, 0.01f, "聚落人口");
            Assert.AreEqual(987f, loadedSettlement.wealth, 0.01f, "聚落财富");
            Assert.AreEqual(777, loadedSettlement.environmentalPressureDays, "环境压力应入档");
            Assert.AreEqual("water_loss", loadedSettlement.lastDeclineCause, "衰退原因应入档");
            Assert.AreEqual(3, loaded.CityFoundedCount, "城市建立计数应入档");
            Assert.AreEqual(2, loaded.CityCollapsedCount, "城市死亡计数应入档");
            Assert.AreEqual(4, loaded.RiverAvulsionCount, "改道计数应入档");
            Assert.AreEqual("water_loss", loaded.LastCityDeclineCause, "最近死因应入档");
            Assert.AreEqual(practiceBeforeSave, loaded.Innovations.GetPractice(realmId, InnovationField.Agriculture), 0.001f, "政权实践应入档");
            Assert.AreEqual(654, loadedSettlement.garrison, "聚落守军");
            Assert.AreEqual(4, loadedSettlement.constructionTier, "建造等级");
            Assert.AreEqual(SettlementLevel.LevelIII, loadedSettlement.settlementLevel, "聚落等级");
            Assert.AreEqual(42f, loadedSettlement.market.GetStock(53), 0.01f, "市场库存");
            Assert.AreEqual(1, loadedSettlement.districts.Count, "城市区划");
            Assert.AreEqual("测试东市", loadedSettlement.districts[0].districtName);
            Assert.AreEqual(1, loadedSettlement.facilities.Count, "设施");
            Assert.AreEqual(40f, loadedSettlement.facilities[0].repairProgress, 0.01f, "设施修复进度");

            Assert.IsTrue(loaded.armies.TryGetValue(army.armyId, out var loadedArmy), "军队应还原");
            Assert.AreEqual(61f, loadedArmy.morale, 0.01f, "军队士气");
            Assert.AreEqual(44f, loadedArmy.supply, 0.01f, "军队补给");
            Assert.AreEqual(78f, loadedArmy.organization, 0.01f, "军队组织");
            Assert.AreEqual(55f, loadedArmy.training, 0.01f, "军队训练");
            Assert.AreEqual(3f, loadedArmy.unitCounts[100], 0.001f, "军队编制");
            Assert.AreEqual(modifiedTile, loadedArmy.currentTileIndex, "军队位置");
            Assert.AreEqual(modifiedTile, loadedArmy.moveTargetTile, "军队行军目标");
            Assert.AreEqual(0.25f, loadedArmy.moveProgress, 0.001f, "军队行军进度");

            Assert.AreEqual(realmId, loaded.PlayerRealmId, "玩家政权");
            Assert.IsTrue(loaded.realms.TryGetValue(realmId, out var loadedRealm), "政权应还原");
            Assert.AreEqual(12345f, loadedRealm.treasury, 0.01f, "国库");
            Assert.AreEqual("title_roundtrip", loadedRealm.primaryTitleKey, "主头衔");
            Assert.AreEqual(realm.heirId, loadedRealm.heirId, "继承人");
            Assert.AreEqual(RealmForm.Nomadic, loadedRealm.realmForm, "政权形态");
            Assert.AreEqual(NomadicSubtype.Steppe, loadedRealm.nomadicSubtype, "游牧亚型");
            Assert.AreEqual(primaryCultureId, loadedRealm.primaryCultureId, "主体文化");
            Assert.IsFalse(loadedRealm.successionLawFromCulture, "继承法来源");
            Assert.AreEqual(42, loadedRealm.senateSeats, "议会席位");

            var loadedChronicle = loaded.GetChronicle();
            Assert.AreEqual(900, loadedChronicle.CurrentTick, "编年史 tick");
            Assert.AreEqual(321, loadedChronicle.CurrentYear, "编年史 year");
            ChronicleEntry loadedEntry = loadedChronicle.GetEntries().Find(e => e.entryId == chronicleEntryId);
            Assert.IsNotNull(loadedEntry, "编年史条目应保留原 entryId");
            Assert.AreEqual("编年史往返", loadedEntry.description, "编年史描述");
            Assert.IsTrue(loadedEntry.major, "编年史重大标记");
            CollectionAssert.Contains(loadedEntry.participants, realmId, "编年史参与者");

            Assert.IsTrue(TitleInstanceRegistry.TryGet("title_save_roundtrip", out TitleInstance loadedTitle),
                "头衔实例应还原");
            Assert.AreEqual("realm_title", loadedTitle.definitionId, "头衔模板");
            Assert.AreEqual(2, loadedTitle.holders.Count, "头衔持有数");
            Assert.AreEqual(TitleHolderRole.Sole, loadedTitle.holders[0].role, "主持有角色");
            Assert.AreEqual(66f, loadedTitle.holders[0].legitimacy, 0.001f, "主持有合法性");
            Assert.AreEqual(TitleHolderRole.CoRuler, loadedTitle.holders[1].role, "共治持有角色");
            Assert.AreEqual(50, loadedTitle.holders[1].endDay, "旧持有结束日");

            var loadedDiplomacy = loaded.GetDiplomacyManager();
            Assert.IsNotNull(loadedDiplomacy, "外交管理器应重建");
            DiplomaticRelation loadedRelation = loadedDiplomacy.GetRelation(realmId, otherRealmId);
            Assert.IsNotNull(loadedRelation, "外交关系应还原");
            Assert.AreEqual(77f, loadedRelation.relation, 0.001f, "关系值");
            Assert.AreEqual(88f, loadedRelation.trust, 0.001f, "信任");
            Assert.AreEqual(66f, loadedRelation.threat, 0.001f, "威胁");
            Assert.IsTrue(loadedRelation.isAtWar, "战争状态");
            Assert.AreEqual(91f, loadedRelation.hostilityLevel, 0.001f, "敌意");
            Assert.AreEqual(1, loadedRelation.activeAlliances.Count, "同盟数");
            Assert.AreEqual(AllianceType.DefensiveAlliance, loadedRelation.activeAlliances[0].type, "同盟类型");
            Assert.AreEqual(1, loadedRelation.casusBelliList.Count, "CB 数");
            Assert.AreEqual(555, loadedRelation.casusBelliList[0].cbId, "CB id");
            Assert.AreEqual(1, loadedRelation.activeTreaties.Count, "条约数");
            Assert.AreEqual(901, loadedRelation.activeTreaties[0].treatyId, "条约 id");
            Subordination loadedSubordination = loadedDiplomacy.GetSubordination(realmId, otherRealmId);
            Assert.IsNotNull(loadedSubordination, "从属应还原");
            Assert.AreEqual(SubordinationType.Vassal, loadedSubordination.type, "从属类型");
            Assert.AreEqual(0.125f, loadedSubordination.tributeRatio, 0.001f, "贡赋比例");
            Assert.AreEqual(0.4f, loadedSubordination.autonomy, 0.001f, "自治度");
            Assert.IsTrue(ReferenceEquals(loadedRelation.subordination, loadedSubordination),
                "关系与从属表应指向同一对象");

            Assert.AreEqual(77, loaded.Plans.CurrentDay, "计划系统当前日");
            Assert.IsTrue(loaded.Plans.TryGetPlan(plan.planId, out Plan loadedPlan), "计划应还原");
            Assert.AreEqual(PlanType.Research, loadedPlan.type, "计划类型");
            Assert.AreEqual(PlanState.Executing, loadedPlan.state, "计划状态");
            Assert.AreEqual(PlanPhase.Execution, loadedPlan.phase, "计划阶段");
            Assert.AreEqual(realmId, loadedPlan.ownerId, "计划 owner");
            Assert.AreEqual(0.35f, loadedPlan.progress, 0.001f, "计划进度");
            Assert.AreEqual(7f, loadedPlan.elapsedDays, 0.001f, "计划已历天数");
            Assert.AreEqual(100, loadedPlan.lastStateChangeDay, "计划状态变更日");
            Assert.AreEqual(1, loadedPlan.participants.Count, "计划参与者");
            Assert.AreEqual(4242, loadedPlan.participants[0].characterId, "参与者 id");
            Assert.AreEqual("scholar", loadedPlan.participants[0].role, "参与者角色");
            Assert.AreEqual(1, loadedPlan.requirements.Count, "计划需求");
            Assert.AreEqual("manpower", loadedPlan.requirements[0].key, "需求键");
            Assert.AreEqual(12f, loadedPlan.requirements[0].amount, 0.001f, "需求量");
            Assert.AreEqual("study", loadedPlan.currentActivity, "当前活动");
            Assert.AreEqual("study_def", loadedPlan.activity.definitionId, "活动定义");
            Assert.AreEqual("has_library", loadedPlan.waitCondition.ConditionId, "等待条件");
            Assert.IsTrue(loadedPlan.isWaiting, "等待标记");
            Assert.AreEqual("partial", loadedPlan.resultCode, "结果码");
            Assert.AreEqual("测试中间态", loadedPlan.resultSummary, "结果摘要");

            AIController loadedAI = loaded.GetAIManager()?.GetController(realmId);
            Assert.IsNotNull(loadedAI, "AI 控制器应还原");
            Assert.AreEqual("测试AI", loadedAI.personality.personalityName, "AI 人格名");
            Assert.AreEqual(0.7f, loadedAI.personality.expansionBias, 0.001f, "AI 扩张偏好");
            Assert.AreEqual(12.5f, loadedAI.DecisionTimer, 0.001f, "AI 决策计时");
            Assert.AreEqual(AIGoal.ExpandTerritory, loadedAI.GetCurrentGoal(), "AI 当前目标");
            Assert.AreEqual(otherRealmId, loadedAI.GetTargetRealm(), "AI 目标政权");
            Assert.AreEqual(modifiedTile, loadedAI.TargetTileIndex, "AI 目标地块");
            Assert.AreEqual(9, loadedAI.RaidCooldown, "AI 掠袭冷却");
            Assert.AreEqual(1, loadedAI.personality.preferredDomains.Count, "AI 偏好领域");

            InnovationKnowledgeSystem loadedKnowledge = loaded.ResearchPlans.Knowledge;
            Assert.AreEqual(2, loadedKnowledge.GetMastery(4242, 101), "革新掌握等级");
            Assert.AreEqual(3.5f, loadedKnowledge.GetPractice(4242, 101), 0.001f, "革新练习量");

            var loadedJudicial = loaded.GetOrCreateJudicialSystem(realmId);
            Assert.IsNotNull(loadedJudicial, "司法系统应还原");
            Assert.AreEqual("测试司法", loadedJudicial.judicialSystemName, "司法名称");
            Assert.AreEqual(0.77f, loadedJudicial.judicialEfficiency, 0.001f, "司法效率");
            Assert.AreEqual(0.11f, loadedJudicial.corruption, 0.001f, "司法腐败");
            Assert.AreEqual(0.66f, loadedJudicial.lawEnforcement, 0.001f, "执法力度");
            Assert.AreEqual(0.44f, loadedJudicial.severity, 0.001f, "严苛度");
            Assert.IsFalse(loadedJudicial.useCapitalPunishment, "死刑开关");
            Assert.IsFalse(loadedJudicial.useCorporalPunishment, "体刑开关");
            Assert.IsFalse(loadedJudicial.useExile, "流放开关");
            Assert.IsFalse(loadedJudicial.useSlavery, "奴役开关");

            AdminDivision loadedDivision = null;
            foreach (var d in loadedRealm.adminDivisions)
            {
                if (d.divisionId == 9001) { loadedDivision = d; break; }
            }
            Assert.IsNotNull(loadedDivision, "行政区划应还原");
            Assert.AreEqual("title_ruler", loadedDivision.titleId, "区划头衔");
            Assert.AreEqual(42, loadedDivision.holderCharacterId, "区划 holder");

            Assert.Greater(researchedInnovation, 0, "前置：应能完成一项革新研究（否则说明内容/前置条件异常）");
            Assert.IsTrue(loaded.Innovations.HasInnovation(realmId, researchedInnovation),
                "已完成的革新应还原（SaveVersion 14）");
        }
    }
}
