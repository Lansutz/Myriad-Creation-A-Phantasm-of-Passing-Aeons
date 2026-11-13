using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using MyriadCreation.Content.Definitions;
using MyriadCreation.Simulation.Definitions;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>P2.4：聚落形态建造门槛外提后的完整性与转换。</summary>
    public class BuildableRequirementContentTests
    {
        [Serializable]
        private class Wrapper
        {
            public List<BuildableRequirementDef> buildableTypes = new List<BuildableRequirementDef>();
        }

        private static string Path()
        {
            return System.IO.Path.Combine(Application.dataPath, "StreamingAssets/Base/Buildable/BuildableTypes.json");
        }

        [Test]
        public void ShippedBuildables_AreCompleteAndUnique()
        {
            Assert.IsTrue(File.Exists(Path()), "找不到 BuildableTypes.json: " + Path());
            Wrapper wrapper = JsonUtility.FromJson<Wrapper>(File.ReadAllText(Path()));
            Assert.IsNotNull(wrapper);
            Assert.AreEqual(30, wrapper.buildableTypes.Count);

            var types = new HashSet<int>();
            foreach (BuildableRequirementDef def in wrapper.buildableTypes)
            {
                Assert.IsTrue(types.Add(def.buildableType), "重复形态: " + def.buildableType);
                Assert.IsTrue(Enum.IsDefined(typeof(BuildableType), def.buildableType));
                Assert.IsFalse(string.IsNullOrEmpty(def.name));
                Assert.GreaterOrEqual(def.minSlope, 0f);
                Assert.GreaterOrEqual(def.maxSlope, def.minSlope);
            }
        }

        [Test]
        public void Loader_ConvertsBottleneckBarrier()
        {
            Assert.IsTrue(BuildableRequirementLoader.LoadFromFile(Path()));
            Assert.IsTrue(BuildableRequirementLoader.HasContent);

            BuildingRequirement barrier = BuildableRequirementLoader.Requirements[BuildableType.Barrier];
            Assert.AreEqual("关隘", barrier.name);
            Assert.IsTrue(barrier.requireBottleneck);
            Assert.AreEqual(6, barrier.allowedBiomes.Length);
            Assert.AreEqual(5f, barrier.minSlope, 0.001f);
            Assert.AreEqual(40f, barrier.maxSlope, 0.001f);
        }
    }
}
