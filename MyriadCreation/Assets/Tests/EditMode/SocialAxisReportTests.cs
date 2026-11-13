using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using MyriadCreation.Core.Data;
using MyriadCreation.Simulation.State;
using MyriadCreation.Simulation.WorldState;
using MyriadCreation.World.Generation;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>
    /// D25 四轴人口报告：四轴各一套；身份/职业按位分摊；四轴人口合计与人口块总量相等。
    /// </summary>
    public class SocialAxisReportTests
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
            var go = new GameObject("SocialAxisWorld");
            _objects.Add(go);
            var world = go.AddComponent<GameWorld>();
            world.mapWidth = 48;
            world.mapHeight = 24;
            world.SetupOptions = WorldSetupOptions.CreateStandard();
            world.InitializeWorld();
            world.GenerateTerrain(67);
            return world;
        }

        private static RealmData FirstRealm(GameWorld world)
        {
            foreach (var realm in world.realms.Values) return realm;
            return null;
        }

        [Test]
        public void FourAxes_EachReturnsReport_AndConservesPopulation()
        {
            var world = NewWorld();
            RealmData realm = FirstRealm(world);
            Assert.IsNotNull(realm, "前置：应有政权");
            float expected = 0f;
            for (int i = 0; i < world.tiles.Length; i++)
            {
                if (!world.tiles[i].exists || !world.tiles[i].isLand || world.tiles[i].ownerRealmId != realm.realmId || world.tiles[i].populationBlocks == null) continue;
                foreach (PopulationBlock block in world.tiles[i].populationBlocks) expected += block.count;
            }
            Assert.Greater(expected, 0f, "前置：应有可统计人口");

            List<SocialAxisReport> reports = world.GetSocialAxisReports(realm.realmId);

            Assert.AreEqual(4, reports.Count, "四轴各一套报告");
            Assert.AreEqual(SocialAxis.Class, reports[0].axis);
            Assert.AreEqual(SocialAxis.Stratum, reports[1].axis);
            Assert.AreEqual(SocialAxis.Identity, reports[2].axis);
            Assert.AreEqual(SocialAxis.Occupation, reports[3].axis);
            foreach (SocialAxisReport report in reports)
            {
                Assert.AreEqual(expected, report.totalPopulation, 0.01f, report.axis + " 总人口应守恒");
                float sum = 0f;
                foreach (SocialAxisBucket bucket in report.buckets) sum += bucket.population;
                Assert.AreEqual(expected, sum, 0.01f, report.axis + " 分桶合计应等于总人口");
                Assert.Greater(report.buckets.Count, 0, report.axis + " 应有分桶");
                Assert.That(report.axisSatisfaction, Is.InRange(0f, 100f), report.axis + " 轴满足度应在 0–100");
                foreach (SocialAxisBucket bucket in report.buckets)
                {
                    Assert.That(bucket.needsSatisfaction, Is.InRange(0f, 100f), bucket.key + " 桶满足度应在 0–100");
                }
            }
        }

        [Test]
        public void AxisWeights_ChangeUnrestReproducibly()
        {
            var world = NewWorld();
            RealmData realm = FirstRealm(world);
            Assert.IsNotNull(realm);
            List<SocialAxisReport> reports = world.GetSocialAxisReports(realm.realmId);
            SocialAxisReport lowest = reports[0];
            SocialAxisReport highest = reports[0];
            foreach (SocialAxisReport report in reports)
            {
                if (report.axisSatisfaction < lowest.axisSatisfaction) lowest = report;
                if (report.axisSatisfaction > highest.axisSatisfaction) highest = report;
            }

            var lowWeight = new SocialAxisWeights { classWeight = 0f, stratumWeight = 0f, identityWeight = 0f, occupationWeight = 0f };
            SetWeight(lowWeight, lowest.axis, 1f);
            var highWeight = new SocialAxisWeights { classWeight = 0f, stratumWeight = 0f, identityWeight = 0f, occupationWeight = 0f };
            SetWeight(highWeight, highest.axis, 1f);

            float unrestLow = world.GetSocialAxisUnrest(realm.realmId, lowWeight);
            float unrestHigh = world.GetSocialAxisUnrest(realm.realmId, highWeight);
            float unrestLowAgain = world.GetSocialAxisUnrest(realm.realmId, lowWeight);

            Assert.That(unrestLow, Is.InRange(0f, 100f));
            Assert.That(unrestHigh, Is.InRange(0f, 100f));
            Assert.Less(unrestHigh, unrestLow, "权重压在高满足度轴上，unrest 应更低");
            Assert.AreEqual(unrestLow, unrestLowAgain, 0.001f, "同权重结果应可复现");
        }

        private static void SetWeight(SocialAxisWeights weights, SocialAxis axis, float value)
        {
            switch (axis)
            {
                case SocialAxis.Class: weights.classWeight = value; break;
                case SocialAxis.Stratum: weights.stratumWeight = value; break;
                case SocialAxis.Identity: weights.identityWeight = value; break;
                case SocialAxis.Occupation: weights.occupationWeight = value; break;
            }
        }

    }
}
