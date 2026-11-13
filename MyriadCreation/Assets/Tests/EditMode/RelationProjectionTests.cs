using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using MyriadCreation.Core.Data;
using MyriadCreation.Core.Enums;
using MyriadCreation.Simulation.Actors;
using MyriadCreation.Simulation.Systems;
using MyriadCreation.Simulation.State;
using MyriadCreation.Simulation.WorldState;
using MyriadCreation.World.Anchor;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>旧权威→关系投影、端点校验、营地锚点。</summary>
    public class RelationProjectionTests
    {
        private static GameWorld MinimalWorld()
        {
            var world = new GameObject("ProjectionWorld").AddComponent<GameWorld>();
            world.mapWidth = 1;
            world.mapHeight = 1;
            world.tiles = new[] { new TileData { exists = true, isLand = true, ownerRealmId = 1, occupyingRealmId = 2 } };
            world.realms = new Dictionary<int, RealmData>();
            world.anchors = new Dictionary<int, AnchorData>();
            world.armies = new Dictionary<int, MyriadCreation.Simulation.State.Army>();
            world.realms[1] = new RealmData { realmId = 1 };
            world.realms[2] = new RealmData { realmId = 2, suzerainId = 1 };
            world.realms[1].vassalIds.Add(2);
            world.anchors[10] = new AnchorData { anchorId = 10, tileIndex = 0, provinceId = 0 };
            return world;
        }

        [Test]
        public void Projection_UsesLegacyAuthorities_ReadOnly()
        {
            var world = MinimalWorld();
            Assert.AreEqual(2, world.realms.Count, "realms setup");
            Assert.AreEqual(1, world.anchors.Count, "anchors setup");
            Assert.AreEqual(1, world.realms[2].suzerainId, "suzerain setup");
            var projected = RelationProjection.Build(world);
            Assert.Greater(projected.Count, 0, "projection count");

            Assert.IsTrue(projected.Exists(r => r.type == RelationType.Vassalage
                && r.from.id == 1 && r.to.id == 2), "附庸投影");
            Assert.IsTrue(projected.Exists(r => r.type == RelationType.DirectRule
                && r.from.id == 1 && r.to.kind == EntityKind.Anchor && r.to.id == 10), "领属投影");
            Assert.IsTrue(projected.Exists(r => r.type == RelationType.MilitaryOccupation
                && r.from.id == 2), "占领投影");
            Assert.IsTrue(projected.TrueForAll(r => r.isProjection), "全部标记为投影");
            Assert.AreEqual(0, RelationRegistry.Capture().Count, "投影不写注册表");
        }

        [Test]
        public void EndpointValidator_DropsDanglingRelations()
        {
            var world = MinimalWorld();
            var relations = new List<EntityRelation>
            {
                new EntityRelation
                {
                    from = new EntityRef(EntityKind.Realm, 1),
                    to = new EntityRef(EntityKind.Anchor, 10),
                    type = RelationType.DirectRule,
                },
                new EntityRelation
                {
                    from = new EntityRef(EntityKind.Realm, 1),
                    to = new EntityRef(EntityKind.Anchor, 999),
                    type = RelationType.DirectRule,
                },
                new EntityRelation
                {
                    from = new EntityRef(EntityKind.Realm, 404),
                    to = new EntityRef(EntityKind.Realm, 1),
                    type = RelationType.Vassalage,
                },
            };

            var valid = RelationEndpointValidator.Filter(world, relations, out var dropped);
            Assert.AreEqual(1, valid.Count);
            Assert.AreEqual(2, dropped.Count);
        }

        [Test]
        public void CampEstablish_AssignsAnchor()
        {
            var world = new GameObject("CampWorld").AddComponent<GameWorld>();
            world.mapWidth = 1;
            world.mapHeight = 1;
            world.tiles = new[] { new TileData { exists = true, isLand = true, provinceId = 3 } };
            world.anchors = new Dictionary<int, AnchorData>();
            var manager = new CampManager(world);

            var camp = manager.EstablishCamp(CampType.Temporary, 0, 20, 1);
            Assert.IsNotNull(camp);
            Assert.GreaterOrEqual(camp.anchorId, 0, "营地必须有锚点");
            Assert.IsTrue(world.anchors.ContainsKey(camp.anchorId));
            Assert.AreEqual(0, world.anchors[camp.anchorId].tileIndex);
        }
    }
}
