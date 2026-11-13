using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using MyriadCreation.Content.Definitions;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>P2.4：灾害表外提后的完整性与转换。</summary>
    public class DisasterContentTests
    {
        [Serializable]
        private class Wrapper
        {
            public List<DisasterContentDef> disasters = new List<DisasterContentDef>();
        }

        [TearDown]
        public void TearDown()
        {
            DisasterCatalog.ResetContent();
        }

        [Test]
        public void ShippedDisasters_AreCompleteAndUnique()
        {
            string path = Path.Combine(Application.dataPath, "StreamingAssets/Base/Disaster/Disasters.json");
            Assert.IsTrue(File.Exists(path), "找不到 Disasters.json: " + path);
            Wrapper wrapper = JsonUtility.FromJson<Wrapper>(File.ReadAllText(path));
            Assert.IsNotNull(wrapper);
            Assert.AreEqual(12, wrapper.disasters.Count);

            var types = new HashSet<int>();
            foreach (DisasterContentDef def in wrapper.disasters)
            {
                Assert.IsTrue(types.Add(def.type), "重复灾害类型: " + def.type);
                Assert.IsTrue(Enum.IsDefined(typeof(DisasterType), def.type));
                Assert.IsFalse(string.IsNullOrEmpty(def.name));
                Assert.IsTrue(Enum.IsDefined(typeof(DisasterCategory), def.category));
                Assert.GreaterOrEqual(def.minDuration, 1);
                Assert.GreaterOrEqual(def.maxDuration, def.minDuration);
                Assert.GreaterOrEqual(def.baseSeverity, 0f);
            }
        }

        [Test]
        public void LoadFromContent_ConvertsEnumsAndClampsDuration()
        {
            DisasterCatalog.LoadFromContent(new List<DisasterContentDef>
            {
                new DisasterContentDef
                {
                    type = (int)DisasterType.Earthquake,
                    name = "外提地震",
                    category = (int)DisasterCategory.Geological,
                    baseFrequency = 0.01f,
                    minDuration = 0,
                    maxDuration = 0,
                    baseSeverity = 80f
                }
            });

            Assert.IsTrue(DisasterCatalog.IsLoaded);
            DisasterDef def = DisasterCatalog.All[DisasterType.Earthquake];
            Assert.AreEqual("外提地震", def.name);
            Assert.AreEqual(DisasterCategory.Geological, def.category);
            Assert.AreEqual(1, def.minDuration, "最小时长至少 1 天");
            Assert.GreaterOrEqual(def.maxDuration, def.minDuration);
        }
    }
}
