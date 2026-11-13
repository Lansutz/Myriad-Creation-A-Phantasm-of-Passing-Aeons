using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using MyriadCreation.Content.Definitions;
using MyriadCreation.Simulation.Definitions;
using MyriadCreation.Simulation.State;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>P2.4：特质表外提后可由 JSON 加载，字段与代码表一致。</summary>
    public class TraitContentTests
    {
        [Serializable]
        private class Wrapper
        {
            public List<TraitContentDef> traits = new List<TraitContentDef>();
        }

        private static string Path()
        {
            return System.IO.Path.Combine(Application.dataPath, "StreamingAssets/Base/Trait/Traits.json");
        }

        [Test]
        public void ShippedTraits_AreCompleteAndUnique()
        {
            Assert.IsTrue(File.Exists(Path()), "找不到 Traits.json: " + Path());
            Wrapper wrapper = JsonUtility.FromJson<Wrapper>(File.ReadAllText(Path()));
            Assert.IsNotNull(wrapper);
            Assert.AreEqual(108, wrapper.traits.Count);

            var ids = new HashSet<string>();
            foreach (TraitContentDef def in wrapper.traits)
            {
                Assert.IsFalse(string.IsNullOrEmpty(def.traitId));
                Assert.IsTrue(ids.Add(def.traitId), "重复特质 id: " + def.traitId);
                Assert.IsFalse(string.IsNullOrEmpty(def.traitName));
                Assert.IsTrue(Enum.IsDefined(typeof(TraitCategory), def.category));
                Assert.IsTrue(Enum.IsDefined(typeof(TraitTier), def.tier));
            }
        }

        [Test]
        public void Loader_ConvertsTraitsAndMods()
        {
            Assert.IsTrue(TraitContentLoader.LoadFromFile(Path()));
            Assert.AreEqual(108, TraitContentLoader.Traits.Length);

            PersonalityTrait brave = Array.Find(TraitContentLoader.Traits, t => t.traitId == "brave_1");
            Assert.IsNotNull(brave);
            Assert.AreEqual("勇气", brave.traitName);
            Assert.AreEqual(1f, brave.martialMod, 0.001f);
            CollectionAssert.Contains(brave.conflictingTraits, "craven_1");
        }
    }
}
