using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using MyriadCreation.Content.Definitions;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>P2.4：绰号表已外提到 StreamingAssets/Base/Epithet/Epithets.json，代码表只作兜底。</summary>
    public class EpithetContentTests
    {
        [Serializable]
        private class Wrapper
        {
            public List<EpithetDef> epithets = new List<EpithetDef>();
        }

        [Test]
        public void ShippedEpithets_AreCompleteAndUnique()
        {
            string path = Path.Combine(Application.dataPath, "StreamingAssets/Base/Epithet/Epithets.json");
            Assert.IsTrue(File.Exists(path), "找不到 Epithets.json: " + path);

            Wrapper wrapper = JsonUtility.FromJson<Wrapper>(File.ReadAllText(path));
            Assert.IsNotNull(wrapper);
            Assert.AreEqual(81, wrapper.epithets.Count, "外提表应与原代码表条目数一致");

            var ids = new HashSet<string>();
            foreach (EpithetDef def in wrapper.epithets)
            {
                Assert.IsFalse(string.IsNullOrEmpty(def.id));
                Assert.IsTrue(ids.Add(def.id), "重复 id: " + def.id);
                Assert.IsFalse(string.IsNullOrEmpty(def.name), "缺名称: " + def.id);
                Assert.IsTrue(Enum.IsDefined(typeof(EpithetConnotation), def.connotation), "非法 connotation: " + def.id);
                Assert.IsTrue(Enum.IsDefined(typeof(EpithetTier), def.tier), "非法 tier: " + def.id);
            }
        }

        [Test]
        public void CatalogFallback_StillResolvesCodeTable()
        {
            EpithetCatalog.ResetContent();
            EpithetDef brave = EpithetCatalog.Get("epithet_brave");
            Assert.IsNotNull(brave);
            Assert.AreEqual("勇敢者", brave.name);
        }

        [Test]
        public void LoadFromContent_OverridesFallback()
        {
            EpithetCatalog.ResetContent();
            EpithetCatalog.LoadFromContent(new List<EpithetDef>
            {
                new EpithetDef { id = "epithet_brave", name = "外提勇敢者", connotation = EpithetConnotation.Positive, tier = EpithetTier.Common }
            });

            Assert.AreEqual("外提勇敢者", EpithetCatalog.Get("epithet_brave").name);
            EpithetCatalog.ResetContent();
        }
    }
}
