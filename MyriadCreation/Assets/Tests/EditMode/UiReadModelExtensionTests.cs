using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using MyriadCreation.Content.Definitions;
using MyriadCreation.Core.Contracts;
using MyriadCreation.Core.Data;
using MyriadCreation.Core.Enums;
using MyriadCreation.Simulation.Culture;
using MyriadCreation.Simulation.Faith;
using MyriadCreation.Simulation.FaithHost;
using MyriadCreation.Simulation.State;
using MyriadCreation.Simulation.Systems;
using MyriadCreation.Simulation.WorldState;
using MyriadCreation.World.Generation;
using UnityEngine;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>
    /// UI 4b3：IWorldReadModel 扩展（Faith/Society/SettlementPeople/Culture/Innovation）测试。
    /// 小世界构造 GameWorld+RealmSociety+Characters+InnovationTree；断言聚合可强转、结果与具体 API 一致、null 安全。
    /// </summary>
    public class UiReadModelExtensionTests
    {
        private const int RealmA = 1;
        private const int CultureA = 3;
        private const int FaithA = 12345;
        private const int AnchorA = 7;
        private const int AnchorB = 8;
        private const int RulerId = 100;
        private const int NobleId = 101;
        private const int OtherId = 102;
        private const int InnovationId = 5;
        private const int ObserverRealm = 1;

        private GameObject _go;
        private GameWorld _world;
        private WorldReadModel _model;

        [SetUp]
        public void SetUp()
        {
            _go = new GameObject("UiReadModelExtensionWorld");
            _world = _go.AddComponent<GameWorld>();
            _world.mapWidth = 8;
            _world.mapHeight = 6;
            _world.SetupOptions = WorldSetupOptions.CreateStandard();
            _world.InitializeWorld();

            if (_world.realms == null) _world.realms = new Dictionary<int, RealmData>();
            if (_world.cultures == null) _world.cultures = new Dictionary<int, CultureData>();
            if (_world.settlements == null) _world.settlements = new Dictionary<int, MyriadCreation.World.Settlement.SettlementData>();
            if (_world.anchors == null) _world.anchors = new Dictionary<int, MyriadCreation.World.Anchor.AnchorData>();

            _world.cultures[CultureA] = new CultureData
            {
                cultureId = CultureA,
                cultureName = "Test Culture",
                stage = (GameEnums.CultureStage)2
            };
            _world.realms[RealmA] = new RealmData
            {
                realmId = RealmA,
                realmName = "Realm A",
                primaryCultureId = CultureA,
                stateReligionId = FaithA,
                statePatronSaintId = 555
            };

            InjectSociety();
            InjectCharacters();
            InjectInnovations();
            InjectFaith();
            InjectFactions();
            InjectRegime();
            InjectInnovationCandidatesAndGoods();

            _model = new WorldReadModel(_world);
        }

        [TearDown]
        public void TearDown()
        {
            CharacterKnowledgeRegistry.Clear();
            if (_go != null) UnityEngine.Object.DestroyImmediate(_go);
        }

        private void InjectSociety()
        {
            var society = new RealmSociety
            {
                realmId = RealmA,
                totalPopulation = 1234f,
                unrestScore = 27f,
                dominantClass = GameEnums.SocialClass.Peasant,
                mostRestlessClass = GameEnums.SocialClass.Slave
            };
            society.classes[GameEnums.SocialClass.Peasant] = new ClassProfile
            {
                socialClass = GameEnums.SocialClass.Peasant,
                population = 1000f,
                populationShare = 0.8f,
                satisfaction = 55f,
                loyalty = 60f,
                organization = 0.3f,
                influence = 25f,
                unrest = 33f,
                support = 44f,
                chiefGrievance = ClassNeedDimension.TaxBurden,
                chiefGrievanceReason = "税负过重",
                needReport = new ClassNeedReport
                {
                    dimensions = new Dictionary<ClassNeedDimension, NeedDimensionScore>
                    {
                        [ClassNeedDimension.Subsistence] = new NeedDimensionScore
                        {
                            dimension = ClassNeedDimension.Subsistence,
                            score = 51f,
                            reason = "收成一般"
                        }
                    }
                }
            };

            var politicsField = typeof(GameWorld).GetField("_politicsSimulationSystem",
                BindingFlags.Instance | BindingFlags.NonPublic);
            var politics = politicsField.GetValue(_world);
            Assert.IsNotNull(politics, "InitializeWorld 应创建 PoliticsSimulationSystem");
            var cacheField = typeof(PoliticsSimulationSystem).GetField("_societyCache",
                BindingFlags.Instance | BindingFlags.NonPublic);
            var cache = (Dictionary<int, RealmSociety>)cacheField.GetValue(politics);
            cache[RealmA] = society;
        }

        private void InjectCharacters()
        {
            CharacterManager manager = new CharacterManager();
            typeof(GameWorld).GetField("_characterManager", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(_world, manager);
            var charsField = typeof(CharacterManager).GetField("_characters",
                BindingFlags.Instance | BindingFlags.NonPublic);
            var chars = (Dictionary<int, CharacterData>)charsField.GetValue(manager);

            chars[RulerId] = new CharacterData
            {
                characterId = RulerId,
                firstName = "Alice",
                lastName = "Ruler",
                age = 40,
                isMale = false,
                realmId = RealmA,
                role = CharacterRole.Ruler,
                isPublicFigure = true,
                currentSettlementAnchorId = AnchorA,
                deathDay = -1
            };
            chars[NobleId] = new CharacterData
            {
                characterId = NobleId,
                firstName = "Bob",
                lastName = "Noble",
                age = 30,
                isMale = true,
                realmId = RealmA,
                role = CharacterRole.Noble,
                currentSettlementAnchorId = AnchorA,
                deathDay = -1
            };
            chars[OtherId] = new CharacterData
            {
                characterId = OtherId,
                firstName = "Carol",
                lastName = "Other",
                age = 25,
                isMale = false,
                realmId = RealmA,
                role = CharacterRole.Merchant,
                isPublicFigure = true,
                currentSettlementAnchorId = AnchorB,
                deathDay = -1
            };

            CharacterKnowledgeRegistry.Clear();
            CharacterKnowledgeRegistry.Learn(ObserverRealm, chars[RulerId], 1, "test");
            CharacterKnowledgeRegistry.Learn(ObserverRealm, chars[NobleId], 1, "test");
        }

        private void InjectInnovations()
        {
            InnovationTree tree = _world.GetInnovationTree();
            Assert.IsNotNull(tree, "InitializeWorld 应创建 InnovationTree");
            tree.RegisterInnovation(new InnovationDef
            {
                innovationId = InnovationId,
                innovationName = "Test Innovation"
            });
            var innovationsField = typeof(InnovationTree).GetField("_realmInnovations",
                BindingFlags.Instance | BindingFlags.NonPublic);
            var innovations = (Dictionary<int, HashSet<int>>)innovationsField.GetValue(tree);
            innovations[RealmA] = new HashSet<int> { InnovationId };
        }

        private void InjectFaith()
        {
            var faith = new FaithSystem
            {
                faithId = FaithA,
                faithName = "Test Faith",
                fervor = 70f,
                type = FaithType.Monotheistic,
                orgType = FaithOrganizationType.Episcopal,
                hierarchyLevel = 3,
                highPriestCharacterId = 5,
                churchInfluence = 40f
            };
            faith.doctrines.Add(new Doctrine
            {
                name = "Test Doctrine",
                description = "Test doctrine description",
                category = (DoctrineCategory)0,
                strictness = 0.4f
            });
            var faiths = (List<FaithSystem>)typeof(GameWorld)
                .GetField("_faithSystems", BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(_world);
            faiths.Clear();
            faiths.Add(faith);

            CanonizationSystem.Reset();
            var saintsField = typeof(CanonizationSystem).GetField("_saintsByFaith",
                BindingFlags.Static | BindingFlags.NonPublic);
            var saints = (Dictionary<int, List<SaintDef>>)saintsField.GetValue(null);
            saints[FaithA] = new List<SaintDef>
            {
                new SaintDef
                {
                    saintId = 555,
                    saintName = "Patron Saint",
                    domain = "peace",
                    faithId = FaithA,
                    canonizationPiety = 90f
                }
            };
        }

        private void InjectFactions()
        {
            var politicsField = typeof(GameWorld).GetField("_politicsSimulationSystem",
                BindingFlags.Instance | BindingFlags.NonPublic);
            var politics = (PoliticsSimulationSystem)politicsField.GetValue(_world);
            FactionManager manager = politics.Factions;
            var factionsField = typeof(FactionManager).GetField("_factionsByRealm",
                BindingFlags.Instance | BindingFlags.NonPublic);
            var byRealm = (Dictionary<int, List<Faction>>)factionsField.GetValue(manager);
            byRealm[RealmA] = new List<Faction>
            {
                new Faction
                {
                    factionId = 201,
                    realmId = RealmA,
                    factionName = "Old Guard",
                    stance = FactionStance.Conservative,
                    power = 40f,
                    cohesion = 60f,
                    isInGovernment = true,
                    leaderCharacterId = NobleId,
                    primaryClass = GameEnums.SocialClass.NobilityClergy
                },
                new Faction
                {
                    factionId = 202,
                    realmId = RealmA,
                    factionName = "Reformers",
                    stance = FactionStance.Reformist,
                    power = 20f,
                    cohesion = 40f,
                    isInGovernment = false
                }
            };
        }

        private void InjectRegime()
        {
            RegimeChangeDynamics dynamics = _world.RegimeDynamics;
            var state = new RegimeChangeState
            {
                realmId = RealmA,
                tension = new StructuralTension
                {
                    classMismatch = 50f,
                    fiscalMilitary = 40f,
                    legitimacyErosion = 30f,
                    total = 60f
                },
                institutionalInertia = 5f,
                compositionEstablishedDay = 1,
                history = new List<string> { "第一日 关键节点开启" },
                activeJuncture = new ActiveJuncture
                {
                    type = CriticalJunctureType.EliteSplit,
                    startDay = 1,
                    remainingDays = 30,
                    severity = 55f,
                    resolved = false,
                    outcome = JunctureOutcomeType.Reform
                }
            };
            var statesField = typeof(RegimeChangeDynamics).GetField("_states",
                BindingFlags.Instance | BindingFlags.NonPublic);
            var states = (Dictionary<int, RegimeChangeState>)statesField.GetValue(dynamics);
            states[RealmA] = state;
        }

        private void InjectInnovationCandidatesAndGoods()
        {
            InnovationTree tree = _world.GetInnovationTree();
            tree.RegisterInnovation(new InnovationDef
            {
                innovationId = 6,
                innovationName = "Candidate Innovation"
            });
            tree.RegisterInnovation(new InnovationDef
            {
                innovationId = 7,
                innovationName = "Locked Innovation",
                prerequisiteTree = InnovationPrerequisiteNode.Leaf(6)
            });
            _world.goodsDefs[100] = new GoodsDef
            {
                goodsId = 100,
                goodsName = "Ore",
                requiredInnovation = InnovationId
            };
            _world.goodsDefs[101] = new GoodsDef
            {
                goodsId = 101,
                goodsName = "Secret Ore",
                requiredInnovation = 999
            };
        }

        [Test]
        public void Aggregate_IsAssignableToExtendedUiContracts()
        {
            Assert.IsInstanceOf<IWorldReadModel>(_model);
            Assert.IsInstanceOf<IFaithQuery>(_model);
            Assert.IsInstanceOf<ISocietyQuery>(_model);
            Assert.IsInstanceOf<ISettlementPeopleQuery>(_model);
            Assert.IsInstanceOf<ICultureQuery>(_model);
            Assert.IsInstanceOf<IInnovationQuery>(_model);
            Assert.IsInstanceOf<IFactionQuery>(_model);
            Assert.IsInstanceOf<IRegimeQuery>(_model);
        }

        [Test]
        public void FaithAndCulture_ReuseExistingContracts()
        {
            Assert.AreEqual(FaithA, _model.GetStateReligionId(RealmA));
            Assert.IsTrue(_model.HasFaith(FaithA));
            Assert.AreEqual("Test Faith", _model.GetFaithName(FaithA));
            Assert.AreEqual(70f, _model.GetFervor(FaithA), 0.0001f);
            Assert.AreEqual(0, _model.GetHolySiteTiles(FaithA).Count);
            Assert.IsTrue(_model.TryGetFaithSnapshot(FaithA, out FaithSnapshotDto faith));
            Assert.AreEqual("大主教区制", faith.hierarchyName);

            Assert.AreEqual(CultureA, _model.GetPrimaryCultureId(RealmA));
            Assert.AreEqual("Test Culture", _model.GetCultureName(CultureA));
            Assert.AreEqual(2, _model.GetCultureStage(CultureA));
            Assert.AreNotEqual("Unknown", _model.GetCultureStageKey(2));
            Assert.IsNull(_model.GetSociety(999));
        }

        [Test]
        public void Society_MatchesRealmSociety()
        {
            SocietySnapshotDto society = _model.GetSociety(RealmA);
            Assert.IsNotNull(society);
            Assert.AreEqual(RealmA, society.realmId);
            Assert.AreEqual("Realm A", society.realmName);
            Assert.AreEqual(1234f, society.totalPopulation, 0.0001f);
            Assert.AreEqual(27f, society.unrestScore, 0.0001f);
            Assert.AreEqual((int)GameEnums.SocialClass.Peasant, society.dominantClassId);
            Assert.AreEqual("农民", society.dominantClassName);
            Assert.AreEqual((int)GameEnums.SocialClass.Slave, society.mostRestlessClassId);
            Assert.AreEqual(5, society.classes.Count);

            SocialClassSnapshotDto peasant = null;
            foreach (var item in society.classes)
            {
                if (item.classId == (int)GameEnums.SocialClass.Peasant)
                {
                    peasant = item;
                    break;
                }
            }
            Assert.IsNotNull(peasant);
            Assert.IsTrue(peasant.unlocked);
            Assert.AreEqual(1000f, peasant.population, 0.0001f);
            Assert.AreEqual(0.8f, peasant.populationShare, 0.0001f);
            Assert.AreEqual(55f, peasant.satisfaction, 0.0001f);
            Assert.AreEqual(60f, peasant.loyalty, 0.0001f);
            Assert.AreEqual(33f, peasant.unrest, 0.0001f);
            Assert.AreEqual((int)ClassNeedDimension.TaxBurden, peasant.chiefGrievanceId);
            Assert.AreEqual("税负过重", peasant.chiefGrievanceReason);
            Assert.AreEqual(1, peasant.needScores.Count);
            Assert.AreEqual((int)ClassNeedDimension.Subsistence, peasant.needScores[0].dimensionId);
            Assert.AreEqual("生存保障", peasant.needScores[0].dimensionName);
            Assert.AreEqual(51f, peasant.needScores[0].score, 0.0001f);

            Assert.IsNull(_model.GetSociety(999));
        }

        [Test]
        public void SettlementPeople_ReturnsKnownAndPublicFigures()
        {
            IReadOnlyList<CharacterRowDto> known = _model.GetKnownCharactersAtAnchor(AnchorA, ObserverRealm);
            Assert.AreEqual(2, known.Count);
            Assert.AreEqual(RulerId, known[0].characterId);
            Assert.AreEqual(NobleId, known[1].characterId);

            IReadOnlyList<CharacterRowDto> publicFigures = _model.GetPublicFiguresAtAnchor(AnchorA);
            Assert.AreEqual(1, publicFigures.Count);
            Assert.AreEqual(RulerId, publicFigures[0].characterId);

            Assert.AreEqual(1, _model.GetPublicFiguresAtAnchor(AnchorB).Count);
            Assert.AreEqual(0, _model.GetKnownCharactersAtAnchor(AnchorB, ObserverRealm).Count);
            Assert.AreEqual(0, _model.GetKnownCharactersAtAnchor(-1, ObserverRealm).Count);
            Assert.AreEqual(0, _model.GetPublicFiguresAtAnchor(-1).Count);
        }

        [Test]
        public void Innovation_MatchesInnovationTree()
        {
            Assert.AreEqual(1, _model.GetResearchedCount(RealmA));
            IReadOnlyList<int> ids = _model.GetResearchedInnovationIds(RealmA);
            Assert.AreEqual(1, ids.Count);
            Assert.AreEqual(InnovationId, ids[0]);
            InnovationSnapshotDto snapshot = _model.GetInnovationSnapshot(RealmA);
            Assert.AreEqual(RealmA, snapshot.realmId);
            Assert.AreEqual(1, snapshot.researchedCount);
            Assert.AreEqual(1, snapshot.researchedInnovationIds.Count);
            Assert.IsTrue(_model.HasInnovation(RealmA, InnovationId));
            Assert.IsFalse(_model.HasInnovation(RealmA, 999));
            Assert.AreEqual("Test Innovation", _model.GetInnovationName(InnovationId));
            Assert.AreEqual("", _model.GetInnovationName(int.MaxValue));
            Assert.AreEqual(0, _model.GetResearchedCount(int.MaxValue));
            Assert.AreEqual(0, _model.GetResearchedInnovationIds(int.MaxValue).Count);
        }

        [Test]
        public void FaithPanelFields_AndPatronSaint_AreExposed()
        {
            Assert.IsTrue(_model.TryGetFaithSnapshot(FaithA, out FaithSnapshotDto faith));
            Assert.AreEqual((int)FaithType.Monotheistic, faith.faithType);
            Assert.AreEqual((int)FaithOrganizationType.Episcopal, faith.faithOrganizationType);
            Assert.AreEqual(3, faith.hierarchyLevel);
            Assert.AreEqual("大主教区制", faith.hierarchyName);
            Assert.IsTrue(faith.isStateReligion);
            Assert.GreaterOrEqual(faith.reformPillars.Count, 1);
            Assert.AreEqual(555, _model.GetPatronSaintId(RealmA));
            Assert.AreEqual("Patron Saint", _model.GetPatronSaintName(RealmA));
            Assert.AreEqual(-1, _model.GetPatronSaintId(999));
            Assert.AreEqual("", _model.GetPatronSaintName(999));
        }

        [Test]
        public void Factions_MatchFactionManager()
        {
            IReadOnlyList<FactionSnapshotDto> factions = _model.GetFactions(RealmA);
            Assert.AreEqual(2, factions.Count);
            Assert.AreEqual(201, factions[0].factionId);
            Assert.AreEqual("Old Guard", factions[0].factionName);
            Assert.AreEqual((int)FactionStance.Conservative, factions[0].stanceId);
            Assert.AreEqual("保守派", factions[0].stanceName);
            Assert.AreEqual(40f, factions[0].power, 0.0001f);
            Assert.AreEqual(40f / 60f * 100f, factions[0].share, 0.0001f);
            Assert.IsTrue(factions[0].isRuling);
            Assert.AreEqual(202, factions[1].factionId);
            Assert.IsFalse(factions[1].isRuling);
            Assert.AreEqual(0, _model.GetFactions(999).Count);
        }

        [Test]
        public void Regime_MatchesRegimeChangeState()
        {
            RegimeSnapshotDto regime = _model.GetRegime(RealmA);
            Assert.IsNotNull(regime);
            Assert.AreEqual(RealmA, regime.realmId);
            Assert.AreEqual(60f, regime.tensionTotal, 0.0001f);
            Assert.AreEqual(40f, regime.tensionFiscalMilitary, 0.0001f);
            Assert.IsTrue(regime.hasOpenWindow);
            Assert.AreEqual((int)CriticalJunctureType.EliteSplit, regime.junctureType);
            Assert.AreEqual("精英分裂", regime.junctureName);
            Assert.AreEqual((int)JunctureOutcomeType.Reform, regime.junctureOutcome);
            Assert.AreEqual("改革", regime.junctureOutcomeName);
            Assert.AreEqual(30, regime.junctureRemainingDays);
            Assert.AreEqual(1, regime.activeEvents.Count);
            Assert.GreaterOrEqual(regime.coupRisk, 36f);
            Assert.LessOrEqual(regime.coupRisk, 100f);
            Assert.IsNull(_model.GetRegime(999));
        }

        [Test]
        public void Innovation_AvailabilityAndResourceAccess_AreExposed()
        {
            IReadOnlyList<InnovationAvailabilityDto> available = _model.GetAvailableInnovations(RealmA);
            Assert.GreaterOrEqual(available.Count, 1);
            InnovationAvailabilityDto target = null;
            foreach (InnovationAvailabilityDto item in available)
            {
                if (item.innovationId == 6) { target = item; break; }
            }
            Assert.IsNotNull(target, "可用创新列表应包含注入的 6 号创新");
            Assert.IsFalse(target.researched);
            Assert.IsTrue(target.canResearch);
            Assert.AreEqual(0, target.prerequisiteIds.Count);

            Assert.IsTrue(_model.CanResearch(RealmA, 6));
            Assert.IsFalse(_model.CanResearch(RealmA, 7));
            IReadOnlyList<int> prerequisites = _model.GetPrerequisiteInnovationIds(7);
            Assert.AreEqual(1, prerequisites.Count);
            Assert.AreEqual(6, prerequisites[0]);

            Assert.IsTrue(_model.IsTechAccessibleForGoods(100, RealmA));
            Assert.IsFalse(_model.IsTechAccessibleForGoods(101, RealmA));
            Assert.IsTrue(_model.IsResourceDepthAccessible((int)ResourceAccessDepth.Surface, RealmA));
            Assert.IsFalse(_model.IsResourceDepthAccessible((int)ResourceAccessDepth.Deep, RealmA));
            Assert.IsTrue(_model.CanAccessResource(100, (int)ResourceAccessDepth.Surface, RealmA));
            Assert.IsFalse(_model.CanAccessResource(101, (int)ResourceAccessDepth.Surface, RealmA));

            Assert.IsNotNull(_model.GetAvailableInnovations(999));
            Assert.IsFalse(_model.CanResearch(999, 7));
        }

        [Test]
        public void NullWorld_ExtendedMembersAreSafe()
        {
            var model = new WorldReadModel(null);
            Assert.AreEqual(-1, model.GetStateReligionId(0));
            Assert.IsFalse(model.HasFaith(0));
            Assert.AreEqual("", model.GetFaithName(0));
            Assert.AreEqual(0f, model.GetFervor(0), 0.0001f);
            Assert.AreEqual(0, model.GetHolySiteTiles(0).Count);
            Assert.IsFalse(model.TryGetFaithSnapshot(0, out _));
            Assert.IsNull(model.GetSociety(0));
            Assert.AreEqual(0, model.GetKnownCharactersAtAnchor(0, 0).Count);
            Assert.AreEqual(0, model.GetPublicFiguresAtAnchor(0).Count);
            Assert.AreEqual(-1, model.GetPrimaryCultureId(0));
            Assert.AreEqual("", model.GetCultureName(0));
            Assert.AreEqual(-1, model.GetCultureStage(0));
            Assert.AreEqual("Unknown", model.GetCultureStageKey(0));
            Assert.AreEqual(0, model.GetResearchedCount(0));
            Assert.AreEqual(0, model.GetResearchedInnovationIds(0).Count);
            Assert.AreEqual(0, model.GetInnovationSnapshot(0).researchedCount);
            Assert.IsFalse(model.HasInnovation(0, 0));
            Assert.AreEqual("", model.GetInnovationName(0));
            Assert.AreEqual(0, model.GetFactions(0).Count);
            Assert.IsNull(model.GetRegime(0));
            Assert.AreEqual(0, model.GetAvailableInnovations(0).Count);
            Assert.AreEqual(0, model.GetPrerequisiteInnovationIds(0).Count);
            Assert.IsFalse(model.CanResearch(0, 0));
            Assert.IsFalse(model.IsTechAccessibleForGoods(0, 0));
            Assert.IsFalse(model.CanAccessResource(0, 0, 0));
            Assert.AreEqual(-1, model.GetPatronSaintId(0));
            Assert.AreEqual("", model.GetPatronSaintName(0));
        }
    }
}
