using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using MyriadCreation.Core.Contracts;
using MyriadCreation.Core.Data;
using MyriadCreation.Simulation.State;
using MyriadCreation.Simulation.WorldState;
using MyriadCreation.UI.Common;
using MyriadCreation.UI.Panels;
using MyriadCreation.UI.Presentation;
using MyriadCreation.World;
using MyriadCreation.World.Settlement;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>
    /// B8-4a：UI 去 Simulation 首批解耦回归。
    /// 覆盖：WorldReadModelAdapter 与 GameWorld 一致/空世界安全；UIManager 无 view 不抛；
    /// GovernmentPanel 注入 ILawQuery/ILawCommand 后命令委托到注入实现（不依赖 Unity UI 布局）。
    /// </summary>
    public class UiDecouplingTests
    {
        private readonly List<GameObject> _created = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            foreach (var go in _created)
            {
                if (go != null) Object.DestroyImmediate(go);
            }
            _created.Clear();
        }

        private GameWorld CreateWorld(int width, int height)
        {
            var go = new GameObject("UiDecouplingWorld");
            _created.Add(go);
            var world = go.AddComponent<GameWorld>();
            world.mapWidth = width;
            world.mapHeight = height;
            world.tiles = new TileData[width * height];
            for (int i = 0; i < world.tiles.Length; i++)
            {
                world.tiles[i] = new TileData
                {
                    tileIndex = i,
                    exists = true,
                    isLand = i % 2 == 0,
                };
            }
            world.provinces = new Dictionary<int, Province> { { 0, new Province() } };
            world.settlements = new Dictionary<int, SettlementData> { { 0, new SettlementData() } };
            world.realms = new Dictionary<int, RealmData> { { 0, new RealmData { realmId = 0 } } };
            world.startYear = 3;
            world.startDay = 10;
            world.currentYear = 5;
            world.currentDay = 42;
            return world;
        }

        [Test]
        public void WorldReadModelAdapter_MatchesGameWorld()
        {
            var world = CreateWorld(3, 2);
            var view = new WorldReadModelAdapter(world);

            Assert.AreEqual(3, view.MapWidth);
            Assert.AreEqual(2, view.MapHeight);
            Assert.AreEqual(6, view.TotalTiles);
            // 手工小世界没有 SeaLandGenerator，GameWorld.GetLandTileCount()/GetSeaTileCount() 返回 0；
            // 适配器必须与 GameWorld 查询一致（不能自行另算一套）。
            Assert.AreEqual(world.GetLandTileCount(), view.LandTiles);
            Assert.AreEqual(world.GetSeaTileCount(), view.SeaTiles);
            Assert.AreEqual(1, view.ProvinceCount);
            Assert.AreEqual(1, view.SettlementCount);
            Assert.AreEqual(1, view.RealmCount);
            Assert.AreEqual(5, view.CurrentYear);
            Assert.AreEqual(42, view.CurrentDay);
            Assert.AreEqual(3, view.StartYear);
            Assert.AreEqual(10, view.StartDay);
        }

        [Test]
        public void WorldReadModelAdapter_NullWorld_IsSafe()
        {
            var view = new WorldReadModelAdapter(null);
            Assert.DoesNotThrow(() =>
            {
                _ = view.MapWidth;
                _ = view.MapHeight;
                _ = view.TotalTiles;
                _ = view.LandTiles;
                _ = view.SeaTiles;
                _ = view.ProvinceCount;
                _ = view.SettlementCount;
                _ = view.RealmCount;
                _ = view.CurrentYear;
                _ = view.CurrentDay;
                _ = view.StartYear;
                _ = view.StartDay;
            });
            Assert.AreEqual(0, view.MapWidth);
            Assert.AreEqual(0, view.TotalTiles);
        }

        [Test]
        public void UIManager_WithoutMapStatsView_DoesNotThrow()
        {
            var go = new GameObject("UiDecouplingUIManager");
            _created.Add(go);
            var ui = go.AddComponent<UIManager>();

            Assert.DoesNotThrow(() => ui.SetMapStatsView(null));
        }

        [Test]
        public void UIManager_SetWorldReadModel_UsesViewForTopBar()
        {
            var world = CreateWorld(3, 2);
            var uiGo = new GameObject("UiDecouplingUIManagerView");
            _created.Add(uiGo);
            var ui = uiGo.AddComponent<UIManager>();
            var dateText = CreateText("UiDecouplingDate");
            var mapText = CreateText("UiDecouplingMap");
            SetPrivateField(ui, "dateText", dateText);
            SetPrivateField(ui, "mapInfoText", mapText);

            ui.SetWorldReadModel(new WorldReadModel(world));

            Assert.IsInstanceOf<WorldReadModel>(ui.WorldReadModel);
            Assert.AreEqual("已历 2 年 32 天", dateText.text);
            StringAssert.Contains("地图 3×2", mapText.text);
            StringAssert.Contains("地块 6", mapText.text);
        }

        [Test]
        public void UIManager_WithoutWorldReadModel_FallsBackToDashWithoutThrow()
        {
            var world = CreateWorld(3, 2);
            var uiGo = new GameObject("UiDecouplingUIManagerFallback");
            _created.Add(uiGo);
            var ui = uiGo.AddComponent<UIManager>();
            var dateText = CreateText("UiDecouplingDateFallback");
            var mapText = CreateText("UiDecouplingMapFallback");
            SetPrivateField(ui, "dateText", dateText);
            SetPrivateField(ui, "mapInfoText", mapText);

            Assert.DoesNotThrow(() => ui.SetWorldReadModel(null));
            Assert.AreEqual("-", dateText.text);
            Assert.AreEqual("-", mapText.text);
        }

        [Test]
        public void RealmOverviewText_ContractOverload_UsesView()
        {
            var world = CreateWorld(2, 2);
            var realm = world.realms[0];
            realm.realmName = "测试国";
            realm.treasury = 123f;
            realm.stability = 45f;
            realm.centralization = 0.5f;
            world.currentYear = 9;
            world.startYear = 1;

            var view = new WorldReadModel(world);
            string viaView = RealmOverviewText.Build(view, 0);

            StringAssert.Contains("测试国", viaView);
            StringAssert.Contains("123", viaView);
        }

        [Test]
        public void ClassPanelText_ContractOverload_UsesSocietySnapshot()
        {
            var dto = new SocietySnapshotDto
            {
                realmId = 0,
                realmName = "测试国",
                totalPopulation = 100f,
                unrestScore = 30f,
                dominantClassName = "农民",
                mostRestlessClassName = "农民",
                classes = new List<SocialClassSnapshotDto>
                {
                    new SocialClassSnapshotDto
                    {
                        classId = 3,
                        className = "农民",
                        population = 100f,
                        populationShare = 0.5f,
                        satisfaction = 60f,
                        loyalty = 55f,
                        organization = 0.4f,
                        influence = 20f,
                        unrest = 30f,
                        support = 50f,
                        chiefGrievanceReason = "税负偏重",
                        needScores = new List<ClassNeedScoreDto>
                        {
                            new ClassNeedScoreDto { dimensionId = 2, dimensionName = "税负合理", score = 70f }
                        }
                    }
                }
            };
            var query = new FakeSocietyQuery(dto);

            string text = ClassPanelText.Build(query, 0);

            StringAssert.Contains("农民", text);
            StringAssert.Contains("50.0%", text);
            StringAssert.Contains("需求维度", text);
        }

        [Test]
        public void ClassPanelText_NullSociety_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => ClassPanelText.Build((SocietySnapshotDto)null));
        }

        [Test]
        public void SocietyPanelText_ContractOverload_UsesSocietyQuery()
        {
            var dto = new SocietySnapshotDto
            {
                realmName = "测试国",
                classes = new List<SocialClassSnapshotDto>
                {
                    new SocialClassSnapshotDto { className = "农民", population = 10f, populationShare = 1f }
                }
            };
            var query = new FakeSocietyQuery(dto);

            string text = SocietyPanelText.Build(query, 0);

            StringAssert.Contains("农民", text);
            Assert.DoesNotThrow(() => SocietyPanelText.Build((ISocietyQuery)null, 0));
        }

        [Test]
        public void ReligionPanelText_ContractOverload_UsesFaithQuery()
        {
            var faith = new FakeFaithQuery
            {
                StateReligionId = 3,
                Snapshot = new FaithSnapshotDto { faithId = 3, faithName = "圣光", fervor = 75f, holySiteCount = 2 }
            };

            string text = ReligionPanelText.Build((IFaithQuery)faith, 0, -1);

            StringAssert.Contains("圣光", text);
            StringAssert.Contains("75", text);
            StringAssert.Contains("2 处", text);
        }

        [Test]
        public void WorldReadModel_PeopleAndInnovationQueries_AreSafe()
        {
            var world = CreateWorld(2, 2);
            var view = new WorldReadModel(world);

            Assert.DoesNotThrow(() => _ = view.GetResearchedCount(0));
            Assert.DoesNotThrow(() => _ = view.GetResearchedInnovationIds(0));
            Assert.DoesNotThrow(() => _ = view.GetKnownCharactersAtAnchor(0, 0));
            Assert.DoesNotThrow(() => _ = view.GetPublicFiguresAtAnchor(0));
        }

        [Test]
        public void SettlementPeopleText_DescribeCharacterRow_UsesContractDto()
        {
            var row = new CharacterRowDto
            {
                characterId = 1,
                name = "甲",
                role = 1,
                flags = CharacterRowDto.FlagRuler
            };

            string label = SettlementPeopleText.Describe((IWorldReadModel)null, row);

            StringAssert.Contains("甲", label);
            StringAssert.Contains("领袖", label);
        }

        private TextMeshProUGUI CreateText(string name)
        {
            var go = new GameObject(name);
            _created.Add(go);
            return go.AddComponent<TextMeshProUGUI>();
        }

        [Test]
        public void GovernmentPanel_UsesInjectedLawCommand()
        {
            var world = CreateWorld(2, 2);
            world.PlayerRealmId = 0;
            var panelGo = new GameObject("UiDecouplingGovernmentPanel");
            _created.Add(panelGo);
            var panel = panelGo.AddComponent<GovernmentPanel>();

            var query = new FakeLawQuery();
            var command = new FakeLawCommand();
            panel.SetLawServices(query, command);
            panel.SetWorldReadModel(new WorldReadModel(world));
            Assert.AreSame(query, panel.LawQuery);
            Assert.AreSame(command, panel.LawCommand);

            InvokePrivate(panel, "TryRevokeDecree", "decree-1");

            Assert.AreEqual(1, command.RevokedCount, "撤销法令命令应委托给注入的 ILawCommand");
        }

        [Test]
        public void GovernmentPanel_UsesInjectedDecreeCommand()
        {
            var world = CreateWorld(2, 2);
            world.PlayerRealmId = 0;
            var panelGo = new GameObject("UiDecouplingGovernmentDecreePanel");
            _created.Add(panelGo);
            var panel = panelGo.AddComponent<GovernmentPanel>();
            var decree = new FakeDecreeCommand();

            panel.SetLawServices(new FakeLawQuery(), new FakeLawCommand());
            panel.SetDecreeCommand(decree);
            panel.SetWorldReadModel(new WorldReadModel(world));
            InvokePrivate(panel, "TryEnactDecree", "decree-1", "测试法令");

            Assert.AreEqual(1, decree.IssuedCount, "法令下发命令应委托给注入的 IDecreeCommand");
        }

        [Test]
        public void SettlementDetailPanel_ContractQueryInjection_DoesNotThrow()
        {
            var panelGo = new GameObject("UiDecouplingSettlementPanel");
            _created.Add(panelGo);
            var panel = panelGo.AddComponent<MyriadCreation.UI.Panels.SettlementDetailPanel>();
            var query = new FakeSettlementQuery();

            Assert.DoesNotThrow(() => panel.SetSettlementQuery(query));
            Assert.AreSame(query, panel.SettlementQuery);
            Assert.DoesNotThrow(() => panel.SetSettlementQuery(null));
        }

        [Test]
        public void UI_Source_NoSimulationFamilyUsing()
        {
            string uiRoot = Path.Combine(Application.dataPath, "UI");
            Assert.IsTrue(Directory.Exists(uiRoot), "找不到 Assets/UI：" + uiRoot);

            var offenders = new List<string>();
            foreach (string file in Directory.GetFiles(uiRoot, "*.cs", SearchOption.AllDirectories))
            {
                string text = File.ReadAllText(file);
                if (Regex.IsMatch(text,
                    @"^\s*using\s+MyriadCreation\.(Simulation|Laws|Faith|Culture)(\.|\s*;)",
                    RegexOptions.Multiline))
                {
                    offenders.Add(file.Substring(uiRoot.Length));
                }
            }

            Assert.IsEmpty(offenders, "UI 仍引用 Simulation 家族：" + string.Join(", ", offenders));
        }

        // ---------------- 反射/假实现 ----------------

        private static void SetPrivateField(object target, string field, object value)
        {
            var info = target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(info, "找不到私有字段：" + field);
            info.SetValue(target, value);
        }

        private static void InvokePrivate(object target, string method, params object[] args)
        {
            var info = target.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(info, "找不到私有方法：" + method);
            info.Invoke(target, args);
        }

        private sealed class FakeSocietyQuery : ISocietyQuery
        {
            private readonly SocietySnapshotDto _society;

            public FakeSocietyQuery(SocietySnapshotDto society)
            {
                _society = society;
            }

            public SocietySnapshotDto GetSociety(int realmId) => _society;
        }

        private sealed class FakeFaithQuery : IFaithQuery
        {
            public int StateReligionId = -1;
            public FaithSnapshotDto Snapshot;

            public int GetStateReligionId(int realmId) => StateReligionId;

            public bool HasFaith(int faithId) => Snapshot != null && Snapshot.faithId == faithId;

            public string GetFaithName(int faithId) => Snapshot != null ? Snapshot.faithName : "";

            public float GetFervor(int faithId) => Snapshot != null ? Snapshot.fervor : 0f;

            public IReadOnlyList<int> GetHolySiteTiles(int faithId)
            {
                if (Snapshot == null || Snapshot.holySiteCount <= 0)
                {
                    return new List<int>();
                }
                var list = new List<int>();
                for (int i = 0; i < Snapshot.holySiteCount; i++) list.Add(i);
                return list;
            }

            public bool TryGetFaithSnapshot(int faithId, out FaithSnapshotDto snapshot)
            {
                snapshot = Snapshot;
                return Snapshot != null;
            }

            public bool CanDeclareGreatHolyWar(int realmId) => false;

            public IReadOnlyList<FaithSaintDto> GetSaints(int faithId) => new List<FaithSaintDto>();

            public IReadOnlyList<FaithDoctrineDto> GetDoctrines(int faithId) => new List<FaithDoctrineDto>();

            public int GetPatronSaintId(int realmId) => -1;

            public string GetPatronSaintName(int realmId) => "";
        }

        private sealed class FakeDecreeCommand : IDecreeCommand
        {
            public int IssuedCount;

            public int IssueDecreeOrders(string decreeId, int realmId)
            {
                IssuedCount++;
                return 1;
            }

            public void TickDecreeOrders() { }
        }

        private sealed class FakeSettlementQuery : ISettlementQuery
        {
            public SettlementSnapshotDto GetSettlement(int anchorId) => null;

            public IReadOnlyList<SettlementSnapshotDto> GetSettlementsInRealm(int realmId)
                => new List<SettlementSnapshotDto>();

            public IReadOnlyList<SettlementSnapshotDto> GetSettlementsNearTile(int tileIndex, int radius)
                => new List<SettlementSnapshotDto>();
        }

        private sealed class FakeLawQuery : ILawQuery
        {
            public readonly LawGovernmentViewDto View = new LawGovernmentViewDto();

            public string GetActiveLaw(int realmId, string groupId) => "";

            public IReadOnlyList<PendingLawInfoDto> GetPendingLaws(int realmId) => new List<PendingLawInfoDto>();

            public IReadOnlyList<string> GetKnownLaws(int realmId) => new List<string>();

            public LawGovernmentViewDto GetGovernmentView(int realmId, int currentDay) => View;
        }

        private sealed class FakeLawCommand : ILawCommand
        {
            public int RevokedCount;

            public void EnsureKnownLaws(int realmId) { }

            public int TickPendingLaws(int realmId, int currentDay) => 0;

            public void TickDecreeExpiry(int realmId, int currentDay) { }

            public bool BeginEnactLaw(int realmId, string lawId, int proposerCharacterId, int currentDay, out string reason)
            {
                reason = null;
                return true;
            }

            public bool CancelPendingLaw(int realmId, string groupId, int requesterCharacterId, out string reason)
            {
                reason = null;
                return true;
            }

            public bool EnactDecree(int realmId, string decreeId, int currentDay, out string reason)
            {
                reason = null;
                return true;
            }

            public bool RevokeDecree(int realmId, string decreeId, out string reason)
            {
                RevokedCount++;
                reason = null;
                return true;
            }

            public bool ForceSetLaw(int realmId, string groupId, string lawId, int currentDay, out string reason)
            {
                reason = null;
                return true;
            }
        }
    }
}
