using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using MyriadCreation.Content.Definitions;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>P2.4：经济原型文案外提后可由 JSON 加载，代码兜底仍可用。</summary>
    public class EconomicalArchetypeContentTests
    {
        [Serializable]
        private class Wrapper
        {
            public List<EconomicalArchetypeDef> archetypes = new List<EconomicalArchetypeDef>();
        }

        private static string Path()
        {
            return System.IO.Path.Combine(Application.dataPath, "StreamingAssets/Base/Archetype/EconomicalArchetypes.json");
        }

        [Test]
        public void ShippedArchetypes_AreCompleteAndUnique()
        {
            Assert.IsTrue(File.Exists(Path()), "找不到 EconomicalArchetypes.json: " + Path());
            Wrapper wrapper = JsonUtility.FromJson<Wrapper>(File.ReadAllText(Path()));
            Assert.IsNotNull(wrapper);
            Assert.AreEqual(11, wrapper.archetypes.Count);

            var types = new HashSet<int>();
            foreach (EconomicalArchetypeDef def in wrapper.archetypes)
            {
                Assert.IsTrue(types.Add(def.archetype));
                Assert.IsTrue(Enum.IsDefined(typeof(EconomicalArchetype), def.archetype));
                Assert.IsFalse(string.IsNullOrEmpty(def.displayName));
            }
        }

        [Test]
        public void Loader_ConvertsBiasAndName()
        {
            Assert.IsTrue(EconomicalArchetypeLoader.LoadFromFile(Path()));
            Assert.AreEqual(11, EconomicalArchetypeLoader.Archetypes.Length);

            EconomicalArchetypeInfo warlike = Array.Find(
                EconomicalArchetypeLoader.Archetypes, a => a.archetype == EconomicalArchetype.Warlike);
            Assert.IsNotNull(warlike);
            Assert.AreEqual("好战之君", warlike.displayName);
            Assert.AreEqual(0.8f, warlike.warBias, 0.001f);
            Assert.AreEqual(-0.2f, warlike.buildBias, 0.001f);
        }
    }
}
