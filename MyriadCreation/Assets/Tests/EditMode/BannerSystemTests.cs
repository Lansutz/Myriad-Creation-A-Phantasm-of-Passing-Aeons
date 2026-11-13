using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using MyriadCreation.Infrastructure.Save;
using MyriadCreation.Simulation.State;
using MyriadCreation.Simulation.WorldState;
using MyriadCreation.World;
using MyriadCreation.World.Generation;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>
    /// P3.4 政权旗帜：无旗/军旗/家旗/国族旗是四种合法状态；
    /// 家旗要家族有纹章，国族旗要民族主义类革新；生成必须确定性。
    /// </summary>
    public class BannerSystemTests
    {
        private readonly List<GameObject> _objects = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            foreach (var go in _objects)
            {
                if (go != null) Object.DestroyImmediate(go);
            }
            _objects.Clear();
        }

        private GameWorld NewWorld()
        {
            var go = new GameObject("BannerWorld");
            _objects.Add(go);
            var world = go.AddComponent<GameWorld>();
            world.mapWidth = 48;
            world.mapHeight = 24;
            world.SetupOptions = WorldSetupOptions.CreateStandard();
            world.InitializeWorld();
            world.GenerateTerrain(97);
            return world;
        }

        private static RealmData FirstRealm(GameWorld world)
        {
            foreach (var realm in world.realms.Values) return realm;
            return null;
        }

        [Test]
        public void RealmWithRuler_GetsMilitaryBannerByDefault()
        {
            var world = NewWorld();
            RealmData realm = FirstRealm(world);
            Assert.IsNotNull(realm);

            BannerKind kind = BannerSystem.Ensure(realm, world.Characters, world.Innovations);

            Assert.AreEqual(BannerKind.MilitaryBanner, kind, "早期政权的默认旗帜是军旗，而不是国旗");
            StringAssert.StartsWith("MilitaryBanner|", realm.bannerPattern);
            Assert.IsNotEmpty(realm.bannerColors);
        }

        [Test]
        public void HouseholdWithArms_GetsHouseholdBanner()
        {
            var world = NewWorld();
            RealmData realm = FirstRealm(world);
            CharacterData ruler = world.Characters.GetCharacter(realm.GetSupremeRulerId());
            Assert.IsNotNull(ruler);
            FamilyNode family = world.Characters.GetFamily(ruler.familyId);
            Assert.IsNotNull(family);
            family.coaPattern = "perPale|bend|heater";
            family.coaColors = "#d4af37|#b23a3a|#6b2f3a";

            BannerKind kind = BannerSystem.Ensure(realm, world.Characters, world.Innovations);

            Assert.AreEqual(BannerKind.HouseholdBanner, kind, "有纹章的统治家族才有家旗");
            StringAssert.StartsWith("HouseholdBanner|", realm.bannerPattern);
        }

        [Test]
        public void Nationalism_GetsStateFlagOnlyForSedentaryRealm()
        {
            var world = NewWorld();
            RealmData realm = FirstRealm(world);
            realm.realmForm = MyriadCreation.Simulation.Systems.RealmForm.Sedentary;
            Assert.IsTrue(world.Innovations.GrantInnovation(realm.realmId, BannerSystem.NationalismInnovationId),
                "前置：内容表应有民族主义革新 1017");

            BannerKind kind = BannerSystem.Ensure(realm, world.Characters, world.Innovations);

            Assert.AreEqual(BannerKind.StateFlag, kind, "获得民族主义革新的定居政体才有国族旗");
            StringAssert.StartsWith("StateFlag|", realm.bannerPattern);
        }

        [Test]
        public void RealmWithoutRuler_HasNoBanner()
        {
            var realm = new RealmData { realmId = 0, monarchId = -1, consulId = -1 };

            BannerKind kind = BannerSystem.Ensure(realm, null, null);

            Assert.AreEqual(BannerKind.None, kind, "无君主的空位政体是无旗合法状态");
            Assert.IsEmpty(realm.bannerPattern);
        }

        [Test]
        public void Generation_IsDeterministicAndDistinct()
        {
            BannerSystem.Generate(1, "甲", BannerKind.MilitaryBanner, out string p1, out string c1);
            BannerSystem.Generate(1, "甲", BannerKind.MilitaryBanner, out string p2, out string c2);
            BannerSystem.Generate(2, "乙", BannerKind.MilitaryBanner, out string p3, out string c3);

            Assert.AreEqual(p1, p2, "同输入必须同布局");
            Assert.AreEqual(c1, c2, "同输入必须同配色");
            Assert.AreNotEqual(p1 + c1, p3 + c3, "不同政权不应共用同一面旗");
        }

        [Test]
        public void BannerFields_RoundTripThroughSave()
        {
            var realm = new RealmData
            {
                realmId = 5,
                bannerKind = BannerKind.HouseholdBanner,
                bannerPattern = "HouseholdBanner|cross",
                bannerColors = "#d4af37|#b23a3a|#1b1b1b"
            };

            RealmOfficeSaveData dto = RealmOfficeSaveData.FromRealm(realm);
            var restored = new RealmData { realmId = 5 };
            dto.ApplyTo(restored);

            Assert.AreEqual(BannerKind.HouseholdBanner, restored.bannerKind);
            Assert.AreEqual("HouseholdBanner|cross", restored.bannerPattern);
            Assert.AreEqual("#d4af37|#b23a3a|#1b1b1b", restored.bannerColors);
        }
    }
}
