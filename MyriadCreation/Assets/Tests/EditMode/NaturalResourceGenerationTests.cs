using System.Reflection;
using NUnit.Framework;
using MyriadCreation.Simulation.WorldState;
using UnityEngine;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>自然资源必须有 originBiomes，否则 ResourceGenerator 永远不生成；生皮/黏土也应有自然来源。</summary>
    public class NaturalResourceGenerationTests
    {
        private GameObject _go;

        [SetUp]
        public void Setup() { _go = new GameObject("NaturalResourceTestWorld"); }
        [TearDown]
        public void TearDown() { if (_go != null) Object.DestroyImmediate(_go); }

        [Test]
        public void NaturalGoods_HaveOriginBiomes()
        {
            var world = _go.AddComponent<GameWorld>();
            typeof(GameWorld).GetMethod("InitializeGoodsDefs", BindingFlags.NonPublic | BindingFlags.Instance)
                .Invoke(world, null);
            foreach (var pair in world.goodsDefs)
            {
                if (!pair.Value.isNaturalResource) continue;
                Assert.IsNotNull(pair.Value.originBiomes, $"物资 {pair.Key} 缺 originBiomes");
                Assert.Greater(pair.Value.originBiomes.Count, 0, $"物资 {pair.Key} 没有可生成生物群系");
            }
            Assert.IsTrue(world.goodsDefs[140].isNaturalResource, "黏土应有自然来源");
            Assert.IsTrue(world.goodsDefs[130].isNaturalResource, "生皮应有野生来源");
        }
    }
}
