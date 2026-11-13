using System.Collections.Generic;
using NUnit.Framework;
using MyriadCreation.Core.Contracts;
using MyriadCreation.Core.Data;
using MyriadCreation.Simulation.Laws;
using MyriadCreation.Simulation.State;
using MyriadCreation.Simulation.Systems;
using MyriadCreation.Simulation.WorldState;
using MyriadCreation.World.Anchor;
using MyriadCreation.World.Settlement;
using UnityEngine;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>
    /// UI 4c：IDecreeCommand 适配器测试。
    /// 覆盖委托到 GameWorld.IssueDecreeOrders 的计数/状态变化、重复调用现有语义、null 安全。
    /// </summary>
    public class UiCommandContractsTests
    {
        private GameObject _go;
        private GameWorld _world;

        [SetUp]
        public void SetUp()
        {
            _go = new GameObject("UiCommandTestWorld");
            _world = _go.AddComponent<GameWorld>();
            _world.mapWidth = 4;
            _world.mapHeight = 4;
            _world.tiles = new TileData[16];
            for (int i = 0; i < _world.tiles.Length; i++)
            {
                _world.tiles[i] = new TileData
                {
                    tileIndex = i,
                    exists = true,
                    ownerRealmId = 0
                };
            }
            if (_world.realms == null) _world.realms = new Dictionary<int, RealmData>();
            if (_world.settlements == null) _world.settlements = new Dictionary<int, SettlementData>();
            if (_world.anchors == null) _world.anchors = new Dictionary<int, AnchorData>();

            var realm = new RealmData
            {
                realmId = 0,
                realmName = "Command Realm",
                monarchId = -1,
                decreeState = new RealmDecreeState { orders = new List<DecreeOrder>() },
                adminDivisions = new List<AdminDivision>
                {
                    new AdminDivision { divisionId = 1, realmId = 0, level = 1, tiles = new HashSet<int> { 0, 1 } }
                }
            };
            _world.realms[0] = realm;

            // 源聚落控制目标聚落：IssueDecreeOrders 会生成 1 条 target=2 的政令命令。
            AnchorData sourceAnchor = new AnchorData
            {
                anchorId = 1,
                tileIndex = 0,
                controllerAnchorId = -1
            };
            sourceAnchor.controlledAnchorIds.Add(2);
            SettlementData source = new SettlementData
            {
                anchorId = 1,
                siteName = "Source",
                isCapital = true
            };
            source.anchor = sourceAnchor;

            AnchorData targetAnchor = new AnchorData
            {
                anchorId = 2,
                tileIndex = 1,
                controllerAnchorId = 1
            };
            SettlementData target = new SettlementData
            {
                anchorId = 2,
                siteName = "Target"
            };
            target.anchor = targetAnchor;

            _world.anchors[1] = sourceAnchor;
            _world.anchors[2] = targetAnchor;
            _world.settlements[1] = source;
            _world.settlements[2] = target;
        }

        [TearDown]
        public void TearDown()
        {
            if (_go != null) UnityEngine.Object.DestroyImmediate(_go);
        }

        [Test]
        public void IssueDecreeOrders_DelegatesToGameWorld_AndAddsOrders()
        {
            IDecreeCommand command = _world.CreateDecreeCommand();
            Assert.IsNotNull(command);
            Assert.IsNotNull(_world.DecreeCommand);

            RealmData realm = _world.realms[0];
            int before = realm.decreeState.orders.Count;
            int issued = command.IssueDecreeOrders("decree_test", 0);
            Assert.Greater(issued, 0, "应有 1 条以上向下级聚落的政令命令");
            Assert.AreEqual(before + issued, realm.decreeState.orders.Count);

            // 现有语义：同一法令再次下发会再追加命令（非幂等，适配器不改变该行为）。
            int again = command.IssueDecreeOrders("decree_test", 0);
            Assert.Greater(again, 0);
            Assert.AreEqual(before + issued + again, realm.decreeState.orders.Count);
        }

        [Test]
        public void TickDecreeOrders_ChangesPendingState_AndIsRepeatSafe()
        {
            var command = _world.CreateDecreeCommand();
            int issued = command.IssueDecreeOrders("decree_tick", 0);
            Assert.Greater(issued, 0);

            RealmData realm = _world.realms[0];
            var before = realm.decreeState.orders[realm.decreeState.orders.Count - 1];
            Assert.AreEqual(DecreeOrderState.Pending, before.state, "下发后初始应为 Pending");

            Assert.DoesNotThrow(() => command.TickDecreeOrders());
            // 没有 MapActorManager 时现有 Tick 标记 Failed（传令官失联）；这是现有语义，不是适配器新增逻辑。
            Assert.AreEqual(DecreeOrderState.Failed, before.state);

            Assert.DoesNotThrow(() => command.TickDecreeOrders());
        }

        [Test]
        public void DecreeCommand_NullAndInvalidInputs_AreSafe()
        {
            Assert.AreEqual(0, new DecreeCommandAdapter(null).IssueDecreeOrders("decree", 0));
            Assert.DoesNotThrow(() => new DecreeCommandAdapter(null).TickDecreeOrders());

            var command = _world.CreateDecreeCommand();
            Assert.AreEqual(0, command.IssueDecreeOrders(null, 0));
            Assert.AreEqual(0, command.IssueDecreeOrders("", 0));
            Assert.AreEqual(0, command.IssueDecreeOrders("decree", 999));

            GameWorld empty = new GameObject("EmptyDecreeWorld").AddComponent<GameWorld>();
            try
            {
                empty.realms = null;
                empty.tiles = null;
                empty.settlements = null;
                var emptyCommand = empty.CreateDecreeCommand();
                Assert.AreEqual(0, emptyCommand.IssueDecreeOrders("decree", 0));
                Assert.DoesNotThrow(() => emptyCommand.TickDecreeOrders());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(empty.gameObject);
            }
        }
    }
}
