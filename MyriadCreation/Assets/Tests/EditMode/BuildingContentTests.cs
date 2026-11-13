using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using MyriadCreation.Core.Data;
using MyriadCreation.Content.Definitions;
using MyriadCreation.Simulation.Systems;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>P2.4：建筑表外提后，内容优先、代码兜底仍可用。</summary>
    public class BuildingContentTests
    {
        [Serializable]
        private class Wrapper
        {
            public List<BuildingContentDef> buildings = new List<BuildingContentDef>();
        }

        [TearDown]
        public void TearDown()
        {
            BuildingCatalog.ResetContent();
        }

        [Test]
        public void ShippedBuildings_AreCompleteAndUnique()
        {
            string path = Path.Combine(Application.dataPath, "StreamingAssets/Base/Building/Buildings.json");
            Assert.IsTrue(File.Exists(path), "找不到 Buildings.json: " + path);
            Wrapper wrapper = JsonUtility.FromJson<Wrapper>(File.ReadAllText(path));
            Assert.IsNotNull(wrapper);
            Assert.AreEqual(29, wrapper.buildings.Count);

            var ids = new HashSet<int>();
            foreach (BuildingContentDef def in wrapper.buildings)
            {
                Assert.Greater(def.buildingId, 0);
                Assert.IsTrue(ids.Add(def.buildingId), "重复建筑 id: " + def.buildingId);
                Assert.IsFalse(string.IsNullOrEmpty(def.buildingName));
                Assert.Greater(def.tier, 0);
                Assert.Greater(def.buildDays, 0);
                foreach (BuildingMaterialCostDef cost in def.materialCost)
                {
                    Assert.Greater(cost.goodsId, 0);
                    Assert.Greater(cost.amount, 0f);
                }
            }
        }

        [Test]
        public void LoadFromContent_ConvertsCategoriesAndMaterials()
        {
            BuildingCatalog.LoadFromContent(new List<BuildingContentDef>
            {
                new BuildingContentDef
                {
                    buildingId = 100,
                    buildingName = "外提农田",
                    category = (int)BuildingCategory.Craft,
                    tier = 2,
                    buildCost = 150f,
                    buildDays = 25,
                    materialCost = new List<BuildingMaterialCostDef>
                    {
                        new BuildingMaterialCostDef { goodsId = 30, amount = 12f }
                    }
                }
            });

            Assert.IsTrue(BuildingCatalog.IsLoaded);
            BuildingDef def = BuildingCatalog.All[100];
            Assert.AreEqual("外提农田", def.buildingName);
            Assert.AreEqual(BuildingCategory.Craft, def.category);
            Assert.AreEqual(25, def.buildDays);
            Assert.AreEqual(12f, def.materialCost[30], 0.001f);
        }

        [Test]
        public void BuildingSystem_UsesContent_WhenLoaded_AndFallbackOtherwise()
        {
            BuildingSystem fallback = new BuildingSystem(new TileData[1]);
            Assert.IsTrue(fallback.GetBuildingDef(100).HasValue, "无内容时应走代码兜底");
            Assert.AreEqual("农田", fallback.GetBuildingDef(100).Value.buildingName);

            BuildingCatalog.LoadFromContent(new List<BuildingContentDef>
            {
                new BuildingContentDef { buildingId = 100, buildingName = "内容农田", tier = 1, buildCost = 1f, buildDays = 10 }
            });
            BuildingSystem fromContent = new BuildingSystem(new TileData[1]);
            Assert.AreEqual("内容农田", fromContent.GetBuildingDef(100).Value.buildingName);
        }
    }
}
